namespace BusinessLogicLayer.Contracts.Communications;

public sealed class CommunicationMessageDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset VisibleFromUtc { get; set; }

    public DateTimeOffset DeadlineAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public int? CreatedByManagerId { get; set; }

    public string CreatedByManagerName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
