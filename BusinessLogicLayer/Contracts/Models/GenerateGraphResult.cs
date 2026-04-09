namespace BusinessLogicLayer.Contracts.Models;

/// <summary>
/// Result returned by graph generation flows.
/// It captures both what the generator produced and what was actually written to storage.
/// </summary>
public sealed class GenerateGraphResult
{
    /// <summary>
    /// Container that owns the graph.
    /// </summary>
    public int ContainerId { get; set; }

    /// <summary>
    /// Graph that generation was executed for.
    /// </summary>
    public int GraphId { get; set; }

    /// <summary>
    /// Number of slots produced by the generation algorithm before persistence rules are applied.
    /// </summary>
    public int GeneratedSlotsCount { get; set; }

    /// <summary>
    /// Number of slots actually written back to storage.
    /// This may differ from <see cref="GeneratedSlotsCount"/> during dry-runs or overwrite decisions.
    /// </summary>
    public int WrittenSlotsCount { get; set; }

    /// <summary>
    /// Generated slots optionally returned to callers for preview or response rendering.
    /// </summary>
    public IList<ScheduleSlotModel> Slots { get; set; } = new List<ScheduleSlotModel>();
}
