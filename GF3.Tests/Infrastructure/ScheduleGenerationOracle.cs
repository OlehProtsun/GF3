using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;

namespace GF3.Tests.Infrastructure;

internal sealed record GenerationViolation(string Code, string ScenarioId, int DayOfMonth, int? EmployeeOrdinal, string Description);
internal sealed record GenerationMetrics(int GeneratedCount, int AssignedCount, int UnfurnishedCount,
    int ConflictDays, int CoverageGapMinutes, int TotalAssignedMinutes, IReadOnlyDictionary<int, int> PerEmployeeMinutes,
    int TotalMinHourDeficitMinutes, long TotalMinHourDeficitSquared, double? DesiredHourDeficit,
    double? RestPenalty, double ElapsedMilliseconds, IReadOnlyDictionary<string, int> ViolationCountsByCode,
    int FullDayOverrideReviews);
internal sealed record GenerationCheckResult(IReadOnlyList<GenerationViolation> HardViolations, GenerationMetrics Metrics);

internal static class ScheduleGenerationOracle
{
    public static GenerationCheckResult Check(GenerationScenario scenario, IReadOnlyCollection<ScheduleSlotModel> slots,
        int? priorityShiftHours = null, bool priorityWeekendsOff = true)
    {
        var schedule = scenario.Schedule;
        var violations = new List<GenerationViolation>();
        var employees = scenario.Employees.Where(e => e.EmployeeId > 0).GroupBy(e => e.EmployeeId).Select(g => g.First()).ToArray();
        var ordinals = employees.Select((e, i) => (e.EmployeeId, Ordinal: i + 1)).ToDictionary(x => x.EmployeeId, x => x.Ordinal);
        void Fail(string code, int day = 0, int? employee = null)
            => violations.Add(new(code, scenario.ScenarioId, day,
                employee.HasValue && ordinals.TryGetValue(employee.Value, out var ordinal) ? ordinal : null, code));
        var validCalendar = schedule.Year is >= 1900 and <= 9999 && schedule.Month is >= 1 and <= 12;
        var days = validCalendar ? DateTime.DaysInMonth(schedule.Year, schedule.Month) : 0;
        var templates = new[] { schedule.Shift1Time, schedule.Shift2Time }
            .Select(text => ReadRange(text, out var from, out var to) ? (from, to) : (-1, -1))
            .Where(t => t.Item1 >= 0).ToArray();
        var expected = employees.Length == 0 ? 0 : days * templates.Length * Math.Max(0, schedule.PeoplePerShift);
        if (slots.Count != expected) Fail("PositionCount");
        var worked = new List<(int Day, int Employee, int From, int To)>();
        var positionKeys = new HashSet<(int Day, int Shift, int Slot)>();
        var shiftEmployees = new HashSet<(int Day, int Shift, int Employee)>();
        // Emission order is day, template, position. Times alone cannot identify a template:
        // the engine can extend a partial assignment across a template boundary.
        var dayOffsets = new Dictionary<int, int>();
        foreach (var slot in slots)
        {
            if (slot.DayOfMonth < 1 || slot.DayOfMonth > days) Fail("Date", slot.DayOfMonth, slot.EmployeeId);
            dayOffsets.TryGetValue(slot.DayOfMonth, out var offset);
            dayOffsets[slot.DayOfMonth] = offset + 1;
            var shift = schedule.PeoplePerShift > 0 ? offset / schedule.PeoplePerShift : -1;
            if (shift < 0 || shift >= templates.Length || slot.SlotNo < 1 || slot.SlotNo > schedule.PeoplePerShift)
                Fail("Position", slot.DayOfMonth, slot.EmployeeId);
            if (!positionKeys.Add((slot.DayOfMonth, shift, slot.SlotNo))) Fail("DuplicatePosition", slot.DayOfMonth);
            var legal = ReadTime(slot.FromTime, out var from) && ReadTime(slot.ToTime, out _) ;
            ReadTime(slot.ToTime, out var to);
            if (!legal || to <= from) { Fail("Time", slot.DayOfMonth, slot.EmployeeId); continue; }
            if (slot.Status == SlotStatus.UNFURNISHED)
            {
                if (slot.EmployeeId.HasValue) Fail("UnfurnishedOwner", slot.DayOfMonth, slot.EmployeeId);
                if (shift >= 0 && shift < templates.Length && (from != templates[shift].Item1 || to != templates[shift].Item2))
                    Fail("UnfurnishedTime", slot.DayOfMonth);
                continue;
            }
            if (slot.Status != SlotStatus.ASSIGNED) { Fail("Status", slot.DayOfMonth); continue; }
            if (templates.Length > 0 && (from < templates.Min(t => t.Item1) || to > templates.Max(t => t.Item2)))
                Fail("TemplateBounds", slot.DayOfMonth, slot.EmployeeId);
            if (!slot.EmployeeId.HasValue || !ordinals.ContainsKey(slot.EmployeeId.Value))
            { Fail("Enrollment", slot.DayOfMonth); continue; }
            var id = slot.EmployeeId.Value;
            if (!shiftEmployees.Add((slot.DayOfMonth, shift, id))) Fail("DuplicateEmployeeInShift", slot.DayOfMonth, id);
            var window = Window(scenario, id, slot.DayOfMonth);
            var priority = priorityShiftHours.HasValue && employees.Single(e => e.EmployeeId == id).IsPriority;
            if (priority)
            {
                if (to - from != priorityShiftHours!.Value * 60) Fail("PriorityDuration", slot.DayOfMonth, id);
                if (priorityWeekendsOff && new DateOnly(schedule.Year, schedule.Month, slot.DayOfMonth).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    Fail("PriorityWeekend", slot.DayOfMonth, id);
                if (window.To <= window.From || from >= window.To || to <= window.From) Fail("Availability", slot.DayOfMonth, id);
                foreach (var group in scenario.AvailabilityGroups.Where(g => g.Id == 0 && g.Name == "Saved schedule commitments" && g.Year == schedule.Year && g.Month == schedule.Month))
                foreach (var entry in group.Members.Where(m => m.EmployeeId == id).SelectMany(m => m.Days).Where(d => d.DayOfMonth == slot.DayOfMonth))
                    if (entry.Kind == AvailabilityKind.NONE || entry.Kind == AvailabilityKind.INT &&
                        (!ReadRange(entry.IntervalStr, out var freeFrom, out var freeTo) || from < freeFrom || to > freeTo)) Fail("ExternalOverlap", slot.DayOfMonth, id);
                for (var minute = from; minute < to; minute++)
                    if (!templates.Any(t => t.Item1 <= minute && t.Item2 > minute)) { Fail("TemplateGap", slot.DayOfMonth, id); break; }
                if (worked.Any(w => w.Day == slot.DayOfMonth && w.Employee == id)) Fail("PriorityDuplicateDay", slot.DayOfMonth, id);
            }
            else if (from < window.From || to > window.To || window.To <= window.From) Fail("Availability", slot.DayOfMonth, id);
            foreach (var external in scenario.ExternalSlots ?? [])
                if (external.EmployeeId == id && external.Status == SlotStatus.ASSIGNED && external.DayOfMonth == slot.DayOfMonth
                    && ReadTime(external.FromTime, out var externalFrom) && ReadTime(external.ToTime, out var externalTo)
                    && from < externalTo && externalFrom < to) Fail("ExternalOverlap", slot.DayOfMonth, id);
            worked.Add((slot.DayOfMonth, id, from, to));
        }
        foreach (var day in worked.GroupBy(w => (w.Employee, w.Day)))
        {
            var ordered = day.OrderBy(w => w.From).ToArray();
            var end = -1;
            foreach (var item in ordered)
            {
                if (item.From < end) Fail("Overlap", item.Day, item.Employee);
                end = Math.Max(end, item.To);
            }
        }
        var minutes = new Dictionary<int, int>();
        var deficits = new List<int>();
        var reviews = 0;
        foreach (var employee in employees)
        {
            var assignments = worked.Where(w => w.Employee == employee.EmployeeId).ToArray();
            var total = assignments.Sum(w => w.To - w.From);
            minutes[ordinals[employee.EmployeeId]] = total;
            deficits.Add(Math.Max(0, (employee.MinHoursMonth ?? 0) * 60 - total));
            if (schedule.MaxHoursPerEmpMonth > 0 && total - schedule.MaxHoursPerEmpMonth * 60 > 0.00006)
                Fail("MaxHours", employee: employee.EmployeeId);
            var workedDays = assignments.Select(w => w.Day).Distinct().Order().ToArray();
            if (schedule.MaxConsecutiveDays > 0 && LongestRun(workedDays) > schedule.MaxConsecutiveDays)
                Fail("WorkStreak", employee: employee.EmployeeId);
            var full = assignments.GroupBy(w => w.Day).Where(g => g.Count() >= 2).Select(g => g.Key).Order().ToArray();
            var strictFull = full.Where(d => (employee.MinHoursMonth ?? 0) <= 0 || !HasExplicitFullDay(scenario, employee.EmployeeId, d, templates)).ToArray();
            var fullLimit = schedule.MaxFullPerMonth > 0 && full.Length > schedule.MaxFullPerMonth;
            var streakLimit = schedule.MaxConsecutiveFull > 0 && LongestRun(full) > schedule.MaxConsecutiveFull;
            if (fullLimit)
            {
                if (strictFull.Length > schedule.MaxFullPerMonth) Fail("MaxFullDays", employee: employee.EmployeeId);
                else reviews++;
            }
            if (streakLimit)
            {
                if (LongestRun(strictFull) > schedule.MaxConsecutiveFull) Fail("FullStreak", employee: employee.EmployeeId);
                else reviews++;
            }
        }
        if (minutes.Values.Sum() != worked.Sum(w => w.To - w.From)) Fail("DurationSum");
        var gap = 0;
        var conflictDays = new HashSet<int>(violations.Where(v => v.Code is "Overlap" or "ExternalOverlap").Select(v => v.DayOfMonth));
        for (var day = 1; day <= days; day++)
        {
            var dayWork = worked.Where(w => w.Day == day).ToArray();
            foreach (var template in templates)
                for (var minute = template.Item1; minute < template.Item2; minute++)
                {
                    var count = dayWork.Where(w => w.From <= minute && w.To > minute).Select(w => w.Employee).Distinct().Count();
                    var missing = Math.Max(0, schedule.PeoplePerShift - count);
                    if (priorityShiftHours.HasValue && count > schedule.PeoplePerShift) Fail("Overstaffing", day);
                    gap += missing;
                    if (missing > 0) conflictDays.Add(day);
                }
        }
        return new(violations, new(slots.Count, slots.Count(s => s.Status == SlotStatus.ASSIGNED),
            slots.Count(s => s.Status == SlotStatus.UNFURNISHED), conflictDays.Count, gap,
            minutes.Values.Sum(), minutes, deficits.Sum(), deficits.Sum(d => (long)d * d),
            DesiredDeficit(scenario, employees, templates, days, minutes, ordinals), null, 0,
            violations.GroupBy(v => v.Code).ToDictionary(g => g.Key, g => g.Count()), reviews));
    }

    internal static (int From, int To) Window(GenerationScenario scenario, int employee, int day)
    {
        var from = 0;
        var to = 1440;
        foreach (var entry in scenario.AvailabilityGroups.Where(g => g.Year == scenario.Schedule.Year && g.Month == scenario.Schedule.Month)
                     .SelectMany(g => g.Members).Where(m => m.EmployeeId == employee).SelectMany(m => m.Days).Where(d => d.DayOfMonth == day))
        {
            if (entry.Kind == AvailabilityKind.NONE) return (1440, 0);
            if (ReadRange(entry.IntervalStr, out var start, out var end)) { from = Math.Max(from, start); to = Math.Min(to, end); }
            else if (entry.Kind == AvailabilityKind.INT) return (1440, 0);
        }
        return (from, to);
    }

    private static double DesiredDeficit(GenerationScenario scenario, ScheduleEmployeeModel[] employees,
        (int, int)[] templates, int days, IReadOnlyDictionary<int, int> minutes, Dictionary<int, int> ordinals)
    {
        if (employees.Length == 0) return 0;
        var capacities = employees.Select(employee =>
        {
            var potential = Enumerable.Range(1, days).Sum(day =>
            {
                var window = Window(scenario, employee.EmployeeId, day);
                return templates.Sum(t => Math.Max(0, Math.Min(t.Item2, window.To) - Math.Max(t.Item1, window.From)));
            });
            return scenario.Schedule.MaxHoursPerEmpMonth > 0 ? Math.Min(potential, scenario.Schedule.MaxHoursPerEmpMonth * 60d) : potential;
        }).ToArray();
        var minimum = employees.Select((employee, index) => Math.Min(capacities[index], Math.Max(0, employee.MinHoursMonth ?? 0) * 60d)).ToArray();
        var demand = templates.Sum(t => t.Item2 - t.Item1) * days * Math.Max(0, scenario.Schedule.PeoplePerShift);
        var target = Math.Min(Math.Max(demand, minimum.Sum()), capacities.Sum());
        var low = 0d;
        var high = capacities.Max();
        // Solve the capped equal-share water level without reusing the engine's allocator.
        for (var iteration = 0; iteration < 60; iteration++)
        {
            var level = (low + high) / 2;
            var assigned = capacities.Select((cap, index) => Math.Min(cap, minimum[index] + level)).Sum();
            if (assigned < target) low = level; else high = level;
        }
        return employees.Select((employee, index) => Math.Max(0,
            Math.Min(capacities[index], minimum[index] + low) - minutes.GetValueOrDefault(ordinals[employee.EmployeeId]))).Sum() / 60;
    }

    private static bool HasExplicitFullDay(GenerationScenario scenario, int employee, int day, (int, int)[] shifts)
        => shifts.Length == 2 && scenario.AvailabilityGroups.Where(g => g.Year == scenario.Schedule.Year && g.Month == scenario.Schedule.Month)
            .SelectMany(g => g.Members).Where(m => m.EmployeeId == employee).SelectMany(m => m.Days)
            .Any(d => d.DayOfMonth == day && d.Kind == AvailabilityKind.INT && ReadRange(d.IntervalStr, out var from, out var to)
                && from <= shifts[0].Item1 && to >= shifts[1].Item2);

    internal static int LongestRun(IEnumerable<int> days)
    {
        var best = 0; var current = 0; var previous = -2;
        foreach (var day in days.Distinct().Order()) { current = day == previous + 1 ? current + 1 : 1; best = Math.Max(best, current); previous = day; }
        return best;
    }

    internal static bool ReadTime(string? text, out int minutes)
    {
        minutes = 0;
        var parts = text?.Trim().Split(':');
        if (parts?.Length != 2 || !int.TryParse(parts[0], out var hour) || !int.TryParse(parts[1], out var minute)
            || hour < 0 || hour > 24 || minute < 0 || minute > 59 || hour == 24 && minute != 0) return false;
        minutes = hour * 60 + minute;
        return true;
    }

    internal static bool ReadRange(string? text, out int from, out int to)
    {
        from = to = 0;
        var parts = text?.Replace('–', '-').Replace('—', '-').Split('-', StringSplitOptions.TrimEntries);
        return parts?.Length == 2 && ReadTime(parts[0], out from) && ReadTime(parts[1], out to) && to > from;
    }
}
