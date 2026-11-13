namespace BusinessLogicLayer.Contracts.Enums;

/// <summary>
/// Publication state that controls employee visibility for an availability group.
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
