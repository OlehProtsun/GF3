namespace BusinessLogicLayer.Contracts.Enums;

/// <summary>
/// Availability semantics used by availability-group day entries.
/// </summary>
public enum AvailabilityKind
{
    /// <summary>
    /// The employee is available for the whole day.
    /// </summary>
    ANY,

    /// <summary>
    /// The employee is unavailable for the whole day.
    /// </summary>
    NONE,

    /// <summary>
    /// The employee is available only within the supplied interval string.
    /// </summary>
    INT,
}
