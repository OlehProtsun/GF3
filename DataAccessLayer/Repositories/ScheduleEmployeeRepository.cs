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
}
