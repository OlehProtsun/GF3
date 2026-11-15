namespace WebApi.Contracts.WorkflowLogs;

public sealed class DeleteWorkflowLogsRequest
{
    public bool DeleteAll { get; set; }

    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }
}

public sealed class WorkflowLogDeleteResultDto
{
    public int DeletedCount { get; set; }
}
