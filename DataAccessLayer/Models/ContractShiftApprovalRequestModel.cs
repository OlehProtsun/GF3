using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models;

[Table("contract_shift_approval_request")]
public sealed class ContractShiftApprovalRequestModel
{
    [Key, Column("id")] public int Id { get; set; }
    [Required, MaxLength(24), Column("kind")] public string Kind { get; set; } = "claim";
    [Required, MaxLength(16), Column("status")] public string Status { get; set; } = "pending";
    [Column("employee_id")] public int EmployeeId { get; set; }
    [Column("schedule_id")] public int ScheduleId { get; set; }
    [Column("schedule_slot_id")] public int ScheduleSlotId { get; set; }
    [Column("original_day_of_month")] public int OriginalDayOfMonth { get; set; }
    [Required, MaxLength(5), Column("original_from_time")] public string OriginalFromTime { get; set; } = "";
    [Required, MaxLength(5), Column("original_to_time")] public string OriginalToTime { get; set; } = "";
    [Column("return_original_day_of_month")] public int? ReturnOriginalDayOfMonth { get; set; }
    [MaxLength(5), Column("return_original_from_time")] public string? ReturnOriginalFromTime { get; set; }
    [MaxLength(5), Column("return_original_to_time")] public string? ReturnOriginalToTime { get; set; }
    [Column("return_year")] public int? ReturnYear { get; set; }
    [Column("return_month")] public int? ReturnMonth { get; set; }
    [Column("return_shop_id")] public int? ReturnShopId { get; set; }
    [Column("shift_swap_request_id")] public int? ShiftSwapRequestId { get; set; }
    [Column("resulting_shift_swap_request_id")] public int? ResultingShiftSwapRequestId { get; set; }
    [Column("target_employee_id")] public int? TargetEmployeeId { get; set; }
    [MaxLength(16), Column("request_kind")] public string? RequestKind { get; set; }
    [Column("allow_partial_acceptance")] public bool AllowPartialAcceptance { get; set; }
    [Required, MaxLength(5), Column("from_time")] public string FromTime { get; set; } = "";
    [Required, MaxLength(5), Column("to_time")] public string ToTime { get; set; } = "";
    [Column("return_schedule_id")] public int? ReturnScheduleId { get; set; }
    [Column("return_schedule_slot_id")] public int? ReturnScheduleSlotId { get; set; }
    [MaxLength(5), Column("return_from_time")] public string? ReturnFromTime { get; set; }
    [MaxLength(5), Column("return_to_time")] public string? ReturnToTime { get; set; }
    [Column("created_at_utc")] public DateTimeOffset CreatedAtUtc { get; set; }
    [Column("reviewed_at_utc")] public DateTimeOffset? ReviewedAtUtc { get; set; }
    [Column("reviewed_by_manager_id")] public int? ReviewedByManagerId { get; set; }
    [Column("cancelled_at_utc")] public DateTimeOffset? CancelledAtUtc { get; set; }
    [Column("shop_id")] public int ShopId { get; set; }
    [Column("year")] public int Year { get; set; }
    [Column("month")] public int Month { get; set; }
    [MaxLength(250), Column("failure_reason")] public string? FailureReason { get; set; }
}
