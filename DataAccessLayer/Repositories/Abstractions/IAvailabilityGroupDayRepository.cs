using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for per-day availability entries of a group member.
/// </summary>
public interface IAvailabilityGroupDayRepository : IBaseRepository<AvailabilityGroupDayModel>
{
    /// <summary>
    /// Returns all day entries for one availability-group member ordered by day.
    /// </summary>
    Task<List<AvailabilityGroupDayModel>> GetByMemberIdAsync(int memberId, CancellationToken ct = default);

    /// <summary>
    /// Deletes all day entries that belong to one group member.
    /// </summary>
    Task DeleteByMemberIdAsync(int memberId, CancellationToken ct = default);

    /// <summary>
    /// Returns all day entries that belong to one availability group.
    /// </summary>
    Task<List<AvailabilityGroupDayModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default);

    /// <summary>
    /// Persists multiple day entries in one batch.
    /// </summary>
    Task AddRangeAsync(IEnumerable<AvailabilityGroupDayModel> days, CancellationToken ct = default);
}
