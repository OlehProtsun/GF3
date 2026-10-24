namespace WebApi.Contracts.ShiftSwaps;

public sealed class CreateManagerShiftSwapRequest
{
    public int ManualColumnId { get; set; }
    public int DayOfMonth { get; set; }
    public string FromTime { get; set; } = string.Empty;
    public string ToTime { get; set; } = string.Empty;
    public int? TargetEmployeeId { get; set; }
}
