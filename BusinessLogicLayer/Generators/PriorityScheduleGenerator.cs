using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using static BusinessLogicLayer.Generators.PriorityScheduleRules;

namespace BusinessLogicLayer.Generators;

public sealed class PriorityScheduleGenerator(ScheduleGenerator legacy)
{
    private const int EvaluationBudget = 50_000;
    private const int ImprovementPasses = 6;
    private const int ReorientationEvaluationBudget = 20_000;
    private const int ReorientationPasses = 2;
    private const int ReorientationTrialLimit = 64;
    private const int CoordinatedEvaluationBudget = 10_000;
    private const int CoordinatedTrialLimit = 16;
    private const int PriorityRestartLimit = 2;
    private enum GenerationPhase { Reservation, Hints, Fill, Improvement, Reorientation, Coordinated }

    public Task<IList<ScheduleSlotModel>> GenerateAsync(ScheduleModel schedule,
        IReadOnlyList<AvailabilityGroupModel> availabilities, IReadOnlyList<ScheduleEmployeeModel> employees,
        int shiftHours, bool weekendsOff, IProgress<int>? progress, CancellationToken ct, bool respectAvailability = true)
        => GenerateCoreAsync(schedule, availabilities, employees, shiftHours, weekendsOff, progress, ct, respectAvailability, null);

    internal Task<IList<ScheduleSlotModel>> GenerateAuditedAsync(ScheduleModel schedule,
        IReadOnlyList<AvailabilityGroupModel> availabilities, IReadOnlyList<ScheduleEmployeeModel> employees,
        int shiftHours, bool weekendsOff, bool respectAvailability, PriorityGenerationAudit audit, CancellationToken ct = default)
        => GenerateCoreAsync(schedule, availabilities, employees, shiftHours, weekendsOff, null, ct, respectAvailability, audit);

