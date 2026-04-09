namespace BusinessLogicLayer.Schedule;

/// <summary>
/// Shared constants used by the schedule matrix/export utilities.
/// Keeping them in one place ensures that matrix builders, summaries, and exports stay consistent.
/// </summary>
public static class ScheduleMatrixConstants
{
    public const string DayColumnName = "DayOfMonth";
    public const string ConflictColumnName = "Conflict";
    public const string WeekendColumnName = "IsWeekend";
    public const string EmptyMark = "-";

    /// <summary>
    /// Accepted time formats for slot and shift parsing in matrix/export utilities.
    /// </summary>
    public static readonly string[] TimeFormats =
    [
        @"h\:mm",
        @"hh\:mm",
        @"h\:mm\:ss",
        @"hh\:mm\:ss",
    ];
}
