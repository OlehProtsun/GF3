namespace BusinessLogicLayer.Schedule
{
    public static class ScheduleMatrixConstants
    {
        public const string DayColumnName = "DayOfMonth";
        public const string ConflictColumnName = "Conflict";
        public const string WeekendColumnName = "IsWeekend";
        public const string EmptyMark = "-";
        public static readonly string[] TimeFormats = { @"h\:mm", @"hh\:mm", @"h\:mm\:ss", @"hh\:mm\:ss" };
    }
}
