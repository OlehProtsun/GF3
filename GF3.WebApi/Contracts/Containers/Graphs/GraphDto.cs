namespace WebApi.Contracts.Containers.Graphs;

/// <summary>
/// Canonical API representation of one persisted graph.
/// </summary>
public sealed class GraphDto
{
    /// <summary>
    /// Persistent graph identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Container that owns the graph.
    /// </summary>
    public int ContainerId { get; set; }

    /// <summary>
    /// Shop or location the graph was built for.
    /// </summary>
    public int ShopId { get; set; }

    /// <summary>
    /// Human-readable graph name shown in the planner UI.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Calendar year that the graph applies to.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Calendar month that the graph applies to.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Publication state exposed to assigned employees.
    /// </summary>
    public string PublicationStatus { get; set; } = "private";

    public bool AllowSwap { get; set; } = true;

    /// <summary>
    /// Number of employees required in each shift interval.
    /// </summary>
    public int PeoplePerShift { get; set; }

    /// <summary>
    /// First shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    public string Shift1Time { get; set; } = string.Empty;

    /// <summary>
    /// Second shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    public string Shift2Time { get; set; } = string.Empty;

    /// <summary>
    /// Upper bound for employee hours within the month.
    /// </summary>
    public int MaxHoursPerEmpMonth { get; set; }

    /// <summary>
    /// Maximum allowed number of worked days in a row.
    /// </summary>
    public int MaxConsecutiveDays { get; set; }

    /// <summary>
    /// Maximum allowed number of consecutive full-shift days.
    /// </summary>
    public int MaxConsecutiveFull { get; set; }

    /// <summary>
    /// Maximum allowed number of full shifts within the whole month.
    /// </summary>
    public int MaxFullPerMonth { get; set; }

    /// <summary>
    /// Optional free-form planner note.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Optional availability group that constrains generation.
    /// </summary>
    public int? AvailabilityGroupId { get; set; }

    /// <summary>
    /// Latest recorded manager edit or accepted employee swap for this schedule.
    /// Null is reserved for legacy schedules that have no matching workflow history yet.
    /// </summary>
    public DateTimeOffset? LastUpdatedAtUtc { get; set; }
}
