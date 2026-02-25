using BusinessLogicLayer.Contracts.Models;

namespace WPFApp.Applications.Matrix.Schedule
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
            var result = BusinessLogicLayer.Schedule.ScheduleTotalsCalculator.Calculate(employees, slots);
            return new TotalsResult
            {
                TotalEmployees = result.TotalEmployees,
                TotalDuration = result.TotalDuration,
                PerEmployeeDuration = result.PerEmployeeDuration
            };
        }

        public static string FormatHoursMinutes(TimeSpan t) => BusinessLogicLayer.Schedule.ScheduleTotalsCalculator.FormatHoursMinutes(t);
    }
}
