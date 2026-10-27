using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;

internal sealed record ReplayMetrics(int GeneratedCount, int AssignedCount, int UnfurnishedCount, int ConflictDays,
    int CoverageGapMinutes, int TotalAssignedMinutes, int TotalMinHourDeficitMinutes, long TotalMinHourDeficitSquared,
    IReadOnlyDictionary<string, int> Violations, int PriorityDeficitMinutes, int RegularDeficitMinutes);

internal sealed record ReplayQualityMetrics(int DemandMinutes, int EnrolledMinimumMinutes, bool CapacityBoundsApplicable,
    int OptimisticCapacityMinutes, int CoverageGapLowerBoundMinutes, int MinimumDeficitLowerBoundMinutes,
    int CoverageGapAboveLowerBoundMinutes, int MinimumDeficitAboveLowerBoundMinutes,
    bool SelectedAvailabilityConfigured, int EmployeesWithoutSelectedAvailability, int MissingAvailabilityDays,
    int RelevantExternalAssignedSlots, int RelevantExternalEmployeeDays,
    int MinimumAssignedMinutes, int MaximumAssignedMinutes, double AssignedMinutesStandardDeviation,
    double? LowestMinimumAttainment, double? HighestMinimumAttainment, int FullDayOverrideReviews,
    int PriorityOptimisticCapacityMinutes, int PriorityUnavoidableDeficitMinutes,
    double? LowestCapacityAttainment, double? HighestCapacityAttainment);

internal sealed record ReplayLocalQualityResult(bool ImprovementFound, int StatesChecked, bool SearchLimited,
    int ChangedSlots, int MinimumDeficitMinutes, long MinimumDeficitSquared,
    string Objective = "minimum-deficit/squared-deficit", double[]? BaselineObjective = null, double[]? WitnessObjective = null);

internal sealed class ReplayCase
{
    public required string CaseId { get; init; }
    public string Engine { get; init; } = "legacy";
    public string Variant { get; init; } = "stored";
    public int PriorityCount { get; init; }
    public int? PriorityShiftHours { get; init; }
    public bool? PriorityWeekendsOff { get; init; }
    public string? OrderedOutputSha256 { get; init; }
    public string? SkipReason { get; init; }
    public int EmployeeCount { get; init; }
    public int AvailabilityEntryCount { get; init; }
    public object? Settings { get; init; }
    public ReplayMetrics? Generated { get; init; }
    public ReplayMetrics? SavedComparison { get; init; }
    public ReplayQualityMetrics? Quality { get; init; }
    public ReplayLocalQualityResult? LocalQuality { get; init; }
    public List<double> DurationsMilliseconds { get; } = [];
    public List<double> PreviewMilliseconds { get; } = [];
    public double MedianMilliseconds => DurationsMilliseconds.Order().Skip((DurationsMilliseconds.Count - 1) / 2).Take(DurationsMilliseconds.Count % 2 == 0 ? 2 : 1).DefaultIfEmpty().Average();
    public double MaxMilliseconds => DurationsMilliseconds.DefaultIfEmpty().Max();
    public bool RepeatStable { get; set; } = true;
    public IReadOnlyDictionary<string, int> HardViolations { get; set; } = new Dictionary<string, int>();
    public bool Failed => SkipReason is not null || !RepeatStable || HardViolations.Count > 0;
    public string Classification => SkipReason == "generation-time-limit" ? "timeout" : SkipReason == "generation-runtime-error" ? "runtime error"
        : SkipReason is not null ? "input/schema error" : HardViolations.Count > 0 ? "hard invariant"
        : !RepeatStable ? "nondeterminism" : Generated?.CoverageGapMinutes > 0 || Generated?.TotalMinHourDeficitMinutes > 0 ? "PARTIAL" : "complete";

    internal static ReplayCase Create(string id, ReplayInput input, ReplayMetrics generated, IReadOnlyCollection<ScheduleSlotModel> slots, string engine = "legacy", string variant = "stored") => new()
    {
        Engine = engine, Variant = variant, PriorityCount = input.Employees.Count(e => e.IsPriority),
        PriorityShiftHours = input.PriorityShiftHours, PriorityWeekendsOff = input.PriorityShiftHours.HasValue ? input.PriorityWeekendsOff : null,
        OrderedOutputSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            Encoding.UTF8.GetBytes(string.Join('\n', ReplayChecks.Normalize(slots))))),
        CaseId = id, EmployeeCount = input.Employees.Count,
        AvailabilityEntryCount = input.Groups.SelectMany(g => g.Members).Sum(m => m.Days.Count),
        Settings = new
        {
            input.Schedule.Year, input.Schedule.Month, input.Schedule.PeoplePerShift,
            input.Schedule.Shift1Time, input.Schedule.Shift2Time, input.Schedule.MaxHoursPerEmpMonth,
            input.Schedule.MaxConsecutiveDays, input.Schedule.MaxConsecutiveFull, input.Schedule.MaxFullPerMonth
        },
        Generated = generated, SavedComparison = ReplayChecks.Check(input, input.BaselineSlots, checkPositions: false),
        Quality = ReplayChecks.Quality(input, slots, generated),
        LocalQuality = generated.Violations.Count == 0 ? ReplayChecks.FindLocalMinimumImprovement(input, slots, generated) : null
    };

    internal static ReplayCase Skipped(string id, string reason, string engine = "legacy", string variant = "stored") => new() { CaseId = id, SkipReason = reason, Engine = engine, Variant = variant };
}

