using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;
using WebApi.Contracts.AvailabilityGroups.Members;
using WebApi.Contracts.AvailabilityGroups.Slots;

namespace WebApi.Mappers;

/// <summary>
/// Maps nested availability-group resources such as members and day slots.
/// </summary>
public static class AvailabilityGroupNestedMapper
{
    public static AvailabilityGroupMemberDto ToMemberDto(this AvailabilityGroupMemberModel model) => new()
    {
        Id = model.Id,
        AvailabilityGroupId = model.AvailabilityGroupId,
        EmployeeId = model.EmployeeId,
        DisplayOrder = model.DisplayOrder,
    };

    public static AvailabilityGroupMemberModel ToCreateMemberModel(this CreateAvailabilityGroupMemberRequest request, int groupId)
        => MapMemberModel(groupId, request.EmployeeId, request.DisplayOrder);

    public static AvailabilityGroupMemberModel ToUpdateMemberModel(this UpdateAvailabilityGroupMemberRequest request, int groupId, int memberId)
        => MapMemberModel(groupId, request.EmployeeId, request.DisplayOrder, memberId);

    public static AvailabilitySlotDto ToSlotDto(this AvailabilityGroupDayModel model) => new()
    {
        Id = model.Id,
        AvailabilityGroupMemberId = model.AvailabilityGroupMemberId,
        DayOfMonth = model.DayOfMonth,
        Kind = model.Kind,
        IntervalStr = model.IntervalStr,
    };

    public static AvailabilityGroupDayModel ToCreateSlotModel(this CreateAvailabilitySlotRequest request)
        => MapSlotModel(request.AvailabilityGroupMemberId, request.DayOfMonth, request.Kind, request.IntervalStr);

    public static AvailabilityGroupDayModel ToUpdateSlotModel(this UpdateAvailabilitySlotRequest request, int slotId)
        => MapSlotModel(request.AvailabilityGroupMemberId, request.DayOfMonth, request.Kind, request.IntervalStr, slotId);

    private static AvailabilityGroupMemberModel MapMemberModel(int groupId, int employeeId, int displayOrder, int id = 0)
        => new()
        {
            Id = id,
            AvailabilityGroupId = groupId,
            EmployeeId = employeeId,
            DisplayOrder = displayOrder,
        };

    private static AvailabilityGroupDayModel MapSlotModel(
        int availabilityGroupMemberId,
        int dayOfMonth,
        AvailabilityKind kind,
        string? intervalStr,
        int id = 0)
        => new()
        {
            Id = id,
            AvailabilityGroupMemberId = availabilityGroupMemberId,
            DayOfMonth = dayOfMonth,
            Kind = kind,
            IntervalStr = intervalStr,
        };
}
