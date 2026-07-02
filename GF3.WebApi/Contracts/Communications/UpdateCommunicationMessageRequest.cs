namespace WebApi.Contracts.Communications;

public sealed class UpdateCommunicationMessageRequest
{
    public string? Title { get; set; }

    public string? Body { get; set; }

    public DateTimeOffset? VisibleFromUtc { get; set; }

    public DateTimeOffset? DeadlineAtUtc { get; set; }
}
