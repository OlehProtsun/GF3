using BusinessLogicLayer.Contracts.Enums;

namespace WebApi.Contracts.AvailabilityGroups;

/// <summary>
/// Flattened API representation of one availability item.
/// This shape combines member-level and day-level information so the frontend can render and edit
/// the monthly matrix without additional join logic.
/// </summary>
public sealed class AvailabilityGroupItemDto
{
    /// <summary>
    /// Member row that owns the day entry.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Employee represented by the member row.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Stable row order used by the planner UI.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Concrete day-entry identifier.
    /// </summary>
    public int DayId { get; set; }

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