internal static class ReplayReportWriter
{
    internal static async Task WriteAsync(string output, IReadOnlyList<ReplayCase> cases, double elapsed, CancellationToken ct,
        string engine = "legacy", string variant = "stored", string? sourceFingerprint = null, string? quickCheck = null)
    {
        foreach (var name in new[] { "summary.json", "failures.json", "cases.csv" })
        {
            var file = new FileInfo(Path.Combine(output, name));
            if (file.LinkTarget is not null || file.Exists && (file.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ReplayInputException("linked-report-file-refused");
        }
        var options = new JsonSerializerOptions { WriteIndented = true };
        var checkout = await GitAsync("rev-parse", "HEAD");
        var workingTree = await GitAsync("status", "--porcelain");
        var summary = new
        {
            BaselineSourceCommit = SqliteReplaySource.PinnedCommit, ExecutedCheckout = checkout,
            ExecutedGeneratorAssemblySha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                await File.ReadAllBytesAsync(typeof(BusinessLogicLayer.Generators.PriorityScheduleGenerator).Assembly.Location, ct))),
            WorkingTreeModified = workingTree is null ? (bool?)null : workingTree.Length > 0,
            Mode = "offline-read-only-preview", Engine = engine, Variant = variant, SourceFingerprint = sourceFingerprint,
            QuickCheckBefore = quickCheck, QuickCheckAfter = quickCheck, FingerprintUnchanged = sourceFingerprint is not null, RunUtc = DateTimeOffset.UtcNow,
            SchemaCompatibility = "compatible", SourceReadOnly = true, EndToEndMilliseconds = elapsed,
            PeakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64, CaseCount = cases.Count,
            SkippedCount = cases.Count(c => c.SkipReason is not null), FailureCount = cases.Count(c => c.Failed), Cases = cases
        };
        // Fixed file names and structured values prevent arbitrary input text entering reports.
        await File.WriteAllTextAsync(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, options), ct);
        await File.WriteAllTextAsync(Path.Combine(output, "failures.json"), JsonSerializer.Serialize(cases.Where(c => c.Failed).Select(c => new
        {
            c.CaseId, c.Engine, c.Variant, c.Classification, c.SkipReason, c.RepeatStable, c.HardViolations
        }), options), ct);
        var csv = new StringBuilder("case,engine,variant,classification,employees,priorities,shiftHours,weekendsOff,availabilityEntries,generated,assigned,unfurnished,conflictDays,coverageGapMinutes,minDeficitMinutes,priorityDeficitMinutes,regularDeficitMinutes,minDeficitSquared,medianMilliseconds,maxMilliseconds,stable,hardViolations,orderedOutputSha256\n");
        foreach (var c in cases)
            csv.AppendLine(string.Join(",", new object?[]
            {
                c.CaseId, c.Engine, c.Variant, c.Classification, c.EmployeeCount, c.PriorityCount, c.PriorityShiftHours, c.PriorityWeekendsOff, c.AvailabilityEntryCount, c.Generated?.GeneratedCount,
                c.Generated?.AssignedCount, c.Generated?.UnfurnishedCount, c.Generated?.ConflictDays, c.Generated?.CoverageGapMinutes,
                c.Generated?.TotalMinHourDeficitMinutes, c.Generated?.PriorityDeficitMinutes, c.Generated?.RegularDeficitMinutes, c.Generated?.TotalMinHourDeficitSquared,
                c.MedianMilliseconds.ToString("F3", CultureInfo.InvariantCulture), c.MaxMilliseconds.ToString("F3", CultureInfo.InvariantCulture),
                c.RepeatStable, c.HardViolations.Values.Sum(), c.OrderedOutputSha256
            }));
        await File.WriteAllTextAsync(Path.Combine(output, "cases.csv"), csv.ToString(), ct);
    }

    private static async Task<string?> GitAsync(params string[] arguments)
    {
        try
        {
            var start = new ProcessStartInfo("git") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            var text = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(entireProcessTree: true); return null; }
            await error;
            return process.ExitCode == 0 ? (await text).Trim() : null;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException) { return null; }
    }
}

