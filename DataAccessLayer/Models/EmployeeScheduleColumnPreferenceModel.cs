using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("employee_schedule_column_preference")]
public sealed class EmployeeScheduleColumnPreferenceModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("schedule_id")]
    public int ScheduleId { get; set; }

    [Column("column_order_json")]
    public string ColumnOrderJson { get; set; } = "[]";

    [Column("updated_at_utc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public EmployeeModel Employee { get; set; } = null!;

    public ScheduleModel Schedule { get; set; } = null!;
}