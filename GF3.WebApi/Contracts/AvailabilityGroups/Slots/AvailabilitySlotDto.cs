using BusinessLogicLayer.Contracts.Enums;

namespace WebApi.Contracts.AvailabilityGroups.Slots;

/// <summary>
/// API representation of one day entry inside an availability group.
/// </summary>
public sealed class AvailabilitySlotDto
{
    /// <summary>
    /// Persistent day-entry identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Member row that owns the day entry.
    /// </summary>
    public int AvailabilityGroupMemberId { get; set; }

    /// <summary>
    /// Calendar day within the group month.
    /// </summary>
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Availability mode for the day.
    /// </summary>
    public AvailabilityKind Kind { get; set; }

    /// <summary>
    /// Optional normalized interval string used only for interval-based availability.
    /// </summary>
    public string? IntervalStr { get; set; }
}
