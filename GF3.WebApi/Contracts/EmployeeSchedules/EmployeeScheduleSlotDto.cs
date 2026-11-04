namespace WebApi.Contracts.EmployeeSchedules;

/// <summary>
/// One assigned shift inside a published schedule.
/// </summary>
public sealed class EmployeeScheduleSlotDto
{
    public int Id { get; set; }
    public int DayOfMonth { get; set; }
    public int SlotNo { get; set; }
    public int? EmployeeId { get; set; }
    public string FromTime { get; set; } = string.Empty;
    public string ToTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
