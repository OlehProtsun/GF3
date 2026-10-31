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

    /// <summary>
    /// Replaces all employee assignments for one schedule in one database batch.
    /// </summary>
    Task ReplaceForScheduleAsync(int scheduleId, IEnumerable<ScheduleEmployeeModel> employees, CancellationToken ct = default);

    /// <summary>
    /// Returns whether a schedule already has a specific employee assignment.
    /// </summary>
    Task<bool> ExistsForScheduleAsync(int scheduleId, int employeeId, int? excludeId = null, CancellationToken ct = default);
}
