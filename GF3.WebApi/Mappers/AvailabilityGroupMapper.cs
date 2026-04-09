using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.AvailabilityGroups;

namespace WebApi.Mappers;

/// <summary>
/// Maps top-level availability-group contracts and flattened availability items.
/// </summary>
public static class AvailabilityGroupMapper
{
    public static AvailabilityGroupDto ToApiDto(this AvailabilityGroupModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Year = model.Year,
        Month = model.Month,
    };

    public static AvailabilityGroupModel ToCreateModel(this CreateAvailabilityGroupRequest request)
        => MapAvailabilityGroupModel(request.Name, request.Year, request.Month);

    public static AvailabilityGroupModel ToUpdateModel(this UpdateAvailabilityGroupRequest request, int id)
        => MapAvailabilityGroupModel(request.Name, request.Year, request.Month, id);

    /// <summary>
    /// Flattens group members and their day entries into the compact list shape used by the API.
    /// </summary>
    public static IEnumerable<AvailabilityGroupItemDto> ToItemDtos(
        this IEnumerable<AvailabilityGroupMemberModel> members,
        IEnumerable<AvailabilityGroupDayModel> days)
    {
        var membersById = members.ToDictionary(member => member.Id, member => member);

        return days
            .Where(day => membersById.ContainsKey(day.AvailabilityGroupMemberId))
            .Select(day =>
            {
                var member = membersById[day.AvailabilityGroupMemberId];
                return new AvailabilityGroupItemDto
                {
                    MemberId = day.AvailabilityGroupMemberId,
                    EmployeeId = member.EmployeeId,
                    DisplayOrder = member.DisplayOrder,
                    DayId = day.Id,
                    DayOfMonth = day.DayOfMonth,
                    Kind = day.Kind,
                    IntervalStr = day.IntervalStr,
                };
            });
    }

    private static AvailabilityGroupModel MapAvailabilityGroupModel(string name, int year, int month, int id = 0)
        => new()
        {
            Id = id,
            Name = name,
            Year = year,
            Month = month,
        };
}
