using BusinessLogicLayer.Contracts.Enums;

namespace WebApi.Contracts.Containers.Graphs.Slots;

/// <summary>
/// API representation of one concrete graph slot.
/// </summary>
public sealed class GraphSlotDto
{
    /// <summary>
    /// Persistent slot identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Graph that owns the slot.
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// Calendar day within the graph month.
    /// </summary>
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Position index inside the same shift interval.
    /// </summary>
    public int SlotNo { get; set; }

    /// <summary>
    /// Slot start time in <c>HH:mm</c> format.
    /// </summary>
    public string FromTime { get; set; } = string.Empty;

    /// <summary>
    /// Slot end time in <c>HH:mm</c> format.
    /// </summary>
    public string ToTime { get; set; } = string.Empty;

    /// <summary>
    /// Optional employee assigned to the slot.
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Current staffing status of the slot.
    /// </summary>
    public SlotStatus Status { get; set; }
}