internal static class ReplayChecks
{
    internal static ReplayMetrics Check(ReplayInput input, IReadOnlyCollection<ScheduleSlotModel> slots, bool checkPositions = true)
    {
        var schedule = input.Schedule;
        var days = DateTime.DaysInMonth(schedule.Year, schedule.Month);
        var shifts = new[] { schedule.Shift1Time, schedule.Shift2Time }.Select(s => Range(s, out var from, out var to) ? (from, to) : (-1, -1)).Where(s => s.Item1 >= 0).ToArray();
        var employees = input.Employees.Where(e => e.EmployeeId > 0).GroupBy(e => e.EmployeeId).Select(g => g.First()).ToArray();
        var ids = employees.Select(e => e.EmployeeId).ToHashSet();
        var priorityIds = input.PriorityShiftHours.HasValue ? employees.Where(e => e.IsPriority).Select(e => e.EmployeeId).ToHashSet() : [];
        var violations = new Dictionary<string, int>();
        void Fail(string code) => violations[code] = violations.GetValueOrDefault(code) + 1;
        if (checkPositions && slots.Count != (employees.Length == 0 ? 0 : days * schedule.PeoplePerShift * shifts.Length)) Fail("PositionCount");
        var worked = new List<(int Day, int Employee, int From, int To)>();
        var offsets = new Dictionary<int, int>();
        var positions = new HashSet<(int, int, int)>();
        var shiftEmployees = new HashSet<(int, int, int)>();
        var overlapDays = new HashSet<int>();
        foreach (var slot in slots)
        {
            if (slot.DayOfMonth < 1 || slot.DayOfMonth > days) Fail("Date");
            var offset = offsets.GetValueOrDefault(slot.DayOfMonth);
            offsets[slot.DayOfMonth] = offset + 1;
            var shift = offset / schedule.PeoplePerShift;
            if (checkPositions)
            {
                if (shift >= shifts.Length || slot.SlotNo < 1 || slot.SlotNo > schedule.PeoplePerShift) Fail("Position");
                if (!positions.Add((slot.DayOfMonth, shift, slot.SlotNo))) Fail("DuplicatePosition");
            }
            if (!Time(slot.FromTime, out var from) || !Time(slot.ToTime, out var to) || to <= from) { Fail("Time"); continue; }
            if (slot.Status == SlotStatus.UNFURNISHED)
            {
                if (slot.EmployeeId.HasValue) Fail("UnfurnishedOwner");
                if (checkPositions && shift < shifts.Length && (from != shifts[shift].Item1 || to != shifts[shift].Item2)) Fail("UnfurnishedTime");
                continue;
            }
            if (slot.Status != SlotStatus.ASSIGNED) { Fail("Status"); continue; }
            if (shifts.Length > 0 && (from < shifts.Min(s => s.Item1) || to > shifts.Max(s => s.Item2))) Fail("TemplateBounds");
            if (!slot.EmployeeId.HasValue || !ids.Contains(slot.EmployeeId.Value)) { Fail("Enrollment"); continue; }
            var employee = slot.EmployeeId.Value;
            if (checkPositions && !shiftEmployees.Add((slot.DayOfMonth, shift, employee))) Fail("DuplicateEmployeeInShift");
            var window = Window(input, employee, slot.DayOfMonth);
            if (priorityIds.Contains(employee))
            {
                if (to - from != input.PriorityShiftHours * 60) Fail("PriorityDuration");
                if (input.PriorityWeekendsOff && slot.DayOfMonth >= 1 && slot.DayOfMonth <= days &&
                    new DateOnly(schedule.Year, schedule.Month, slot.DayOfMonth).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) Fail("PriorityWeekend");
                if (window.To <= window.From || from >= window.To || to <= window.From) Fail("Availability");
                if (worked.Any(w => w.Employee == employee && w.Day == slot.DayOfMonth)) Fail("PriorityDuplicateDay");
                for (var minute = from; minute < to; minute++)
                    if (!shifts.Any(s => s.Item1 <= minute && minute < s.Item2)) { Fail("TemplateGap"); break; }
            }
            else if (from < window.From || to > window.To || window.To <= window.From) Fail("Availability");
            foreach (var external in input.ExternalSlots)
                if (external.EmployeeId == employee && external.Status == SlotStatus.ASSIGNED && external.DayOfMonth == slot.DayOfMonth
                    && Time(external.FromTime, out var start) && Time(external.ToTime, out var end) && from < end && start < to)
                {
                    Fail("ExternalOverlap");
                    overlapDays.Add(slot.DayOfMonth);
                }
            worked.Add((slot.DayOfMonth, employee, from, to));
        }
        foreach (var day in worked.GroupBy(w => (w.Employee, w.Day)))
        {
            var end = -1;
            foreach (var item in day.OrderBy(w => w.From))
            {
                if (item.From < end) { Fail("Overlap"); overlapDays.Add(item.Day); }
                end = Math.Max(end, item.To);
            }
        }
        var deficits = new List<int>();
        var priorityDeficit = 0;
        foreach (var employee in employees)
        {
            var assignments = worked.Where(w => w.Employee == employee.EmployeeId).ToArray();
            var total = assignments.Sum(w => w.To - w.From);
            if (schedule.MaxHoursPerEmpMonth > 0 && total - schedule.MaxHoursPerEmpMonth * 60 > 0.00006) Fail("MaxHours");
            if (schedule.MaxConsecutiveDays > 0 && LongestRun(assignments.Select(w => w.Day)) > schedule.MaxConsecutiveDays) Fail("WorkStreak");
            var full = assignments.GroupBy(w => w.Day).Where(g => g.Count() >= 2).Select(g => g.Key).Order().ToArray();
            var strict = full.Where(day => (employee.MinHoursMonth ?? 0) <= 0 || !ExplicitFullDay(input, employee.EmployeeId, day, shifts)).ToArray();
            if (schedule.MaxFullPerMonth > 0 && strict.Length > schedule.MaxFullPerMonth) Fail("MaxFullDays");
            if (schedule.MaxConsecutiveFull > 0 && LongestRun(strict) > schedule.MaxConsecutiveFull) Fail("FullStreak");
            var deficit = Math.Max(0, (employee.MinHoursMonth ?? 0) * 60 - total);
            deficits.Add(deficit);
            if (priorityIds.Contains(employee.EmployeeId)) priorityDeficit += deficit;
        }
        var gap = 0;
        for (var day = 1; day <= days; day++)
        {
            var dayWork = worked.Where(w => w.Day == day).ToArray();
            foreach (var shift in priorityIds.Count > 0 ? Union(shifts) : shifts)
                for (var minute = shift.Item1; minute < shift.Item2; minute++)
                {
                    var count = dayWork.Where(w => w.From <= minute && w.To > minute).Select(w => w.Employee).Distinct().Count();
                    if (priorityIds.Count > 0 && count > schedule.PeoplePerShift) Fail("Overstaffing");
                    var missing = Math.Max(0, schedule.PeoplePerShift - count);
                    gap += missing;
                    if (missing > 0) overlapDays.Add(day);
                }
        }
        return new(slots.Count, slots.Count(s => s.Status == SlotStatus.ASSIGNED), slots.Count(s => s.Status == SlotStatus.UNFURNISHED),
            overlapDays.Count, gap, worked.Sum(w => w.To - w.From), deficits.Sum(), deficits.Sum(d => (long)d * d), violations,
            priorityDeficit, deficits.Sum() - priorityDeficit);
    }

