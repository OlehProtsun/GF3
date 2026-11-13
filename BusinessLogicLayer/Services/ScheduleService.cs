using System.Globalization;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Manages persisted schedules together with their nested details.
/// The service treats a schedule as an aggregate root: when we save the full schedule payload,
/// employees, slots, and cell styles are replaced as a whole so the final database state matches
/// the editor state exactly.
/// </summary>
public class ScheduleService : IScheduleService
{
    private const string SaveBeforeGenerationMessage = "You can't save a schedule until something has been generated. Please run generation first.";

    private readonly IScheduleRepository _scheduleRepo;
    private readonly IScheduleEmployeeRepository _employeeRepo;
    private readonly IScheduleSlotRepository _slotRepo;
    private readonly IScheduleCellStyleRepository _cellStyleRepo;

    public ScheduleService(
        IScheduleRepository scheduleRepo,
        IScheduleEmployeeRepository employeeRepo,
        IScheduleSlotRepository slotRepo,
        IScheduleCellStyleRepository cellStyleRepo)
    {
        _scheduleRepo = scheduleRepo;
        _employeeRepo = employeeRepo;
        _slotRepo = slotRepo;
        _cellStyleRepo = cellStyleRepo;
    }

    public async Task<ScheduleModel?> GetAsync(int id, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedAsync(token => _scheduleRepo.GetByIdAsync(id, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<ScheduleModel>> GetAllAsync(CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(_scheduleRepo.GetAllAsync, x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<ScheduleModel> CreateAsync(ScheduleModel entity, CancellationToken ct = default)
    {
        NormalizeSchedule(entity);
        return (await _scheduleRepo.AddAsync(entity.ToDal(), ct).ConfigureAwait(false)).ToContract();
    }

    public async Task UpdateAsync(ScheduleModel entity, CancellationToken ct = default)
    {
        NormalizeSchedule(entity);
        await _scheduleRepo.UpdateAsync(entity.ToDal(), ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _scheduleRepo.DeleteAsync(id, ct);

    public async Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _scheduleRepo.GetByContainerAsync(containerId, value, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => await ServiceMappingHelper.GetMappedListAsync(token => _scheduleRepo.GetByValueAsync(value, token), x => x.ToContract(), ct).ConfigureAwait(false);

    public async Task SaveWithDetailsAsync(
        ScheduleModel schedule,
        IEnumerable<ScheduleEmployeeModel> employees,
        IEnumerable<ScheduleSlotModel> slots,
        IEnumerable<ScheduleCellStyleModel> cellStyles,
        CancellationToken ct = default)
    {
        NormalizeSchedule(schedule);

        var employeeList = employees?.ToList() ?? [];
        var slotList = slots?.ToList() ?? [];
        EnsureGenerated(schedule, slotList);

        var normalizedStyles = NormalizeStyles(cellStyles);
        var scheduleId = await UpsertScheduleAsync(schedule, ct).ConfigureAwait(false);

        await ReplaceScheduleEmployeesAsync(scheduleId, employeeList, ct).ConfigureAwait(false);
        await ReplaceScheduleSlotsAsync(scheduleId, slotList, ct).ConfigureAwait(false);
        await ReplaceScheduleCellStylesAsync(scheduleId, normalizedStyles, ct).ConfigureAwait(false);
    }

    public async Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepo.GetDetailedAsync(id, ct).ConfigureAwait(false);
        if (schedule is null)
        {
            return null;
        }

        return schedule.ToContract();
    }

    private async Task<int> UpsertScheduleAsync(ScheduleModel schedule, CancellationToken ct)
    {
        var dalSchedule = schedule.ToDal();
        if (dalSchedule.Id == 0)
        {
            dalSchedule = await _scheduleRepo.AddAsync(dalSchedule, ct).ConfigureAwait(false);
        }
        else
        {
            await _scheduleRepo.UpdateAsync(dalSchedule, ct).ConfigureAwait(false);
        }

        return dalSchedule.Id;
    }

    /// <summary>
    /// The editor submits the full employee collection for a schedule. Replacing it entirely
    /// keeps the persistence model simple and avoids subtle "partial patch" bugs where removed
    /// employees would otherwise remain attached to the schedule.
    /// </summary>
    private async Task ReplaceScheduleEmployeesAsync(
        int scheduleId,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        CancellationToken ct)
    {
        await _employeeRepo
            .ReplaceForScheduleAsync(scheduleId, employees.Select(employee => employee.ToDal()), ct)
            .ConfigureAwait(false);
    }

    private async Task ReplaceScheduleSlotsAsync(
        int scheduleId,
        IReadOnlyList<ScheduleSlotModel> slots,
        CancellationToken ct)
    {
        await _slotRepo
            .ReplaceForScheduleAsync(scheduleId, slots.Select(slot => slot.ToDal()), overwrite: true, ct)
            .ConfigureAwait(false);
    }

    private async Task ReplaceScheduleCellStylesAsync(
        int scheduleId,
        IReadOnlyList<ScheduleCellStyleModel> styles,
        CancellationToken ct)
    {
        await _cellStyleRepo
            .ReplaceForScheduleAsync(scheduleId, styles.Select(style => style.ToDal()), ct)
            .ConfigureAwait(false);
    }

    private static List<ScheduleCellStyleModel> NormalizeStyles(IEnumerable<ScheduleCellStyleModel> cellStyles)
    {
        return (cellStyles ?? Enumerable.Empty<ScheduleCellStyleModel>())
            .Where(style => style.BackgroundColorArgb.HasValue || style.TextColorArgb.HasValue)
            .ToList();
    }

    private static void NormalizeSchedule(ScheduleModel schedule)
    {
        schedule.Note = string.IsNullOrWhiteSpace(schedule.Note) ? null : schedule.Note.Trim();
        schedule.Shift1Time = NormalizeShift(schedule.Shift1Time, "Shift1");
        schedule.Shift2Time = NormalizeShift(schedule.Shift2Time, "Shift2");
    }

    /// <summary>
    /// Shift values are accepted in a forgiving user-facing format, but they are always persisted
    /// in one canonical representation: <c>HH:mm - HH:mm</c>. That gives the rest of the codebase
    /// a stable shape for comparisons, exports, and generation logic.
    /// </summary>
    private static string NormalizeShift(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException($"{label} is required.");

        var normalized = value.Trim()
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-');

        var parts = normalized.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new ValidationException($"{label} format must be HH:mm - HH:mm.");

        if (!TimeSpan.TryParseExact(parts[0], [@"h\:mm", @"hh\:mm"], CultureInfo.InvariantCulture, out var from) ||
            !TimeSpan.TryParseExact(parts[1], [@"h\:mm", @"hh\:mm"], CultureInfo.InvariantCulture, out var to))
            throw new ValidationException($"{label} format must be HH:mm - HH:mm.");

        if (to <= from)
            throw new ValidationException($"{label} end must be later than start.");

        return $"{from:hh\\:mm} - {to:hh\\:mm}";
    }

    private static void EnsureGenerated(ScheduleModel schedule, IReadOnlyCollection<ScheduleSlotModel> slots)
    {
        if (schedule.AvailabilityGroupId is null || schedule.AvailabilityGroupId <= 0)
            throw new ValidationException(SaveBeforeGenerationMessage);

        if (slots.Count == 0)
            throw new ValidationException(SaveBeforeGenerationMessage);
    }
}
