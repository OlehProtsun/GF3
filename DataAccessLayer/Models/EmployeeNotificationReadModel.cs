using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("employee_notification_read")]
public sealed class EmployeeNotificationReadModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("notification_id")]
    public string NotificationId { get; set; } = null!;

    [Column("read_at_utc")]
    public DateTimeOffset ReadAtUtc { get; set; }

    public EmployeeModel Employee { get; set; } = null!;
}