    private static (int, int)[] Union((int, int)[] shifts)
    {
        var result = new List<(int From, int To)>();
        foreach (var shift in shifts.OrderBy(s => s.Item1))
            if (result.Count == 0 || shift.Item1 > result[^1].To) result.Add(shift);
            else result[^1] = (result[^1].From, Math.Max(result[^1].To, shift.Item2));
        return result.ToArray();
    }

    private static (int From, int To) Window(ReplayInput input, int employee, int day)
    {
        var from = 0; var to = 1440;
        foreach (var entry in input.Groups.Where(g => g.Year == input.Schedule.Year && g.Month == input.Schedule.Month)
                     .SelectMany(g => g.Members).Where(m => m.EmployeeId == employee).SelectMany(m => m.Days).Where(d => d.DayOfMonth == day))
        {
            if (entry.Kind == AvailabilityKind.NONE) return (1440, 0);
            if (Range(entry.IntervalStr, out var start, out var end)) { from = Math.Max(from, start); to = Math.Min(to, end); }
            else if (entry.Kind == AvailabilityKind.INT) return (1440, 0);
        }
        return (from, to);
    }

    private static bool ExplicitFullDay(ReplayInput input, int employee, int day, (int, int)[] shifts)
        => shifts.Length == 2 && input.Groups.Where(g => g.Year == input.Schedule.Year && g.Month == input.Schedule.Month)
            .SelectMany(g => g.Members).Where(m => m.EmployeeId == employee).SelectMany(m => m.Days)
            .Any(d => d.DayOfMonth == day && d.Kind == AvailabilityKind.INT && Range(d.IntervalStr, out var from, out var to)
                && from <= shifts[0].Item1 && to >= shifts[1].Item2);

