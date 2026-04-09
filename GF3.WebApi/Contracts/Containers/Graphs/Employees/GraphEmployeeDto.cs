namespace WebApi.Contracts.Containers.Graphs.Employees;

/// <summary>
/// API representation of one employee assignment inside a graph.
/// </summary>
public sealed class GraphEmployeeDto
{
    /// <summary>
    /// Persistent assignment identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Graph that owns the employee assignment.
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// Employee participating in the graph.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Optional minimum monthly hours target used by the generator.
    /// </summary>
    public int? MinHoursMonth { get; set; }

    /// <summary>
    /// Stable row order used by the planner UI.
    /// </summary>
    public int DisplayOrder { get; set; }
}
