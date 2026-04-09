namespace BusinessLogicLayer.Contracts.Enums;

/// <summary>
/// Assignment state of one concrete schedule slot.
/// </summary>
public enum SlotStatus
{
    /// <summary>
    /// The slot exists but is not assigned to an employee yet.
    /// </summary>
    UNFURNISHED,

    /// <summary>
    /// The slot is assigned to a concrete employee.
    /// </summary>
    ASSIGNED,
}
