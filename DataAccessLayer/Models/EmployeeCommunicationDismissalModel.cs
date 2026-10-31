using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

/// <summary>
/// Per-employee opt-out for one active communication message.
/// </summary>
[Table("employee_communication_dismissal")]
public class EmployeeCommunicationDismissalModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("communication_message_id")]
    public int CommunicationMessageId { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("dismissed_at_utc")]
    public DateTimeOffset DismissedAtUtc { get; set; }

    public CommunicationMessageModel CommunicationMessage { get; set; } = null!;

    public EmployeeModel Employee { get; set; } = null!;
}
