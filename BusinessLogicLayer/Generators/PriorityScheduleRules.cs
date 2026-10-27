using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Schedule;

namespace BusinessLogicLayer.Generators;

internal static class PriorityScheduleRules
{
    internal readonly record struct Interval(int Start, int End)
    {
        public int Minutes => End - Start;
        public bool Contains(Interval other) => Start <= other.Start && End >= other.End;
        public bool Overlaps(Interval other) => Start < other.End && other.Start < End;
        public Interval Intersect(Interval other) => new(Math.Max(Start, other.Start), Math.Min(End, other.End));
    }

    internal sealed record DayConstraint(Interval Original, Interval External, bool ExplicitFullDay);
    internal sealed record Assignment(int Day, int Shift, int Lane, int EmployeeId, Interval Time, bool LockedPriority);

    internal static List<Interval> ParseShifts(ScheduleModel schedule)
    {
        var shifts = new List<Interval>();
        foreach (var text in new[] { schedule.Shift1Time, schedule.Shift2Time })
        {
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (!TryInterval(text, out var interval))
                throw ValidationException.ForField(nameof(schedule.Shift1Time), "Invalid shift interval.");
            shifts.Add(interval);
        }
        if (shifts.Count == 0 || schedule.PeoplePerShift < 1 || schedule.Year is < 1 or > 9999 || schedule.Month is < 1 or > 12)
            throw ValidationException.ForField(nameof(schedule.Shift1Time), "Invalid schedule configuration.");
        return shifts;
    }

