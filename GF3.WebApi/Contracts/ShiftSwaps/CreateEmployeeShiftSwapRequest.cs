namespace WebApi.Contracts.ShiftSwaps;

public sealed class CreateEmployeeShiftSwapRequest
{
    public string? RequestKind { get; set; }
    public int? ReturnScheduleId { get; set; }
    public int? ReturnScheduleSlotId { get; set; }
    public string? ReturnFromTime { get; set; }
    public string? ReturnToTime { get; set; }
    public int ScheduleId { get; set; }
    public int ScheduleSlotId { get; set; }
    public string? FromTime { get; set; }
    public string? ToTime { get; set; }
    public int? TargetEmployeeId { get; set; }
}
