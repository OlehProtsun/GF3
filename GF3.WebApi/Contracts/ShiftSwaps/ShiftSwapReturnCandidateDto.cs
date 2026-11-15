namespace WebApi.Contracts.ShiftSwaps;

public sealed class ShiftSwapReturnCandidateDto
{
    public bool RequiresWholeShift { get; set; }
    public int ScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int DayOfMonth { get; set; }
    public int SlotId { get; set; }
    public string FromTime { get; set; } = string.Empty;
    public string ToTime { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool CanSelect { get; set; }
    public string? UnavailableReason { get; set; }
    public string? SuggestedFromTime { get; set; }
    public string? SuggestedToTime { get; set; }
    public List<ShiftSwapEligibleWindowDto> EligibleWindows { get; set; } = [];
}

public sealed record ShiftSwapEligibleWindowDto(string FromTime, string ToTime);

public sealed class ShiftSwapReturnCandidatePageDto
{
    public List<ShiftSwapReturnCandidateDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
