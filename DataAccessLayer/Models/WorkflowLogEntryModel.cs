using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Models;

[Table("workflow_log_entry")]
[Index(nameof(OccurredAtUtc), Name = "ix_workflow_log_occurred_at")]
[Index(nameof(ActorRole), nameof(OccurredAtUtc), Name = "ix_workflow_log_role_time")]
public class WorkflowLogEntryModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("occurred_at_utc")]
    public DateTimeOffset OccurredAtUtc { get; set; }

    [Required]
    [Column("actor_role")]
    public string ActorRole { get; set; } = string.Empty;

    [Column("actor_employee_id")]
    public int? ActorEmployeeId { get; set; }

    [Required]
    [Column("actor_name")]
    public string ActorName { get; set; } = string.Empty;

    [Required]
    [Column("action")]
    public string Action { get; set; } = string.Empty;
}
