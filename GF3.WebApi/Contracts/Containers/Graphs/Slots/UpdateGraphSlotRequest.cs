using BusinessLogicLayer.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.Slots;

/// <summary>
/// Request payload used to update one existing slot inside a persisted graph.
/// The route identifies the slot, while this payload carries the editable slot fields.
/// </summary>
public sealed class UpdateGraphSlotRequest
{
    /// <summary>
    /// Calendar day within the graph month.
    /// </summary>
    [Required]
    [Range(1, 31)]
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Position index inside the same shift interval when multiple employees are required.
    /// </summary>
    [Required]
    [Range(1, 100)]
    public int SlotNo { get; set; }

    /// <summary>
    /// Slot start time in <c>HH:mm</c> format.
    /// </summary>
    [Required]
    [RegularExpression("^([01]\\d|2[0-3]):[0-5]\\d$")]
    public string FromTime { get; set; } = string.Empty;

    /// <summary>
    /// Slot end time in <c>HH:mm</c> format.
    /// </summary>
    [Required]
    [RegularExpression("^([01]\\d|2[0-3]):[0-5]\\d$")]
    public string ToTime { get; set; } = string.Empty;

    /// <summary>
    /// Optional employee assigned to the slot.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Current staffing status of the slot.
    /// </summary>
    [Required]
    public SlotStatus Status { get; set; }
}
