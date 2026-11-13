namespace WebApi.Contracts.WorkflowLogs;

public sealed class WorkflowLogSettingsDto
{
    public bool IsEnabled { get; set; }

    public string Audience { get; set; } = string.Empty;
}
