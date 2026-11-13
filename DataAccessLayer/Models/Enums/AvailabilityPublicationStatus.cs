namespace DataAccessLayer.Models.Enums;

/// <summary>
/// Storage-level publication state for monthly availability groups.
/// </summary>
public enum AvailabilityPublicationStatus
{
    /// <summary>
    /// Visible only to managers.
    /// </summary>
    Private,

    /// <summary>
    /// Visible to assigned employees within the configured publication window.
    /// </summary>
    Public,
}
