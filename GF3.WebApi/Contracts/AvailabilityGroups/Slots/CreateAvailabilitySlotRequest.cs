using BusinessLogicLayer.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.AvailabilityGroups.Slots;

/// <summary>
/// Request payload used to create one day entry in an availability group.
/// </summary>
public sealed class CreateAvailabilitySlotRequest
{
    /// <summary>
    /// Member row that owns the day entry.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AvailabilityGroupMemberId { get; set; }

    /// <summary>
    /// Calendar day within the group month.
    /// </summary>
    [Range(1, 31)]
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Availability mode for the day.
    /// </summary>
    [Required]
    public AvailabilityKind Kind { get; set; }

    /// <summary>
    /// Optional normalized interval string used only for interval-based availability.
    /// </summary>
    [StringLength(200)]
    public string? IntervalStr { get; set; }
}
