using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for employee assignments inside one schedule graph.
/// </summary>
public class ScheduleEmployeeRepository : GenericRepository<ScheduleEmployeeModel>, IScheduleEmployeeRepository
{
    public ScheduleEmployeeRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public async Task<List<ScheduleEmployeeModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(scheduleEmployee => scheduleEmployee.Employee)
            .Where(scheduleEmployee => scheduleEmployee.ScheduleId == scheduleId)
            .OrderBy(scheduleEmployee => scheduleEmployee.DisplayOrder)
            .ThenBy(scheduleEmployee => scheduleEmployee.Employee != null ? scheduleEmployee.Employee.FirstName : string.Empty)
            .ThenBy(scheduleEmployee => scheduleEmployee.Employee != null ? scheduleEmployee.Employee.LastName : string.Empty)
            .ThenBy(scheduleEmployee => scheduleEmployee.EmployeeId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task ReplaceForScheduleAsync(int scheduleId, IEnumerable<ScheduleEmployeeModel> employees, CancellationToken ct = default)
    {
        var employeeList = employees?.ToList() ?? [];
        foreach (var employee in employeeList)
        {
            employee.Id = 0;
            employee.ScheduleId = scheduleId;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        await _set
            .Where(employee => employee.ScheduleId == scheduleId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        if (employeeList.Count > 0)
        {
            await _set.AddRangeAsync(employeeList, ct).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        _db.ChangeTracker.Clear();
    }

    /// <inheritdoc />
    public Task<bool> ExistsForScheduleAsync(int scheduleId, int employeeId, int? excludeId = null, CancellationToken ct = default)
        => _set
            .AsNoTracking()
            .AnyAsync(
                employee =>
                    employee.ScheduleId == scheduleId &&
                    employee.EmployeeId == employeeId &&
                    (!excludeId.HasValue || employee.Id != excludeId.Value),
                ct);
}
