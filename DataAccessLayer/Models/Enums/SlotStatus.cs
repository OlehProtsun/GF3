namespace DataAccessLayer.Models.Enums;

/// <summary>
/// Storage-level status of one schedule slot.
/// This enum captures whether the slot still needs staffing or is already assigned to an employee.
/// </summary>
public enum SlotStatus
{
    /// <summary>
    /// The slot exists but no employee has been assigned yet.
    /// </summary>
    UNFURNISHED,

    /// <summary>
    /// The slot is fully assigned to an employee.
    /// </summary>
    ASSIGNED,
}