    private async Task<IList<ScheduleSlotModel>> GenerateCoreAsync(ScheduleModel schedule,
        IReadOnlyList<AvailabilityGroupModel> availabilities, IReadOnlyList<ScheduleEmployeeModel> employees,
        int shiftHours, bool weekendsOff, IProgress<int>? progress, CancellationToken ct, bool respectAvailability,
        PriorityGenerationAudit? audit, IList<ScheduleSlotModel>? cachedHints = null, bool allowRestarts = true, int lookaheadPasses = -1, bool originalLaneOrder = false)
    {
        audit?.Reset();
        ct.ThrowIfCancellationRequested();
        var shifts = ParseShifts(schedule);
        var roster = employees.Where(e => e.EmployeeId > 0).GroupBy(e => e.EmployeeId)
            .Select(g => g.OrderByDescending(e => e.IsPriority).ThenByDescending(e => e.MinHoursMonth).First())
            .OrderBy(e => e.EmployeeId).ToArray();
        var employeeIndex = roster.Select((employee, index) => (employee.EmployeeId, index)).ToDictionary(e => e.EmployeeId, e => e.index);
        var employeeById = roster.ToDictionary(e => e.EmployeeId);
        var constraints = BuildDayConstraints(schedule, availabilities, roster, weekendsOff, respectAvailability);
        var days = DateTime.DaysInMonth(schedule.Year, schedule.Month);
        var assignments = new List<Assignment>();
        var positionLimit = checked(days * shifts.Count * schedule.PeoplePerShift);
        var evaluations = new int[6];
        var reorientationTrials = 0;
        var acceptedAnchorChanges = 0;
        var coordinatedTrials = 0;
        var phase = GenerationPhase.Reservation;
        var priorityCandidates = roster.Where(e => e.IsPriority).ToDictionary(e => e.EmployeeId,
            e => Enumerable.Range(1, days).SelectMany(day => BuildPriorityIntervals(constraints[(e.EmployeeId, day)], shiftHours, shifts)
                .Where(time => audit is null || !audit.FixedAnchors.TryGetValue((e.EmployeeId, day), out var anchor) || anchor == time)
                .Select(time => (Day: day, Time: time))).ToArray());
        if (audit?.CollectDiagnostics == true)
        {
            foreach (var employee in roster.Where(e => e.IsPriority))
            {
                foreach (var candidate in priorityCandidates[employee.EmployeeId])
                {
                    if (candidate.Time.Start == shifts.Min(s => s.Start)) audit.EarlyCandidates++;
                    else if (candidate.Time.End == shifts.Max(s => s.End)) audit.LateCandidates++;
                }
                foreach (var day in Enumerable.Range(1, days))
                {
                    var constraint = constraints[(employee.EmployeeId, day)];
                    var weekendBlocked = (weekendsOff || !respectAvailability) &&
                        new DateOnly(schedule.Year, schedule.Month, day).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    if (weekendBlocked) audit.WeekendPolicyBlockedDays++;
                    else if (constraint.Original.Minutes <= 0) audit.AvailabilityBlockedDays++;
                    if (!Envelopes(shifts).Any(s => s.Intersect(constraint.External).Minutes >= shiftHours * 60)) audit.ExternalBlockedDays++;
                    if (audit.FixedAnchors.ContainsKey((employee.EmployeeId, day)) &&
                        !priorityCandidates[employee.EmployeeId].Any(c => c.Day == day)) audit.UnavailableFixedAnchors++;
                }
            }
        }
        var potential = roster.ToDictionary(e => e.EmployeeId, e => Math.Max(1, Math.Min(
            schedule.MaxHoursPerEmpMonth > 0 ? (long)schedule.MaxHoursPerEmpMonth * 60 : int.MaxValue, e.IsPriority
            ? priorityCandidates[e.EmployeeId].Select(c => c.Day).Distinct().Count() * shiftHours * 60
            : Enumerable.Range(1, days).Sum(day => Envelopes(shifts).Sum(s => Math.Max(0,
                s.Intersect(constraints[(e.EmployeeId, day)].Original).Intersect(constraints[(e.EmployeeId, day)].External).Minutes))))));
        var priorityAvailableDays = priorityCandidates.ToDictionary(p => p.Key, p => p.Value.Select(c => c.Day).ToHashSet());
        var priorityCalendarFitsStreak = priorityAvailableDays.ToDictionary(p => p.Key,
            p => schedule.MaxConsecutiveDays <= 0 || LongestRun(p.Value) <= schedule.MaxConsecutiveDays);
        var priorityDayCapacity = new Dictionary<(int Employee, int Day), int>();
        var allPriority = roster.All(e => e.IsPriority);

        // Reserve priority work before asking the legacy engine for ordinary employee guidance.
        while (assignments.Count < positionLimit)
        {
            ct.ThrowIfCancellationRequested();
            Assignment? chosen = null;
            (double Need, int Scarcity, int Minutes, int CapacityLoss, int Load, int Streak, int Id, int Day, int Start) best = default;
            foreach (var employee in roster.Where(e => e.IsPriority))
            {
                var worked = ComputeWorkedMinutes(assignments, employee.EmployeeId);
                var targetMinutes = Math.Min((long)Math.Max(0, employee.MinHoursMonth ?? 0) * 60, potential[employee.EmployeeId]);
                if (worked >= targetMinutes) continue;
                var need = (targetMinutes - worked) / (double)Math.Max(shiftHours * 60L, targetMinutes);
                foreach (var candidate in priorityCandidates[employee.EmployeeId])
                {
                    ct.ThrowIfCancellationRequested();
                    if (assignments.Any(a => a.Day == candidate.Day && a.EmployeeId == employee.EmployeeId)) continue;
                    var requiredDays = allPriority
                        ? Math.Min(priorityAvailableDays[employee.EmployeeId].Count, schedule.MaxHoursPerEmpMonth > 0 ? schedule.MaxHoursPerEmpMonth / shiftHours : days)
                        : (int)((targetMinutes + shiftHours * 60 - 1) / (shiftHours * 60));
                    var capacityLoss = Math.Max(0, requiredDays - AttainablePriorityDays(employee.EmployeeId, candidate.Day));
                    foreach (var shift in Enumerable.Range(0, shifts.Count).OrderBy(index =>
                                 Math.Abs(shifts[index].Start - candidate.Time.Start) + Math.Abs(shifts[index].End - candidate.Time.End)))
                    {
                        var assignment = FindPosition(employee, candidate.Day, shift, candidate.Time, true);
                        if (assignment is null) continue;
                        var score = (-need, priorityCandidates[employee.EmployeeId].Length, -candidate.Time.Minutes, capacityLoss,
                            assignments.Count(a => a.Day == candidate.Day),
                            LongestRun(assignments.Where(a => a.EmployeeId == employee.EmployeeId).Select(a => a.Day).Append(candidate.Day)),
                            employee.EmployeeId, candidate.Day, candidate.Time.Start);
                        if (chosen is null || score.CompareTo(best) < 0) { chosen = assignment; best = score; }
                        break;
                    }
                }
            }
            if (chosen is null) break;
            assignments.Add(chosen);
            foreach (var key in priorityDayCapacity.Keys.Where(k => k.Employee == chosen.EmployeeId).ToArray()) priorityDayCapacity.Remove(key);
        }
        var initialPriorityAnchors = assignments.Where(a => a.LockedPriority).ToDictionary(a => (a.EmployeeId, a.Day), a => a.Time);
        progress?.Report(30);
        phase = GenerationPhase.Hints;
        var regular = roster.Where(e => !e.IsPriority).ToArray();
        // Guidance is optional: do not invoke its full legacy search when the roster/position
        // pair-work estimate already exceeds this phase's quota. Filling remains unconditional.
        var hintWorkEstimate = (long)positionLimit * regular.Length * Math.Max(1, regular.Length - 1);
        IList<ScheduleSlotModel> hints = cachedHints ?? (regular.Length == 0 || hintWorkEstimate > EvaluationBudget ? [] :
            await legacy.GenerateAsync(schedule, availabilities, regular.Select((e, index) => new ScheduleEmployeeModel
            {
                EmployeeId = e.EmployeeId,
                MinHoursMonth = e.MinHoursMonth,
                DisplayOrder = index
            }).ToArray(), null, ct).ConfigureAwait(false));
        if (audit?.CollectDiagnostics == true && cachedHints is null && regular.Length > 0 && hintWorkEstimate <= EvaluationBudget)
            audit.LegacyGuidanceBuilds++;

        // Clip proposals against reservations; they supply guidance, never immutable output.
        ApplyHints();
        progress?.Report(60);

        // Fill minute-level residual demand, considering targets and attainable employee capacity.
        FillResidual();
        // Additional exact shifts can fill residual holes without taking work away from regular staff.
        phase = GenerationPhase.Reservation;
        foreach (var employee in roster.Where(e => e.IsPriority))
        foreach (var candidate in priorityCandidates[employee.EmployeeId])
        {
            ct.ThrowIfCancellationRequested();
            if (assignments.Count >= positionLimit) break;
            if (assignments.Any(a => a.Day == candidate.Day && a.EmployeeId == employee.EmployeeId)) continue;
            foreach (var shift in Enumerable.Range(0, shifts.Count))
            {
                var addition = FindPosition(employee, candidate.Day, shift, candidate.Time, true);
                if (addition is null) continue;
                assignments.Add(addition);
                break;
            }
        }
        phase = GenerationPhase.Improvement;
        var bestQuality = Quality(assignments);
        var optimizationWorked = new int[roster.Length];
        var optimizationDays = new int[roster.Length * (days + 1)];
        var optimizationRuns = new int[roster.Length];
        RefreshOptimizationState();
        var regularPasses = 0;
        ImproveRegularAssignments();
        if (lookaheadPasses < 0)
        {
            if (audit?.CollectDiagnostics == true) audit.BeforeReorientation = Quality(assignments);
            ImprovePriorityAnchors();
            if (audit?.CollectDiagnostics == true) audit.AfterReorientation = Quality(assignments);
            // A rebuilt day can reopen ordinary exchanges; use the remaining original quota.
            if (acceptedAnchorChanges > 0) ImproveRegularAssignments();
            var beforeCoordinated = acceptedAnchorChanges;
            ImprovePriorityAnchors(coordinated: true);
            if (acceptedAnchorChanges > beforeCoordinated) ImproveRegularAssignments();
            ImprovePriorityOwnership();
            if (audit?.CollectDiagnostics == true) audit.BeforeRestarts = Quality(assignments);
            // Reuse the expensive ordinary guidance when an initial anchor traps monthly allocation.
            if (allowRestarts && hints.Count > 0 && regular.Length > 0 && (bestQuality[0] > 0 || bestQuality[1] > 0 || bestQuality[2] > 0))
            {
                var acceptedRestartAnchors = new Dictionary<(int Employee, int Day), Interval>();
                var candidates = assignments.Where(a => a.LockedPriority).OrderBy(a => a.Day).ThenBy(a => a.EmployeeId)
                    .Select(a => (Current: a, Alternative: priorityCandidates[a.EmployeeId].Where(c => c.Day == a.Day &&
                        c.Time != initialPriorityAnchors.GetValueOrDefault((a.EmployeeId, a.Day), a.Time)).OrderBy(c => c.Time.Start).FirstOrDefault().Time))
                    .Where(c => c.Alternative.Minutes > 0).ToArray();
                if (priorityCandidates.Count == 1 && candidates.Length > PriorityRestartLimit)
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    var projected = new List<(Assignment Current, Interval Alternative, double[] Score)>();
                    foreach (var candidate in candidates.Take(32))
                    {
                        var probe = Probe(candidate.Current, candidate.Alternative);
                        await GenerateCoreAsync(schedule, availabilities, employees, shiftHours, weekendsOff, null, ct, respectAvailability, probe, hints, false, 0).ConfigureAwait(false);
                        if (audit is not null) audit.RankingCandidateEvaluations += probe.RootCandidateEvaluations;
                        projected.Add((candidate.Current, candidate.Alternative, Quality(probe.Assignments!)));
                        if (audit?.CollectDiagnostics == true) { audit.RestartProjectionTrials++; audit.RestartProjectionScores.Add(new(candidate.Current.Day, candidate.Alternative.Start, projected[^1].Score)); }
                    }
                    var comparer = Comparer<double[]>.Create(Compare);
                    var refined = new List<(Assignment Current, Interval Alternative, double[] Score)>();
                    // Preserve the original starting seeds: a shallow projection is not a dominance proof.
                    var shortlist = projected.Take(PriorityRestartLimit).Concat(projected.OrderBy(c => c.Score, comparer).ThenBy(c => c.Current.Day))
                        .DistinctBy(c => (c.Current.EmployeeId, c.Current.Day, c.Alternative)).Take(8);
                    foreach (var candidate in shortlist)
                    {
                        var probe = Probe(candidate.Current, candidate.Alternative);
                        await GenerateCoreAsync(schedule, availabilities, employees, shiftHours, weekendsOff, null, ct, respectAvailability, probe, hints, false, 1).ConfigureAwait(false);
                        if (audit is not null) audit.RankingCandidateEvaluations += probe.RootCandidateEvaluations;
                        refined.Add((candidate.Current, candidate.Alternative, Quality(probe.Assignments!)));
                        if (audit?.CollectDiagnostics == true) { audit.RestartLookaheadTrials++; audit.RestartLookaheadScores.Add(new(candidate.Current.Day, candidate.Alternative.Start, refined[^1].Score)); }
                    }
                    var first = refined.OrderBy(c => c.Score, comparer).ThenBy(c => c.Current.Day).First();
                    // Diversify only search ordering. Accepted output still uses the unchanged full objective.
                    var second = refined.Where(c => c.Current != first.Current && c.Score[0] <= first.Score[0] && c.Score[1] <= first.Score[1] + shiftHours * 60L)
                        .OrderBy(c => c.Score[3]).ThenBy(c => c.Score, comparer).ThenBy(c => c.Current.Day).FirstOrDefault();
                    if (second.Current is null) second = refined.Where(c => c.Current != first.Current).OrderBy(c => c.Score, comparer).ThenBy(c => c.Current.Day).First();
                    candidates = [(first.Current, first.Alternative), (second.Current, second.Alternative)];
                    if (audit?.CollectDiagnostics == true) audit.RestartRankingMilliseconds = timer.Elapsed.TotalMilliseconds;
                }
                var attempts = 0;
                foreach (var candidate in candidates)
                {
                    ct.ThrowIfCancellationRequested();
                    if (attempts >= PriorityRestartLimit) break;
                    var restart = Probe(candidate.Current, candidate.Alternative);
                    attempts++;
                    if (audit?.CollectDiagnostics == true) audit.RestartSelectedDays.Add(candidate.Current.Day);
                    audit?.BeforeRestart?.Invoke();
                    await GenerateCoreAsync(schedule, availabilities, employees, shiftHours, weekendsOff, null, ct,
                        respectAvailability, restart, hints, allowRestarts: false, originalLaneOrder: true).ConfigureAwait(false);
                    if (audit is not null) audit.RestartCandidateEvaluations += restart.RootCandidateEvaluations;
                    var trial = restart.Assignments!;
                    var quality = Quality(trial);
                    if (audit?.CollectDiagnostics == true)
                    {
                        audit.RestartTrials++;
                        audit.RestartObjectives.Add(quality);
                    }
                    if (Compare(quality, bestQuality) >= 0 || !ValidState(trial)) continue;
                    assignments = trial.ToList(); bestQuality = quality; RefreshOptimizationState();
                    acceptedRestartAnchors[(candidate.Current.EmployeeId, candidate.Current.Day)] = candidate.Alternative;
                    if (audit?.CollectDiagnostics == true) audit.AcceptedRestarts++;
                }

                PriorityGenerationAudit Probe(Assignment current, Interval alternative)
                {
                    var probe = new PriorityGenerationAudit { CollectDiagnostics = false };
                    if (audit is not null) foreach (var anchor in audit.FixedAnchors) probe.FixedAnchors[anchor.Key] = anchor.Value;
                    foreach (var anchor in acceptedRestartAnchors) probe.FixedAnchors[anchor.Key] = anchor.Value;
                    probe.FixedAnchors[(current.EmployeeId, current.Day)] = alternative;
                    return probe;
                }
            }
            CloseRegularPairs();
        }
        ct.ThrowIfCancellationRequested();
        ValidateOutput(schedule, roster, assignments, constraints, shifts, shiftHours);
        if (audit is not null) audit.Assignments = assignments.ToArray();
        if (audit is not null) audit.RootCandidateEvaluations = evaluations.Sum();
        if (audit?.CollectDiagnostics == true)
        {
            audit.FinalObjective = Quality(assignments);
            audit.Trials = reorientationTrials;
            audit.Evaluations = evaluations[(int)GenerationPhase.Reorientation];
            audit.BudgetExhausted = audit.Evaluations >= ReorientationEvaluationBudget;
            audit.TrialLimitReached = reorientationTrials >= ReorientationTrialLimit;
            audit.CoordinatedEvaluations = evaluations[(int)GenerationPhase.Coordinated];
            audit.CoordinatedTrials = coordinatedTrials;
            audit.CoordinatedBudgetExhausted = audit.CoordinatedEvaluations >= CoordinatedEvaluationBudget;
        }
        System.Diagnostics.Activity.Current?.SetTag("gf3.priority.reorientation.evaluations", evaluations[(int)GenerationPhase.Reorientation]);
        System.Diagnostics.Activity.Current?.SetTag("gf3.priority.reorientation.trials", reorientationTrials);
        System.Diagnostics.Activity.Current?.SetTag("gf3.priority.reorientation.accepted", acceptedAnchorChanges);
        System.Diagnostics.Activity.Current?.SetTag("gf3.priority.improvement.evaluations", evaluations[(int)GenerationPhase.Improvement]);
        System.Diagnostics.Activity.Current?.SetTag("gf3.priority.improvement.passes", regularPasses);
        var output = new List<ScheduleSlotModel>(days * shifts.Count * schedule.PeoplePerShift);
        for (var day = 1; day <= days; day++)
        for (var shift = 0; shift < shifts.Count; shift++)
        for (var lane = 1; lane <= schedule.PeoplePerShift; lane++)
        {
            var assignment = assignments.SingleOrDefault(a => a.Day == day && a.Shift == shift && a.Lane == lane);
            var time = assignment?.Time ?? shifts[shift];
            output.Add(new ScheduleSlotModel { Id = 0, ScheduleId = schedule.Id, DayOfMonth = day, SlotNo = lane,
                EmployeeId = assignment?.EmployeeId, FromTime = Format(time.Start), ToTime = Format(time.End),
                Status = assignment is null ? SlotStatus.UNFURNISHED : SlotStatus.ASSIGNED });
        }
        if (output.Where(s => s.EmployeeId.HasValue).Sum(s => { TryInterval($"{s.FromTime}-{s.ToTime}", out var time); return time.Minutes; }) != assignments.Sum(a => a.Time.Minutes))
            throw new InvalidOperationException("Generated work totals do not match.");
        progress?.Report(100);
        return output;

