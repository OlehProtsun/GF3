namespace WebApi.Contracts.ShiftSwaps;

public sealed class CreateEmployeeShiftSwapRequest
{
    public int ScheduleId { get; set; }
    public int ScheduleSlotId { get; set; }
    public string? FromTime { get; set; }
    public string? ToTime { get; set; }
    public int? TargetEmployeeId { get; set; }
}
