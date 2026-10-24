namespace WebApi.Contracts.ShiftCorrections;

public sealed class ShiftCorrectionRequestDto
{
    public int Id { get; init; }
    public int ContainerId { get; init; }
    public int ScheduleId { get; init; }
    public int ScheduleSlotId { get; init; }
    public string ScheduleName { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public int DayOfMonth { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string OriginalFromTime { get; init; } = string.Empty;
    public string OriginalToTime { get; init; } = string.Empty;
    public string RequestedFromTime { get; init; } = string.Empty;
    public string RequestedToTime { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? ReviewedAtUtc { get; init; }
    public string? ReviewedByManagerName { get; init; }
}
