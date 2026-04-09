using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Business operations for availability groups and their nested members/day entries.
/// </summary>
public interface IAvailabilityGroupService : IBaseService<AvailabilityGroupModel>
{
    /// <summary>
    /// Searches availability groups by name, related employee names, and date tokens.
    /// </summary>
    Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Saves a full availability group aggregate together with the supplied member/day payload.
    /// </summary>
    Task SaveGroupAsync(
        AvailabilityGroupModel group,
        IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload,
        CancellationToken ct = default);

    /// <summary>
    /// Loads a fully expanded availability group aggregate for editing.
    /// </summary>
    Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)>
        LoadFullAsync(int groupId, CancellationToken ct = default);

    /// <summary>
    /// Returns all members of the specified availability group.
    /// </summary>
    Task<List<AvailabilityGroupMemberModel>> GetMembersAsync(int groupId, CancellationToken ct = default);

    /// <summary>
    /// Creates one member in the specified availability group.
    /// </summary>
    Task<AvailabilityGroupMemberModel> CreateMemberAsync(int groupId, AvailabilityGroupMemberModel model, CancellationToken ct = default);

    /// <summary>
    /// Updates one member in the specified availability group.
    /// </summary>
    Task UpdateMemberAsync(int groupId, int memberId, AvailabilityGroupMemberModel model, CancellationToken ct = default);

    /// <summary>
    /// Deletes one member from the specified availability group.
    /// </summary>
    Task DeleteMemberAsync(int groupId, int memberId, CancellationToken ct = default);

    /// <summary>
    /// Returns all day entries that belong to the specified availability group.
    /// </summary>
    Task<List<AvailabilityGroupDayModel>> GetSlotsAsync(int groupId, CancellationToken ct = default);

    /// <summary>
    /// Creates one day entry inside the specified availability group.
    /// </summary>
    Task<AvailabilityGroupDayModel> CreateSlotAsync(int groupId, AvailabilityGroupDayModel model, CancellationToken ct = default);

    /// <summary>
    /// Updates one day entry inside the specified availability group.
    /// </summary>
    Task UpdateSlotAsync(int groupId, int slotId, AvailabilityGroupDayModel model, CancellationToken ct = default);

    /// <summary>
    /// Deletes one day entry from the specified availability group.
    /// </summary>
    Task DeleteSlotAsync(int groupId, int slotId, CancellationToken ct = default);
}
