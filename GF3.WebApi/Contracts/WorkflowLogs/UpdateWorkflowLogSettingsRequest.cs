using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.WorkflowLogs;

public sealed class UpdateWorkflowLogSettingsRequest
{
    public bool IsEnabled { get; set; }

    [Required]
    public string Audience { get; set; } = string.Empty;
}
