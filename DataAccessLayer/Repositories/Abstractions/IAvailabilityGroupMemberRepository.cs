using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for availability-group members.
/// </summary>
public interface IAvailabilityGroupMemberRepository : IBaseRepository<AvailabilityGroupMemberModel>
{
    /// <summary>
    /// Returns all members of one availability group ordered for stable rendering and editing.
    /// </summary>
    Task<List<AvailabilityGroupMemberModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default);

    /// <summary>
    /// Returns one member record for a specific group/employee pair.
    /// </summary>
    Task<AvailabilityGroupMemberModel?> GetByGroupAndEmployeeAsync(int groupId, int employeeId, CancellationToken ct = default);
}
