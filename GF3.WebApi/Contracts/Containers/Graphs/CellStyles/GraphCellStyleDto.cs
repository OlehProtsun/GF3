namespace WebApi.Contracts.Containers.Graphs.CellStyles;

/// <summary>
/// API representation of one per-cell style override.
/// </summary>
public sealed class GraphCellStyleDto
{
    /// <summary>
    /// Persistent style identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Graph that owns the styled cell.
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// Calendar day within the graph month.
    /// </summary>
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Employee row that the style belongs to.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Optional ARGB background color override.
    /// </summary>
    public int? BackgroundColorArgb { get; set; }

    /// <summary>
    /// Optional ARGB text color override.
    /// </summary>
    public int? TextColorArgb { get; set; }
}
