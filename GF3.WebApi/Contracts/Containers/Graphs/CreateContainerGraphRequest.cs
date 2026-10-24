using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs;

/// <summary>
/// Legacy-compatible request payload used by container-scoped graph creation flows.
/// It intentionally mirrors <see cref="CreateGraphRequest"/> so older endpoints remain compatible without
/// introducing a second set of graph configuration rules.
/// </summary>
public sealed class CreateContainerGraphRequest
{
    /// <summary>
    /// Shop that the graph belongs to.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ShopId { get; set; }

    /// <summary>
    /// Human-readable graph name shown in the planner UI.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Calendar year that the graph is generated for.
    /// </summary>
    [Range(1, 9999)]
    public int Year { get; set; }

    /// <summary>
    /// Calendar month that the graph is generated for.
    /// </summary>
    [Range(1, 12)]
    public int Month { get; set; }

    /// <summary>
    /// Publication state exposed to assigned employees.
    /// </summary>
    public string? PublicationStatus { get; set; } = "private";

    /// <summary>
    /// Number of employees required in each shift interval.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PeoplePerShift { get; set; }

    /// <summary>
    /// First shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Shift1Time { get; set; } = string.Empty;

    /// <summary>
    /// Second shift interval in <c>HH:mm - HH:mm</c> format.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Shift2Time { get; set; } = string.Empty;

    /// <summary>
    /// Upper bound for employee hours within the month.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxHoursPerEmpMonth { get; set; }

    /// <summary>
    /// Maximum allowed number of worked days in a row.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxConsecutiveDays { get; set; }

    /// <summary>
    /// Maximum allowed number of consecutive full-shift days.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxConsecutiveFull { get; set; }

    /// <summary>
    /// Maximum allowed number of full shifts within the whole month.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxFullPerMonth { get; set; }

    /// <summary>
    /// Optional free-form planner note stored together with the graph.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Optional availability group that constrains assignment decisions during generation.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? AvailabilityGroupId { get; set; }
}
