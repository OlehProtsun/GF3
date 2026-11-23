namespace BusinessLogicLayer.Contracts.Communications;

public sealed class CreateCommunicationMessageRequest
{
    public IReadOnlyList<CommunicationContentBlock>? Blocks { get; init; }
    public CommunicationGifAttachment? Photo { get; set; }
    public CommunicationGifAttachment? Gif { get; set; }

    public string? Title { get; set; }

    public string? Body { get; set; }

    public DateTimeOffset? VisibleFromUtc { get; set; }

    public DateTimeOffset? DeadlineAtUtc { get; set; }
}
