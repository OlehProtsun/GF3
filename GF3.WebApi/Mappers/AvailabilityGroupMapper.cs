using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Common;
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
        PublicationStatus = ToApiPublicationStatus(model.PublicationStatus),
        VisibleFromUtc = model.VisibleFromUtc,
        VisibleToUtc = model.VisibleToUtc,
    };

    public static AvailabilityGroupModel ToCreateModel(this CreateAvailabilityGroupRequest request)
        => MapAvailabilityGroupModel(
            request.Name,
            request.Year,
            request.Month,
            request.PublicationStatus,
            request.VisibleFromUtc,
            request.VisibleToUtc);

    public static AvailabilityGroupModel ToUpdateModel(this UpdateAvailabilityGroupRequest request, int id)
        => MapAvailabilityGroupModel(
            request.Name,
            request.Year,
            request.Month,
            request.PublicationStatus,
            request.VisibleFromUtc,
            request.VisibleToUtc,
            id);

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

    private static AvailabilityGroupModel MapAvailabilityGroupModel(
        string name,
        int year,
        int month,
        string? publicationStatus,
        DateTimeOffset? visibleFromUtc,
        DateTimeOffset? visibleToUtc,
        int id = 0)
        => new()
        {
            Id = id,
            Name = name,
            Year = year,
            Month = month,
            PublicationStatus = ParsePublicationStatus(publicationStatus),
            VisibleFromUtc = visibleFromUtc,
            VisibleToUtc = visibleToUtc,
        };

    private static string ToApiPublicationStatus(AvailabilityPublicationStatus status)
        => status.ToString().ToLowerInvariant();

    private static AvailabilityPublicationStatus ParsePublicationStatus(string? value)
    {
        var normalized = (value ?? "private").Trim();

        if (string.Equals(normalized, "private", StringComparison.OrdinalIgnoreCase))
        {
            return AvailabilityPublicationStatus.Private;
        }

        if (string.Equals(normalized, "public", StringComparison.OrdinalIgnoreCase))
        {
            return AvailabilityPublicationStatus.Public;
        }

        throw ValidationException.ForField(nameof(CreateAvailabilityGroupRequest.PublicationStatus), "Publication status must be private or public.");
    }
}
