namespace WebApi.Contracts.Containers.Graphs;

/// <summary>
/// Request payload that controls graph generation behavior.
/// It separates generation intent from graph configuration itself: overwrite semantics, dry-run behavior,
/// and whether the generated slots should be included in the response.
/// </summary>
public sealed class GenerateGraphRequest
{
    /// <summary>
    /// When <see langword="true"/>, previously persisted slots may be replaced by the newly generated result.
    /// </summary>
    public bool Overwrite { get; set; } = true;

    /// <summary>
    /// When enabled, generation is evaluated without persisting the resulting slots.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Controls whether the API returns the generated slots together with generation counters.
    /// </summary>
    public bool ReturnSlots { get; set; } = true;
}
