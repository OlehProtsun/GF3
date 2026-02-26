using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IAvailabilityGroupService : IBaseService<AvailabilityGroupModel>
    {
        Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default);

        Task SaveGroupAsync(
            AvailabilityGroupModel group,
            IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload,
            CancellationToken ct = default);

        Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)>
            LoadFullAsync(int groupId, CancellationToken ct = default);

        Task<List<AvailabilityGroupMemberModel>> GetMembersAsync(int groupId, CancellationToken ct = default);
        Task<AvailabilityGroupMemberModel> CreateMemberAsync(int groupId, AvailabilityGroupMemberModel model, CancellationToken ct = default);
        Task UpdateMemberAsync(int groupId, int memberId, AvailabilityGroupMemberModel model, CancellationToken ct = default);
        Task DeleteMemberAsync(int groupId, int memberId, CancellationToken ct = default);

        Task<List<AvailabilityGroupDayModel>> GetSlotsAsync(int groupId, CancellationToken ct = default);
        Task<AvailabilityGroupDayModel> CreateSlotAsync(int groupId, AvailabilityGroupDayModel model, CancellationToken ct = default);
        Task UpdateSlotAsync(int groupId, int slotId, AvailabilityGroupDayModel model, CancellationToken ct = default);
        Task DeleteSlotAsync(int groupId, int slotId, CancellationToken ct = default);
    }
}
