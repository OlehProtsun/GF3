using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for per-cell visual style overrides in a schedule export/edit matrix.
/// </summary>
public interface IScheduleCellStyleRepository : IBaseRepository<ScheduleCellStyleModel>
{
    /// <summary>
    /// Returns all style overrides for one schedule.
    /// </summary>
    Task<List<ScheduleCellStyleModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default);

    /// <summary>
    /// Returns a style override for one schedule cell.
    /// </summary>
    Task<ScheduleCellStyleModel?> GetByScheduleCellAsync(int scheduleId, int dayOfMonth, int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Replaces all style overrides for one schedule in one database batch.
    /// </summary>
    Task ReplaceForScheduleAsync(int scheduleId, IEnumerable<ScheduleCellStyleModel> styles, CancellationToken ct = default);
}
