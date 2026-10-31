namespace WebApi.Contracts.ShiftSwaps;

public sealed class ContractShiftApprovalDto
{
    public int Id { get; set; }
    public string Kind { get; set; } = "claim";
    public string Status { get; set; } = "pending";
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int? FromEmployeeId { get; set; }
    public string? FromEmployeeName { get; set; }
    public int? ToEmployeeId { get; set; }
    public string? ToEmployeeName { get; set; }
    public bool? IsManagerCreated { get; set; }
    public int? ReturnDayOfMonth { get; set; }
    public int? ReturnYear { get; set; }
    public int? ReturnMonth { get; set; }
    public int ScheduleId { get; set; }
    public int ScheduleSlotId { get; set; }
    public string ScheduleName { get; set; } = "";
    public int ShopId { get; set; }
    public string ShopName { get; set; } = "";
    public int Year { get; set; }
    public int Month { get; set; }
    public int DayOfMonth { get; set; }
    public string FromTime { get; set; } = "";
    public string ToTime { get; set; } = "";
    public int? ShiftSwapRequestId { get; set; }
    public int? ResultingShiftSwapRequestId { get; set; }
    public int? TargetEmployeeId { get; set; }
    public string? RequestKind { get; set; }
    public int? ReturnScheduleId { get; set; }
    public int? ReturnScheduleSlotId { get; set; }
    public string? ReturnFromTime { get; set; }
    public string? ReturnToTime { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public int? ReviewedByManagerId { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? ApprovalReason { get; set; }
    public string? MinimumShopName { get; set; }
    public int? MinimumYear { get; set; }
    public int? MinimumMonth { get; set; }
    public int? MinimumMinutes { get; set; }
    public int? ProjectedMinutes { get; set; }
    public bool IsStale { get; set; }
    public string? FailureReason { get; set; }
}