        int PhaseBudget() => phase switch
        {
            GenerationPhase.Coordinated => CoordinatedEvaluationBudget,
            GenerationPhase.Reorientation => ReorientationEvaluationBudget,
            _ => EvaluationBudget
        };

        void ApplyHints(bool priorityTrial = false)
        {
            foreach (var hint in hints.Where(h => h.EmployeeId.HasValue))
            {
                ct.ThrowIfCancellationRequested();
                if (priorityTrial && evaluations[(int)phase] >= PhaseBudget()) return;
                var employee = regular.FirstOrDefault(e => e.EmployeeId == hint.EmployeeId);
                if (employee is null || !TryInterval($"{hint.FromTime}-{hint.ToTime}", out var time)) continue;
                foreach (var shift in Enumerable.Range(0, shifts.Count))
                foreach (var remainder in FreeIntervals(employee, hint.DayOfMonth, shift, time))
                {
                    var candidate = FindPosition(employee, hint.DayOfMonth, shift, remainder, false);
                    if (candidate is not null) { assignments.Add(candidate); break; }
                }
            }
        }

        int AttainablePriorityDays(int employeeId, int candidateDay)
        {
            if (priorityCalendarFitsStreak[employeeId]) return priorityAvailableDays[employeeId].Count;
            if (priorityDayCapacity.TryGetValue((employeeId, candidateDay), out var cached)) return cached;
            var limit = schedule.MaxConsecutiveDays > 0 ? Math.Min(days, schedule.MaxConsecutiveDays) : days;
            Span<bool> forced = stackalloc bool[32];
            forced.Clear();
            foreach (var own in assignments.Where(a => a.EmployeeId == employeeId)) forced[own.Day] = true;
            forced[candidateDay] = true;
            Span<int> state = stackalloc int[32]; Span<int> next = stackalloc int[32];
            state.Fill(-1000); state[0] = 0;
            for (var day = 1; day <= days; day++)
            {
                ct.ThrowIfCancellationRequested();
                next.Fill(-1000);
                for (var run = 0; run <= limit; run++)
                {
                    if (state[run] < 0) continue;
                    if (!forced[day]) next[0] = Math.Max(next[0], state[run]);
                    if (run < limit && priorityAvailableDays[employeeId].Contains(day)) next[run + 1] = Math.Max(next[run + 1], state[run] + 1);
                }
                var swap = state; state = next; next = swap;
            }
            var capacity = 0;
            for (var run = 0; run <= limit; run++) capacity = Math.Max(capacity, state[run]);
            priorityDayCapacity[(employeeId, candidateDay)] = capacity;
            return capacity;
        }

