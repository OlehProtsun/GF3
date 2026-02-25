using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Schedule
{
    public static class ScheduleTotalsCalculator
    {
        public sealed class TotalsResult
        {
            public int TotalEmployees { get; init; }
            public TimeSpan TotalDuration { get; init; }
            public IReadOnlyDictionary<int, TimeSpan> PerEmployeeDuration { get; init; } = new Dictionary<int, TimeSpan>();
        }

        public static TotalsResult Calculate(IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots)
        {
            var empIds = new HashSet<int>(employees.Select(x => x.EmployeeId));
            var total = TimeSpan.Zero;
            var perEmp = new Dictionary<int, TimeSpan>(Math.Max(8, empIds.Count));

            foreach (var s in slots)
            {
                if (!s.EmployeeId.HasValue || !empIds.Contains(s.EmployeeId.Value))
                    continue;

                if (!ScheduleMatrixEngine.TryParseTime(s.FromTime, out var from) || !ScheduleMatrixEngine.TryParseTime(s.ToTime, out var to))
                    continue;

                var dur = to - from;
                if (dur < TimeSpan.Zero)
                    dur += TimeSpan.FromHours(24);

                total += dur;
                perEmp[s.EmployeeId.Value] = perEmp.TryGetValue(s.EmployeeId.Value, out var curr) ? curr + dur : dur;
            }

            return new TotalsResult { TotalEmployees = empIds.Count, TotalDuration = total, PerEmployeeDuration = perEmp };
        }

        public static string FormatHoursMinutes(TimeSpan t) => $"{(int)t.TotalHours}h {t.Minutes}m";
    }
}
