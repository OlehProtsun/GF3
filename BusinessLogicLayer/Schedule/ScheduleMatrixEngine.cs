using System.Globalization;

namespace BusinessLogicLayer.Schedule
{
    public static class ScheduleMatrixEngine
    {
        public static bool TryParseTime(string? s, out TimeSpan t)
        {
            s = (s ?? string.Empty).Trim();
            return TimeSpan.TryParseExact(
                s,
                ScheduleMatrixConstants.TimeFormats,
                CultureInfo.InvariantCulture,
                out t);
        }
    }
}
