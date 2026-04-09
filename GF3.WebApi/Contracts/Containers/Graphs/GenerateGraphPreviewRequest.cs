using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.Containers.Graphs;

/// <summary>
/// Request payload used to generate a preview from a client-side graph snapshot.
/// This allows the frontend to validate scheduling changes before the graph is persisted.
/// </summary>
public sealed class GenerateGraphPreviewRequest
{
    /// <summary>
    /// Optional existing graph identifier when the preview is based on an already persisted graph.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? GraphId { get; set; }

    /// <summary>
    /// Graph configuration that should be previewed by the generator.
    /// </summary>
    [Required]
    public CreateGraphRequest Graph { get; set; } = new();

    /// <summary>
    /// Employees that participate in the preview graph together with their per-employee generation settings.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<GenerateGraphPreviewEmployeeRequest> Employees { get; set; } = new();
}

/// <summary>
/// One employee assignment included in a graph preview request.
/// </summary>
public sealed class GenerateGraphPreviewEmployeeRequest
{
    /// <summary>
    /// Employee that should participate in preview generation.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Optional minimum monthly hours target used by the generator.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? MinHoursMonth { get; set; }

    /// <summary>
    /// Stable row order used when previewing the planner matrix.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
