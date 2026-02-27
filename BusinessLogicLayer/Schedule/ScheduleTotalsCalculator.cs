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

        // ScheduleTotalsCalculator.cs

        public static TotalsResult Calculate(IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots)
        {
            static int GetEmpId(ScheduleEmployeeModel e)
                => (e.Employee?.Id is int navId && navId > 0) ? navId : e.EmployeeId;

            var empIds = new HashSet<int>(employees.Select(GetEmpId));
            var total = TimeSpan.Zero;
            var perEmp = new Dictionary<int, TimeSpan>(Math.Max(8, empIds.Count));

            foreach (var s in slots)
            {
                var slotEmpId = s.EmployeeId ?? (s.Employee?.Id);
                if (!slotEmpId.HasValue || !empIds.Contains(slotEmpId.Value))
                    continue;

                if (!ScheduleMatrixEngine.TryParseTime(s.FromTime, out var from) ||
                    !ScheduleMatrixEngine.TryParseTime(s.ToTime, out var to))
                    continue;

                var dur = to - from;
                if (dur < TimeSpan.Zero)
                    dur += TimeSpan.FromHours(24);

                total += dur;
                perEmp[slotEmpId.Value] = perEmp.TryGetValue(slotEmpId.Value, out var curr) ? curr + dur : dur;
            }

            return new TotalsResult { TotalEmployees = empIds.Count, TotalDuration = total, PerEmployeeDuration = perEmp };
        }

        public static string FormatHoursMinutes(TimeSpan t) => $"{(int)t.TotalHours}h {t.Minutes}m";
    }
}
