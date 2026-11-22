namespace WebApi.Contracts.ShiftCorrections;

public sealed class ShiftCorrectionRequestDto
{
    public int Id { get; init; }
    public int ContainerId { get; init; }
    public int ScheduleId { get; init; }
    public int? ScheduleSlotId { get; init; }
    public bool IsDayOffRequest { get; init; }
    public string RequestType { get; init; } = string.Empty;
    public IReadOnlyList<ShiftCorrectionOriginalShiftDto> OriginalShifts { get; init; } = [];
    public string ScheduleName { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public int DayOfMonth { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string? OriginalFromTime { get; init; }
    public string? OriginalToTime { get; init; }
    public string? RequestedFromTime { get; init; }
    public string? RequestedToTime { get; init; }
    public string? Comment { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? ReviewedAtUtc { get; init; }
    public string? ReviewedByManagerName { get; init; }
}

public sealed record ShiftCorrectionOriginalShiftDto(int SlotId, string FromTime, string ToTime);
