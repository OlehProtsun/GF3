using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.Employees;

/// <summary>
/// Request payload used to update one employee assignment inside a graph.
/// The route identifies the assignment, while this payload carries the editable employee-specific settings.
/// </summary>
public sealed class UpdateGraphEmployeeRequest
{
    /// <summary>
    /// Employee that should participate in the graph.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Optional minimum monthly hours target used by the generator.
    /// </summary>
    public int? MinHoursMonth { get; set; }

    /// <summary>
    /// Stable row order used by the planner UI.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
