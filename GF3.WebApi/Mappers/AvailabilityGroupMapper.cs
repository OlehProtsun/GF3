using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.AvailabilityGroups;

namespace WebApi.Mappers;

public static class AvailabilityGroupMapper
{
    public static AvailabilityGroupDto ToApiDto(this AvailabilityGroupModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Year = model.Year,
        Month = model.Month
    };

    public static AvailabilityGroupModel ToCreateModel(this CreateAvailabilityGroupRequest request) => new()
    {
        Name = request.Name,
        Year = request.Year,
        Month = request.Month
    };

    public static AvailabilityGroupModel ToUpdateModel(this UpdateAvailabilityGroupRequest request, int id) => new()
    {
        Id = id,
        Name = request.Name,
        Year = request.Year,
        Month = request.Month
    };

    public static IEnumerable<AvailabilityGroupItemDto> ToItemDtos(
        this IEnumerable<AvailabilityGroupMemberModel> members,
        IEnumerable<AvailabilityGroupDayModel> days)
    {
        var membersById = members.ToDictionary(x => x.Id, x => x);

        return days
            .Where(day => membersById.ContainsKey(day.AvailabilityGroupMemberId))
            .Select(day => new AvailabilityGroupItemDto
            {
                MemberId = day.AvailabilityGroupMemberId,
                EmployeeId = membersById[day.AvailabilityGroupMemberId].EmployeeId,
                DayId = day.Id,
                DayOfMonth = day.DayOfMonth,
                Kind = day.Kind,
                IntervalStr = day.IntervalStr
            });
    }
}