    internal static bool TryInterval(string? text, out Interval interval)
    {
        interval = default;
        var parts = (text ?? "").Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !ScheduleMatrixEngine.TryParseTime(parts[0], out var from) ||
            !ScheduleMatrixEngine.TryParseTime(parts[1], out var to) || from.TotalMinutes != (int)from.TotalMinutes ||
            to.TotalMinutes != (int)to.TotalMinutes || from.TotalMinutes < 0 || to.TotalMinutes > 1440 || to <= from) return false;
        interval = new((int)from.TotalMinutes, (int)to.TotalMinutes);
        return true;
    }

    internal static Dictionary<(int Employee, int Day), DayConstraint> BuildDayConstraints(ScheduleModel schedule,
        IReadOnlyList<AvailabilityGroupModel> groups, IReadOnlyList<ScheduleEmployeeModel> employees, bool weekendsOff)
    {
        var result = new Dictionary<(int, int), DayConstraint>();
        var days = DateTime.DaysInMonth(schedule.Year, schedule.Month);
        var shifts = ParseShifts(schedule);
        foreach (var employee in employees)
        for (var day = 1; day <= days; day++)
        {
            var original = new Interval(0, 1440);
            var external = original;
            var explicitFull = false;
            foreach (var group in groups.Where(g => g.Year == schedule.Year && g.Month == schedule.Month))
            foreach (var entry in group.Members.Where(m => m.EmployeeId == employee.EmployeeId)
                         .SelectMany(m => m.Days).Where(d => d.DayOfMonth == day))
            {
                var window = new Interval(0, 1440);
                if (entry.Kind == AvailabilityKind.NONE) window = default;
                else if (entry.Kind == AvailabilityKind.INT)
                {
                    if (entry.IntervalStr is null || !AvailabilityCodeParser.TryNormalizeInterval(entry.IntervalStr, out var normalized) ||
                        !TryInterval(normalized, out window)) window = default;
                    else if (window.Start <= shifts.Min(s => s.Start) && window.End >= shifts.Max(s => s.End)) explicitFull = true;
                }
                if (group.Id == 0 && group.Name == "Saved schedule commitments") external = external.Intersect(window);
                else original = original.Intersect(window);
            }
            if (employee.IsPriority && weekendsOff && new DateOnly(schedule.Year, schedule.Month, day).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                original = default;
            result[(employee.EmployeeId, day)] = new(original, external, explicitFull);
        }
        return result;
    }

    internal static List<Interval> Envelopes(IReadOnlyList<Interval> shifts)
    {
        var result = new List<Interval>();
        foreach (var shift in shifts.OrderBy(s => s.Start))
        {
            if (result.Count == 0 || shift.Start > result[^1].End) result.Add(shift);
            else result[^1] = new(result[^1].Start, Math.Max(result[^1].End, shift.End));
        }
        return result;
    }

    internal static IEnumerable<Interval> BuildPriorityIntervals(DayConstraint constraint, int shiftHours, IReadOnlyList<Interval> shifts)
    {
        if (constraint.Original.Minutes <= 0 || constraint.External.Minutes <= 0) yield break;
        var duration = shiftHours * 60;
        var candidates = new HashSet<Interval>();
        foreach (var envelope in Envelopes(shifts))
        {
            var free = envelope.Intersect(constraint.External);
            var positive = constraint.Original.Intersect(envelope);
            if (free.Minutes < duration || positive.Minutes <= 0) continue;
            var first = shifts.OrderBy(s => s.Start).First();
            var last = shifts.OrderBy(s => s.End).Last();
            var morning = shifts.Count > 1 && positive.End <= first.End && positive.Start < last.Start;
            var evening = shifts.Count > 1 && positive.Start >= last.Start && positive.End > first.End;
            var starts = morning ? new[] { positive.Start } : evening ? new[] { positive.End - duration }
                : new[] { Math.Max(free.Start, positive.Start), Math.Min(free.End, positive.End) - duration,
                    free.Start, free.End - duration };
            foreach (var anchor in starts)
            {
                var start = Math.Clamp(anchor, free.Start, free.End - duration);
                var candidate = new Interval(start, start + duration);
                if (free.Contains(candidate) && candidate.Overlaps(positive)) candidates.Add(candidate);
            }
        }
        foreach (var candidate in candidates.OrderBy(c => c.Start)) yield return candidate;
    }

    internal static int ComputeWorkedMinutes(IEnumerable<Assignment> assignments, int employeeId) =>
        assignments.Where(a => a.EmployeeId == employeeId).Sum(a => a.Time.Minutes);

    internal static int LongestRun(IEnumerable<int> source)
    {
        var longest = 0;
        var run = 0;
        var previous = -2;
        foreach (var day in source.Distinct().Order())
        {
            run = day == previous + 1 ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = day;
        }
        return longest;
    }

    internal static bool CheckMonthlyAndConsecutiveLimits(ScheduleModel schedule, ScheduleEmployeeModel employee,
        Assignment candidate, IReadOnlyList<Assignment> assignments, IReadOnlyDictionary<(int, int), DayConstraint> constraints, int shiftCount)
    {
        var own = assignments.Where(a => a.EmployeeId == employee.EmployeeId).ToList();
        var minutes = own.Sum(a => a.Time.Minutes);
        if (schedule.MaxHoursPerEmpMonth > 0 && minutes + candidate.Time.Minutes > (long)schedule.MaxHoursPerEmpMonth * 60) return false;
        own.Add(candidate);
        if (schedule.MaxConsecutiveDays > 0 && LongestRun(own.Select(a => a.Day)) > schedule.MaxConsecutiveDays) return false;
        if (employee.IsPriority) return own.Count(a => a.Day == candidate.Day) == 1;
        if (own.Count(a => a.Day == candidate.Day) > shiftCount) return false;
        var fullDays = own.GroupBy(a => a.Day).Where(g => g.Count() >= 2).Select(g => g.Key).ToArray();
        var exceeds = schedule.MaxFullPerMonth > 0 && fullDays.Length > schedule.MaxFullPerMonth ||
            schedule.MaxConsecutiveFull > 0 && LongestRun(fullDays) > schedule.MaxConsecutiveFull;
        return !exceeds || constraints[(employee.EmployeeId, candidate.Day)].ExplicitFullDay &&
            minutes < (long)(employee.MinHoursMonth ?? 0) * 60;
    }

    internal static int[] GetCoverageBreakpoints(IEnumerable<Interval> shifts, IEnumerable<Assignment> assignments) =>
        shifts.SelectMany(s => new[] { s.Start, s.End }).Concat(assignments.SelectMany(a => new[] { a.Time.Start, a.Time.End }))
            .Distinct().Order().ToArray();

    internal static bool ValidateAssignment(ScheduleModel schedule, ScheduleEmployeeModel employee, Assignment candidate,
        IReadOnlyList<Assignment> assignments, IReadOnlyDictionary<(int, int), DayConstraint> constraints,
        IReadOnlyList<Interval> shifts, int hours)
    {
        if (candidate.EmployeeId != employee.EmployeeId || employee.EmployeeId <= 0 || candidate.Shift < 0 || candidate.Shift >= shifts.Count ||
            candidate.Lane < 1 || candidate.Lane > schedule.PeoplePerShift || candidate.Time.Minutes <= 0 ||
            !Envelopes(shifts).Any(e => e.Contains(candidate.Time)) || !candidate.Time.Overlaps(shifts[candidate.Shift])) return false;
        var constraint = constraints[(employee.EmployeeId, candidate.Day)];
        if (!constraint.External.Contains(candidate.Time)) return false;
        if (employee.IsPriority)
        {
            if (!BuildPriorityIntervals(constraint, hours, shifts).Contains(candidate.Time)) return false;
        }
        else if (!constraint.Original.Contains(candidate.Time)) return false;
        var today = assignments.Where(a => a.Day == candidate.Day).ToArray();
        if (today.Any(a => a.Shift == candidate.Shift && (a.Lane == candidate.Lane || a.EmployeeId == candidate.EmployeeId) ||
            a.EmployeeId == candidate.EmployeeId && a.Time.Overlaps(candidate.Time))) return false;
        var points = GetCoverageBreakpoints(shifts, today.Append(candidate));
        for (var i = 0; i + 1 < points.Length; i++)
            if (candidate.Time.Start <= points[i] && candidate.Time.End >= points[i + 1] &&
                today.Count(a => a.Time.Start <= points[i] && a.Time.End >= points[i + 1]) >= schedule.PeoplePerShift) return false;
        return CheckMonthlyAndConsecutiveLimits(schedule, employee, candidate, assignments, constraints, shifts.Count);
    }

    internal static void ValidateOutput(ScheduleModel schedule, IReadOnlyList<ScheduleEmployeeModel> employees,
        IReadOnlyList<Assignment> assignments, IReadOnlyDictionary<(int, int), DayConstraint> constraints, IReadOnlyList<Interval> shifts, int hours)
    {
        var accepted = new List<Assignment>();
        // Replay additions with minimum-directed full-day overrides in their original construction order.
        foreach (var assignment in assignments)
        {
            var employee = employees.Single(e => e.EmployeeId == assignment.EmployeeId);
            if (!ValidateAssignment(schedule, employee, assignment, accepted, constraints, shifts, hours))
                throw ValidationException.ForField("Slots", "Generated schedule violates scheduling constraints.");
            accepted.Add(assignment);
        }
    }

    internal static string Format(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";
}

