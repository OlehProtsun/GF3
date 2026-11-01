namespace WebApi.Contracts.ShiftSwaps;

public sealed class ShiftSwapOfferPreviewDto
{
    public bool RequiresManagerException { get; set; }
    public string? ManagerExceptionReason { get; set; }
    public int? MinimumMinutes { get; set; }
    public int? ProjectedMinutes { get; set; }
    public bool AllowPartialAcceptance { get; set; } = false;
    public string RequestKind { get; set; } = "give";
    public int? TargetEmployeeId { get; set; }
    public string TargetEmployeeName { get; set; } = "Everyone";
    public ShiftSwapReturnCandidateDto Source { get; set; } = new();
    public ShiftSwapReturnCandidateDto? Return { get; set; }
    public List<ShiftSwapPreviewMonthStatsDto> ProjectedMonths { get; set; } = [];
    public bool IsValid { get; set; }
    public string? UnavailableReason { get; set; }
}

public sealed record ShiftSwapPreviewMonthStatsDto(
    int Year, int Month, double HoursBefore, double HoursAfter,
    int WorkDaysBefore, int WorkDaysAfter, int FreeDaysBefore, int FreeDaysAfter);
