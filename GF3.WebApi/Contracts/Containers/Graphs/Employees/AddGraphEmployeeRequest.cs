using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs.Employees;

/// <summary>
/// Request payload used to add one employee assignment to a graph.
/// This record links an employee to the graph and optionally supplies generation hints such as minimum hours.
/// </summary>
public sealed class AddGraphEmployeeRequest
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
