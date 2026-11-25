

namespace WebApi.Contracts.Communications;

public sealed class CommunicationGifForm
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTimeOffset? VisibleFromUtc { get; set; }
    public DateTimeOffset? DeadlineAtUtc { get; set; }

    public string? Blocks { get; set; }

    public bool RemoveGif { get; set; }
    public bool RemovePhoto { get; set; }
}
