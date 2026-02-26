using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.AvailabilityGroups.Members;
using WebApi.Contracts.AvailabilityGroups.Slots;

namespace WebApi.Mappers;

public static class AvailabilityGroupNestedMapper
{
    public static AvailabilityGroupMemberDto ToMemberDto(this AvailabilityGroupMemberModel model) => new()
    {
        Id = model.Id,
        AvailabilityGroupId = model.AvailabilityGroupId,
        EmployeeId = model.EmployeeId
    };

    public static AvailabilityGroupMemberModel ToCreateMemberModel(this CreateAvailabilityGroupMemberRequest request, int groupId) => new()
    {
        AvailabilityGroupId = groupId,
        EmployeeId = request.EmployeeId
    };

    public static AvailabilityGroupMemberModel ToUpdateMemberModel(this UpdateAvailabilityGroupMemberRequest request, int groupId, int memberId) => new()
    {
        Id = memberId,
        AvailabilityGroupId = groupId,
        EmployeeId = request.EmployeeId
    };

    public static AvailabilitySlotDto ToSlotDto(this AvailabilityGroupDayModel model) => new()
    {
        Id = model.Id,
        AvailabilityGroupMemberId = model.AvailabilityGroupMemberId,
        DayOfMonth = model.DayOfMonth,
        Kind = model.Kind,
        IntervalStr = model.IntervalStr
    };

    public static AvailabilityGroupDayModel ToCreateSlotModel(this CreateAvailabilitySlotRequest request) => new()
    {
        AvailabilityGroupMemberId = request.AvailabilityGroupMemberId,
        DayOfMonth = request.DayOfMonth,
        Kind = request.Kind,
        IntervalStr = request.IntervalStr
    };

    public static AvailabilityGroupDayModel ToUpdateSlotModel(this UpdateAvailabilitySlotRequest request, int slotId) => new()
    {
        Id = slotId,
        AvailabilityGroupMemberId = request.AvailabilityGroupMemberId,
        DayOfMonth = request.DayOfMonth,
        Kind = request.Kind,
        IntervalStr = request.IntervalStr
    };
}
