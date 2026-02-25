using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using System.Globalization;

namespace BusinessLogicLayer.Services;

public class ScheduleService : IScheduleService
{
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
        => (await _scheduleRepo.GetByIdAsync(id, ct).ConfigureAwait(false))?.ToContract();

    public async Task<List<ScheduleModel>> GetAllAsync(CancellationToken ct = default)
        => (await _scheduleRepo.GetAllAsync(ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();

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
        => (await _scheduleRepo.GetByContainerAsync(containerId, value, ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();

    public async Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default)
        => (await _scheduleRepo.GetByValueAsync(value, ct).ConfigureAwait(false)).Select(x => x.ToContract()).ToList();

    public async Task SaveWithDetailsAsync(
        ScheduleModel schedule,
        IEnumerable<ScheduleEmployeeModel> employees,
        IEnumerable<ScheduleSlotModel> slots,
        IEnumerable<ScheduleCellStyleModel> cellStyles,
        CancellationToken ct = default)
    {
        NormalizeSchedule(schedule);
        EnsureGenerated(schedule, slots);

        var employeeList = employees?.ToList() ?? new List<ScheduleEmployeeModel>();
        var slotList = slots?.ToList() ?? new List<ScheduleSlotModel>();
        var normalizedStyles = (cellStyles ?? Enumerable.Empty<ScheduleCellStyleModel>())
            .Where(cs => cs.BackgroundColorArgb.HasValue || cs.TextColorArgb.HasValue)
            .ToList();

        var dalSchedule = schedule.ToDal();
        if (dalSchedule.Id == 0)
            dalSchedule = await _scheduleRepo.AddAsync(dalSchedule, ct).ConfigureAwait(false);
        else
            await _scheduleRepo.UpdateAsync(dalSchedule, ct).ConfigureAwait(false);

        var scheduleId = dalSchedule.Id;

        var existingEmployees = await _employeeRepo.GetByScheduleAsync(scheduleId, ct).ConfigureAwait(false);
        foreach (var e in existingEmployees)
            await _employeeRepo.DeleteAsync(e.Id, ct).ConfigureAwait(false);

        foreach (var e in employeeList)
        {
            var dal = e.ToDal();
            dal.Id = 0;
            dal.ScheduleId = scheduleId;
            await _employeeRepo.AddAsync(dal, ct).ConfigureAwait(false);
        }

        var existingSlots = await _slotRepo.GetByScheduleAsync(scheduleId, ct).ConfigureAwait(false);
        foreach (var s in existingSlots)
            await _slotRepo.DeleteAsync(s.Id, ct).ConfigureAwait(false);

        foreach (var s in slotList)
        {
            var dal = s.ToDal();
            dal.Id = 0;
            dal.ScheduleId = scheduleId;
            await _slotRepo.AddAsync(dal, ct).ConfigureAwait(false);
        }

        var existingStyles = await _cellStyleRepo.GetByScheduleAsync(scheduleId, ct).ConfigureAwait(false);
        foreach (var style in existingStyles)
            await _cellStyleRepo.DeleteAsync(style.Id, ct).ConfigureAwait(false);

        foreach (var style in normalizedStyles)
        {
            var dal = style.ToDal();
            dal.Id = 0;
            dal.ScheduleId = scheduleId;
            await _cellStyleRepo.AddAsync(dal, ct).ConfigureAwait(false);
        }
    }

    public async Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepo.GetDetailedAsync(id, ct).ConfigureAwait(false);
        if (schedule is null)
            return null;

        schedule.Employees = (await _employeeRepo.GetByScheduleAsync(id, ct).ConfigureAwait(false)).ToList();
        schedule.Slots = (await _slotRepo.GetByScheduleAsync(id, ct).ConfigureAwait(false)).ToList();
        schedule.CellStyles = (await _cellStyleRepo.GetByScheduleAsync(id, ct).ConfigureAwait(false)).ToList();

        return schedule.ToContract();
    }

    private static void NormalizeSchedule(ScheduleModel schedule)
    {
        schedule.Note = string.IsNullOrWhiteSpace(schedule.Note) ? null : schedule.Note.Trim();
        schedule.Shift1Time = NormalizeShift(schedule.Shift1Time, "Shift1");
        schedule.Shift2Time = NormalizeShift(schedule.Shift2Time, "Shift2");
    }

    private static string NormalizeShift(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException($"{label} is required.");

        var s = value.Trim().Replace('–', '-').Replace('—', '-').Replace('−', '-');
        var parts = s.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new ValidationException($"{label} format must be HH:mm - HH:mm.");

        if (!TimeSpan.TryParseExact(parts[0], new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var from) ||
            !TimeSpan.TryParseExact(parts[1], new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var to))
            throw new ValidationException($"{label} format must be HH:mm - HH:mm.");

        if (to <= from)
            throw new ValidationException($"{label} end must be later than start.");

        return $"{from:hh\\:mm} - {to:hh\\:mm}";
    }

    private static void EnsureGenerated(ScheduleModel schedule, IEnumerable<ScheduleSlotModel> slots)
    {
        if (schedule.AvailabilityGroupId is null || schedule.AvailabilityGroupId <= 0)
            throw new ValidationException("You can’t save a schedule until something has been generated. Please run generation first.");

        if (slots == null || !slots.Any())
            throw new ValidationException("You can’t save a schedule until something has been generated. Please run generation first.");
    }
}