        void ImproveRegularAssignments()
        {
            var budget = EvaluationBudget;
            for (var pass = 0; pass < (lookaheadPasses >= 0 ? lookaheadPasses : ImprovementPasses) && evaluations[(int)phase] < budget; pass++)
            {
                regularPasses++;
                ct.ThrowIfCancellationRequested();
                var improved = false;
                foreach (var current in assignments.Where(a => !a.LockedPriority).ToArray())
                {
                    if (evaluations[(int)phase] >= budget) break;
                    var originalIndex = assignments.IndexOf(current);
                    if (originalIndex < 0) continue;
                    var without = assignments.Where((_, index) => index != originalIndex).ToArray();
                    List<Assignment>? replacement = null;
                    var candidateQuality = bestQuality;
                    foreach (var employee in regular)
                    foreach (var interval in FreeIntervals(employee, current.Day, current.Shift, shifts[current.Shift], without))
                    {
                        var candidate = new Assignment(current.Day, current.Shift, current.Lane, employee.EmployeeId, interval, false);
                        if (candidate == current) continue;
                        var trial = assignments.ToList();
                        trial[originalIndex] = candidate;
                        var quality = Quality(trial);
                        if (Compare(quality, candidateQuality) < 0 && Evaluate(employee, candidate, without) && ValidState(trial))
                        { replacement = trial; candidateQuality = quality; }
                    }
                    if (replacement is not null) { assignments = replacement; improved = true; bestQuality = candidateQuality; RefreshOptimizationState(); }
                }
                // A bounded pair exchange can unblock targets without displacing reserved priority work.
                var movable = assignments.Where(a => !a.LockedPriority).ToArray();
                for (var i = 0; i < movable.Length && evaluations[(int)phase] < budget; i++)
                for (var j = i + 1; j < movable.Length && evaluations[(int)phase] < budget; j++)
                {
                    ct.ThrowIfCancellationRequested();
                    var a = movable[i]; var b = movable[j];
                    if (a.EmployeeId == b.EmployeeId || !assignments.Contains(a) || !assignments.Contains(b)) continue;
                    if (!CanImprovePair(a, b)) continue;
                    var ia = assignments.IndexOf(a); var ib = assignments.IndexOf(b);
                    var without = assignments.Where((_, index) => index != ia && index != ib).ToArray();
                    var nextA = a with { EmployeeId = b.EmployeeId };
                    var nextB = b with { EmployeeId = a.EmployeeId };
                    var trial = assignments.ToList();
                    trial[ia] = nextA; trial[ib] = nextB;
                    var quality = Quality(trial);
                    if (Compare(quality, bestQuality) < 0 && Evaluate(employeeById[nextA.EmployeeId], nextA, without) &&
                        Evaluate(employeeById[nextB.EmployeeId], nextB, without.Append(nextA).ToArray()) && ValidState(trial))
                    {
                        assignments = trial;
                        bestQuality = quality; improved = true;
                        RefreshOptimizationState();
                    }
                }
                // Move a regular worker into a vacancy only when another worker can take the
                // original interval atomically. This avoids requiring a worsening first move.
                for (var day = 1; day <= days && evaluations[(int)phase] < budget; day++)
                for (var shift = 0; shift < shifts.Count && evaluations[(int)phase] < budget; shift++)
                for (var lane = 1; lane <= schedule.PeoplePerShift && evaluations[(int)phase] < budget; lane++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (assignments.Any(a => a.Day == day && a.Shift == shift && a.Lane == lane)) continue;
                    var accepted = false;
                    foreach (var current in assignments.Where(a => !a.LockedPriority).ToArray())
                    {
                        if (accepted || evaluations[(int)phase] >= budget) break;
                        var index = assignments.IndexOf(current);
                        var without = assignments.Where((_, i) => i != index).ToArray();
                        var employee = employeeById[current.EmployeeId];
                        var remaining = schedule.MaxHoursPerEmpMonth > 0 ? (long)schedule.MaxHoursPerEmpMonth * 60 - ComputeWorkedMinutes(without, employee.EmployeeId) : int.MaxValue;
                        if (remaining <= 0) continue;
                        foreach (var free in FreeIntervals(employee, day, shift, shifts[shift], without))
                        {
                            if (accepted || evaluations[(int)phase] >= budget) break;
                            var time = free.Minutes > remaining ? new Interval(free.Start, free.Start + (int)remaining) : free;
                            var addition = new Assignment(day, shift, lane, employee.EmployeeId, time, false);
                            if (!Evaluate(employee, addition, without)) continue;
                            foreach (var receiver in regular.Where(e => e.EmployeeId != employee.EmployeeId))
                            {
                                ct.ThrowIfCancellationRequested();
                                var replacement = current with { EmployeeId = receiver.EmployeeId };
                                var trial = assignments.ToList();
                                trial[index] = replacement; trial.Add(addition);
                                var quality = Quality(trial);
                                if (Compare(quality, bestQuality) >= 0 || !Evaluate(receiver, replacement, without.Append(addition).ToArray()) || !ValidState(trial)) continue;
                                assignments = trial; bestQuality = quality; improved = true; accepted = true; RefreshOptimizationState();
                                break;
                            }
                        }
                    }
                }
                var before = assignments.Count;
                FillResidual();
                if (assignments.Count > before) { improved = true; bestQuality = Quality(assignments); RefreshOptimizationState(); }
                if (!improved) break;
            }
        }

