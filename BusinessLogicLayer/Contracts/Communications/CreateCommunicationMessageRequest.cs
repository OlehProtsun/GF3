namespace BusinessLogicLayer.Contracts.Communications;

public sealed class CreateCommunicationMessageRequest
{
    public string? Title { get; set; }

    public string? Body { get; set; }

    public DateTimeOffset? VisibleFromUtc { get; set; }

    public DateTimeOffset? DeadlineAtUtc { get; set; }
}
