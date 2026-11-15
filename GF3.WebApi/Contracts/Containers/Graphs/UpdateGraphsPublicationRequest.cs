namespace WebApi.Contracts.Containers.Graphs;

public sealed class UpdateGraphsPublicationRequest
{
    public string PublicationStatus { get; set; } = "private";

    public bool? AllowSwap { get; set; }
}