        void ImprovePriorityOwnership()
        {
            if (priorityCandidates.Count < 2) return;
            // Reuse the remaining ordinary improvement quota; interval, staffing and coverage stay fixed.
            for (var pass = 0; pass < ImprovementPasses && evaluations[(int)GenerationPhase.Improvement] < EvaluationBudget; pass++)
            {
                var improved = false;
                foreach (var current in assignments.Where(a => a.LockedPriority).OrderBy(a => a.Day).ThenBy(a => a.EmployeeId).ToArray())
                {
                    ct.ThrowIfCancellationRequested();
                    if (evaluations[(int)GenerationPhase.Improvement] >= EvaluationBudget) return;
                    var index = assignments.IndexOf(current);
                    if (index < 0 || audit?.FixedAnchors.ContainsKey((current.EmployeeId, current.Day)) == true) continue;
                    foreach (var receiver in roster.Where(e => e.IsPriority && e.EmployeeId != current.EmployeeId))
                    {
                        if (!priorityCandidates[receiver.EmployeeId].Any(c => c.Day == current.Day && c.Time == current.Time)) continue;
                        if (audit?.CollectDiagnostics == true) audit.OwnershipTrials++;
                        var replacement = current with { EmployeeId = receiver.EmployeeId };
                        var trial = assignments.ToList(); trial[index] = replacement;
                        var quality = Quality(trial);
                        if (Compare(quality, bestQuality) >= 0 ||
                            !Evaluate(receiver, replacement, assignments.Where((_, i) => i != index).ToArray()) || !ValidState(trial)) continue;
                        assignments = trial; bestQuality = quality; RefreshOptimizationState(); improved = true;
                        if (audit?.CollectDiagnostics == true) audit.OwnershipTransfers++;
                        break;
                    }
                }
                if (!improved) break;
            }
        }

