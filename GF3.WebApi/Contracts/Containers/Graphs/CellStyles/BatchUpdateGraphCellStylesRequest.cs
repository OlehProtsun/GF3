using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.CellStyles;

public sealed class BatchUpdateGraphCellStylesRequest : IValidatableObject
{
    [Required]
    public required List<GraphCellStyleFieldPatchDto> Changes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var keys = new HashSet<(int EmployeeId, int DayOfMonth)>();
        foreach (var change in Changes)
        {
            if (change is null || !change.ChangeBackgroundColor && !change.ChangeTextColor ||
                !keys.Add((change.EmployeeId, change.DayOfMonth)))
            {
                yield return new ValidationResult("Each cell must appear once and change at least one color.", [nameof(Changes)]);
                yield break;
            }
        }
    }
}

public sealed class GraphCellStyleFieldPatchDto
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; init; }

    [Range(1, 31)]
    public int DayOfMonth { get; init; }

    public bool ChangeBackgroundColor { get; init; }
    public int? ExpectedBackgroundColorArgb { get; init; }
    public int? BackgroundColorArgb { get; init; }
    public bool ChangeTextColor { get; init; }
    public int? ExpectedTextColorArgb { get; init; }
    public int? TextColorArgb { get; init; }
}
