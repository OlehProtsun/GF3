using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for employee assignments that belong to a schedule graph.
/// </summary>
public interface IScheduleEmployeeRepository : IBaseRepository<ScheduleEmployeeModel>
{
    /// <summary>
    /// Returns all employee assignments for one schedule ordered for stable UI rendering.
    /// </summary>
    Task<List<ScheduleEmployeeModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default);
}
