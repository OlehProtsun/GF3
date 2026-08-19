using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Models;

[Table("shift_correction_request")]
[Index(nameof(ScheduleId), nameof(Status), Name = "ix_shift_correction_schedule_status")]
[Index(nameof(EmployeeId), nameof(Status), Name = "ix_shift_correction_employee_status")]
public sealed class ShiftCorrectionRequestModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("schedule_id")]
    public int ScheduleId { get; set; }

    public ScheduleModel Schedule { get; set; } = null!;

    [Column("schedule_slot_id")]
    public int ScheduleSlotId { get; set; }

    [Column("day_of_month")]
    public int DayOfMonth { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    public EmployeeModel Employee { get; set; } = null!;

    [Column("original_from_time")]
    public string OriginalFromTime { get; set; } = string.Empty;

    [Column("original_to_time")]
    public string OriginalToTime { get; set; } = string.Empty;

    [Column("requested_from_time")]
    public string RequestedFromTime { get; set; } = string.Empty;

    [Column("requested_to_time")]
    public string RequestedToTime { get; set; } = string.Empty;

    [Column("status")]
    public ShiftCorrectionStatus Status { get; set; } = ShiftCorrectionStatus.Pending;

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [Column("reviewed_at_utc")]
    public DateTimeOffset? ReviewedAtUtc { get; set; }

    [Column("reviewed_by_manager_id")]
    public int? ReviewedByManagerId { get; set; }

    public ManagerAccountModel? ReviewedByManager { get; set; }
}
