using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.CellStyles;

/// <summary>
/// Request payload used to create or update one per-cell style override.
/// </summary>
public sealed class UpsertGraphCellStyleRequest
{
    /// <summary>
    /// Calendar day within the graph month.
    /// </summary>
    [Required]
    [Range(1, 31)]
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Employee row that the style should be applied to.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
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
