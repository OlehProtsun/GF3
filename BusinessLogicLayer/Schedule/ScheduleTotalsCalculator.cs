using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Schedule;

/// <summary>
/// Calculates aggregate duration statistics for schedules and exports.
/// </summary>
public static class ScheduleTotalsCalculator
{
    /// <summary>
    /// Immutable summary returned by <see cref="Calculate"/>.
    /// </summary>
    public sealed class TotalsResult
    {
        public int TotalEmployees { get; init; }
        public TimeSpan TotalDuration { get; init; }
        public IReadOnlyDictionary<int, TimeSpan> PerEmployeeDuration { get; init; } = new Dictionary<int, TimeSpan>();
    }

    /// <summary>
    /// Calculates total and per-employee duration across all assigned slots that belong to the
    /// supplied employee set.
    /// </summary>
    public static TotalsResult Calculate(IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots)
    {
        var employeeIds = new HashSet<int>(employees.Select(GetEffectiveEmployeeId));
        var totalDuration = TimeSpan.Zero;
        var durationByEmployee = new Dictionary<int, TimeSpan>(Math.Max(8, employeeIds.Count));

        var assignedSlotsCount = 0;
        var matchedSlotsCount = 0;
        var parseOkCount = 0;

        foreach (var slot in slots)
        {
            var slotEmployeeId = slot.EmployeeId ?? slot.Employee?.Id;
            if (slotEmployeeId.HasValue)
            {
                assignedSlotsCount++;
            }

            if (!slotEmployeeId.HasValue || !employeeIds.Contains(slotEmployeeId.Value))
            {
                continue;
            }

            matchedSlotsCount++;

            if (!TryParseSlotDuration(slot, out var duration))
            {
                continue;
            }

            parseOkCount++;
            totalDuration += duration;
            durationByEmployee[slotEmployeeId.Value] = durationByEmployee.TryGetValue(slotEmployeeId.Value, out var current)
                ? current + duration
                : duration;
        }

        LogDebugDiagnostics(employeeIds.Count, assignedSlotsCount, matchedSlotsCount, parseOkCount);

        return new TotalsResult
        {
            TotalEmployees = employeeIds.Count,
            TotalDuration = totalDuration,
            PerEmployeeDuration = durationByEmployee,
        };
    }

    /// <summary>
    /// Formats duration in the compact `Xh Ym` shape used by exports and admin summaries.
    /// </summary>
    public static string FormatHoursMinutes(TimeSpan duration)
        => $"{(int)duration.TotalHours}h {duration.Minutes}m";

    private static int GetEffectiveEmployeeId(ScheduleEmployeeModel employee)
        => employee.Employee?.Id is int navigationId && navigationId > 0
            ? navigationId
            : employee.EmployeeId;

    private static bool TryParseSlotDuration(ScheduleSlotModel slot, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;

        if (!ScheduleMatrixEngine.TryParseTime(slot.FromTime, out var fromTime)
            || !ScheduleMatrixEngine.TryParseTime(slot.ToTime, out var toTime))
        {
            return false;
        }

        duration = toTime - fromTime;
        if (duration < TimeSpan.Zero)
        {
            duration += TimeSpan.FromHours(24);
        }

        return true;
    }

    private static void LogDebugDiagnostics(int employeeCount, int assignedSlotsCount, int matchedSlotsCount, int parseOkCount)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("GF3_EXPORT_DEBUG"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Console.WriteLine(
            $"GF3 export debug totals: empIds.Count={employeeCount}, assignedSlotsCount={assignedSlotsCount}, matchedSlotsCount={matchedSlotsCount}, parseOkCount={parseOkCount}");
    }
}
