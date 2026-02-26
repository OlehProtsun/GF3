using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.CellStyles;

public sealed class UpsertGraphCellStyleRequest
{
    [Required]
    [Range(1, 31)]
    public int DayOfMonth { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    public int? BackgroundColorArgb { get; set; }
    public int? TextColorArgb { get; set; }
}
