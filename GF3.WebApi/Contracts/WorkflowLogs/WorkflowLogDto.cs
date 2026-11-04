namespace WebApi.Contracts.WorkflowLogs;

public sealed class WorkflowLogDto
{
    public int Id { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string ActorRole { get; set; } = string.Empty;

    public int? ActorEmployeeId { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
}