    internal static ReplayQualityMetrics Quality(ReplayInput input, IReadOnlyCollection<ScheduleSlotModel> slots, ReplayMetrics metrics)
    {
        var schedule = input.Schedule;
        var days = DateTime.DaysInMonth(schedule.Year, schedule.Month);
        var shifts = new[] { schedule.Shift1Time, schedule.Shift2Time }.Select(s => Range(s, out var from, out var to) ? (from, to) : (-1, -1)).Where(s => s.Item1 >= 0).ToArray();
        var employees = input.Employees.Where(e => e.EmployeeId > 0).GroupBy(e => e.EmployeeId).Select(g => g.First()).ToArray();
        var ids = employees.Select(e => e.EmployeeId).ToHashSet();
        var external = input.ExternalSlots.Where(s => s.Status == SlotStatus.ASSIGNED && s.EmployeeId.HasValue && ids.Contains(s.EmployeeId.Value)).ToArray();
        var occupied = external.GroupBy(s => (Employee: s.EmployeeId!.Value, Day: s.DayOfMonth)).ToDictionary(g => g.Key,
            g => g.Select(s => { Time(s.FromTime, out var from); Time(s.ToTime, out var to); return (From: from, To: to); }).ToArray());
        var availableCounts = Enumerable.Range(0, days + 1).Select(_ => new int[1440]).ToArray();
        var priorityEnabled = input.PriorityShiftHours.HasValue && employees.Any(e => e.IsPriority);
        var demandShifts = priorityEnabled ? Union(shifts) : shifts;
        var demand = demandShifts.Sum(s => s.Item2 - s.Item1) * days * schedule.PeoplePerShift;
        var boundsApplicable = priorityEnabled || shifts.Length == 1 || shifts.Length == 2 && shifts[0].Item2 == shifts[1].Item1;
        var capacity = 0;
        var priorityCapacity = 0;
        var priorityUnavoidableDeficit = 0;
        var individualMinimumDeficits = 0;
        var minimumTargets = 0;
        var missingMembers = 0;
        var missingDays = 0;
        var totals = new List<int>();
        var attainments = new List<double>();
        var capacityAttainments = new List<double>();
        var reviews = 0;
        foreach (var employee in employees)
        {
            var entries = input.Groups.SelectMany(g => g.Members).Where(m => m.EmployeeId == employee.EmployeeId).SelectMany(m => m.Days).ToArray();
            if (entries.Length == 0) missingMembers++;
            missingDays += days - entries.Select(d => d.DayOfMonth).Distinct().Count();
            var dayPotential = new int[days + 1];
            for (var day = 1; day <= days; day++)
            {
                var window = Window(input, employee.EmployeeId, day);
                occupied.TryGetValue((employee.EmployeeId, day), out var blocked);
                if (priorityEnabled && employee.IsPriority)
                {
                    var duration = input.PriorityShiftHours!.Value * 60;
                    if (window.To <= window.From || input.PriorityWeekendsOff &&
                        new DateOnly(schedule.Year, schedule.Month, day).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                    var free = Union(shifts).ToList();
                    foreach (var block in blocked ?? [])
                        free = free.SelectMany(w => w.Item1 >= block.To || w.Item2 <= block.From ? new[] { w } :
                            new[] { (w.Item1, Math.Min(w.Item2, block.From)), (Math.Max(w.Item1, block.To), w.Item2) }
                                .Where(p => p.Item2 > p.Item1)).ToList();
                    foreach (var interval in free)
                    {
                        var earliest = Math.Max(interval.Item1, window.From - duration + 1);
                        var latest = Math.Min(interval.Item2 - duration, window.To - 1);
                        if (earliest > latest) continue;
                        dayPotential[day] = duration;
                        for (var minute = earliest; minute < latest + duration; minute++) availableCounts[day][minute]++;
                    }
                    continue;
                }
                for (var minute = Math.Max(window.From, shifts.Min(s => s.Item1)); minute < Math.Min(window.To, shifts.Max(s => s.Item2)); minute++)
                {
                    if (blocked is not null && blocked.Any(b => minute >= b.From && minute < b.To)) continue;
                    dayPotential[day]++;
                    if (shifts.Any(s => minute >= s.Item1 && minute < s.Item2)) availableCounts[day][minute]++;
                }
            }
            // Relax staffing/full-day limits, but retain the work streak and monthly cap.
            // This is an optimistic capacity bound, not an optimizer or a feasible schedule.
            var streak = schedule.MaxConsecutiveDays > 0 ? Math.Min(days, schedule.MaxConsecutiveDays) : days;
            var state = Enumerable.Repeat(-1, streak + 1).ToArray();
            state[0] = 0;
            for (var day = 1; day <= days; day++)
            {
                var next = Enumerable.Repeat(-1, streak + 1).ToArray();
                next[0] = state.Max();
                if (dayPotential[day] > 0)
                    for (var length = 1; length <= streak; length++)
                        if (state[length - 1] >= 0) next[length] = state[length - 1] + dayPotential[day];
                state = next;
            }
            var cap = state.Max();
            if (schedule.MaxHoursPerEmpMonth > 0) cap = Math.Min(cap, schedule.MaxHoursPerEmpMonth * 60);
            capacity += cap;
            var target = Math.Max(0, employee.MinHoursMonth ?? 0) * 60;
            if (priorityEnabled && employee.IsPriority)
            {
                priorityCapacity += cap;
                priorityUnavoidableDeficit += Math.Max(0, target - cap);
            }
            minimumTargets += target;
            individualMinimumDeficits += Math.Max(0, target - cap);
            var assignments = slots.Where(s => s.Status == SlotStatus.ASSIGNED && s.EmployeeId == employee.EmployeeId).ToArray();
            var total = assignments.Sum(s => { Time(s.FromTime, out var from); Time(s.ToTime, out var to); return Math.Max(0, to - from); });
            totals.Add(total);
            if (cap > 0) capacityAttainments.Add(total / (double)cap);
            if (target > 0) attainments.Add((double)total / target);
            var full = assignments.GroupBy(s => s.DayOfMonth).Where(g => g.Count() >= 2).Select(g => g.Key).Order().ToArray();
            var strict = full.Where(day => target <= 0 || !ExplicitFullDay(input, employee.EmployeeId, day, shifts)).ToArray();
            if (schedule.MaxFullPerMonth > 0 && full.Length > schedule.MaxFullPerMonth && strict.Length <= schedule.MaxFullPerMonth) reviews++;
            if (schedule.MaxConsecutiveFull > 0 && LongestRun(full) > schedule.MaxConsecutiveFull && LongestRun(strict) <= schedule.MaxConsecutiveFull) reviews++;
        }
        var headcountGap = 0;
        for (var day = 1; day <= days; day++)
            foreach (var shift in demandShifts)
                for (var minute = shift.Item1; minute < shift.Item2; minute++)
                    headcountGap += Math.Max(0, schedule.PeoplePerShift - availableCounts[day][minute]);
        var coverageBound = Math.Max(headcountGap, boundsApplicable ? demand - capacity : 0);
        var minimumBound = Math.Max(individualMinimumDeficits, boundsApplicable ? minimumTargets - demand : 0);
        var mean = totals.DefaultIfEmpty().Average();
        return new(demand, minimumTargets, boundsApplicable, capacity, coverageBound, minimumBound,
            Math.Max(0, metrics.CoverageGapMinutes - coverageBound), Math.Max(0, metrics.TotalMinHourDeficitMinutes - minimumBound),
            schedule.AvailabilityGroupId is > 0, missingMembers, missingDays, external.Length, occupied.Count,
            totals.DefaultIfEmpty().Min(), totals.DefaultIfEmpty().Max(),
            Math.Sqrt(totals.Select(t => (t - mean) * (t - mean)).DefaultIfEmpty().Average()),
            attainments.Count == 0 ? null : attainments.Min(), attainments.Count == 0 ? null : attainments.Max(), reviews,
            priorityCapacity, priorityUnavoidableDeficit,
            capacityAttainments.Count == 0 ? null : capacityAttainments.Min(), capacityAttainments.Count == 0 ? null : capacityAttainments.Max());
    }

    internal static ReplayLocalQualityResult FindLocalMinimumImprovement(ReplayInput input, IReadOnlyCollection<ScheduleSlotModel> slots, ReplayMetrics baseline)
    {
        if (baseline.TotalMinHourDeficitMinutes == 0) return new(false, 0, false, 0, 0, 0);
        var items = slots.ToArray();
        var employees = input.Employees.Where(e => e.EmployeeId > 0).GroupBy(e => e.EmployeeId).Select(g => g.First()).OrderBy(e => e.EmployeeId).ToArray();
        var locked = input.PriorityShiftHours.HasValue ? employees.Where(e => e.IsPriority).Select(e => e.EmployeeId).ToHashSet() : [];
        var targets = employees.ToDictionary(e => e.EmployeeId, e => Math.Max(0, e.MinHoursMonth ?? 0) * 60);
        var windows = employees.SelectMany(e => Enumerable.Range(1, DateTime.DaysInMonth(input.Schedule.Year, input.Schedule.Month))
            .Select(day => (Key: (e.EmployeeId, day), Value: Window(input, e.EmployeeId, day)))).ToDictionary(x => x.Key, x => x.Value);
        var durations = items.Select(s => { Time(s.FromTime, out var from); Time(s.ToTime, out var to); return to - from; }).ToArray();
        var starts = items.Select(s => { Time(s.FromTime, out var from); return from; }).ToArray();
        var ends = items.Select(s => { Time(s.ToTime, out var to); return to; }).ToArray();
        var offsets = new Dictionary<int, int>();
        var shift = items.Select(s => { var index = offsets.GetValueOrDefault(s.DayOfMonth); offsets[s.DayOfMonth] = index + 1; return index / input.Schedule.PeoplePerShift; }).ToArray();
        var external = input.ExternalSlots.Where(s => s.Status == SlotStatus.ASSIGNED && s.EmployeeId.HasValue)
            .GroupBy(s => (s.EmployeeId!.Value, s.DayOfMonth)).ToDictionary(g => g.Key,
                g => g.Select(s => { Time(s.FromTime, out var from); Time(s.ToTime, out var to); return (From: from, To: to); }).ToArray());
        var totals = employees.ToDictionary(e => e.EmployeeId, e => items.Select((s, index) => (s, index)).Where(x => x.s.EmployeeId == e.EmployeeId && x.s.Status == SlotStatus.ASSIGNED).Sum(x => durations[x.index]));
        var appObjective = locked.Count > 0;
        var templates = new[] { input.Schedule.Shift1Time, input.Schedule.Shift2Time }
            .Select(s => Range(s, out var from, out var to) ? (from, to) : (-1, -1)).Where(s => s.Item1 >= 0).ToArray();
        var potential = appObjective ? employees.ToDictionary(e => e.EmployeeId, Potential) : new Dictionary<int, int>();
        var baselineVector = appObjective ? Vector() : null;
        const string priorityObjective = "priority-deficit/coverage-gap/regular-deficit/normalized-deficit/fairness/rest/vacancies";
        int Potential(ScheduleEmployeeModel employee)
        {
            var capacity = 0;
            for (var day = 1; day <= DateTime.DaysInMonth(input.Schedule.Year, input.Schedule.Month); day++)
            {
                var window = windows[(employee.EmployeeId, day)];
                var free = new List<(int From, int To)> { (templates.Min(t => t.Item1), templates.Max(t => t.Item2)) };
                if (external.TryGetValue((employee.EmployeeId, day), out var blocked))
                    foreach (var block in blocked.Where(b => templates.Any(t => b.From < t.Item2 && t.Item1 < b.To)))
                        free = free.SelectMany(w => w.From >= block.To || w.To <= block.From ? new[] { w } :
                            new[] { (w.From, Math.Min(w.To, block.From)), (Math.Max(w.From, block.To), w.To) }
                                .Where(p => p.Item2 > p.Item1)).ToList();
                if (free.Count == 0) continue;
                var allowed = free.OrderByDescending(w => templates.Sum(t => Math.Max(0, Math.Min(w.To, t.Item2) - Math.Max(w.From, t.Item1))))
                    .ThenByDescending(w => w.To - w.From).ThenBy(w => w.From).First();
                if (!employee.IsPriority)
                    capacity += Union(templates).Sum(t => Math.Max(0, Math.Min(Math.Min(t.Item2, allowed.To), window.To) - Math.Max(Math.Max(t.Item1, allowed.From), window.From)));
                else if (window.To > window.From && !(input.PriorityWeekendsOff && new DateOnly(input.Schedule.Year, input.Schedule.Month, day).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
                {
                    var duration = input.PriorityShiftHours!.Value * 60;
                    if (Union(templates).Any(t => Math.Max(Math.Max(t.Item1, allowed.From), window.From - duration + 1) <=
                        Math.Min(Math.Min(t.Item2, allowed.To) - duration, window.To - 1))) capacity += duration;
                }
            }
            return Math.Max(1, input.Schedule.MaxHoursPerEmpMonth > 0 ? Math.Min(capacity, input.Schedule.MaxHoursPerEmpMonth * 60) : capacity);
        }
        double[] Vector()
        {
            double priority = 0, regular = 0, normalized = 0, fairness = 0, rest = 0;
            foreach (var employee in employees)
            {
                var id = employee.EmployeeId;
                var deficit = Math.Max(0, targets[id] - totals[id]);
                if (employee.IsPriority) priority += deficit;
                else { regular += deficit; fairness += Math.Pow(totals[id] / (double)potential[id], 2); }
                normalized += Math.Pow(deficit / (double)Math.Max(1, Math.Min(potential[id], targets[id])), 2);
                var run = LongestRun(items.Where(s => s.EmployeeId == id && s.Status == SlotStatus.ASSIGNED).Select(s => s.DayOfMonth));
                rest += run * run;
            }
            return [priority, baseline.CoverageGapMinutes, regular, normalized, fairness, rest, items.Count(s => s.Status == SlotStatus.UNFURNISHED)];
        }
        static int Compare(double[] first, double[] second)
        {
            for (var index = 0; index < first.Length; index++) { var difference = first[index].CompareTo(second[index]); if (difference != 0) return difference; }
            return 0;
        }
        var watch = Stopwatch.StartNew();
        var states = 0;
        bool Limited() => states >= 20000 || watch.Elapsed > TimeSpan.FromSeconds(1);
        bool Safe(int employee)
        {
            if (input.Schedule.MaxHoursPerEmpMonth > 0 && totals[employee] > input.Schedule.MaxHoursPerEmpMonth * 60) return false;
            var positions = Enumerable.Range(0, items.Length).Where(i => items[i].EmployeeId == employee && items[i].Status == SlotStatus.ASSIGNED).ToArray();
            var fullDays = new List<int>();
            foreach (var day in positions.GroupBy(i => items[i].DayOfMonth))
            {
                if (day.Select(i => shift[i]).Distinct().Count() != day.Count()) return false;
                var window = windows[(employee, day.Key)];
                var previousEnd = -1;
                foreach (var index in day.OrderBy(i => starts[i]))
                {
                    if (starts[index] < window.From || ends[index] > window.To || starts[index] < previousEnd) return false;
                    if (external.TryGetValue((employee, day.Key), out var blocked) && blocked.Any(b => starts[index] < b.To && b.From < ends[index])) return false;
                    previousEnd = ends[index];
                }
                if (day.Count() >= 2) fullDays.Add(day.Key);
            }
            // Stricter than the explicit override: a witness must be safe without inferring
            // an unrecoverable assignment-time minimum state.
            return (input.Schedule.MaxConsecutiveDays <= 0 || LongestRun(positions.Select(i => items[i].DayOfMonth)) <= input.Schedule.MaxConsecutiveDays)
                && (input.Schedule.MaxFullPerMonth <= 0 || fullDays.Count <= input.Schedule.MaxFullPerMonth)
                && (input.Schedule.MaxConsecutiveFull <= 0 || LongestRun(fullDays) <= input.Schedule.MaxConsecutiveFull);
        }
        ReplayLocalQualityResult? Evaluate(int first, int second, int changed)
        {
            states++;
            if (!Safe(first) || !Safe(second)) return null;
            var deficits = totals.Select(t => Math.Max(0, targets[t.Key] - t.Value)).ToArray();
            var sum = deficits.Sum();
            var square = deficits.Sum(d => (long)d * d);
            var vector = appObjective ? Vector() : null;
            if (appObjective ? Compare(vector!, baselineVector!) >= 0 : sum > baseline.TotalMinHourDeficitMinutes || sum == baseline.TotalMinHourDeficitMinutes && square >= baseline.TotalMinHourDeficitSquared) return null;
            var check = Check(input, items);
            if (check.Violations.Count != 0 || check.CoverageGapMinutes != baseline.CoverageGapMinutes || check.ConflictDays != baseline.ConflictDays)
                throw new ReplayInputException("local-quality-witness-validation-disagreement");
            return new(true, states, false, changed, sum, square, appObjective ? priorityObjective : "minimum-deficit/squared-deficit", baselineVector, vector);
        }
        for (var i = 0; i < items.Length && !Limited(); i++)
        {
            if (items[i].Status != SlotStatus.ASSIGNED || items[i].EmployeeId is not int donor || locked.Contains(donor)) continue;
            foreach (var receiver in targets.Keys.Where(id => id != donor && !locked.Contains(id)))
            {
                items[i].EmployeeId = receiver;
                totals[donor] -= durations[i]; totals[receiver] += durations[i];
                ReplayLocalQualityResult? result;
                try { result = Evaluate(donor, receiver, 1); }
                finally { items[i].EmployeeId = donor; totals[donor] += durations[i]; totals[receiver] -= durations[i]; }
                if (result is not null) return result;
                if (Limited()) break;
            }
        }
        for (var i = 0; i < items.Length && !Limited(); i++)
            for (var j = i + 1; j < items.Length && !Limited(); j++)
            {
                if (items[i].Status != SlotStatus.ASSIGNED || items[j].Status != SlotStatus.ASSIGNED
                    || items[i].EmployeeId is not int first || items[j].EmployeeId is not int second || first == second
                    || locked.Contains(first) || locked.Contains(second)) continue;
                items[i].EmployeeId = second; items[j].EmployeeId = first;
                var delta = durations[j] - durations[i]; totals[first] += delta; totals[second] -= delta;
                ReplayLocalQualityResult? result;
                try { result = Evaluate(first, second, 2); }
                finally { items[i].EmployeeId = first; items[j].EmployeeId = second; totals[first] -= delta; totals[second] += delta; }
                if (result is not null) return result;
            }
        return new(false, states, Limited(), 0, baseline.TotalMinHourDeficitMinutes, baseline.TotalMinHourDeficitSquared,
            appObjective ? priorityObjective : "minimum-deficit/squared-deficit", baselineVector);
    }

    private static int LongestRun(IEnumerable<int> days)
    {
        var best = 0; var current = 0; var previous = -2;
        foreach (var day in days.Distinct().Order()) { current = day == previous + 1 ? current + 1 : 1; best = Math.Max(best, current); previous = day; }
        return best;
    }

    internal static bool Time(string? text, out int minutes)
    {
        minutes = 0;
        var parts = text?.Trim().Split(':');
        if (parts?.Length != 2 || !int.TryParse(parts[0], out var hour) || !int.TryParse(parts[1], out var minute)
            || hour < 0 || hour > 24 || minute < 0 || minute > 59 || hour == 24 && minute != 0) return false;
        minutes = hour * 60 + minute;
        return true;
    }

    internal static bool Range(string? text, out int from, out int to)
    {
        from = to = 0;
        var parts = text?.Replace('–', '-').Replace('—', '-').Split('-', StringSplitOptions.TrimEntries);
        return parts?.Length == 2 && Time(parts[0], out from) && Time(parts[1], out to) && to > from;
    }

    internal static string[] Normalize(IEnumerable<ScheduleSlotModel> slots) => slots.Select(s =>
        $"{s.DayOfMonth:D2}|{s.SlotNo}|{s.EmployeeId}|{s.Status}|{s.FromTime}|{s.ToTime}").ToArray();
}
