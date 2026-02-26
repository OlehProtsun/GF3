namespace WebApi.Contracts.Containers.Graphs;

public sealed class GenerateGraphRequest
{
    public bool Overwrite { get; set; } = true;
    public bool DryRun { get; set; } = false;
    public bool ReturnSlots { get; set; } = true;
}
