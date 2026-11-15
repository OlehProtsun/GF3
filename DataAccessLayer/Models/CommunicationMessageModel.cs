using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Manager-authored message shown to employees until its deadline passes or an employee dismisses it.
/// </summary>
[Table("communication_message")]
public class CommunicationMessageModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    [Column("title")]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(4000)]
    [Column("body")]
    public string Body { get; set; } = null!;

    [Column("visible_from_utc")]
    public DateTimeOffset? VisibleFromUtc { get; set; }

    [Column("deadline_at_utc")]
    public DateTimeOffset DeadlineAtUtc { get; set; }

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Column("created_by_manager_id")]
    public int? CreatedByManagerId { get; set; }

    [Required]
    [MaxLength(160)]
    [Column("created_by_manager_name")]
    public string CreatedByManagerName { get; set; } = null!;

    public ManagerAccountModel? CreatedByManager { get; set; }

    public ICollection<EmployeeCommunicationDismissalModel> Dismissals { get; } = [];
}
