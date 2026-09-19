using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("shift_swap_request")]
[Index(nameof(ScheduleId), nameof(Status), Name = "ix_shift_swap_schedule_status")]
[Index(nameof(TargetEmployeeId), nameof(Status), Name = "ix_shift_swap_target_status")]
[Index(nameof(FromEmployeeId), nameof(Status), Name = "ix_shift_swap_from_status")]
public class ShiftSwapRequestModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("schedule_id")]
    public int ScheduleId { get; set; }

    public ScheduleModel Schedule { get; set; } = null!;

    [Column("schedule_slot_id")]
    public int? ScheduleSlotId { get; set; }

    public ScheduleSlotModel? ScheduleSlot { get; set; }

    [Column("archived_views_json")]
    public string? ArchivedViewsJson { get; set; }

    [Column("offered_from_time")]
    public string? OfferedFromTime { get; set; }

    [Column("offered_to_time")]
    public string? OfferedToTime { get; set; }

    [Column("from_employee_id")]
    public int? FromEmployeeId { get; set; }

    public EmployeeModel? FromEmployee { get; set; }

    [Required]
    [Column("is_manager_created")]
    public bool IsManagerCreated { get; set; }

    [Column("manual_column_id")]
    public int? ManualColumnId { get; set; }

    [Column("target_employee_id")]
    public int? TargetEmployeeId { get; set; }

    public EmployeeModel? TargetEmployee { get; set; }

    [Column("accepted_by_employee_id")]
    public int? AcceptedByEmployeeId { get; set; }

    public EmployeeModel? AcceptedByEmployee { get; set; }

    [Required]
    [Column("visibility")]
    public ShiftSwapVisibility Visibility { get; set; } = ShiftSwapVisibility.Public;

    [Required]
    [Column("status")]
    public ShiftSwapStatus Status { get; set; } = ShiftSwapStatus.Open;

    [Required]
    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Column("accepted_at_utc")]
    public DateTimeOffset? AcceptedAtUtc { get; set; }

    [Column("cancelled_at_utc")]
    public DateTimeOffset? CancelledAtUtc { get; set; }
}
