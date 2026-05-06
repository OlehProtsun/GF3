namespace WebApi.Contracts.ShiftSwaps;

public sealed class ShiftSwapDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int ScheduleSlotId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int DayOfMonth { get; set; }
    public string FromTime { get; set; } = string.Empty;
    public string ToTime { get; set; } = string.Empty;
    public int? FromEmployeeId { get; set; }
    public string FromEmployeeName { get; set; } = string.Empty;
    public int? TargetEmployeeId { get; set; }
    public string? TargetEmployeeName { get; set; }
    public int? AcceptedByEmployeeId { get; set; }
    public string? AcceptedByEmployeeName { get; set; }
    public string Visibility { get; set; } = "public";
    public string Status { get; set; } = "open";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public double ShiftHours { get; set; }
    public double CurrentEmployeeHoursBefore { get; set; }
    public double CurrentEmployeeHoursAfter { get; set; }
    public double FromEmployeeHoursBefore { get; set; }
    public double FromEmployeeHoursAfter { get; set; }
    public bool IsManagerCreated { get; set; }
    public int? ManualColumnId { get; set; }
    public string? ManualColumnName { get; set; }
    public bool IsCreatedByCurrentEmployee { get; set; }
    public bool CanAccept { get; set; }
    public bool CanCancel { get; set; }
}
