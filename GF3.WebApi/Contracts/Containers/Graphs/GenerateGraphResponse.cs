using WebApi.Contracts.Containers.Graphs.Slots;

namespace WebApi.Contracts.Containers.Graphs;

/// <summary>
/// Response returned after a graph generation attempt.
/// It summarizes what the generator produced and, when requested, may include the generated slot payload itself.
/// </summary>
public sealed class GenerateGraphResponse
{
    /// <summary>
    /// Container that owns the graph.
    /// </summary>
    public int ContainerId { get; set; }

    /// <summary>
    /// Graph that generation was performed for.
    /// </summary>
    public int GraphId { get; set; }

    /// <summary>
    /// Number of slots produced by the generation algorithm before persistence rules are applied.
    /// </summary>
    public int GeneratedSlotsCount { get; set; }

    /// <summary>
    /// Number of slots actually written back to storage.
    /// This may differ from <see cref="GeneratedSlotsCount"/> during dry-runs or partial overwrite scenarios.
    /// </summary>
    public int WrittenSlotsCount { get; set; }

    /// <summary>
    /// Optional slot payload returned when the caller requests generated slot details.
    /// </summary>
    public IEnumerable<GraphSlotDto>? Slots { get; set; }
}
