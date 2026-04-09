using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for concrete schedule slots.
/// </summary>
public interface IScheduleSlotRepository : IBaseRepository<ScheduleSlotModel>
{
    /// <summary>
    /// Returns all slots for one schedule ordered by day and slot number.
    /// </summary>
    Task<List<ScheduleSlotModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default);

    /// <summary>
    /// Replaces or appends slots for one schedule in a transactional batch.
    /// </summary>
    Task<int> ReplaceForScheduleAsync(int scheduleId, IEnumerable<ScheduleSlotModel> slots, bool overwrite, CancellationToken ct = default);
}
