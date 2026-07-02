using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("shift_swap_history")]
[Index(nameof(SourceShiftSwapRequestId), IsUnique = true, Name = "ux_shift_swap_history_source_request")]
[Index(nameof(ScheduleId), nameof(AcceptedAtUtc), Name = "ix_shift_swap_history_schedule_accepted")]
public class ShiftSwapHistoryModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("source_shift_swap_request_id")]
    public int SourceShiftSwapRequestId { get; set; }

    [Required]
    [Column("schedule_id")]
    public int ScheduleId { get; set; }

    public ScheduleModel Schedule { get; set; } = null!;

    [Required]
    [Column("schedule_slot_id")]
    public int ScheduleSlotId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("schedule_name")]
    public string ScheduleName { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    [Column("container_name")]
    public string ContainerName { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    [Column("shop_name")]
    public string ShopName { get; set; } = null!;

    [Required]
    [Column("year")]
    public int Year { get; set; }

    [Required]
    [Column("month")]
    public int Month { get; set; }

    [Required]
    [Column("day_of_month")]
    public int DayOfMonth { get; set; }

    [Required]
    [MaxLength(5)]
    [Column("from_time")]
    public string FromTime { get; set; } = null!;

    [Required]
    [MaxLength(5)]
    [Column("to_time")]
    public string ToTime { get; set; } = null!;

    [Column("from_employee_id")]
    public int? FromEmployeeId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("from_employee_name")]
    public string FromEmployeeName { get; set; } = null!;

    [Column("target_employee_id")]
    public int? TargetEmployeeId { get; set; }

    [MaxLength(200)]
    [Column("target_employee_name")]
    public string? TargetEmployeeName { get; set; }

    [Required]
    [Column("accepted_by_employee_id")]
    public int AcceptedByEmployeeId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("accepted_by_employee_name")]
    public string AcceptedByEmployeeName { get; set; } = null!;

    [Required]
    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Required]
    [Column("accepted_at_utc")]
    public DateTimeOffset AcceptedAtUtc { get; set; }

    [Required]
    [Column("is_manager_created")]
    public bool IsManagerCreated { get; set; }

    [Column("manual_column_id")]
    public int? ManualColumnId { get; set; }

    [MaxLength(200)]
    [Column("manual_column_name")]
    public string? ManualColumnName { get; set; }

    [Required]
    [Column("before_snapshot_json")]
    public string BeforeSnapshotJson { get; set; } = """{"rows":[]}""";

    [Required]
    [Column("after_snapshot_json")]
    public string AfterSnapshotJson { get; set; } = """{"rows":[]}""";
}
