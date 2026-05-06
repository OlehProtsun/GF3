using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for saved schedule graphs.
/// Besides CRUD, this repository centralizes the rich includes and search filters used by list,
/// detail, edit, and export flows.
/// </summary>
public class ScheduleRepository : GenericRepository<ScheduleModel>, IScheduleRepository
{
    public ScheduleRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public override async Task<List<ScheduleModel>> GetAllAsync(CancellationToken ct = default)
        => await CreateListQuery()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default)
    {
        var query = CreateListQuery()
            .Where(schedule => schedule.ContainerId == containerId);

        if (!string.IsNullOrWhiteSpace(value))
        {
            query = ApplyScheduleSearch(query, value);
        }

        return await query.ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return await GetAllAsync(ct).ConfigureAwait(false);
        }

        return await ApplyScheduleSearch(CreateListQuery(), value)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<ScheduleModel>> GetPublishedForEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(schedule => schedule.Container)
            .Include(schedule => schedule.Shop)
            .Include(schedule => schedule.Employees)
                .ThenInclude(scheduleEmployee => scheduleEmployee.Employee)
            .Include(schedule => schedule.Slots)
            .Where(schedule =>
                schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                schedule.Employees.Any(scheduleEmployee => scheduleEmployee.EmployeeId == employeeId))
            .OrderByDescending(schedule => schedule.Year)
            .ThenByDescending(schedule => schedule.Month)
            .ThenBy(schedule => schedule.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(schedule => schedule.Container)
            .Include(schedule => schedule.Shop)
            .Include(schedule => schedule.Slots)
            .Include(schedule => schedule.Employees)
                .ThenInclude(scheduleEmployee => scheduleEmployee.Employee)
            .FirstOrDefaultAsync(schedule => schedule.Id == id, ct)
            .ConfigureAwait(false);

    private IQueryable<ScheduleModel> CreateListQuery()
        => _set
            .AsNoTracking()
            .Include(schedule => schedule.Container)
            .Include(schedule => schedule.Shop);

    /// <summary>
    /// Applies the shared schedule search logic used by both global lists and container-scoped lists.
    /// Numeric search tokens can match year/month, while textual tokens match graph, note, container,
    /// and shop names.
    /// </summary>
    private static IQueryable<ScheduleModel> ApplyScheduleSearch(IQueryable<ScheduleModel> query, string rawValue)
    {
        var normalized = NormalizeSearchValue(rawValue);
        var hasNumericValue = int.TryParse(normalized, out var numericValue);

        return query.Where(schedule =>
            schedule.Name.ToLower().Contains(normalized) ||
            (schedule.Note != null && schedule.Note.ToLower().Contains(normalized)) ||
            schedule.Container.Name.ToLower().Contains(normalized) ||
            schedule.Shop.Name.ToLower().Contains(normalized) ||
            (hasNumericValue && (schedule.Year == numericValue || schedule.Month == numericValue)));
    }

    private static string NormalizeSearchValue(string? value)
        => (value ?? string.Empty).Trim().ToLower();
}
