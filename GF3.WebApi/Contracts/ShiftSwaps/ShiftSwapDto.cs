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
    public int CurrentEmployeeWorkDaysBefore { get; set; }
    public int CurrentEmployeeWorkDaysAfter { get; set; }
    public int CurrentEmployeeFreeDaysBefore { get; set; }
    public int CurrentEmployeeFreeDaysAfter { get; set; }
    public double FromEmployeeHoursBefore { get; set; }
    public double FromEmployeeHoursAfter { get; set; }
    public bool IsManagerCreated { get; set; }
    public int? ManualColumnId { get; set; }
    public string? ManualColumnName { get; set; }
    public bool IsCreatedByCurrentEmployee { get; set; }
    public bool IsScheduleLocked { get; set; }
    public bool CanAccept { get; set; }
    public string? AcceptanceUnavailableReason { get; set; }
    public bool CanCancel { get; set; }
    public ShiftSwapScheduleSnapshotDto? BeforeSnapshot { get; set; }
    public ShiftSwapScheduleSnapshotDto? AfterSnapshot { get; set; }
}

public sealed class ShiftSwapScheduleSnapshotDto
{
    public List<ShiftSwapScheduleSnapshotRowDto> Rows { get; set; } = [];
}

public sealed class ShiftSwapScheduleSnapshotRowDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Kind { get; set; } = "employee";
    public Dictionary<int, string> DayValues { get; set; } = [];
}
