namespace DataAccessLayer.Models.Enums;

/// <summary>
/// Storage-level publication state for schedule graphs.
/// </summary>
public enum SchedulePublicationStatus
{
    /// <summary>
    /// Visible only to managers.
    /// </summary>
    Private,

    /// <summary>
    /// Visible to assigned employees.
    /// </summary>
    Public,
}
