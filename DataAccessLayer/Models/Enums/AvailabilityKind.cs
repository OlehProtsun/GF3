namespace DataAccessLayer.Models.Enums;

/// <summary>
/// Storage-level representation of employee availability for one calendar day.
/// The business layer exposes a mirrored enum so the persistence model stays decoupled from API contracts.
/// </summary>
public enum AvailabilityKind
{
    /// <summary>
    /// Employee is available for any shift on the day.
    /// </summary>
    ANY,

    /// <summary>
    /// Employee is unavailable on the day.
    /// </summary>
    NONE,

    /// <summary>
    /// Employee is available only within a specific time interval.
    /// The concrete interval is stored separately in <c>interval_str</c>.
    /// </summary>
    INT,
}
