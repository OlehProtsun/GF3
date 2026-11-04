using System.Text.Json;

namespace DataAccessLayer.Models;

/// <summary>Publication identity is stable; employee eligibility starts at the latest real transition.</summary>
public static class AvailabilityNotificationTiming
{
    public static string Id(AvailabilityGroupModel group)
    {
        var identity = group.PublishedAtUtc is { } published ? $"published:{JsonSerializer.Serialize(published).Trim('"')}"
            : group.NotificationLegacyVisibleFromUtc is { } legacy ? JsonSerializer.Serialize(legacy).Trim('"') : "current";
        return $"availability-public:{group.Id}:{identity}";
    }

    public static DateTimeOffset? OccurredAt(AvailabilityGroupModel group, AvailabilityGroupMemberModel member) =>
        new[] { group.NotificationAvailableAtUtc ?? group.NotificationLegacyVisibleFromUtc, group.PublishedAtUtc, member.JoinedAtUtc }.Max();

    public static bool HasKnownEvent(AvailabilityGroupModel group, AvailabilityGroupMemberModel member) =>
        group.PublishedAtUtc.HasValue || member.JoinedAtUtc.HasValue || group.NotificationAvailableAtUtc.HasValue;
}
