using BusinessLogicLayer.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.Slots;

/// <summary>
/// Request payload used to create one slot inside a persisted graph.
/// A slot represents one concrete staffing position within a specific day and time interval.
/// </summary>
public sealed class CreateGraphSlotRequest
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
