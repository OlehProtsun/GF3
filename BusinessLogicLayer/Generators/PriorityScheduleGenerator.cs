using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using static BusinessLogicLayer.Generators.PriorityScheduleRules;

namespace BusinessLogicLayer.Generators;

public sealed class PriorityScheduleGenerator(ScheduleGenerator legacy)
{
    private const int EvaluationBudget = 50_000;
    private const int ImprovementPasses = 6;
    private enum GenerationPhase { Reservation, Hints, Fill, Improvement }

    public async Task<IList<ScheduleSlotModel>> GenerateAsync(ScheduleModel schedule,
        IReadOnlyList<AvailabilityGroupModel> availabilities, IReadOnlyList<ScheduleEmployeeModel> employees,
        int shiftHours, bool weekendsOff, IProgress<int>? progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var shifts = ParseShifts(schedule);
        var roster = employees.Where(e => e.EmployeeId > 0).GroupBy(e => e.EmployeeId)
            .Select(g => g.OrderByDescending(e => e.IsPriority).ThenByDescending(e => e.MinHoursMonth).First())
            .OrderBy(e => e.EmployeeId).ToArray();
        var employeeIndex = roster.Select((employee, index) => (employee.EmployeeId, index)).ToDictionary(e => e.EmployeeId, e => e.index);
        var employeeById = roster.ToDictionary(e => e.EmployeeId);
        var constraints = BuildDayConstraints(schedule, availabilities, roster, weekendsOff);
        var days = DateTime.DaysInMonth(schedule.Year, schedule.Month);
        var assignments = new List<Assignment>();
        var positionLimit = checked(days * shifts.Count * schedule.PeoplePerShift);
        var evaluations = new int[4];
        var phase = GenerationPhase.Reservation;
        var priorityCandidates = roster.Where(e => e.IsPriority).ToDictionary(e => e.EmployeeId,
            e => Enumerable.Range(1, days).SelectMany(day => BuildPriorityIntervals(constraints[(e.EmployeeId, day)], shiftHours, shifts)
                .Select(time => (Day: day, Time: time))).ToArray());
        var potential = roster.ToDictionary(e => e.EmployeeId, e => Math.Max(1, Math.Min(
            schedule.MaxHoursPerEmpMonth > 0 ? (long)schedule.MaxHoursPerEmpMonth * 60 : int.MaxValue, e.IsPriority
            ? priorityCandidates[e.EmployeeId].Select(c => c.Day).Distinct().Count() * shiftHours * 60
            : Enumerable.Range(1, days).Sum(day => Envelopes(shifts).Sum(s => Math.Max(0,
                s.Intersect(constraints[(e.EmployeeId, day)].Original).Intersect(constraints[(e.EmployeeId, day)].External).Minutes))))));

        // Reserve priority work before asking the legacy engine for ordinary employee guidance.
        while (assignments.Count < positionLimit)
        {
            ct.ThrowIfCancellationRequested();
            Assignment? chosen = null;
            (double Need, int Scarcity, int Minutes, int Load, int Streak, int Id, int Day, int Start) best = default;
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
                    foreach (var shift in Enumerable.Range(0, shifts.Count).OrderBy(index =>
                                 Math.Abs(shifts[index].Start - candidate.Time.Start) + Math.Abs(shifts[index].End - candidate.Time.End)))
                    {
                        var assignment = FindPosition(employee, candidate.Day, shift, candidate.Time, true);
                        if (assignment is null) continue;
                        var score = (-need, priorityCandidates[employee.EmployeeId].Length, -candidate.Time.Minutes,
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
        }
        progress?.Report(30);
        phase = GenerationPhase.Hints;
        var regular = roster.Where(e => !e.IsPriority).ToArray();
        // Guidance is optional: do not invoke its full legacy search when the roster/position
        // pair-work estimate already exceeds this phase's quota. Filling remains unconditional.
        var hintWorkEstimate = (long)positionLimit * regular.Length * Math.Max(1, regular.Length - 1);
        IList<ScheduleSlotModel> hints = regular.Length == 0 || hintWorkEstimate > EvaluationBudget ? [] :
            await legacy.GenerateAsync(schedule, availabilities, regular, null, ct).ConfigureAwait(false);

        // Clip proposals against reservations; they supply guidance, never immutable output.
        foreach (var hint in hints.Where(h => h.EmployeeId.HasValue))
        {
            ct.ThrowIfCancellationRequested();
            var employee = regular.FirstOrDefault(e => e.EmployeeId == hint.EmployeeId);
            if (employee is null || !TryInterval($"{hint.FromTime}-{hint.ToTime}", out var time)) continue;
            foreach (var shift in Enumerable.Range(0, shifts.Count))
            foreach (var remainder in FreeIntervals(employee, hint.DayOfMonth, shift, time))
            {
                var candidate = FindPosition(employee, hint.DayOfMonth, shift, remainder, false);
                if (candidate is not null) { assignments.Add(candidate); break; }
            }
        }
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
        for (var pass = 0; pass < ImprovementPasses && evaluations[(int)phase] < EvaluationBudget; pass++)
        {
            ct.ThrowIfCancellationRequested();
            var improved = false;
            foreach (var current in assignments.Where(a => !a.LockedPriority).ToArray())
            {
                if (evaluations[(int)phase] >= EvaluationBudget) break;
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
            for (var i = 0; i < movable.Length && evaluations[(int)phase] < EvaluationBudget; i++)
            for (var j = i + 1; j < movable.Length && evaluations[(int)phase] < EvaluationBudget; j++)
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
            for (var day = 1; day <= days && evaluations[(int)phase] < EvaluationBudget; day++)
            for (var shift = 0; shift < shifts.Count && evaluations[(int)phase] < EvaluationBudget; shift++)
            for (var lane = 1; lane <= schedule.PeoplePerShift && evaluations[(int)phase] < EvaluationBudget; lane++)
            {
                ct.ThrowIfCancellationRequested();
                if (assignments.Any(a => a.Day == day && a.Shift == shift && a.Lane == lane)) continue;
                var accepted = false;
                foreach (var current in assignments.Where(a => !a.LockedPriority).ToArray())
                {
                    if (accepted || evaluations[(int)phase] >= EvaluationBudget) break;
                    var index = assignments.IndexOf(current);
                    var without = assignments.Where((_, i) => i != index).ToArray();
                    var employee = employeeById[current.EmployeeId];
                    var remaining = schedule.MaxHoursPerEmpMonth > 0 ? (long)schedule.MaxHoursPerEmpMonth * 60 - ComputeWorkedMinutes(without, employee.EmployeeId) : int.MaxValue;
                    if (remaining <= 0) continue;
                    foreach (var free in FreeIntervals(employee, day, shift, shifts[shift], without))
                    {
                        if (accepted || evaluations[(int)phase] >= EvaluationBudget) break;
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
        ct.ThrowIfCancellationRequested();
        ValidateOutput(schedule, roster, assignments, constraints, shifts, shiftHours);
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
            try { ValidateOutput(schedule, roster, state, constraints, shifts, shiftHours); return true; }
            catch (BusinessLogicLayer.Common.ValidationException) { return false; }
        }

        bool Evaluate(ScheduleEmployeeModel employee, Assignment candidate, IReadOnlyList<Assignment>? state = null)
        {
            ct.ThrowIfCancellationRequested();
            if (phase == GenerationPhase.Improvement && evaluations[(int)phase] >= EvaluationBudget) return false;
            evaluations[(int)phase]++;
            return ValidateAssignment(schedule, employee, candidate, state ?? assignments, constraints, shifts, shiftHours);
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

        void FillResidual()
        {
            var previousPhase = phase;
            phase = GenerationPhase.Fill;
            try
            {
                for (var pass = 0; pass < positionLimit && assignments.Count < positionLimit; pass++)
                {
                    var accepted = 0;
                    for (var day = 1; day <= days; day++)
                    for (var shift = 0; shift < shifts.Count; shift++)
                    for (var lane = 1; lane <= schedule.PeoplePerShift; lane++)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (assignments.Any(a => a.Day == day && a.Shift == shift && a.Lane == lane)) continue;
                        Assignment? chosen = null;
                        (int Coverage, double Deficit, double Fairness, int Streak, int Id, int Day, int Shift, int Lane, int Start) best = default;
                        foreach (var employee in regular)
                        {
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

