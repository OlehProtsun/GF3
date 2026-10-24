using System.Data;
using System.Globalization;
using System.Text;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Schedule;

/// <summary>
/// Translates raw schedule slots into a matrix-style view used by exports and reporting screens.
/// The engine deliberately stays stateless: every method derives its result only from the supplied
/// slots/employees, which keeps it deterministic, testable, and easy to reuse in multiple flows.
/// </summary>
public static class ScheduleMatrixEngine
{
    /// <summary>
    /// Parses a time fragment used by schedule slots, shift ranges, and export annotations.
    /// The method first tries the strict formats defined by the scheduling domain and then
    /// falls back to invariant parsing so legacy values stay backward-compatible.
    /// </summary>
    public static bool TryParseTime(string? value, out TimeSpan time)
    {
        value = (value ?? string.Empty).Trim();
        if (TimeSpan.TryParseExact(
            value,
            ScheduleMatrixConstants.TimeFormats,
            CultureInfo.InvariantCulture,
            out time))
        {
            return true;
        }

        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out time);
    }

    /// <summary>
    /// Builds the matrix representation consumed by Excel exports and analytical views.
    /// Each row represents one day of the selected month, while each employee column stores
    /// a merged textual representation of the shifts assigned to that employee on that day.
    /// </summary>
    public static DataTable BuildScheduleTable(
        int year,
        int month,
        IReadOnlyList<ScheduleSlotModel> slots,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        out Dictionary<string, int> colNameToEmpId,
        CancellationToken ct = default)
    {
        colNameToEmpId = new Dictionary<string, int>();
        var table = CreateMatrixTable();
        AddEmployeeColumns(table, employees, colNameToEmpId);

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var slotsByDay = GroupSlotsByDay(slots, daysInMonth);
        var employeeColumns = colNameToEmpId.ToArray();

        for (var day = 1; day <= daysInMonth; day++)
        {
            ct.ThrowIfCancellationRequested();

            var row = BuildDayRow(table, year, month, day, slotsByDay[day], employeeColumns);
            table.Rows.Add(row);
        }

        return table;
    }

    /// <summary>
    /// Detects whether the supplied day contains invalid staffing assignments.
    /// A day is considered conflicting when a slot has no employee assigned or when
    /// one employee receives overlapping intervals within the same day.
    /// </summary>
    public static bool ComputeConflictForDay(IReadOnlyList<ScheduleSlotModel> slots, int day)
    {
        var intervalsByEmployee = BuildIntervalsByEmployee(slots, day, treatMissingEmployeeAsConflict: true, out var hasImmediateConflict);
        if (hasImmediateConflict)
        {
            return true;
        }

        return intervalsByEmployee.Values.Any(HasOverlap);
    }

    /// <summary>
    /// Extends the basic overlap check with staffing validation against the configured shifts.
    /// This is used by exports to flag days where the generated graph does not fully cover
    /// required shift windows with enough distinct employees.
    /// </summary>
    public static bool ComputeConflictForDayWithStaffing(
        IReadOnlyList<ScheduleSlotModel> slots,
        int day,
        int peoplePerShift,
        string? shift1Range,
        string? shift2Range)
    {
        if (peoplePerShift <= 0)
        {
            peoplePerShift = 1;
        }

        var shifts = BuildShiftCoverageWindows(shift1Range, shift2Range);
        if (shifts.Count == 0)
        {
            return ComputeConflictForDay(slots, day);
        }

        var intervalsByEmployee = BuildIntervalsByEmployee(slots, day, treatMissingEmployeeAsConflict: false, out _);
        if (intervalsByEmployee.Values.Any(HasOverlap))
        {
            return true;
        }

        foreach (var shift in shifts)
        {
            var coveredEmployeeIds = new HashSet<int>();
            foreach (var (employeeId, intervals) in intervalsByEmployee)
            {
                if (intervals.Any(interval => interval.from <= shift.from && interval.to >= shift.to))
                {
                    coveredEmployeeIds.Add(employeeId);
                }
            }

            if (coveredEmployeeIds.Count < peoplePerShift)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Merges overlapping or touching time ranges for one employee/day into display intervals,
    /// preserving separate intervals only when there is a break.
    /// </summary>
    public static List<(string from, string to)> MergeIntervalsForDisplay(IEnumerable<ScheduleSlotModel> slots)
    {
        var uniqueIntervals = new HashSet<(int fromMin, int toMin)>();
        var parsedIntervals = new List<(int fromMin, int toMin)>();

        foreach (var slot in slots)
        {
            if (!TryParseTime(slot.FromTime, out var fromTime)) continue;
            if (!TryParseTime(slot.ToTime, out var toTime)) continue;

            var fromMinutes = (int)fromTime.TotalMinutes;
            var toMinutes = (int)toTime.TotalMinutes;
            if (toMinutes < fromMinutes)
            {
                toMinutes += 24 * 60;
            }

            if (uniqueIntervals.Add((fromMinutes, toMinutes)))
            {
                parsedIntervals.Add((fromMinutes, toMinutes));
            }
        }

        if (parsedIntervals.Count == 0)
        {
            return [];
        }

        parsedIntervals.Sort((left, right) =>
        {
            var byStart = left.fromMin.CompareTo(right.fromMin);
            return byStart != 0 ? byStart : left.toMin.CompareTo(right.toMin);
        });

        var merged = new List<(int fromMin, int toMin)>(parsedIntervals.Count);
        var current = parsedIntervals[0];

        for (var i = 1; i < parsedIntervals.Count; i++)
        {
            var next = parsedIntervals[i];
            if (next.fromMin <= current.toMin)
            {
                current.toMin = Math.Max(current.toMin, next.toMin);
                continue;
            }

            merged.Add(current);
            current = next;
        }

        merged.Add(current);

        return merged
            .Select(interval =>
            {
                var from = TimeSpan.FromMinutes(interval.fromMin % (24 * 60));
                var to = TimeSpan.FromMinutes(interval.toMin % (24 * 60));
                return (from: from.ToString(@"hh\:mm"), to: to.ToString(@"hh\:mm"));
            })
            .ToList();
    }

    private static DataTable CreateMatrixTable()
    {
        var table = new DataTable();
        table.Columns.Add(ScheduleMatrixConstants.DayColumnName, typeof(int));
        table.Columns.Add(ScheduleMatrixConstants.ConflictColumnName, typeof(bool));
        table.Columns.Add(ScheduleMatrixConstants.WeekendColumnName, typeof(bool));
        return table;
    }

    private static void AddEmployeeColumns(
        DataTable table,
        IReadOnlyList<ScheduleEmployeeModel> employees,
        IDictionary<string, int> colNameToEmpId)
    {
        var orderedEmployees = (employees ?? Array.Empty<ScheduleEmployeeModel>())
            .Where(employee => employee is not null)
            .Select(employee => new
            {
                Employee = employee,
                EffectiveEmployeeId = GetEffectiveEmployeeId(employee)
            })
            .OrderBy(item => (item.Employee.Employee?.LastName ?? string.Empty).Trim(), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => (item.Employee.Employee?.FirstName ?? string.Empty).Trim(), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.EffectiveEmployeeId)
            .ToList();

        var seenEmployeeIds = new HashSet<int>();
        foreach (var item in orderedEmployees)
        {
            if (!seenEmployeeIds.Add(item.EffectiveEmployeeId))
            {
                continue;
            }

            var columnName = BuildUniqueEmployeeColumnName(table, item.EffectiveEmployeeId);
            var column = table.Columns.Add(columnName, typeof(string));
            column.Caption = BuildEmployeeDisplayName(item.Employee, item.EffectiveEmployeeId);
            colNameToEmpId[columnName] = item.EffectiveEmployeeId;
        }
    }

    private static string BuildUniqueEmployeeColumnName(DataTable table, int employeeId)
    {
        var columnName = $"emp_{employeeId}";
        var suffix = 1;
        while (table.Columns.Contains(columnName))
        {
            columnName = $"emp_{employeeId}_{++suffix}";
        }

        return columnName;
    }

    private static string BuildEmployeeDisplayName(ScheduleEmployeeModel employee, int employeeId)
    {
        var displayName = $"{employee.Employee?.FirstName} {employee.Employee?.LastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName)
            ? $"Employee {employeeId}"
            : displayName;
    }

    private static int GetEffectiveEmployeeId(ScheduleEmployeeModel employee)
        => (employee.Employee?.Id is int navigationId && navigationId > 0)
            ? navigationId
            : employee.EmployeeId;

    private static List<ScheduleSlotModel>?[] GroupSlotsByDay(IReadOnlyList<ScheduleSlotModel> slots, int daysInMonth)
    {
        var slotsByDay = new List<ScheduleSlotModel>?[daysInMonth + 1];
        if (slots is null || slots.Count == 0)
        {
            return slotsByDay;
        }

        foreach (var slot in slots)
        {
            var day = slot.DayOfMonth;
            if ((uint)day > (uint)daysInMonth || day <= 0)
            {
                continue;
            }

            slotsByDay[day] ??= new List<ScheduleSlotModel>(8);
            slotsByDay[day]!.Add(slot);
        }

        return slotsByDay;
    }

    private static DataRow BuildDayRow(
        DataTable table,
        int year,
        int month,
        int day,
        IReadOnlyList<ScheduleSlotModel>? daySlots,
        IReadOnlyList<KeyValuePair<string, int>> employeeColumns)
    {
        var row = table.NewRow();
        row[ScheduleMatrixConstants.DayColumnName] = day;

        var dayOfWeek = new DateTime(year, month, day).DayOfWeek;
        row[ScheduleMatrixConstants.WeekendColumnName] = dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        if (daySlots is null || daySlots.Count == 0)
        {
            row[ScheduleMatrixConstants.ConflictColumnName] = false;
            foreach (var employeeColumn in employeeColumns)
            {
                row[employeeColumn.Key] = ScheduleMatrixConstants.EmptyMark;
            }

            return row;
        }

        var slotsByEmployee = BuildAssignedSlotsByEmployee(daySlots, out var hasImmediateConflict);
        var hasOverlapConflict = slotsByEmployee.Values.Any(HasOverlapInSlots);
        row[ScheduleMatrixConstants.ConflictColumnName] = hasImmediateConflict || hasOverlapConflict;

        foreach (var employeeColumn in employeeColumns)
        {
            if (!slotsByEmployee.TryGetValue(employeeColumn.Value, out var employeeSlots) || employeeSlots.Count == 0)
            {
                row[employeeColumn.Key] = ScheduleMatrixConstants.EmptyMark;
                continue;
            }

            row[employeeColumn.Key] = FormatMergedIntervals(MergeIntervalsForDisplay(employeeSlots));
        }

        return row;
    }

    private static Dictionary<int, List<ScheduleSlotModel>> BuildAssignedSlotsByEmployee(
        IReadOnlyList<ScheduleSlotModel> daySlots,
        out bool hasImmediateConflict)
    {
        hasImmediateConflict = false;
        var byEmployee = new Dictionary<int, List<ScheduleSlotModel>>();

        foreach (var slot in daySlots)
        {
            if (!slot.EmployeeId.HasValue || slot.EmployeeId.Value <= 0)
            {
                hasImmediateConflict = true;
                continue;
            }

            var employeeId = slot.EmployeeId.Value;
            if (!byEmployee.TryGetValue(employeeId, out var employeeSlots))
            {
                employeeSlots = new List<ScheduleSlotModel>(4);
                byEmployee[employeeId] = employeeSlots;
            }

            employeeSlots.Add(slot);
        }

        return byEmployee;
    }

    private static string FormatMergedIntervals(IReadOnlyList<(string from, string to)> intervals)
    {
        if (intervals.Count == 0)
        {
            return ScheduleMatrixConstants.EmptyMark;
        }

        var builder = new StringBuilder(intervals.Count * 14);
        for (var i = 0; i < intervals.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(intervals[i].from).Append(" - ").Append(intervals[i].to);
        }

        return builder.ToString();
    }

    private static Dictionary<int, List<(TimeSpan from, TimeSpan to)>> BuildIntervalsByEmployee(
        IReadOnlyList<ScheduleSlotModel> slots,
        int day,
        bool treatMissingEmployeeAsConflict,
        out bool hasImmediateConflict)
    {
        hasImmediateConflict = false;
        var byEmployee = new Dictionary<int, List<(TimeSpan from, TimeSpan to)>>();

        foreach (var slot in slots)
        {
            if (slot.DayOfMonth != day)
            {
                continue;
            }

            if (!slot.EmployeeId.HasValue || slot.EmployeeId.Value <= 0)
            {
                if (treatMissingEmployeeAsConflict)
                {
                    hasImmediateConflict = true;
                }

                continue;
            }

            if (!TryParseTime(slot.FromTime, out var fromTime)) continue;
            if (!TryParseTime(slot.ToTime, out var toTime)) continue;

            if (toTime < fromTime)
            {
                toTime += TimeSpan.FromHours(24);
            }

            var employeeId = slot.EmployeeId.Value;
            if (!byEmployee.TryGetValue(employeeId, out var intervals))
            {
                intervals = new List<(TimeSpan from, TimeSpan to)>();
                byEmployee[employeeId] = intervals;
            }

            intervals.Add((fromTime, toTime));
        }

        return byEmployee;
    }

    private static bool HasOverlapInSlots(IReadOnlyList<ScheduleSlotModel> slots)
    {
        var intervals = new List<(TimeSpan from, TimeSpan to)>(slots.Count);
        foreach (var slot in slots)
        {
            if (!TryParseTime(slot.FromTime, out var fromTime)) continue;
            if (!TryParseTime(slot.ToTime, out var toTime)) continue;

            if (toTime < fromTime)
            {
                toTime += TimeSpan.FromHours(24);
            }

            intervals.Add((fromTime, toTime));
        }

        return HasOverlap(intervals);
    }

    private static bool HasOverlap(IReadOnlyList<(TimeSpan from, TimeSpan to)> intervals)
    {
        if (intervals.Count <= 1)
        {
            return false;
        }

        var ordered = intervals
            .OrderBy(interval => interval.from)
            .ThenBy(interval => interval.to)
            .ToList();

        var lastEnd = ordered[0].to;
        for (var i = 1; i < ordered.Count; i++)
        {
            var current = ordered[i];
            if (current.from < lastEnd)
            {
                return true;
            }

            if (current.to > lastEnd)
            {
                lastEnd = current.to;
            }
        }

        return false;
    }

    private static List<(TimeSpan from, TimeSpan to)> BuildShiftCoverageWindows(string? shift1Range, string? shift2Range)
    {
        var shifts = new List<(TimeSpan from, TimeSpan to)>(2);
        if (TryParseShiftRange(shift1Range, out var shift1))
        {
            shifts.Add(shift1);
        }

        if (TryParseShiftRange(shift2Range, out var shift2))
        {
            shifts.Add(shift2);
        }

        return shifts;
    }

    private static bool TryParseShiftRange(string? value, out (TimeSpan from, TimeSpan to) shift)
    {
        shift = default;
        value = (value ?? string.Empty).Trim()
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-');

        if (value.Length == 0)
        {
            return false;
        }

        var parts = value.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!TryParseTime(parts[0], out var fromTime)) return false;
        if (!TryParseTime(parts[1], out var toTime)) return false;

        if (toTime < fromTime)
        {
            toTime += TimeSpan.FromHours(24);
        }

        if (toTime == fromTime)
        {
            return false;
        }

        shift = (fromTime, toTime);
        return true;
    }
}
