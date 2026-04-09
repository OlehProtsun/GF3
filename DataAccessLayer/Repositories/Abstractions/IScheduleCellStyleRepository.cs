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
}