        void ImprovePriorityAnchors(bool coordinated = false)
        {
            if (regular.Length == 0) return; // Fixed priority durations alone leave every objective component unchanged.
            var previousPhase = phase;
            phase = coordinated ? GenerationPhase.Coordinated : GenerationPhase.Reorientation;
            var trialLimit = coordinated ? CoordinatedTrialLimit : ReorientationTrialLimit;
            int TrialCount() => coordinated ? coordinatedTrials : reorientationTrials;
            bool Exhausted() => TrialCount() >= trialLimit || evaluations[(int)phase] >= PhaseBudget();
            Assignment[] Locked() => assignments.Where(a => a.LockedPriority).OrderBy(a => a.Day).ThenBy(a => a.EmployeeId)
                .ThenBy(a => a.Shift).ThenBy(a => a.Lane).ThenBy(a => a.Time.Start).ToArray();
            IEnumerable<Assignment> Alternatives(Assignment current)
            {
                foreach (var candidate in priorityCandidates[current.EmployeeId]
                             .Where(c => c.Day == current.Day && c.Time != current.Time).OrderBy(c => c.Time.Start))
                    foreach (var shift in Enumerable.Range(0, shifts.Count).Where(s => shifts[s].Overlaps(candidate.Time))
                                 .OrderBy(s => Math.Abs(shifts[s].Start - candidate.Time.Start) + Math.Abs(shifts[s].End - candidate.Time.End)).ThenBy(s => s))
                    {
                        ct.ThrowIfCancellationRequested();
                        // Refilled workers are selected by time/need, not lane label. Permuting free
                        // lanes produces the same feasible interval state and objective.
                        var lanes = Enumerable.Range(1, schedule.PeoplePerShift).Where(l => !assignments.Any(a => a.LockedPriority && a != current &&
                            a.Day == current.Day && a.Shift == shift && a.Lane == l)).OrderBy(l => l == current.Lane ? 0 : 1).ThenBy(l => l).ToArray();
                        if (lanes.Length == 0) continue;
                        if (audit?.CollectDiagnostics == true && !originalLaneOrder) audit.EquivalentLaneTrialsSkipped += lanes.Length - 1;
                        foreach (var lane in originalLaneOrder ? lanes : lanes.Take(1))
                            yield return current with { Shift = shift, Lane = lane, Time = candidate.Time };
                    }
            }
            bool TryTrial(out bool built, params (Assignment Original, Assignment Alternative)[] changes)
            {
                built = false;
                ct.ThrowIfCancellationRequested();
                if (Exhausted()) return false;
                var changedDays = changes.Select(c => c.Original.Day).ToHashSet();
                var removed = changes.Select(c => c.Original).ToHashSet();
                var trial = assignments.Where(a => !removed.Contains(a) && (a.LockedPriority || !changedDays.Contains(a.Day))).ToList();
                foreach (var change in changes)
                {
                    var next = change.Alternative;
                    if (trial.Any(a => a.Day == next.Day && a.Shift == next.Shift && a.Lane == next.Lane) ||
                        !constraints[(next.EmployeeId, next.Day)].External.Contains(next.Time) ||
                        !Evaluate(employeeById[next.EmployeeId], next, trial)) return false;
                    trial.Add(next);
                }
                if (coordinated) coordinatedTrials++; else reorientationTrials++;
                built = true;
                var accepted = assignments;
                var canonicalQuality = bestQuality;
                var commit = false;
                try
                {
                    assignments = trial;
                    foreach (var day in changedDays.Order()) FillResidual(onlyDay: day, priorityTrial: true);
                    ct.ThrowIfCancellationRequested();
                    var quality = Quality(assignments);
                    if (audit?.CollectDiagnostics == true && changes.Length == 1)
                        audit.AnchorProbes.Add(new(changes[0].Original.Day, changes[0].Alternative.Time.Start, quality));
                    if (Compare(quality, bestQuality) >= 0)
                    { if (audit?.CollectDiagnostics == true) audit.NonImprovingTrials++; return false; }
                    if (!ValidState(assignments)) return false;
                    bestQuality = quality;
                    commit = true;
                    acceptedAnchorChanges += changes.Length;
                    if (audit?.CollectDiagnostics == true)
                    {
                        foreach (var change in changes)
                            if (change.Alternative.Time.Start > change.Original.Time.Start) audit.EarlyToLateChanges++;
                            else audit.LateToEarlyChanges++;
                        if (changes.Length == 2) audit.AcceptedPairedTrials++;
                    }
                    return true;
                }
                finally
                {
                    if (!commit) { assignments = accepted; bestQuality = canonicalQuality; }
                    RefreshOptimizationState();
                }
            }
            try
            {
                for (var pass = 0; !coordinated && pass < ReorientationPasses && !Exhausted(); pass++)
                {
                    var improved = false;
                    foreach (var current in Locked())
                    {
                        if (!assignments.Contains(current)) continue;
                        foreach (var next in Alternatives(current))
                        {
                            if (Exhausted()) break;
                            if (!TryTrial(out _, (current, next))) continue;
                            improved = true; break;
                        }
                    }
                    if (!improved) break;
                }
                if (!coordinated) return;
                // Preserve the complete original search result, then explore bounded larger neighborhoods.
                var locked = Locked();
                for (var first = 0; first < locked.Length && !Exhausted(); first++)
                for (var second = first + 1; second < locked.Length && !Exhausted(); second++)
                {
                    var a = locked[first]; var b = locked[second];
                    if (a.Day == b.Day || !assignments.Contains(a) || !assignments.Contains(b)) continue;
                    var moved = false;
                    foreach (var nextA in Alternatives(a))
                    {
                        if (moved || Exhausted()) break;
                        foreach (var nextB in Alternatives(b))
                        {
                            if (Exhausted()) break;
                            if (audit?.CollectDiagnostics == true) audit.PairedTrials++;
                            if (TryTrial(out var built, (a, nextA), (b, nextB))) moved = true;
                            if (built) break;
                        }
                    }
                }
            }
            finally { phase = previousPhase; }
        }

        void CloseRegularPairs()
        {
            var previous = phase; phase = GenerationPhase.Reorientation;
            try
            {
                for (var pass = 0; pass < ImprovementPasses && evaluations[(int)phase] < PhaseBudget(); pass++)
                {
                    var changed = false; var movable = assignments.Where(a => !a.LockedPriority).ToArray();
                    for (var i = 0; i < movable.Length && evaluations[(int)phase] < PhaseBudget(); i++)
                        for (var j = i + 1; j < movable.Length && evaluations[(int)phase] < PhaseBudget(); j++)
                        {
                            ct.ThrowIfCancellationRequested(); var a = movable[i]; var b = movable[j];
                            if (a.EmployeeId == b.EmployeeId || !assignments.Contains(a) || !assignments.Contains(b) || !CanImprovePair(a, b)) continue;
                            var ia = assignments.IndexOf(a); var ib = assignments.IndexOf(b); var without = assignments.Where((_, k) => k != ia && k != ib).ToArray();
                            var nextA = a with { EmployeeId = b.EmployeeId }; var nextB = b with { EmployeeId = a.EmployeeId };
                            if (!Evaluate(employeeById[nextA.EmployeeId], nextA, without) || !Evaluate(employeeById[nextB.EmployeeId], nextB, without.Append(nextA).ToArray())) continue;
                            var trial = assignments.ToList(); trial[ia] = nextA; trial[ib] = nextB; var score = Quality(trial);
                            if (Compare(score, bestQuality) >= 0 || !ValidState(trial)) continue;
                            assignments = trial; bestQuality = score; RefreshOptimizationState(); changed = true;
                        }
                    if (!changed) break;
                }
            }
            finally { phase = previous; }
        }

