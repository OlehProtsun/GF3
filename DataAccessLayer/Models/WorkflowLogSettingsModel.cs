using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("workflow_log_settings")]
public sealed class WorkflowLogSettingsModel
{
    public const int SingletonId = 1;
    public const string AllAudience = "all";
    public const string ManagersAudience = "managers";
    public const string EmployeesAudience = "employees";

    [Key]
    [Column("id")]
    public int Id { get; set; } = SingletonId;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    [Required]
    [Column("audience")]
    public string Audience { get; set; } = AllAudience;
}
