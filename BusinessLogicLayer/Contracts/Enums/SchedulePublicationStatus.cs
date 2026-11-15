namespace BusinessLogicLayer.Contracts.Enums;

/// <summary>
/// Publication state that controls employee visibility for a schedule graph.
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
