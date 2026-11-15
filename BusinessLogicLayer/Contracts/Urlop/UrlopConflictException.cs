namespace BusinessLogicLayer.Contracts.Urlop;

public sealed class UrlopConflictException(string code, string message,
    IReadOnlyList<DateOnly>? conflictingDates = null, IReadOnlyList<int>? scheduleIds = null) : Exception(message)
{
    public string Code { get; } = code;
    public IReadOnlyList<DateOnly> ConflictingDates { get; } = conflictingDates ?? [];
    public IReadOnlyList<int> ScheduleIds { get; } = scheduleIds ?? [];
}