        void RefreshOptimizationState()
        {
            Array.Clear(optimizationWorked); Array.Clear(optimizationDays); Array.Clear(optimizationRuns);
            foreach (var assignment in assignments)
            {
                var index = employeeIndex[assignment.EmployeeId];
                optimizationWorked[index] += assignment.Time.Minutes;
                optimizationDays[index * (days + 1) + assignment.Day]++;
            }
            for (var index = 0; index < roster.Length; index++)
            {
                var run = 0;
                for (var day = 1; day <= days; day++)
                {
                    run = optimizationDays[index * (days + 1) + day] > 0 ? run + 1 : 0;
                    optimizationRuns[index] = Math.Max(optimizationRuns[index], run);
                }
            }
        }

        bool CanImprovePair(Assignment a, Assignment b)
        {
            // Score fixed-time ownership changes without allocating/copying a whole state.
            // Accumulate in canonical roster order, exactly as Quality does.
            var first = employeeIndex[a.EmployeeId]; var second = employeeIndex[b.EmployeeId];
            var delta = b.Time.Minutes - a.Time.Minutes;
            double deficit = 0, normalized = 0, fairness = 0;
            for (var index = 0; index < roster.Length; index++)
            {
                var employee = roster[index];
                var worked = optimizationWorked[index] + (index == first ? delta : index == second ? -delta : 0);
                var target = Math.Max(0, employee.MinHoursMonth ?? 0) * 60.0;
                var missing = Math.Max(0, target - worked);
                if (!employee.IsPriority) { deficit += missing; fairness += Math.Pow(worked / (double)potential[employee.EmployeeId], 2); }
                normalized += Math.Pow(missing / Math.Max(1, Math.Min(potential[employee.EmployeeId], target)), 2);
            }
            if (deficit != bestQuality[2]) return deficit < bestQuality[2];
            if (normalized != bestQuality[3]) return normalized < bestQuality[3];
            if (fairness != bestQuality[4]) return fairness < bestQuality[4];
            var firstRun = 0; var secondRun = 0; var longestFirst = 0; var longestSecond = 0;
            for (var day = 1; day <= days; day++)
            {
                var firstCount = optimizationDays[first * (days + 1) + day] + (day == b.Day ? 1 : 0) - (day == a.Day ? 1 : 0);
                var secondCount = optimizationDays[second * (days + 1) + day] + (day == a.Day ? 1 : 0) - (day == b.Day ? 1 : 0);
                firstRun = firstCount > 0 ? firstRun + 1 : 0; secondRun = secondCount > 0 ? secondRun + 1 : 0;
                longestFirst = Math.Max(longestFirst, firstRun); longestSecond = Math.Max(longestSecond, secondRun);
            }
            return longestFirst * longestFirst + longestSecond * longestSecond <
                optimizationRuns[first] * optimizationRuns[first] + optimizationRuns[second] * optimizationRuns[second];
        }

        bool ValidState(IReadOnlyList<Assignment> state)
        {
            ct.ThrowIfCancellationRequested();
            if (phase is GenerationPhase.Reorientation or GenerationPhase.Coordinated)
            {
                var accepted = new List<Assignment>();
                foreach (var assignment in state)
                {
                    if (!Evaluate(employeeById[assignment.EmployeeId], assignment, accepted)) return false;
                    accepted.Add(assignment);
                }
                return true;
            }
            try { ValidateOutput(schedule, roster, state, constraints, shifts, shiftHours); return true; }
            catch (BusinessLogicLayer.Common.ValidationException) { return false; }
        }

        bool Evaluate(ScheduleEmployeeModel employee, Assignment candidate, IReadOnlyList<Assignment>? state = null)
        {
            ct.ThrowIfCancellationRequested();
            if (phase is GenerationPhase.Improvement or GenerationPhase.Reorientation or GenerationPhase.Coordinated &&
                evaluations[(int)phase] >= PhaseBudget()) return false;
            evaluations[(int)phase]++;
            var valid = ValidateAssignment(schedule, employee, candidate, state ?? assignments, constraints, shifts, shiftHours);
            if (audit?.CollectDiagnostics == true)
            {
                if (employee.IsPriority)
                {
                    var early = candidate.Time.Start == shifts.Min(s => s.Start);
                    var late = !early && candidate.Time.End == shifts.Max(s => s.End);
                    if (early) { audit.EarlyEvaluations++; if (valid) audit.FeasibleEarlyEvaluations++; }
                    if (late) { audit.LateEvaluations++; if (valid) audit.FeasibleLateEvaluations++; }
                }
                if (!valid) audit.Reject(ExplainRejection(employee, candidate, state ?? assignments));
            }
            return valid;
        }

        string ExplainRejection(ScheduleEmployeeModel employee, Assignment candidate, IReadOnlyList<Assignment> state)
        {
            var constraint = constraints[(employee.EmployeeId, candidate.Day)];
            if (!constraint.External.Contains(candidate.Time)) return "ExternalCommitment";
            if (employee.IsPriority ? !BuildPriorityIntervals(constraint, shiftHours, shifts).Contains(candidate.Time)
                : !constraint.Original.Contains(candidate.Time)) return "Availability";
            var own = state.Where(a => a.EmployeeId == employee.EmployeeId).ToArray();
            if (schedule.MaxHoursPerEmpMonth > 0 && own.Sum(a => a.Time.Minutes) + candidate.Time.Minutes > (long)schedule.MaxHoursPerEmpMonth * 60) return "MonthlyHours";
            if (schedule.MaxConsecutiveDays > 0 && LongestRun(own.Select(a => a.Day).Append(candidate.Day)) > schedule.MaxConsecutiveDays) return "ConsecutiveDays";
            if (state.Any(a => a.Day == candidate.Day && (a.Shift == candidate.Shift && a.Lane == candidate.Lane ||
                a.EmployeeId == candidate.EmployeeId && (a.Time.Overlaps(candidate.Time) || employee.IsPriority)))) return "PositionOrOverlap";
            if (GetCoverageBreakpoints(shifts, state.Where(a => a.Day == candidate.Day).Append(candidate)).Any(minute =>
                minute >= candidate.Time.Start && minute < candidate.Time.End && state.Count(a => a.Day == candidate.Day && a.Time.Start <= minute && minute < a.Time.End) >= schedule.PeoplePerShift)) return "Staffing";
            return "FullDayOrPositionLimit";
        }

        Assignment? FindPosition(ScheduleEmployeeModel employee, int day, int shift, Interval time, bool locked)
        {
            // Prefer the same lane at a touching early/late handover.
            var lanes = Enumerable.Range(1, schedule.PeoplePerShift).OrderBy(lane => assignments.Any(a =>
                a.Day == day && a.Lane == lane && (a.Time.End == time.Start || a.Time.Start == time.End)) ? 0 : 1).ThenBy(lane => lane);
            foreach (var lane in lanes)
            {
                if (assignments.Any(a => a.Day == day && a.Shift == shift && a.Lane == lane)) continue;
                var candidate = new Assignment(day, shift, lane, employee.EmployeeId, time, locked);
                if (Evaluate(employee, candidate)) return candidate;
            }
            return null;
        }

        IEnumerable<Interval> FreeIntervals(ScheduleEmployeeModel employee, int day, int shift, Interval requested, IReadOnlyList<Assignment>? state = null)
        {
            var constraint = constraints[(employee.EmployeeId, day)];
            var window = shifts[shift].Intersect(requested).Intersect(constraint.Original).Intersect(constraint.External);
            if (window.Minutes <= 0) yield break;
            var today = (state ?? assignments).Where(a => a.Day == day).ToArray();
            var points = GetCoverageBreakpoints(new[] { window }, today).Where(p => p >= window.Start && p <= window.End).ToArray();
            int? start = null;
            for (var i = 0; i + 1 < points.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                var free = today.Count(a => a.Time.Start <= points[i] && a.Time.End >= points[i + 1]) < schedule.PeoplePerShift &&
                    !today.Any(a => a.EmployeeId == employee.EmployeeId && a.Time.Start <= points[i] && a.Time.End >= points[i + 1]);
                if (free) start ??= points[i];
                if (start.HasValue && (!free || i == points.Length - 2))
                {
                    var end = free ? points[i + 1] : points[i];
                    yield return new(start.Value, end);
                    start = null;
                }
            }
        }

        void FillResidual(int? onlyDay = null, bool priorityTrial = false)
        {
            var previousPhase = phase;
            if (!priorityTrial) phase = GenerationPhase.Fill;
            try
            {
                for (var pass = 0; pass < positionLimit && assignments.Count < positionLimit; pass++)
                {
                    var accepted = 0;
                    for (var day = onlyDay ?? 1; day <= (onlyDay ?? days); day++)
                    for (var shift = 0; shift < shifts.Count; shift++)
                    for (var lane = 1; lane <= schedule.PeoplePerShift; lane++)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (priorityTrial && evaluations[(int)phase] >= PhaseBudget()) return;
                        if (assignments.Any(a => a.Day == day && a.Shift == shift && a.Lane == lane)) continue;
                        Assignment? chosen = null;
                        (int Coverage, double Deficit, double Fairness, int Streak, int Id, int Day, int Shift, int Lane, int Start) best = default;
                        foreach (var employee in regular)
                        {
                            ct.ThrowIfCancellationRequested();
                            if (priorityTrial && evaluations[(int)phase] >= PhaseBudget()) return;
                            var worked = ComputeWorkedMinutes(assignments, employee.EmployeeId);
                            var target = Math.Min(Math.Max(0, employee.MinHoursMonth ?? 0) * 60.0, potential[employee.EmployeeId]);
                            var remaining = schedule.MaxHoursPerEmpMonth > 0 ? (long)schedule.MaxHoursPerEmpMonth * 60 - worked : int.MaxValue;
                            if (remaining <= 0) continue;
                            foreach (var interval in FreeIntervals(employee, day, shift, shifts[shift]))
                            {
                                var time = interval.Minutes > remaining ? new Interval(interval.Start, interval.Start + (int)remaining) : interval;
                                var candidate = new Assignment(day, shift, lane, employee.EmployeeId, time, false);
                                if (!Evaluate(employee, candidate)) continue;
                                var score = (-time.Minutes, -Math.Min(time.Minutes, Math.Max(0, target - worked)) / Math.Max(1, target),
                                    worked / (double)potential[employee.EmployeeId],
                                    LongestRun(assignments.Where(a => a.EmployeeId == employee.EmployeeId).Select(a => a.Day).Append(day)),
                                    employee.EmployeeId, day, shift, lane, time.Start);
                                if (chosen is null || score.CompareTo(best) < 0) { chosen = candidate; best = score; }
                            }
                        }
                        if (chosen is null) continue;
                        assignments.Add(chosen);
                        accepted++;
                    }
                    if (accepted == 0) break;
                }
            }
            finally { phase = previousPhase; }
        }

        double[] Quality(IReadOnlyList<Assignment> state)
        {
            var worked = new int[roster.Length];
            var workedDays = new bool[roster.Length * (days + 1)];
            var coveredMinutes = 0;
            foreach (var assignment in state)
            {
                var index = employeeIndex[assignment.EmployeeId];
                worked[index] += assignment.Time.Minutes;
                workedDays[index * (days + 1) + assignment.Day] = true;
                coveredMinutes += assignment.Time.Minutes;
            }
            double priorityDeficit = 0, regularDeficit = 0, normalized = 0, fairness = 0, rest = 0;
            for (var index = 0; index < roster.Length; index++)
            {
                var employee = roster[index];
                var target = Math.Max(0, employee.MinHoursMonth ?? 0) * 60.0;
                var deficit = Math.Max(0, target - worked[index]);
                if (employee.IsPriority) priorityDeficit += deficit;
                else { regularDeficit += deficit; fairness += Math.Pow(worked[index] / (double)potential[employee.EmployeeId], 2); }
                normalized += Math.Pow(deficit / Math.Max(1, Math.Min(potential[employee.EmployeeId], target)), 2);
                var longest = 0; var run = 0;
                for (var day = 1; day <= days; day++)
                {
                    run = workedDays[index * (days + 1) + day] ? run + 1 : 0;
                    longest = Math.Max(longest, run);
                }
                rest += longest * longest;
            }
            // Every scored assignment has already passed nonoverlap and staffing bounds, so its
            // duration is exactly its distinct-worker coverage contribution to the shift union.
            var gap = days * Envelopes(shifts).Sum(s => s.Minutes) * schedule.PeoplePerShift - coveredMinutes;
            return [priorityDeficit, gap, regularDeficit, normalized, fairness, rest,
                days * shifts.Count * schedule.PeoplePerShift - state.Count];
        }
    }

    private static int Compare(double[] left, double[] right)
    {
        for (var i = 0; i < left.Length; i++)
        {
            var comparison = left[i].CompareTo(right[i]);
            if (comparison != 0) return comparison;
        }
        return 0;
    }
}

