using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Enums;

namespace BusinessLogicLayer.Generators
{
    /// <summary>
    /// Core scheduling engine that transforms employee availability and graph configuration
    /// into a concrete set of schedule slots for one month.
    /// The generator is intentionally kept self-contained because most of the logic is algorithmic:
    /// it builds normalized matrices up front, scores candidate assignments, and then emits slots.
    /// </summary>
    public sealed class ScheduleGenerator : IScheduleGenerator
    {
        private const double EPS = 1e-9;

        private sealed class ShiftTemplate
        {
            public int Index { get; init; }             // 1 => Shift1, 2 => Shift2
            public string From { get; init; } = null!;  // "HH:mm"
            public string To { get; init; } = null!;    // "HH:mm"

            public int StartMin { get; init; }          // minutes from midnight
            public int EndMin { get; init; }            // minutes from midnight

            public double Hours { get; init; }          // duration in hours (template)
        }

        private readonly struct CandidatePriority
        {
            public CandidatePriority(
                bool pairsOtherShiftSameSlot,
                bool blocksOtherShiftPair,
                bool hasHardNeed,
                bool isCriticalHardNeed,
                double hardSlack,
                double hardCriticality,
                double hardCoverage,
                double hardDeficit,
                double softGap,
                double softCoverage,
                double gainHours,
                double totalHours,
                int fullDays,
                int scarcity,
                int availabilityPressure,
                int roundRobinDistance)
            {
                PairsOtherShiftSameSlot = pairsOtherShiftSameSlot;
                BlocksOtherShiftPair = blocksOtherShiftPair;
                HasHardNeed = hasHardNeed;
                IsCriticalHardNeed = isCriticalHardNeed;
                HardSlack = hardSlack;
                HardCriticality = hardCriticality;
                HardCoverage = hardCoverage;
                HardDeficit = hardDeficit;
                SoftGap = softGap;
                SoftCoverage = softCoverage;
                GainHours = gainHours;
                TotalHours = totalHours;
                FullDays = fullDays;
                Scarcity = scarcity;
                AvailabilityPressure = availabilityPressure;
                RoundRobinDistance = roundRobinDistance;
            }

            public bool PairsOtherShiftSameSlot { get; }
            public bool BlocksOtherShiftPair { get; }
            public bool HasHardNeed { get; }
            public bool IsCriticalHardNeed { get; }
            public double HardSlack { get; }
            public double HardCriticality { get; }
            public double HardCoverage { get; }
            public double HardDeficit { get; }
            public double SoftGap { get; }
            public double SoftCoverage { get; }
            public double GainHours { get; }
            public double TotalHours { get; }
            public int FullDays { get; }
            public int Scarcity { get; }
            public int AvailabilityPressure { get; }
            public int RoundRobinDistance { get; }
        }

        private readonly struct AttemptScore
        {
            public AttemptScore(
                int conflictDays,
                int coverageGap,
                double minHourDeficit,
                double minHourSquaredDeficit,
                double desiredHourDeficit,
                double desiredHourSquaredDeficit,
                double restPenalty,
                int unfurnishedSlots,
                int overlapDays)
            {
                ConflictDays = conflictDays;
                CoverageGap = coverageGap;
                MinHourDeficit = minHourDeficit;
                MinHourSquaredDeficit = minHourSquaredDeficit;
                DesiredHourDeficit = desiredHourDeficit;
                DesiredHourSquaredDeficit = desiredHourSquaredDeficit;
                RestPenalty = restPenalty;
                UnfurnishedSlots = unfurnishedSlots;
                OverlapDays = overlapDays;
            }

            public int ConflictDays { get; }
            public int CoverageGap { get; }
            public double MinHourDeficit { get; }
            public double MinHourSquaredDeficit { get; }
            public double DesiredHourDeficit { get; }
            public double DesiredHourSquaredDeficit { get; }
            public double RestPenalty { get; }
            public int UnfurnishedSlots { get; }
            public int OverlapDays { get; }

            public bool IsBetterThan(AttemptScore other)
            {
                if (ConflictDays != other.ConflictDays)
                    return ConflictDays < other.ConflictDays;

                if (CoverageGap != other.CoverageGap)
                    return CoverageGap < other.CoverageGap;

                if (MinHourDeficit < other.MinHourDeficit - EPS)
                    return true;

                if (MinHourDeficit > other.MinHourDeficit + EPS)
                    return false;

                if (MinHourSquaredDeficit < other.MinHourSquaredDeficit - EPS)
                    return true;

                if (MinHourSquaredDeficit > other.MinHourSquaredDeficit + EPS)
                    return false;

                if (DesiredHourDeficit < other.DesiredHourDeficit - EPS)
                    return true;

                if (DesiredHourDeficit > other.DesiredHourDeficit + EPS)
                    return false;

                if (DesiredHourSquaredDeficit < other.DesiredHourSquaredDeficit - EPS)
                    return true;

                if (DesiredHourSquaredDeficit > other.DesiredHourSquaredDeficit + EPS)
                    return false;

                if (RestPenalty < other.RestPenalty - EPS)
                    return true;

                if (RestPenalty > other.RestPenalty + EPS)
                    return false;

                if (UnfurnishedSlots != other.UnfurnishedSlots)
                    return UnfurnishedSlots < other.UnfurnishedSlots;

                if (OverlapDays != other.OverlapDays)
                    return OverlapDays < other.OverlapDays;

                return false;
            }
        }

        private sealed class ScheduleAttemptState
        {
            public ScheduleAttemptState(
                int[] assigned,
                int[] slotFromMin,
                int[] slotToMin,
                double[] slotHours,
                double[] totalHours,
                int[] shiftsPerDay,
                int[] fullDaysCount,
                AttemptScore score)
            {
                Assigned = assigned;
                SlotFromMin = slotFromMin;
                SlotToMin = slotToMin;
                SlotHours = slotHours;
                TotalHours = totalHours;
                ShiftsPerDay = shiftsPerDay;
                FullDaysCount = fullDaysCount;
                Score = score;
            }

            public int[] Assigned { get; }
            public int[] SlotFromMin { get; }
            public int[] SlotToMin { get; }
            public double[] SlotHours { get; }
            public double[] TotalHours { get; }
            public int[] ShiftsPerDay { get; }
            public int[] FullDaysCount { get; }
            public AttemptScore Score { get; }
        }

        private readonly struct DayCoverageMetrics
        {
            public DayCoverageMetrics(
                int coverage1,
                int coverage2,
                int shift1GapMinutes,
                int shift2GapMinutes,
                int filled1,
                int filled2,
                int unfilledSlots,
                double totalHours,
                bool hasSecondShift)
            {
                Coverage1 = coverage1;
                Coverage2 = coverage2;
                Shift1GapMinutes = shift1GapMinutes;
                Shift2GapMinutes = shift2GapMinutes;
                Filled1 = filled1;
                Filled2 = filled2;
                TotalHours = totalHours;
                HasSecondShift = hasSecondShift;
                CoverageGap = shift1GapMinutes + (hasSecondShift ? shift2GapMinutes : 0);
                UnfilledSlots = unfilledSlots;
            }

            public int Coverage1 { get; }
            public int Coverage2 { get; }
            public int Shift1GapMinutes { get; }
            public int Shift2GapMinutes { get; }
            public int Filled1 { get; }
            public int Filled2 { get; }
            public double TotalHours { get; }
            public int CoverageGap { get; }
            public int UnfilledSlots { get; }
            public bool HasSecondShift { get; }

            public bool IsBetterThan(DayCoverageMetrics other)
            {
                if (CoverageGap != other.CoverageGap)
                    return CoverageGap < other.CoverageGap;

                var worstGap = HasSecondShift ? Math.Max(Shift1GapMinutes, Shift2GapMinutes) : Shift1GapMinutes;
                var otherWorstGap = other.HasSecondShift ? Math.Max(other.Shift1GapMinutes, other.Shift2GapMinutes) : other.Shift1GapMinutes;
                if (worstGap != otherWorstGap)
                    return worstGap < otherWorstGap;

                if (UnfilledSlots != other.UnfilledSlots)
                    return UnfilledSlots < other.UnfilledSlots;

                var totalCoverage = Coverage1 + Coverage2;
                var otherTotalCoverage = other.Coverage1 + other.Coverage2;
                if (totalCoverage != otherTotalCoverage)
                    return totalCoverage > otherTotalCoverage;

                var minCoverage = HasSecondShift ? Math.Min(Coverage1, Coverage2) : Coverage1;
                var otherMinCoverage = other.HasSecondShift ? Math.Min(other.Coverage1, other.Coverage2) : other.Coverage1;
                if (minCoverage != otherMinCoverage)
                    return minCoverage > otherMinCoverage;

                var totalFilled = Filled1 + Filled2;
                var otherTotalFilled = other.Filled1 + other.Filled2;
                if (totalFilled != otherTotalFilled)
                    return totalFilled > otherTotalFilled;

                if (TotalHours > other.TotalHours + EPS)
                    return true;

                if (TotalHours < other.TotalHours - EPS)
                    return false;

                return false;
            }
        }

        private readonly struct ExactDayOption
        {
            public ExactDayOption(
                int stateCode,
                int shift1Slots,
                int shift2Slots,
                int coverage1,
                int coverage2,
                double addedHours,
                double hardGain,
                double softGain,
                int fullDayCount)
            {
                StateCode = stateCode;
                Shift1Slots = shift1Slots;
                Shift2Slots = shift2Slots;
                Coverage1 = coverage1;
                Coverage2 = coverage2;
                AddedHours = addedHours;
                HardGain = hardGain;
                SoftGain = softGain;
                FullDayCount = fullDayCount;
            }

            public int StateCode { get; }
            public int Shift1Slots { get; }
            public int Shift2Slots { get; }
            public int Coverage1 { get; }
            public int Coverage2 { get; }
            public double AddedHours { get; }
            public double HardGain { get; }
            public double SoftGain { get; }
            public int FullDayCount { get; }
        }

        private readonly struct ExactDayScore
        {
            public ExactDayScore(
                int coverage1,
                int coverage2,
                double hardGain,
                double softGain,
                double addedHours,
                int fullDayCount)
            {
                Coverage1 = coverage1;
                Coverage2 = coverage2;
                HardGain = hardGain;
                SoftGain = softGain;
                AddedHours = addedHours;
                FullDayCount = fullDayCount;
            }

            public int Coverage1 { get; }
            public int Coverage2 { get; }
            public double HardGain { get; }
            public double SoftGain { get; }
            public double AddedHours { get; }
            public int FullDayCount { get; }

            public ExactDayScore Add(ExactDayOption option)
                => new(
                    Coverage1 + option.Coverage1,
                    Coverage2 + option.Coverage2,
                    HardGain + option.HardGain,
                    SoftGain + option.SoftGain,
                    AddedHours + option.AddedHours,
                    FullDayCount + option.FullDayCount);

            public bool IsBetterForSameFill(ExactDayScore other)
            {
                var totalCoverage = Coverage1 + Coverage2;
                var otherTotalCoverage = other.Coverage1 + other.Coverage2;
                if (totalCoverage != otherTotalCoverage)
                    return totalCoverage > otherTotalCoverage;

                var minCoverage = Math.Min(Coverage1, Coverage2);
                var otherMinCoverage = Math.Min(other.Coverage1, other.Coverage2);
                if (minCoverage != otherMinCoverage)
                    return minCoverage > otherMinCoverage;

                if (HardGain > other.HardGain + EPS)
                    return true;

                if (HardGain < other.HardGain - EPS)
                    return false;

                if (SoftGain > other.SoftGain + EPS)
                    return true;

                if (SoftGain < other.SoftGain - EPS)
                    return false;

                if (FullDayCount != other.FullDayCount)
                    return FullDayCount < other.FullDayCount;

                if (AddedHours > other.AddedHours + EPS)
                    return true;

                if (AddedHours < other.AddedHours - EPS)
                    return false;

                return false;
            }
        }

        /// <summary>
        /// Asynchronously generates schedule slots for the supplied graph definition.
        /// The public API stays async because callers often invoke generation from web requests
        /// or background jobs, even though the internal algorithm itself is CPU-bound.
        /// </summary>
        public Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress = null,
            CancellationToken ct = default)
            => Task.Run(() => GenerateCore(schedule, availabilities, employees, progress, ct), ct);

        /// <summary>
        /// Synchronous implementation of the generation pipeline.
        /// The method proceeds in clearly separated phases: normalize inputs, build availability
        /// matrices, choose assignments, and finally emit slot models for persistence/export.
        /// </summary>
        private static IList<ScheduleSlotModel> GenerateCore(
            ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress,
            CancellationToken ct)
        {
            // Guards
            if (schedule is null) throw new ArgumentNullException(nameof(schedule));
            if (availabilities is null) throw new ArgumentNullException(nameof(availabilities));
            if (employees is null) throw new ArgumentNullException(nameof(employees));

            if (schedule.Year < 1900 || schedule.Month < 1 || schedule.Month > 12)
                return new List<ScheduleSlotModel>(0);

            if (schedule.PeoplePerShift <= 0)
                return new List<ScheduleSlotModel>(0);

            var shifts = GetShiftTemplates(schedule);
            if (shifts.Count == 0)
                return new List<ScheduleSlotModel>(0);

            var daysInMonth = DateTime.DaysInMonth(schedule.Year, schedule.Month);
            var shiftCount = shifts.Count;                 // 1 or 2
            var pps = schedule.PeoplePerShift;

            var sh1 = shifts[0];
            var sh2 = shiftCount >= 2 ? shifts[1] : null;

            // 1) Unique employees (stable) + MinHoursMonth
            var idToIndex = new Dictionary<int, int>(capacity: 256);
            var employeeIds = new List<int>(capacity: 256);
            var minHoursTmp = new List<double>(capacity: 256);

            foreach (var e in employees)
            {
                var id = e.EmployeeId;
                if (id <= 0) continue;

                var mh = ReadMinHours(e);

                if (idToIndex.TryGetValue(id, out var existing))
                {
                    // keep max min-hours if duplicates exist
                    if (mh > minHoursTmp[existing])
                        minHoursTmp[existing] = mh;
                    continue;
                }

                idToIndex[id] = employeeIds.Count;
                employeeIds.Add(id);
                minHoursTmp.Add(mh);
            }

            var n = employeeIds.Count;
            if (n == 0)
                return new List<ScheduleSlotModel>(0);

            var minHours = minHoursTmp.ToArray();
            var stride = daysInMonth + 1; // for [emp * stride + day]

            // 2) Availability matrices
            //    - unavailable[day*n + emp] = true only when AvailabilityKind.NONE or empty window
            //    - availStartMin / availEndMin describe daily window (minutes from midnight)
            var unavailable = new bool[(daysInMonth + 1) * n];
            var availStartMin = new int[(daysInMonth + 1) * n];
            var availEndMin = new int[(daysInMonth + 1) * n];
            var explicitFullDayAvailability = new bool[(daysInMonth + 1) * n];

            for (var i = 0; i < availStartMin.Length; i++)
            {
                availStartMin[i] = 0;
                availEndMin[i] = 24 * 60;
            }

            foreach (var g in availabilities)
            {
                if (g is null) continue;
                if (g.Year != schedule.Year || g.Month != schedule.Month) continue;

                var members = g.Members;
                if (members is null) continue;

                foreach (var m in members)
                {
                    if (!idToIndex.TryGetValue(m.EmployeeId, out var empIdx))
                        continue;

                    var days = m.Days;
                    if (days is null) continue;

                    foreach (var d in days)
                    {
                        var day = d.DayOfMonth;
                        if (day < 1 || day > daysInMonth) continue;

                        var idx = day * n + empIdx;

                        // NONE blocks completely
                        if (d.Kind == AvailabilityKind.NONE)
                        {
                            unavailable[idx] = true;
                            availStartMin[idx] = 24 * 60;
                            availEndMin[idx] = 0;
                            explicitFullDayAvailability[idx] = false;
                            continue;
                        }

                        // Optional: time window inside a day (e.g. 15:30-24:00 or "09:00 - 15:00")
                        if (TryReadAvailabilityWindow(d, out var fromMin, out var toMin))
                        {
                            fromMin = Clamp(fromMin, 0, 24 * 60);
                            toMin = Clamp(toMin, 0, 24 * 60);
                            if (toMin < fromMin)
                                (fromMin, toMin) = (toMin, fromMin);

                            if (AvailabilityKindRequiresWindow(d.Kind)
                                && shiftCount >= 2
                                && fromMin <= sh1.StartMin
                                && toMin >= sh2!.EndMin)
                            {
                                explicitFullDayAvailability[idx] = true;
                            }

                            // intersection
                            if (fromMin > availStartMin[idx]) availStartMin[idx] = fromMin;
                            if (toMin < availEndMin[idx]) availEndMin[idx] = toMin;

                            if (availEndMin[idx] <= availStartMin[idx])
                            {
                                unavailable[idx] = true;
                                availStartMin[idx] = 24 * 60;
                                availEndMin[idx] = 0;
                                explicitFullDayAvailability[idx] = false;
                            }
                        }
                        else if (AvailabilityKindRequiresWindow(d.Kind))
                        {
                            // Strict mode: interval-like availability without a readable interval
                            // is treated as not schedulable rather than "ANY" (0..24).
                            unavailable[idx] = true;
                            availStartMin[idx] = 24 * 60;
                            availEndMin[idx] = 0;
                            explicitFullDayAvailability[idx] = false;
                        }
                    }
                }
            }

            // 3) Available employees per day + scarcity (how many days they are available)
            var availableByDay = new int[daysInMonth + 1][];
            var scarcityDays = new int[n];

            for (var day = 1; day <= daysInMonth; day++)
            {
                var basePos = day * n;

                var cnt = 0;
                for (var i = 0; i < n; i++)
                    if (!unavailable[basePos + i]) cnt++;

                var arr = new int[cnt];
                var p = 0;
                for (var i = 0; i < n; i++)
                {
                    if (!unavailable[basePos + i])
                    {
                        arr[p++] = i;
                        scarcityDays[i]++; // #available days (smaller => scarcer)
                    }
                }

                availableByDay[day] = arr;
            }

            var remainingPotentialHours = new double[n * stride];
            var totalPotentialHours = new double[n];
            BuildPotentialHours(
                daysInMonth,
                shifts,
                unavailable,
                availStartMin,
                availEndMin,
                stride,
                n,
                remainingPotentialHours,
                totalPotentialHours);

            var desiredHours = BuildDesiredHours(
                minHours,
                totalPotentialHours,
                schedule.MaxHoursPerEmpMonth,
                shifts,
                daysInMonth,
                pps);

            var unavailableBoundaryPressure = BuildUnavailableBoundaryPressure(
                daysInMonth,
                unavailable,
                stride,
                n);

            // 4) Internal schedule storage: assigned slot -> empIdx or -1
            var totalSlots = daysInMonth * shiftCount * pps;
            var assigned = new int[totalSlots];
            Array.Fill(assigned, -1);

            // Per-slot actual timing and hours (hours == 0 when UNFURNISHED)
            var slotFromMin = new int[totalSlots];
            var slotToMin = new int[totalSlots];
            var slotHours = new double[totalSlots];
            InitSlotBaseTimes(daysInMonth, shiftCount, pps, shifts, slotFromMin, slotToMin);
            // slotHours default is 0 for all

            // Stats used by both phases:
            var totalHours = new double[n];
            var shiftsPerDay = new int[n * stride];    // 0..shiftCount
            var fullDaysCount = new int[n];            // days with 2 shifts

            // Phase-1 fast streak state (forward-only, O(1))
            var lastWorkedDay = new int[n];
            var consecutiveDays = new int[n];
            var lastFullDay = new int[n];
            var consecutiveFullDays = new int[n];
            var shiftsToday = new int[n];

            // per-shift stamp to prevent duplicates within same shift
            var assignedStamp = new int[n];
            var stamp = 1;

            var rrCursor = 0;

            progress?.Report(0);

            // =========================
            // PHASE 1: fast greedy (fair)
            // =========================
            for (var day = 1; day <= daysInMonth; day++)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report((int)Math.Round(day * 70d / daysInMonth));

                Array.Clear(shiftsToday, 0, n);

                var availableToday = availableByDay[day];
                if (availableToday.Length == 0)
                    continue;

                for (var s = 0; s < shiftCount; s++)
                {
                    stamp = NextStamp(assignedStamp, stamp);

                    // mark already assigned in this shift
                    PremarkAssignedInShift(assigned, day, s, pps, shiftCount, assignedStamp, stamp);

                    var preferNoSecondShift = (shiftCount >= 2 && shifts[s].Index == 2);

                    for (var slot = 0; slot < pps; slot++)
                    {
                        var pos = SlotIndex(day, s, slot, pps, shiftCount);
                        if (assigned[pos] >= 0) continue;

                        var chosen = PickCandidatePhase1Fast(
                            availableToday,
                            day,
                            s,
                            slot,
                            preferNoSecondShift,
                            schedule,
                            minHours,
                            desiredHours,
                            remainingPotentialHours,
                            scarcityDays,
                            unavailableBoundaryPressure,
                            totalHours,
                            fullDaysCount,
                            shiftsToday,
                            lastWorkedDay,
                            consecutiveDays,
                            lastFullDay,
                            consecutiveFullDays,
                            shiftCount,
                            stride,
                            unavailable,
                            explicitFullDayAvailability,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            availStartMin,
                            availEndMin,
                            shifts,
                            n,
                            pps,
                            assignedStamp,
                            stamp,
                            rrCursor);

                        if (chosen >= 0)
                        {
                            rrCursor = (chosen + 1) % n;

                            AssignPhase1Fast(
                                day, s, slot, chosen,
                                assigned,
                                slotFromMin, slotToMin, slotHours,
                                availStartMin, availEndMin,
                                shifts,
                                totalHours,
                                shiftsPerDay,
                                fullDaysCount,
                                shiftsToday,
                                lastWorkedDay,
                                consecutiveDays,
                                lastFullDay,
                                consecutiveFullDays,
                                shiftCount,
                                stride,
                                pps,
                                n,
                                assignedStamp,
                                stamp);
                        }
                    }
                }
            }

            // =========================================
            // PHASE 2: strict repair + minimize conflicts
            // 2A) fill UNFURNISHED slots first (reduces conflicts)
            // 2B) then strict MinHours (fill UNFURNISHED for deficits, then swap)
            // All checks are ORDER-INDEPENDENT (scan left/right).
            // =========================================
            progress?.Report(75);

            FillAllUnfurnishedOrderIndependent(
                schedule,
                shifts,
                availableByDay,
                minHours,
                desiredHours,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                scarcityDays,
                remainingPotentialHours,
                unavailableBoundaryPressure,
                ref rrCursor,
                dayOrder: null,
                ct);

            progress?.Report(85);

            StrictMinHoursRepairMinConflicts(
                schedule,
                shifts,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                minHours,
                desiredHours,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                remainingPotentialHours,
                unavailableBoundaryPressure,
                n,
                ct);

            TryImproveScheduleWithExactConflictOptimization(
                schedule,
                shifts,
                availableByDay,
                minHours,
                desiredHours,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n,
                ct);

            TryImproveScheduleWithConflictPairRebuild(
                schedule,
                shifts,
                availableByDay,
                minHours,
                desiredHours,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                remainingPotentialHours,
                scarcityDays,
                unavailableBoundaryPressure,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n,
                ct);

            TryImproveScheduleWithConflictWindowRebuild(
                schedule,
                shifts,
                availableByDay,
                minHours,
                desiredHours,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                remainingPotentialHours,
                scarcityDays,
                unavailableBoundaryPressure,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n,
                ct);

            TryImproveScheduleWithFairnessWindowRebuild(
                schedule,
                shifts,
                availableByDay,
                minHours,
                desiredHours,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                remainingPotentialHours,
                scarcityDays,
                unavailableBoundaryPressure,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n,
                ct);

            var baselineScore = EvaluateAttemptScore(
                shifts,
                minHours,
                desiredHours,
                unavailable,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n);

            if (baselineScore.ConflictDays > 0 || baselineScore.CoverageGap > 0)
            {
                progress?.Report(92);

                var improvedAttempt = ExploreAdditionalSchedules(
                    baselineScore,
                    schedule,
                    shifts,
                    availableByDay,
                    minHours,
                    desiredHours,
                    unavailable,
                    explicitFullDayAvailability,
                    availStartMin,
                    availEndMin,
                    remainingPotentialHours,
                    scarcityDays,
                    unavailableBoundaryPressure,
                    daysInMonth,
                    shiftCount,
                    pps,
                    stride,
                    n,
                    ct);

                if (improvedAttempt is not null && improvedAttempt.Score.IsBetterThan(baselineScore))
                {
                    Array.Copy(improvedAttempt.Assigned, assigned, assigned.Length);
                    Array.Copy(improvedAttempt.SlotFromMin, slotFromMin, slotFromMin.Length);
                    Array.Copy(improvedAttempt.SlotToMin, slotToMin, slotToMin.Length);
                    Array.Copy(improvedAttempt.SlotHours, slotHours, slotHours.Length);
                    Array.Copy(improvedAttempt.TotalHours, totalHours, totalHours.Length);
                    Array.Copy(improvedAttempt.ShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                    Array.Copy(improvedAttempt.FullDaysCount, fullDaysCount, fullDaysCount.Length);

                    TryImproveScheduleWithConflictPairRebuild(
                        schedule,
                        shifts,
                        availableByDay,
                        minHours,
                        desiredHours,
                        unavailable,
                        explicitFullDayAvailability,
                        availStartMin,
                        availEndMin,
                        remainingPotentialHours,
                        scarcityDays,
                        unavailableBoundaryPressure,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        totalHours,
                        shiftsPerDay,
                        fullDaysCount,
                        daysInMonth,
                        shiftCount,
                        pps,
                        stride,
                        n,
                        ct);

            TryImproveScheduleWithConflictWindowRebuild(
                schedule,
                shifts,
                availableByDay,
                minHours,
                desiredHours,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                        remainingPotentialHours,
                        scarcityDays,
                        unavailableBoundaryPressure,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        totalHours,
                        shiftsPerDay,
                        fullDaysCount,
                        daysInMonth,
                        shiftCount,
                        pps,
                        stride,
                        n,
                        ct);

            TryImproveScheduleWithFairnessWindowRebuild(
                schedule,
                shifts,
                availableByDay,
                minHours,
                desiredHours,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                        remainingPotentialHours,
                        scarcityDays,
                        unavailableBoundaryPressure,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        totalHours,
                        shiftsPerDay,
                        fullDaysCount,
                        daysInMonth,
                        shiftCount,
                        pps,
                        stride,
                        n,
                        ct);
                }
            }

            progress?.Report(100);

            // Convert to ScheduleSlotModel list
            var result = new List<ScheduleSlotModel>(capacity: totalSlots);

            for (var day = 1; day <= daysInMonth; day++)
            {
                for (var s = 0; s < shiftCount; s++)
                {
                    var sh = shifts[s];
                    var basePos = SlotBase(day, s, pps, shiftCount);

                    for (var slotNo = 1; slotNo <= pps; slotNo++)
                    {
                        var pos = basePos + (slotNo - 1);
                        var empIdx = assigned[pos];

                        var slotModel = new ScheduleSlotModel
                        {
                            DayOfMonth = day,
                            SlotNo = slotNo,
                            FromTime = empIdx >= 0 ? MinutesToHHmm(slotFromMin[pos]) : sh.From,
                            ToTime = empIdx >= 0 ? MinutesToHHmm(slotToMin[pos]) : sh.To,
                            Status = empIdx >= 0 ? SlotStatus.ASSIGNED : SlotStatus.UNFURNISHED
                        };

                        if (empIdx >= 0)
                            slotModel.EmployeeId = employeeIds[empIdx];

                        result.Add(slotModel);
                    }
                }
            }

            return result;
        }

        // =========================
        // Phase 2A: Fill ALL unfurnished (minimize conflicts)
        // =========================
        private static void FillAllUnfurnishedOrderIndependent(
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[][] availableByDay,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int[] scarcityDays,
            double[] remainingPotentialHours,
            int[] unavailableBoundaryPressure,
            ref int rrCursor,
            int[]? dayOrder,
            CancellationToken ct)
        {
            // Greedy fill: for each empty slot choose best candidate (deficit-first, then fairness).
            // Uses order-independent constraint check.

            var n = totalHours.Length;
            var assignedStamp = new int[n];
            var stamp = 1;
            var orderedDays = dayOrder;

            for (var dayIdx = 0; dayIdx < daysInMonth; dayIdx++)
            {
                ct.ThrowIfCancellationRequested();
                var day = orderedDays is not null ? orderedDays[dayIdx] : dayIdx + 1;

                var availableToday = availableByDay[day];
                if (availableToday.Length == 0)
                    continue;

                for (var s = 0; s < shiftCount; s++)
                {
                    stamp = NextStamp(assignedStamp, stamp);
                    PremarkAssignedInShift(assigned, day, s, pps, shiftCount, assignedStamp, stamp);

                    var preferNoSecondShift = (shiftCount >= 2 && shifts[s].Index == 2);

                    var basePos = SlotBase(day, s, pps, shiftCount);

                    for (var slot = 0; slot < pps; slot++)
                    {
                        var pos = basePos + slot;
                        if (assigned[pos] >= 0) continue;

                        // two-pass for Shift2 to avoid full-day, then allow
                        var chosen = FindBestOrderIndependent(
                            availableToday,
                            day,
                            s,
                            slot,
                            restrictSecondShift: preferNoSecondShift,
                            schedule,
                            minHours,
                            desiredHours,
                            remainingPotentialHours,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            scarcityDays,
                            unavailableBoundaryPressure,
                            shifts,
                            n,
                            assignedStamp,
                            stamp,
                            rrCursor);

                        if (chosen < 0 && preferNoSecondShift)
                        {
                            chosen = FindBestOrderIndependent(
                                availableToday,
                                day,
                                s,
                                slot,
                                restrictSecondShift: false,
                                schedule,
                                minHours,
                                desiredHours,
                                remainingPotentialHours,
                                unavailable,
                                explicitFullDayAvailability,
                                availStartMin,
                                availEndMin,
                                assigned,
                                slotFromMin,
                                slotToMin,
                                slotHours,
                                totalHours,
                                shiftsPerDay,
                                fullDaysCount,
                                daysInMonth,
                                shiftCount,
                                pps,
                                stride,
                                scarcityDays,
                                unavailableBoundaryPressure,
                                shifts,
                                n,
                                assignedStamp,
                                stamp,
                                rrCursor);
                        }

                        if (chosen >= 0)
                        {
                            rrCursor = (chosen + 1) % n;

                            AssignToEmptySlot(day, s, slot, chosen,
                                assigned,
                                slotFromMin, slotToMin, slotHours,
                                availStartMin, availEndMin,
                                shifts,
                                totalHours,
                                shiftsPerDay,
                                fullDaysCount,
                                shiftCount,
                                stride,
                                pps,
                                n);

                            assignedStamp[chosen] = stamp;
                        }
                    }
                }
            }
        }

        private static ScheduleAttemptState? ExploreAdditionalSchedules(
            AttemptScore baselineScore,
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[][] availableByDay,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            double[] remainingPotentialHours,
            int[] scarcityDays,
            int[] unavailableBoundaryPressure,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n,
            CancellationToken ct)
        {
            if (daysInMonth <= 0 || shiftCount <= 0 || pps <= 0 || n <= 0)
                return null;

            var dayOrders = BuildExplorationDayOrders(
                shifts,
                unavailable,
                availStartMin,
                availEndMin,
                daysInMonth,
                shiftCount,
                pps,
                n);

            var rrSeeds = BuildRoundRobinSeeds(n);

            var bestScore = baselineScore;
            ScheduleAttemptState? bestState = null;

            for (var orderIndex = 0; orderIndex < dayOrders.Count; orderIndex++)
            {
                var dayOrder = dayOrders[orderIndex];

                for (var seedIndex = 0; seedIndex < rrSeeds.Count; seedIndex++)
                {
                    ct.ThrowIfCancellationRequested();

                    var candidate = RunExplorationAttempt(
                        dayOrder,
                        rrSeeds[seedIndex],
                        schedule,
                        shifts,
                        availableByDay,
                        minHours,
                        desiredHours,
                        unavailable,
                        explicitFullDayAvailability,
                        availStartMin,
                        availEndMin,
                        remainingPotentialHours,
                        scarcityDays,
                        unavailableBoundaryPressure,
                        daysInMonth,
                        shiftCount,
                        pps,
                        stride,
                        n,
                        ct);

                    if (!candidate.Score.IsBetterThan(bestScore))
                        continue;

                    bestScore = candidate.Score;
                    bestState = candidate;

                    if (bestScore.ConflictDays == 0
                        && bestScore.CoverageGap == 0
                        && bestScore.MinHourDeficit <= EPS)
                    {
                        return bestState;
                    }
                }
            }

            return bestState;
        }

        private static List<int> BuildRoundRobinSeeds(int employeeCount)
        {
            var seeds = new List<int>(capacity: Math.Max(1, employeeCount));
            if (employeeCount <= 0)
                return seeds;

            if (employeeCount <= 12)
            {
                for (var i = 0; i < employeeCount; i++)
                    seeds.Add(i);

                return seeds;
            }

            seeds.Add(0);
            var candidateSeeds = new[]
            {
                employeeCount / 4,
                employeeCount / 2,
                (employeeCount * 3) / 4,
                employeeCount - 1
            };

            for (var i = 0; i < candidateSeeds.Length; i++)
            {
                var seed = candidateSeeds[i];
                if (seed <= 0 || seed >= employeeCount || seeds.Contains(seed))
                    continue;

                seeds.Add(seed);
            }

            return seeds;
        }

        private static ScheduleAttemptState RunExplorationAttempt(
            int[] dayOrder,
            int rrSeed,
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[][] availableByDay,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            double[] remainingPotentialHours,
            int[] scarcityDays,
            int[] unavailableBoundaryPressure,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n,
            CancellationToken ct)
        {
            var totalSlots = daysInMonth * shiftCount * pps;

            var assigned = new int[totalSlots];
            Array.Fill(assigned, -1);

            var slotFromMin = new int[totalSlots];
            var slotToMin = new int[totalSlots];
            var slotHours = new double[totalSlots];
            InitSlotBaseTimes(daysInMonth, shiftCount, pps, shifts, slotFromMin, slotToMin);

            var totalHours = new double[n];
            var shiftsPerDay = new int[n * stride];
            var fullDaysCount = new int[n];

            var rrCursor = n <= 0 ? 0 : Clamp(rrSeed, 0, Math.Max(0, n - 1));

            FillAllUnfurnishedOrderIndependent(
                schedule,
                shifts,
                availableByDay,
                minHours,
                desiredHours,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                scarcityDays,
                remainingPotentialHours,
                unavailableBoundaryPressure,
                ref rrCursor,
                dayOrder,
                ct);

            StrictMinHoursRepairMinConflicts(
                schedule,
                shifts,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                minHours,
                desiredHours,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                remainingPotentialHours,
                unavailableBoundaryPressure,
                n,
                ct);

            TryImproveScheduleWithExactConflictOptimization(
                schedule,
                shifts,
                availableByDay,
                minHours,
                desiredHours,
                unavailable,
                explicitFullDayAvailability,
                availStartMin,
                availEndMin,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n,
                ct);

            var score = EvaluateAttemptScore(
                shifts,
                minHours,
                desiredHours,
                unavailable,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n);

            return new ScheduleAttemptState(
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                score);
        }

        private static List<int[]> BuildExplorationDayOrders(
            List<ShiftTemplate> shifts,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int daysInMonth,
            int shiftCount,
            int pps,
            int n)
        {
            var orders = new List<int[]>(capacity: 6)
            {
                BuildSequentialDayOrder(daysInMonth),
                BuildReverseDayOrder(daysInMonth),
                BuildHardestDayFirstOrder(
                    shifts,
                    unavailable,
                    availStartMin,
                    availEndMin,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: false),
                BuildHardestDayFirstOrder(
                    shifts,
                    unavailable,
                    availStartMin,
                    availEndMin,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: true),
                BuildMostConstrainedPairOrder(
                    shifts,
                    unavailable,
                    availStartMin,
                    availEndMin,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: false),
                BuildMostConstrainedPairOrder(
                    shifts,
                    unavailable,
                    availStartMin,
                    availEndMin,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: true)
            };

            return DeduplicateDayOrders(orders);
        }

        private static List<int[]> BuildExactOptimizationDayOrders(
            List<ShiftTemplate> shifts,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int daysInMonth,
            int shiftCount,
            int pps,
            int n)
        {
            var orders = new List<int[]>(capacity: 8)
            {
                BuildCurrentConflictFirstOrder(
                    shifts,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: false),
                BuildCurrentConflictFirstOrder(
                    shifts,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: true),
                BuildSequentialDayOrder(daysInMonth),
                BuildReverseDayOrder(daysInMonth),
                BuildHardestDayFirstOrder(
                    shifts,
                    unavailable,
                    availStartMin,
                    availEndMin,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: false),
                BuildHardestDayFirstOrder(
                    shifts,
                    unavailable,
                    availStartMin,
                    availEndMin,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: true),
                BuildMostConstrainedPairOrder(
                    shifts,
                    unavailable,
                    availStartMin,
                    availEndMin,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: false),
                BuildMostConstrainedPairOrder(
                    shifts,
                    unavailable,
                    availStartMin,
                    availEndMin,
                    daysInMonth,
                    shiftCount,
                    pps,
                    n,
                    tieBreakReverse: true)
            };

            return DeduplicateDayOrders(orders);
        }

        private static List<int[]> DeduplicateDayOrders(List<int[]> orders)
        {
            var deduplicated = new List<int[]>(orders.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.Length == 0)
                    continue;

                var key = string.Join(",", order);
                if (!seen.Add(key))
                    continue;

                deduplicated.Add(order);
            }

            return deduplicated;
        }

        private static int[] BuildSequentialDayOrder(int daysInMonth)
        {
            var order = new int[daysInMonth];
            for (var i = 0; i < daysInMonth; i++)
                order[i] = i + 1;

            return order;
        }

        private static int[] BuildReverseDayOrder(int daysInMonth)
        {
            var order = new int[daysInMonth];
            for (var i = 0; i < daysInMonth; i++)
                order[i] = daysInMonth - i;

            return order;
        }

        private static int[] BuildCurrentConflictFirstOrder(
            List<ShiftTemplate> shifts,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int daysInMonth,
            int shiftCount,
            int pps,
            int n,
            bool tieBreakReverse)
        {
            var order = BuildSequentialDayOrder(daysInMonth);
            Array.Sort(order, (leftDay, rightDay) =>
            {
                var leftMetrics = ComputeDayCoverageMetrics(
                    leftDay,
                    shifts,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    shiftCount,
                    pps,
                    n);
                var rightMetrics = ComputeDayCoverageMetrics(
                    rightDay,
                    shifts,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    shiftCount,
                    pps,
                    n);

                var byCoverageGap = rightMetrics.CoverageGap.CompareTo(leftMetrics.CoverageGap);
                if (byCoverageGap != 0)
                    return byCoverageGap;

                var byUnfilled = rightMetrics.UnfilledSlots.CompareTo(leftMetrics.UnfilledSlots);
                if (byUnfilled != 0)
                    return byUnfilled;

                var leftTotalCoverage = leftMetrics.Coverage1 + leftMetrics.Coverage2;
                var rightTotalCoverage = rightMetrics.Coverage1 + rightMetrics.Coverage2;
                var byTotalCoverage = leftTotalCoverage.CompareTo(rightTotalCoverage);
                if (byTotalCoverage != 0)
                    return byTotalCoverage;

                return tieBreakReverse ? rightDay.CompareTo(leftDay) : leftDay.CompareTo(rightDay);
            });

            return order;
        }

        private static int[] BuildHardestDayFirstOrder(
            List<ShiftTemplate> shifts,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int daysInMonth,
            int shiftCount,
            int pps,
            int n,
            bool tieBreakReverse)
        {
            var order = BuildSequentialDayOrder(daysInMonth);
            Array.Sort(order, (leftDay, rightDay) =>
            {
                ComputeDayOrderingMetrics(leftDay, shifts, unavailable, availStartMin, availEndMin, shiftCount, n, pps, out var leftMinShift, out var leftTotalCoverage, out var leftUnion, out var leftPair, out var leftRequiredFull);
                ComputeDayOrderingMetrics(rightDay, shifts, unavailable, availStartMin, availEndMin, shiftCount, n, pps, out var rightMinShift, out var rightTotalCoverage, out var rightUnion, out var rightPair, out var rightRequiredFull);

                var byMinShift = leftMinShift.CompareTo(rightMinShift);
                if (byMinShift != 0)
                    return byMinShift;

                var byRequiredFull = rightRequiredFull.CompareTo(leftRequiredFull);
                if (byRequiredFull != 0)
                    return byRequiredFull;

                var byTotalCoverage = leftTotalCoverage.CompareTo(rightTotalCoverage);
                if (byTotalCoverage != 0)
                    return byTotalCoverage;

                var byUnion = leftUnion.CompareTo(rightUnion);
                if (byUnion != 0)
                    return byUnion;

                var byPair = leftPair.CompareTo(rightPair);
                if (byPair != 0)
                    return byPair;

                return tieBreakReverse ? rightDay.CompareTo(leftDay) : leftDay.CompareTo(rightDay);
            });

            return order;
        }

        private static int[] BuildMostConstrainedPairOrder(
            List<ShiftTemplate> shifts,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int daysInMonth,
            int shiftCount,
            int pps,
            int n,
            bool tieBreakReverse)
        {
            var order = BuildSequentialDayOrder(daysInMonth);
            Array.Sort(order, (leftDay, rightDay) =>
            {
                ComputeDayOrderingMetrics(leftDay, shifts, unavailable, availStartMin, availEndMin, shiftCount, n, pps, out var leftMinShift, out var leftTotalCoverage, out var leftUnion, out var leftPair, out var leftRequiredFull);
                ComputeDayOrderingMetrics(rightDay, shifts, unavailable, availStartMin, availEndMin, shiftCount, n, pps, out var rightMinShift, out var rightTotalCoverage, out var rightUnion, out var rightPair, out var rightRequiredFull);

                var byRequiredFull = rightRequiredFull.CompareTo(leftRequiredFull);
                if (byRequiredFull != 0)
                    return byRequiredFull;

                var byPair = leftPair.CompareTo(rightPair);
                if (byPair != 0)
                    return byPair;

                var byMinShift = leftMinShift.CompareTo(rightMinShift);
                if (byMinShift != 0)
                    return byMinShift;

                var byUnion = leftUnion.CompareTo(rightUnion);
                if (byUnion != 0)
                    return byUnion;

                var byTotalCoverage = leftTotalCoverage.CompareTo(rightTotalCoverage);
                if (byTotalCoverage != 0)
                    return byTotalCoverage;

                return tieBreakReverse ? rightDay.CompareTo(leftDay) : leftDay.CompareTo(rightDay);
            });

            return order;
        }

        private static void ComputeDayOrderingMetrics(
            int day,
            List<ShiftTemplate> shifts,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int shiftCount,
            int n,
            int pps,
            out int minShiftCoverage,
            out int totalCoverage,
            out int unionCoverage,
            out int pairCoverage,
            out int requiredFullDayWorkers)
        {
            totalCoverage = 0;
            unionCoverage = 0;
            pairCoverage = 0;
            minShiftCoverage = int.MaxValue;

            for (var emp = 0; emp < n; emp++)
            {
                var coversAny = false;
                var coversAll = true;

                for (var shiftIdx = 0; shiftIdx < shiftCount; shiftIdx++)
                {
                    var coversShift = CanFullyCoverShiftTemplate(day, emp, shifts[shiftIdx], unavailable, availStartMin, availEndMin, n);
                    if (coversShift)
                    {
                        totalCoverage++;
                        coversAny = true;
                    }
                    else
                    {
                        coversAll = false;
                    }
                }

                if (coversAny)
                    unionCoverage++;

                if (shiftCount > 1 && coversAll)
                    pairCoverage++;
            }

            for (var shiftIdx = 0; shiftIdx < shiftCount; shiftIdx++)
            {
                var shiftCoverage = 0;
                for (var emp = 0; emp < n; emp++)
                {
                    if (CanFullyCoverShiftTemplate(day, emp, shifts[shiftIdx], unavailable, availStartMin, availEndMin, n))
                        shiftCoverage++;
                }

                if (shiftCoverage < minShiftCoverage)
                    minShiftCoverage = shiftCoverage;
            }

            if (shiftCount <= 1)
            {
                requiredFullDayWorkers = 0;
                return;
            }

            requiredFullDayWorkers = Math.Max(0, shiftCount * pps - unionCoverage);
        }

        private static bool CanFullyCoverShiftTemplate(
            int day,
            int emp,
            ShiftTemplate shift,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int n)
        {
            var idx = day * n + emp;
            if (unavailable[idx])
                return false;

            return availStartMin[idx] <= shift.StartMin && availEndMin[idx] >= shift.EndMin;
        }

        private static bool CanUseExplicitFullDayOverride(
            int emp,
            int day,
            double[] minHours,
            double[] totalHours,
            bool[] explicitFullDayAvailability,
            int shiftCount)
        {
            if (shiftCount < 2)
                return false;

            if (minHours[emp] - totalHours[emp] <= EPS)
                return false;

            return explicitFullDayAvailability[day * minHours.Length + emp];
        }

        private static AttemptScore EvaluateAttemptScore(
            List<ShiftTemplate> shifts,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n)
        {
            var conflictDays = 0;
            var coverageGap = 0;
            var unfurnishedSlots = 0;
            var overlapDays = 0;

            for (var day = 1; day <= daysInMonth; day++)
            {
                var hasGapOrOverlap = false;
                var hasOverlap = false;

                for (var emp = 0; emp < n && !hasOverlap; emp++)
                {
                    var intervalCount = 0;
                    var firstFrom = 0;
                    var firstTo = 0;
                    var secondFrom = 0;
                    var secondTo = 0;

                    for (var shiftIdx = 0; shiftIdx < shiftCount; shiftIdx++)
                    {
                        for (var slotIdx = 0; slotIdx < pps; slotIdx++)
                        {
                            var pos = SlotIndex(day, shiftIdx, slotIdx, pps, shiftCount);
                            if (assigned[pos] != emp)
                                continue;

                            var from = slotFromMin[pos];
                            var to = slotToMin[pos];

                            if (intervalCount == 0)
                            {
                                firstFrom = from;
                                firstTo = to;
                            }
                            else if (intervalCount == 1)
                            {
                                secondFrom = from;
                                secondTo = to;
                            }
                            else
                            {
                                hasOverlap = true;
                                break;
                            }

                            intervalCount++;
                        }

                        if (hasOverlap)
                            break;
                    }

                    if (hasOverlap || intervalCount < 2)
                        continue;

                    if (firstFrom < secondTo && secondFrom < firstTo)
                        hasOverlap = true;
                }

                if (hasOverlap)
                {
                    overlapDays++;
                    hasGapOrOverlap = true;
                }

                var dayMetrics = ComputeDayCoverageMetrics(
                    day,
                    shifts,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    shiftCount,
                    pps,
                    n);

                coverageGap += dayMetrics.CoverageGap;
                unfurnishedSlots += dayMetrics.UnfilledSlots;

                if (dayMetrics.CoverageGap > 0)
                    hasGapOrOverlap = true;

                if (hasGapOrOverlap)
                    conflictDays++;
            }

            var minHourDeficit = 0.0;
            var minHourSquaredDeficit = 0.0;
            var desiredHourDeficit = 0.0;
            var desiredHourSquaredDeficit = 0.0;

            for (var emp = 0; emp < n; emp++)
            {
                var hardDeficit = Math.Max(0.0, minHours[emp] - totalHours[emp]);
                if (hardDeficit > EPS)
                {
                    minHourDeficit += hardDeficit;
                    minHourSquaredDeficit += hardDeficit * hardDeficit;
                }

                var softDeficit = Math.Max(0.0, desiredHours[emp] - totalHours[emp]);
                if (softDeficit > EPS)
                {
                    desiredHourDeficit += softDeficit;
                    desiredHourSquaredDeficit += softDeficit * softDeficit;
                }
            }

            var restPenalty = ComputeGeneratedRestPenalty(
                unavailable,
                shiftsPerDay,
                daysInMonth,
                stride,
                n);

            return new AttemptScore(
                conflictDays,
                coverageGap,
                minHourDeficit,
                minHourSquaredDeficit,
                desiredHourDeficit,
                desiredHourSquaredDeficit,
                restPenalty,
                unfurnishedSlots,
                overlapDays);
        }

        private static double ComputeGeneratedRestPenalty(
            bool[] unavailable,
            int[] shiftsPerDay,
            int daysInMonth,
            int stride,
            int n)
        {
            var penalty = 0.0;

            for (var emp = 0; emp < n; emp++)
            {
                var day = 1;
                while (day <= daysInMonth)
                {
                    var isUnavailable = unavailable[day * n + emp];
                    var isWorking = shiftsPerDay[emp * stride + day] > 0;
                    if (isWorking)
                    {
                        day++;
                        continue;
                    }

                    var runLength = 0;
                    var blockedDays = 0;
                    var availableOffDays = 0;

                    while (day <= daysInMonth)
                    {
                        isUnavailable = unavailable[day * n + emp];
                        isWorking = shiftsPerDay[emp * stride + day] > 0;
                        if (isWorking)
                            break;

                        runLength++;
                        if (isUnavailable)
                            blockedDays++;
                        else
                            availableOffDays++;

                        day++;
                    }

                    if (availableOffDays <= 0)
                        continue;

                    penalty += availableOffDays * availableOffDays;
                    if (blockedDays > 0)
                        penalty += blockedDays * availableOffDays;

                    if (runLength > 4)
                        penalty += (runLength - 4) * 0.5;
                }
            }

            return penalty;
        }

        private static int[] BuildGeneratedOffRunPressure(
            bool[] unavailable,
            int[] shiftsPerDay,
            int daysInMonth,
            int stride,
            int n)
        {
            var pressure = new int[n * stride];

            for (var emp = 0; emp < n; emp++)
            {
                var day = 1;
                while (day <= daysInMonth)
                {
                    if (shiftsPerDay[emp * stride + day] > 0)
                    {
                        day++;
                        continue;
                    }

                    var runStart = day;
                    var blockedDays = 0;
                    var availableOffDays = 0;

                    while (day <= daysInMonth && shiftsPerDay[emp * stride + day] == 0)
                    {
                        if (unavailable[day * n + emp])
                            blockedDays++;
                        else
                            availableOffDays++;

                        day++;
                    }

                    if (availableOffDays <= 0)
                        continue;

                    var runLength = day - runStart;
                    var runPressure = (availableOffDays * availableOffDays)
                        + (blockedDays * availableOffDays)
                        + Math.Max(0, runLength - 3) * 2;

                    for (var runDay = runStart; runDay < day; runDay++)
                    {
                        if (unavailable[runDay * n + emp])
                            continue;

                        pressure[emp * stride + runDay] = runPressure;
                    }
                }
            }

            return pressure;
        }

        private static void TryImproveScheduleWithExactConflictOptimization(
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[][] availableByDay,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n,
            CancellationToken ct)
        {
            var originalScore = EvaluateAttemptScore(
                shifts,
                minHours,
                desiredHours,
                unavailable,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n);

            if (originalScore.ConflictDays <= 0 && originalScore.CoverageGap <= 0)
                return;

            var originalAssigned = (int[])assigned.Clone();
            var originalSlotFrom = (int[])slotFromMin.Clone();
            var originalSlotTo = (int[])slotToMin.Clone();
            var originalSlotHours = (double[])slotHours.Clone();
            var originalTotalHours = (double[])totalHours.Clone();
            var originalShiftsPerDay = (int[])shiftsPerDay.Clone();
            var originalFullDays = (int[])fullDaysCount.Clone();

            var bestScore = originalScore;
            int[]? bestAssigned = null;
            int[]? bestSlotFrom = null;
            int[]? bestSlotTo = null;
            double[]? bestSlotHours = null;
            double[]? bestTotalHours = null;
            int[]? bestShiftsPerDay = null;
            int[]? bestFullDays = null;

            var dayOrders = BuildExactOptimizationDayOrders(
                shifts,
                unavailable,
                availStartMin,
                availEndMin,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                daysInMonth,
                shiftCount,
                pps,
                n);

            for (var orderIndex = 0; orderIndex < dayOrders.Count; orderIndex++)
            {
                Array.Copy(originalAssigned, assigned, assigned.Length);
                Array.Copy(originalSlotFrom, slotFromMin, slotFromMin.Length);
                Array.Copy(originalSlotTo, slotToMin, slotToMin.Length);
                Array.Copy(originalSlotHours, slotHours, slotHours.Length);
                Array.Copy(originalTotalHours, totalHours, totalHours.Length);
                Array.Copy(originalShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                Array.Copy(originalFullDays, fullDaysCount, fullDaysCount.Length);

                OptimizeConflictDaysExact(
                    schedule,
                    shifts,
                    availableByDay,
                    minHours,
                    desiredHours,
                    unavailable,
                    explicitFullDayAvailability,
                    availStartMin,
                    availEndMin,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    totalHours,
                    shiftsPerDay,
                    fullDaysCount,
                    daysInMonth,
                    shiftCount,
                    pps,
                    stride,
                    n,
                    dayOrders[orderIndex],
                    ct);

                var candidateScore = EvaluateAttemptScore(
                    shifts,
                    minHours,
                    desiredHours,
                    unavailable,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    totalHours,
                    shiftsPerDay,
                    daysInMonth,
                    shiftCount,
                    pps,
                    stride,
                    n);

                if (!candidateScore.IsBetterThan(bestScore))
                    continue;

                bestScore = candidateScore;
                bestAssigned = (int[])assigned.Clone();
                bestSlotFrom = (int[])slotFromMin.Clone();
                bestSlotTo = (int[])slotToMin.Clone();
                bestSlotHours = (double[])slotHours.Clone();
                bestTotalHours = (double[])totalHours.Clone();
                bestShiftsPerDay = (int[])shiftsPerDay.Clone();
                bestFullDays = (int[])fullDaysCount.Clone();

                if (bestScore.ConflictDays == 0 && bestScore.CoverageGap == 0 && bestScore.MinHourDeficit <= EPS)
                    break;
            }

            if (bestAssigned is null
                || bestSlotFrom is null
                || bestSlotTo is null
                || bestSlotHours is null
                || bestTotalHours is null
                || bestShiftsPerDay is null
                || bestFullDays is null)
            {
                Array.Copy(originalAssigned, assigned, assigned.Length);
                Array.Copy(originalSlotFrom, slotFromMin, slotFromMin.Length);
                Array.Copy(originalSlotTo, slotToMin, slotToMin.Length);
                Array.Copy(originalSlotHours, slotHours, slotHours.Length);
                Array.Copy(originalTotalHours, totalHours, totalHours.Length);
                Array.Copy(originalShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                Array.Copy(originalFullDays, fullDaysCount, fullDaysCount.Length);
                return;
            }

            Array.Copy(bestAssigned, assigned, assigned.Length);
            Array.Copy(bestSlotFrom, slotFromMin, slotFromMin.Length);
            Array.Copy(bestSlotTo, slotToMin, slotToMin.Length);
            Array.Copy(bestSlotHours, slotHours, slotHours.Length);
            Array.Copy(bestTotalHours, totalHours, totalHours.Length);
            Array.Copy(bestShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
            Array.Copy(bestFullDays, fullDaysCount, fullDaysCount.Length);
        }

        private static void TryImproveScheduleWithConflictPairRebuild(
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[][] availableByDay,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            double[] remainingPotentialHours,
            int[] scarcityDays,
            int[] unavailableBoundaryPressure,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n,
            CancellationToken ct)
        {
            var originalScore = EvaluateAttemptScore(
                shifts,
                minHours,
                desiredHours,
                unavailable,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n);

            if (originalScore.ConflictDays <= 0 && originalScore.CoverageGap <= 0)
                return;

            var orderedConflictDays = BuildConflictDayOrderBySeverity(
                shifts,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                daysInMonth,
                shiftCount,
                pps,
                n);

            if (orderedConflictDays.Count == 0)
                return;

            var candidatePairs = BuildConflictDayPairs(orderedConflictDays, daysInMonth);
            if (candidatePairs.Count == 0)
                return;

            var originalAssigned = (int[])assigned.Clone();
            var originalSlotFrom = (int[])slotFromMin.Clone();
            var originalSlotTo = (int[])slotToMin.Clone();
            var originalSlotHours = (double[])slotHours.Clone();
            var originalTotalHours = (double[])totalHours.Clone();
            var originalShiftsPerDay = (int[])shiftsPerDay.Clone();
            var originalFullDays = (int[])fullDaysCount.Clone();

            var bestScore = originalScore;
            int[]? bestAssigned = null;
            int[]? bestSlotFrom = null;
            int[]? bestSlotTo = null;
            double[]? bestSlotHours = null;
            double[]? bestTotalHours = null;
            int[]? bestShiftsPerDay = null;
            int[]? bestFullDays = null;

            var rrSeeds = BuildRoundRobinSeeds(n);
            var localSeedCount = Math.Min(rrSeeds.Count, 3);

            for (var pairIndex = 0; pairIndex < candidatePairs.Count; pairIndex++)
            {
                var (dayA, dayB) = candidatePairs[pairIndex];

                for (var direction = 0; direction < 2; direction++)
                {
                    var firstDay = direction == 0 ? dayA : dayB;
                    var secondDay = direction == 0 ? dayB : dayA;

                    for (var seedIndex = 0; seedIndex < localSeedCount; seedIndex++)
                    {
                        ct.ThrowIfCancellationRequested();

                        Array.Copy(originalAssigned, assigned, assigned.Length);
                        Array.Copy(originalSlotFrom, slotFromMin, slotFromMin.Length);
                        Array.Copy(originalSlotTo, slotToMin, slotToMin.Length);
                        Array.Copy(originalSlotHours, slotHours, slotHours.Length);
                        Array.Copy(originalTotalHours, totalHours, totalHours.Length);
                        Array.Copy(originalShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                        Array.Copy(originalFullDays, fullDaysCount, fullDaysCount.Length);

                        ClearDayAssignments(
                            firstDay,
                            shifts,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            shiftCount,
                            pps,
                            stride,
                            n);

                        if (secondDay != firstDay)
                        {
                            ClearDayAssignments(
                                secondDay,
                                shifts,
                                assigned,
                                slotFromMin,
                                slotToMin,
                                slotHours,
                                totalHours,
                                shiftsPerDay,
                                fullDaysCount,
                                shiftCount,
                                pps,
                                stride,
                                n);
                        }

                        var baseOrder = BuildCurrentConflictFirstOrder(
                            shifts,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            daysInMonth,
                            shiftCount,
                            pps,
                            n,
                            tieBreakReverse: false);
                        var prioritizedOrder = BuildDayOrderWithPriorityPrefix(baseOrder, firstDay, secondDay);
                        var rrCursor = rrSeeds[seedIndex];

                        FillAllUnfurnishedOrderIndependent(
                            schedule,
                            shifts,
                            availableByDay,
                            minHours,
                            desiredHours,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            scarcityDays,
                            remainingPotentialHours,
                            unavailableBoundaryPressure,
                            ref rrCursor,
                            prioritizedOrder,
                            ct);

                        StrictMinHoursRepairMinConflicts(
                            schedule,
                            shifts,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            minHours,
                            desiredHours,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                shiftCount,
                pps,
                stride,
                remainingPotentialHours,
                unavailableBoundaryPressure,
                n,
                ct);

                        TryImproveScheduleWithExactConflictOptimization(
                            schedule,
                            shifts,
                            availableByDay,
                            minHours,
                            desiredHours,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            n,
                            ct);

                var candidateScore = EvaluateAttemptScore(
                    shifts,
                    minHours,
                    desiredHours,
                    unavailable,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    totalHours,
                    shiftsPerDay,
                    daysInMonth,
                    shiftCount,
                    pps,
                    stride,
                    n);

                        if (!candidateScore.IsBetterThan(bestScore))
                            continue;

                        bestScore = candidateScore;
                        bestAssigned = (int[])assigned.Clone();
                        bestSlotFrom = (int[])slotFromMin.Clone();
                        bestSlotTo = (int[])slotToMin.Clone();
                        bestSlotHours = (double[])slotHours.Clone();
                        bestTotalHours = (double[])totalHours.Clone();
                        bestShiftsPerDay = (int[])shiftsPerDay.Clone();
                        bestFullDays = (int[])fullDaysCount.Clone();

                        if (bestScore.ConflictDays == 0
                            && bestScore.CoverageGap == 0
                            && bestScore.MinHourDeficit <= EPS)
                        {
                            Array.Copy(bestAssigned, assigned, assigned.Length);
                            Array.Copy(bestSlotFrom, slotFromMin, slotFromMin.Length);
                            Array.Copy(bestSlotTo, slotToMin, slotToMin.Length);
                            Array.Copy(bestSlotHours, slotHours, slotHours.Length);
                            Array.Copy(bestTotalHours, totalHours, totalHours.Length);
                            Array.Copy(bestShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                            Array.Copy(bestFullDays, fullDaysCount, fullDaysCount.Length);
                            return;
                        }
                    }
                }
            }

            if (bestAssigned is null
                || bestSlotFrom is null
                || bestSlotTo is null
                || bestSlotHours is null
                || bestTotalHours is null
                || bestShiftsPerDay is null
                || bestFullDays is null)
            {
                Array.Copy(originalAssigned, assigned, assigned.Length);
                Array.Copy(originalSlotFrom, slotFromMin, slotFromMin.Length);
                Array.Copy(originalSlotTo, slotToMin, slotToMin.Length);
                Array.Copy(originalSlotHours, slotHours, slotHours.Length);
                Array.Copy(originalTotalHours, totalHours, totalHours.Length);
                Array.Copy(originalShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                Array.Copy(originalFullDays, fullDaysCount, fullDaysCount.Length);
                return;
            }

            Array.Copy(bestAssigned, assigned, assigned.Length);
            Array.Copy(bestSlotFrom, slotFromMin, slotFromMin.Length);
            Array.Copy(bestSlotTo, slotToMin, slotToMin.Length);
            Array.Copy(bestSlotHours, slotHours, slotHours.Length);
            Array.Copy(bestTotalHours, totalHours, totalHours.Length);
            Array.Copy(bestShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
            Array.Copy(bestFullDays, fullDaysCount, fullDaysCount.Length);
        }

        private static void TryImproveScheduleWithConflictWindowRebuild(
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[][] availableByDay,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            double[] remainingPotentialHours,
            int[] scarcityDays,
            int[] unavailableBoundaryPressure,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n,
            CancellationToken ct)
        {
            var originalScore = EvaluateAttemptScore(
                shifts,
                minHours,
                desiredHours,
                unavailable,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n);

            if (originalScore.ConflictDays <= 0 && originalScore.CoverageGap <= 0)
                return;

            var orderedConflictDays = BuildConflictDayOrderBySeverity(
                shifts,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                daysInMonth,
                shiftCount,
                pps,
                n);

            var candidateWindows = BuildConflictWindows(orderedConflictDays, daysInMonth);
            if (candidateWindows.Count == 0)
                return;

            var originalAssigned = (int[])assigned.Clone();
            var originalSlotFrom = (int[])slotFromMin.Clone();
            var originalSlotTo = (int[])slotToMin.Clone();
            var originalSlotHours = (double[])slotHours.Clone();
            var originalTotalHours = (double[])totalHours.Clone();
            var originalShiftsPerDay = (int[])shiftsPerDay.Clone();
            var originalFullDays = (int[])fullDaysCount.Clone();

            var bestScore = originalScore;
            int[]? bestAssigned = null;
            int[]? bestSlotFrom = null;
            int[]? bestSlotTo = null;
            double[]? bestSlotHours = null;
            double[]? bestTotalHours = null;
            int[]? bestShiftsPerDay = null;
            int[]? bestFullDays = null;

            var rrSeeds = BuildRoundRobinSeeds(n);
            var localSeedCount = Math.Min(rrSeeds.Count, 3);

            for (var windowIndex = 0; windowIndex < candidateWindows.Count; windowIndex++)
            {
                var priorityDays = candidateWindows[windowIndex];
                var reversedPriorityDays = (int[])priorityDays.Clone();
                Array.Reverse(reversedPriorityDays);

                for (var direction = 0; direction < 2; direction++)
                {
                    var orderedPriorityDays = direction == 0 ? priorityDays : reversedPriorityDays;

                    for (var seedIndex = 0; seedIndex < localSeedCount; seedIndex++)
                    {
                        ct.ThrowIfCancellationRequested();

                        Array.Copy(originalAssigned, assigned, assigned.Length);
                        Array.Copy(originalSlotFrom, slotFromMin, slotFromMin.Length);
                        Array.Copy(originalSlotTo, slotToMin, slotToMin.Length);
                        Array.Copy(originalSlotHours, slotHours, slotHours.Length);
                        Array.Copy(originalTotalHours, totalHours, totalHours.Length);
                        Array.Copy(originalShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                        Array.Copy(originalFullDays, fullDaysCount, fullDaysCount.Length);

                        for (var i = 0; i < orderedPriorityDays.Length; i++)
                        {
                            ClearDayAssignments(
                                orderedPriorityDays[i],
                                shifts,
                                assigned,
                                slotFromMin,
                                slotToMin,
                                slotHours,
                                totalHours,
                                shiftsPerDay,
                                fullDaysCount,
                                shiftCount,
                                pps,
                                stride,
                                n);
                        }

                        var baseOrder = BuildCurrentConflictFirstOrder(
                            shifts,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            daysInMonth,
                            shiftCount,
                            pps,
                            n,
                            tieBreakReverse: false);
                        var prioritizedOrder = BuildDayOrderWithPriorityPrefix(baseOrder, orderedPriorityDays);
                        var rrCursor = rrSeeds[seedIndex];

                        FillAllUnfurnishedOrderIndependent(
                            schedule,
                            shifts,
                            availableByDay,
                            minHours,
                            desiredHours,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            scarcityDays,
                            remainingPotentialHours,
                            unavailableBoundaryPressure,
                            ref rrCursor,
                            prioritizedOrder,
                            ct);

                        StrictMinHoursRepairMinConflicts(
                            schedule,
                            shifts,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            minHours,
                            desiredHours,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            remainingPotentialHours,
                            unavailableBoundaryPressure,
                            n,
                            ct);

                        TryImproveScheduleWithExactConflictOptimization(
                            schedule,
                            shifts,
                            availableByDay,
                            minHours,
                            desiredHours,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            n,
                            ct);

                var candidateScore = EvaluateAttemptScore(
                    shifts,
                    minHours,
                    desiredHours,
                    unavailable,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    totalHours,
                    shiftsPerDay,
                    daysInMonth,
                    shiftCount,
                    pps,
                    stride,
                    n);

                        if (!candidateScore.IsBetterThan(bestScore))
                            continue;

                        bestScore = candidateScore;
                        bestAssigned = (int[])assigned.Clone();
                        bestSlotFrom = (int[])slotFromMin.Clone();
                        bestSlotTo = (int[])slotToMin.Clone();
                        bestSlotHours = (double[])slotHours.Clone();
                        bestTotalHours = (double[])totalHours.Clone();
                        bestShiftsPerDay = (int[])shiftsPerDay.Clone();
                        bestFullDays = (int[])fullDaysCount.Clone();

                        if (bestScore.ConflictDays == 0
                            && bestScore.CoverageGap == 0
                            && bestScore.MinHourDeficit <= EPS)
                        {
                            Array.Copy(bestAssigned, assigned, assigned.Length);
                            Array.Copy(bestSlotFrom, slotFromMin, slotFromMin.Length);
                            Array.Copy(bestSlotTo, slotToMin, slotToMin.Length);
                            Array.Copy(bestSlotHours, slotHours, slotHours.Length);
                            Array.Copy(bestTotalHours, totalHours, totalHours.Length);
                            Array.Copy(bestShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                            Array.Copy(bestFullDays, fullDaysCount, fullDaysCount.Length);
                            return;
                        }
                    }
                }
            }

            if (bestAssigned is null
                || bestSlotFrom is null
                || bestSlotTo is null
                || bestSlotHours is null
                || bestTotalHours is null
                || bestShiftsPerDay is null
                || bestFullDays is null)
            {
                Array.Copy(originalAssigned, assigned, assigned.Length);
                Array.Copy(originalSlotFrom, slotFromMin, slotFromMin.Length);
                Array.Copy(originalSlotTo, slotToMin, slotToMin.Length);
                Array.Copy(originalSlotHours, slotHours, slotHours.Length);
                Array.Copy(originalTotalHours, totalHours, totalHours.Length);
                Array.Copy(originalShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                Array.Copy(originalFullDays, fullDaysCount, fullDaysCount.Length);
                return;
            }

            Array.Copy(bestAssigned, assigned, assigned.Length);
            Array.Copy(bestSlotFrom, slotFromMin, slotFromMin.Length);
            Array.Copy(bestSlotTo, slotToMin, slotToMin.Length);
            Array.Copy(bestSlotHours, slotHours, slotHours.Length);
            Array.Copy(bestTotalHours, totalHours, totalHours.Length);
            Array.Copy(bestShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
            Array.Copy(bestFullDays, fullDaysCount, fullDaysCount.Length);
        }

        private static void TryImproveScheduleWithFairnessWindowRebuild(
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[][] availableByDay,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            double[] remainingPotentialHours,
            int[] scarcityDays,
            int[] unavailableBoundaryPressure,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n,
            CancellationToken ct)
        {
            var originalScore = EvaluateAttemptScore(
                shifts,
                minHours,
                desiredHours,
                unavailable,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                daysInMonth,
                shiftCount,
                pps,
                stride,
                n);

            var orderedFairnessDays = BuildFairnessDayOrderBySeverity(
                minHours,
                desiredHours,
                unavailable,
                totalHours,
                shiftsPerDay,
                daysInMonth,
                stride,
                n);

            if (orderedFairnessDays.Count == 0)
                return;

            var candidateWindows = BuildConflictWindows(orderedFairnessDays, daysInMonth);
            if (candidateWindows.Count == 0)
                return;

            var originalAssigned = (int[])assigned.Clone();
            var originalSlotFrom = (int[])slotFromMin.Clone();
            var originalSlotTo = (int[])slotToMin.Clone();
            var originalSlotHours = (double[])slotHours.Clone();
            var originalTotalHours = (double[])totalHours.Clone();
            var originalShiftsPerDay = (int[])shiftsPerDay.Clone();
            var originalFullDays = (int[])fullDaysCount.Clone();

            var bestScore = originalScore;
            int[]? bestAssigned = null;
            int[]? bestSlotFrom = null;
            int[]? bestSlotTo = null;
            double[]? bestSlotHours = null;
            double[]? bestTotalHours = null;
            int[]? bestShiftsPerDay = null;
            int[]? bestFullDays = null;

            var rrSeeds = BuildRoundRobinSeeds(n);
            var localSeedCount = Math.Min(rrSeeds.Count, 4);

            for (var windowIndex = 0; windowIndex < candidateWindows.Count; windowIndex++)
            {
                var priorityDays = candidateWindows[windowIndex];
                var reversedPriorityDays = (int[])priorityDays.Clone();
                Array.Reverse(reversedPriorityDays);

                for (var direction = 0; direction < 2; direction++)
                {
                    var orderedPriorityDays = direction == 0 ? priorityDays : reversedPriorityDays;

                    for (var seedIndex = 0; seedIndex < localSeedCount; seedIndex++)
                    {
                        ct.ThrowIfCancellationRequested();

                        Array.Copy(originalAssigned, assigned, assigned.Length);
                        Array.Copy(originalSlotFrom, slotFromMin, slotFromMin.Length);
                        Array.Copy(originalSlotTo, slotToMin, slotToMin.Length);
                        Array.Copy(originalSlotHours, slotHours, slotHours.Length);
                        Array.Copy(originalTotalHours, totalHours, totalHours.Length);
                        Array.Copy(originalShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                        Array.Copy(originalFullDays, fullDaysCount, fullDaysCount.Length);

                        for (var i = 0; i < orderedPriorityDays.Length; i++)
                        {
                            ClearDayAssignments(
                                orderedPriorityDays[i],
                                shifts,
                                assigned,
                                slotFromMin,
                                slotToMin,
                                slotHours,
                                totalHours,
                                shiftsPerDay,
                                fullDaysCount,
                                shiftCount,
                                pps,
                                stride,
                                n);
                        }

                        var baseOrder = BuildCurrentConflictFirstOrder(
                            shifts,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            daysInMonth,
                            shiftCount,
                            pps,
                            n,
                            tieBreakReverse: false);
                        var prioritizedOrder = BuildDayOrderWithPriorityPrefix(baseOrder, orderedPriorityDays);
                        var rrCursor = rrSeeds[seedIndex];

                        FillAllUnfurnishedOrderIndependent(
                            schedule,
                            shifts,
                            availableByDay,
                            minHours,
                            desiredHours,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            scarcityDays,
                            remainingPotentialHours,
                            unavailableBoundaryPressure,
                            ref rrCursor,
                            prioritizedOrder,
                            ct);

                        StrictMinHoursRepairMinConflicts(
                            schedule,
                            shifts,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            minHours,
                            desiredHours,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            remainingPotentialHours,
                            unavailableBoundaryPressure,
                            n,
                            ct);

                        TryImproveScheduleWithExactConflictOptimization(
                            schedule,
                            shifts,
                            availableByDay,
                            minHours,
                            desiredHours,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            n,
                            ct);

                        var candidateScore = EvaluateAttemptScore(
                            shifts,
                            minHours,
                            desiredHours,
                            unavailable,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            n);

                        if (!candidateScore.IsBetterThan(bestScore))
                            continue;

                        bestScore = candidateScore;
                        bestAssigned = (int[])assigned.Clone();
                        bestSlotFrom = (int[])slotFromMin.Clone();
                        bestSlotTo = (int[])slotToMin.Clone();
                        bestSlotHours = (double[])slotHours.Clone();
                        bestTotalHours = (double[])totalHours.Clone();
                        bestShiftsPerDay = (int[])shiftsPerDay.Clone();
                        bestFullDays = (int[])fullDaysCount.Clone();
                    }
                }
            }

            if (bestAssigned is null
                || bestSlotFrom is null
                || bestSlotTo is null
                || bestSlotHours is null
                || bestTotalHours is null
                || bestShiftsPerDay is null
                || bestFullDays is null)
            {
                Array.Copy(originalAssigned, assigned, assigned.Length);
                Array.Copy(originalSlotFrom, slotFromMin, slotFromMin.Length);
                Array.Copy(originalSlotTo, slotToMin, slotToMin.Length);
                Array.Copy(originalSlotHours, slotHours, slotHours.Length);
                Array.Copy(originalTotalHours, totalHours, totalHours.Length);
                Array.Copy(originalShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
                Array.Copy(originalFullDays, fullDaysCount, fullDaysCount.Length);
                return;
            }

            Array.Copy(bestAssigned, assigned, assigned.Length);
            Array.Copy(bestSlotFrom, slotFromMin, slotFromMin.Length);
            Array.Copy(bestSlotTo, slotToMin, slotToMin.Length);
            Array.Copy(bestSlotHours, slotHours, slotHours.Length);
            Array.Copy(bestTotalHours, totalHours, totalHours.Length);
            Array.Copy(bestShiftsPerDay, shiftsPerDay, shiftsPerDay.Length);
            Array.Copy(bestFullDays, fullDaysCount, fullDaysCount.Length);
        }

        private static List<int> BuildConflictDayOrderBySeverity(
            List<ShiftTemplate> shifts,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int daysInMonth,
            int shiftCount,
            int pps,
            int n)
        {
            var conflictDays = new List<int>();

            for (var day = 1; day <= daysInMonth; day++)
            {
                var metrics = ComputeDayCoverageMetrics(
                    day,
                    shifts,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    shiftCount,
                    pps,
                    n);

                if (metrics.CoverageGap <= 0 && metrics.UnfilledSlots <= 0)
                    continue;

                conflictDays.Add(day);
            }

            conflictDays.Sort((leftDay, rightDay) =>
            {
                var leftMetrics = ComputeDayCoverageMetrics(
                    leftDay,
                    shifts,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    shiftCount,
                    pps,
                    n);
                var rightMetrics = ComputeDayCoverageMetrics(
                    rightDay,
                    shifts,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    shiftCount,
                    pps,
                    n);

                var byCoverageGap = rightMetrics.CoverageGap.CompareTo(leftMetrics.CoverageGap);
                if (byCoverageGap != 0)
                    return byCoverageGap;

                var byUnfilled = rightMetrics.UnfilledSlots.CompareTo(leftMetrics.UnfilledSlots);
                if (byUnfilled != 0)
                    return byUnfilled;

                var leftTotalCoverage = leftMetrics.Coverage1 + leftMetrics.Coverage2;
                var rightTotalCoverage = rightMetrics.Coverage1 + rightMetrics.Coverage2;
                var byTotalCoverage = leftTotalCoverage.CompareTo(rightTotalCoverage);
                if (byTotalCoverage != 0)
                    return byTotalCoverage;

                return leftDay.CompareTo(rightDay);
            });

            return conflictDays;
        }

        private static List<int> BuildFairnessDayOrderBySeverity(
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            double[] totalHours,
            int[] shiftsPerDay,
            int daysInMonth,
            int stride,
            int n)
        {
            var generatedOffRunPressure = BuildGeneratedOffRunPressure(
                unavailable,
                shiftsPerDay,
                daysInMonth,
                stride,
                n);
            var dayScores = new double[daysInMonth + 1];

            for (var emp = 0; emp < n; emp++)
            {
                var hardGap = Math.Max(0.0, minHours[emp] - totalHours[emp]);
                var softGap = Math.Max(0.0, desiredHours[emp] - totalHours[emp]);
                if (hardGap <= EPS && softGap <= EPS)
                    continue;

                for (var day = 1; day <= daysInMonth; day++)
                {
                    if (unavailable[day * n + emp])
                        continue;

                    if (shiftsPerDay[emp * stride + day] > 0)
                        continue;

                    var runPressure = generatedOffRunPressure[emp * stride + day];
                    if (runPressure <= 0)
                        continue;

                    var weightedGap = hardGap > EPS
                        ? (hardGap * 4.0) + softGap
                        : softGap;

                    dayScores[day] += weightedGap + (runPressure * (hardGap > EPS ? 3.0 : 1.5));
                }
            }

            var orderedDays = new List<int>();
            for (var day = 1; day <= daysInMonth; day++)
            {
                if (dayScores[day] > EPS)
                    orderedDays.Add(day);
            }

            orderedDays.Sort((leftDay, rightDay) =>
            {
                var byScore = dayScores[rightDay].CompareTo(dayScores[leftDay]);
                if (byScore != 0)
                    return byScore;

                return leftDay.CompareTo(rightDay);
            });

            return orderedDays;
        }

        private static List<(int DayA, int DayB)> BuildConflictDayPairs(List<int> orderedConflictDays, int daysInMonth)
        {
            var pairs = new List<(int DayA, int DayB)>();
            var seen = new HashSet<long>();
            var maxCoreDays = Math.Min(orderedConflictDays.Count, 5);

            for (var left = 0; left < maxCoreDays; left++)
            {
                for (var right = left + 1; right < maxCoreDays; right++)
                {
                    AddConflictDayPair(pairs, seen, orderedConflictDays[left], orderedConflictDays[right]);
                }
            }

            for (var i = 0; i < maxCoreDays; i++)
            {
                var day = orderedConflictDays[i];
                if (day > 1)
                    AddConflictDayPair(pairs, seen, day, day - 1);

                if (day < daysInMonth)
                    AddConflictDayPair(pairs, seen, day, day + 1);
            }

            if (pairs.Count == 0 && orderedConflictDays.Count == 1)
            {
                var day = orderedConflictDays[0];
                if (day > 1)
                    AddConflictDayPair(pairs, seen, day, day - 1);

                if (day < daysInMonth)
                    AddConflictDayPair(pairs, seen, day, day + 1);
            }

            return pairs;
        }

        private static List<int[]> BuildConflictWindows(List<int> orderedConflictDays, int daysInMonth)
        {
            var windows = new List<int[]>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var maxCoreDays = Math.Min(orderedConflictDays.Count, 5);

            for (var i = 0; i < maxCoreDays; i++)
            {
                AddConflictWindow(windows, seen, daysInMonth, orderedConflictDays[i] - 1, orderedConflictDays[i] + 1);
            }

            for (var i = 0; i < Math.Min(maxCoreDays, 3); i++)
            {
                AddConflictWindow(windows, seen, daysInMonth, orderedConflictDays[i] - 2, orderedConflictDays[i] + 2);
            }

            for (var i = 0; i + 1 < maxCoreDays; i++)
            {
                var left = orderedConflictDays[i];
                var right = orderedConflictDays[i + 1];
                if (right - left > 2)
                    continue;

                AddConflictWindow(windows, seen, daysInMonth, left - 1, right + 1);
            }

            return windows;
        }

        private static void AddConflictDayPair(
            List<(int DayA, int DayB)> pairs,
            HashSet<long> seen,
            int dayA,
            int dayB)
        {
            if (dayA <= 0 || dayB <= 0 || dayA == dayB)
                return;

            var left = Math.Min(dayA, dayB);
            var right = Math.Max(dayA, dayB);
            var key = ((long)left << 32) | (uint)right;
            if (!seen.Add(key))
                return;

            pairs.Add((left, right));
        }

        private static void AddConflictWindow(
            List<int[]> windows,
            HashSet<string> seen,
            int daysInMonth,
            int startDay,
            int endDay)
        {
            startDay = Math.Max(1, startDay);
            endDay = Math.Min(daysInMonth, endDay);
            if (endDay < startDay)
                return;

            var length = endDay - startDay + 1;
            if (length < 3 || length > 5)
                return;

            var days = new int[length];
            for (var i = 0; i < length; i++)
                days[i] = startDay + i;

            var key = string.Join(",", days);
            if (!seen.Add(key))
                return;

            windows.Add(days);
        }

        private static int[] BuildDayOrderWithPriorityPrefix(int[] baseOrder, params int[] priorityDays)
        {
            var prioritized = new int[baseOrder.Length];
            var used = new HashSet<int>();
            var cursor = 0;

            for (var i = 0; i < priorityDays.Length; i++)
            {
                var day = priorityDays[i];
                if (day <= 0 || !used.Add(day))
                    continue;

                prioritized[cursor++] = day;
            }

            for (var i = 0; i < baseOrder.Length; i++)
            {
                var day = baseOrder[i];
                if (!used.Add(day))
                    continue;

                prioritized[cursor++] = day;
            }

            return prioritized;
        }

        private static void OptimizeConflictDaysExact(
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[][] availableByDay,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n,
            int[] dayOrder,
            CancellationToken ct)
        {
            if (daysInMonth <= 0 || pps <= 0 || shiftCount <= 0 || shifts.Count == 0)
                return;

            for (var pass = 0; pass < 2; pass++)
            {
                ct.ThrowIfCancellationRequested();

                var improvedPass = false;
                for (var i = 0; i < dayOrder.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var day = dayOrder[i];
                    var metrics = ComputeDayCoverageMetrics(
                        day,
                        shifts,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        shiftCount,
                        pps,
                        n);

                    if (metrics.CoverageGap <= 0 && metrics.UnfilledSlots <= 0)
                        continue;

                    var availableToday = availableByDay[day];
                    if (availableToday.Length == 0)
                        continue;

                    if (!TryOptimizeDayExact(
                            day,
                            schedule,
                            shifts,
                            availableToday,
                            minHours,
                            desiredHours,
                            unavailable,
                            explicitFullDayAvailability,
                            availStartMin,
                            availEndMin,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            n))
                    {
                        continue;
                    }

                    improvedPass = true;
                }

                if (!improvedPass)
                    break;
            }
        }

        private static bool TryOptimizeDayExact(
            int day,
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            int[] availableToday,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int n)
        {
            var currentMetrics = ComputeDayCoverageMetrics(
                day,
                shifts,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                shiftCount,
                pps,
                n);

            if (currentMetrics.CoverageGap <= 0 && currentMetrics.UnfilledSlots <= 0)
                return false;

            var assignedBackup = (int[])assigned.Clone();
            var slotFromBackup = (int[])slotFromMin.Clone();
            var slotToBackup = (int[])slotToMin.Clone();
            var slotHoursBackup = (double[])slotHours.Clone();
            var totalHoursBackup = (double[])totalHours.Clone();
            var shiftsPerDayBackup = (int[])shiftsPerDay.Clone();
            var fullDaysBackup = (int[])fullDaysCount.Clone();

            ClearDayAssignments(
                day,
                shifts,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                shiftCount,
                pps,
                stride,
                n);

            var employeeCount = availableToday.Length;
            var optionsByEmployee = new List<ExactDayOption>[employeeCount];
            for (var i = 0; i < employeeCount; i++)
            {
                optionsByEmployee[i] = BuildExactDayOptions(
                    availableToday[i],
                    day,
                    schedule,
                    shifts,
                    minHours,
                    desiredHours,
                    unavailable,
                    explicitFullDayAvailability,
                    availStartMin,
                    availEndMin,
                    totalHours,
                    shiftsPerDay,
                    fullDaysCount,
                    daysInMonth,
                    shiftCount,
                    stride,
                    n);
            }

            var has = new bool[employeeCount + 1, pps + 1, pps + 1];
            var scores = new ExactDayScore[employeeCount + 1, pps + 1, pps + 1];
            var prevC1 = new int[employeeCount + 1, pps + 1, pps + 1];
            var prevC2 = new int[employeeCount + 1, pps + 1, pps + 1];
            var prevOption = new int[employeeCount + 1, pps + 1, pps + 1];

            has[0, 0, 0] = true;

            for (var i = 0; i < employeeCount; i++)
            {
                for (var usedShift1 = 0; usedShift1 <= pps; usedShift1++)
                {
                    for (var usedShift2 = 0; usedShift2 <= pps; usedShift2++)
                    {
                        if (!has[i, usedShift1, usedShift2])
                            continue;

                        var baseScore = scores[i, usedShift1, usedShift2];
                        var options = optionsByEmployee[i];
                        for (var optionIndex = 0; optionIndex < options.Count; optionIndex++)
                        {
                            var option = options[optionIndex];
                            var nextShift1 = usedShift1 + option.Shift1Slots;
                            var nextShift2 = usedShift2 + option.Shift2Slots;
                            if (nextShift1 > pps || nextShift2 > pps)
                                continue;

                            var nextScore = baseScore.Add(option);
                            if (has[i + 1, nextShift1, nextShift2]
                                && !nextScore.IsBetterForSameFill(scores[i + 1, nextShift1, nextShift2]))
                            {
                                continue;
                            }

                            has[i + 1, nextShift1, nextShift2] = true;
                            scores[i + 1, nextShift1, nextShift2] = nextScore;
                            prevC1[i + 1, nextShift1, nextShift2] = usedShift1;
                            prevC2[i + 1, nextShift1, nextShift2] = usedShift2;
                            prevOption[i + 1, nextShift1, nextShift2] = optionIndex;
                        }
                    }
                }
            }

            var bestFound = false;
            var bestShift1 = 0;
            var bestShift2 = 0;
            var bestScore = default(ExactDayScore);
            var shift1RequiredCoverage = (shifts[0].EndMin - shifts[0].StartMin) * pps;
            var shift2RequiredCoverage = shiftCount > 1
                ? (shifts[1].EndMin - shifts[1].StartMin) * pps
                : 0;

            for (var usedShift1 = 0; usedShift1 <= pps; usedShift1++)
            {
                for (var usedShift2 = 0; usedShift2 <= pps; usedShift2++)
                {
                    if (!has[employeeCount, usedShift1, usedShift2])
                        continue;

                    var candidateScore = scores[employeeCount, usedShift1, usedShift2];
                    if (!bestFound
                        || IsBetterExactDayPlan(
                            usedShift1,
                            usedShift2,
                            candidateScore,
                            bestShift1,
                            bestShift2,
                            bestScore,
                            pps,
                            shift1RequiredCoverage,
                            shift2RequiredCoverage,
                            hasSecondShift: shiftCount > 1))
                    {
                        bestFound = true;
                        bestShift1 = usedShift1;
                        bestShift2 = usedShift2;
                        bestScore = candidateScore;
                    }
                }
            }

            if (!bestFound)
            {
                Array.Copy(assignedBackup, assigned, assigned.Length);
                Array.Copy(slotFromBackup, slotFromMin, slotFromMin.Length);
                Array.Copy(slotToBackup, slotToMin, slotToMin.Length);
                Array.Copy(slotHoursBackup, slotHours, slotHours.Length);
                Array.Copy(totalHoursBackup, totalHours, totalHours.Length);
                Array.Copy(shiftsPerDayBackup, shiftsPerDay, shiftsPerDay.Length);
                Array.Copy(fullDaysBackup, fullDaysCount, fullDaysCount.Length);
                return false;
            }

            var chosenStates = new int[employeeCount];
            var cursorShift1 = bestShift1;
            var cursorShift2 = bestShift2;
            for (var i = employeeCount; i >= 1; i--)
            {
                var optionIndex = prevOption[i, cursorShift1, cursorShift2];
                var option = optionsByEmployee[i - 1][optionIndex];
                chosenStates[i - 1] = option.StateCode;
                var previousShift1 = prevC1[i, cursorShift1, cursorShift2];
                var previousShift2 = prevC2[i, cursorShift1, cursorShift2];
                cursorShift1 = previousShift1;
                cursorShift2 = previousShift2;
            }

            MaterializeExactDayPlan(
                day,
                availableToday,
                chosenStates,
                shifts,
                availStartMin,
                availEndMin,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                totalHours,
                shiftsPerDay,
                fullDaysCount,
                shiftCount,
                pps,
                stride,
                n);

            var optimizedMetrics = ComputeDayCoverageMetrics(
                day,
                shifts,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                shiftCount,
                pps,
                n);

            if (optimizedMetrics.IsBetterThan(currentMetrics))
                return true;

            Array.Copy(assignedBackup, assigned, assigned.Length);
            Array.Copy(slotFromBackup, slotFromMin, slotFromMin.Length);
            Array.Copy(slotToBackup, slotToMin, slotToMin.Length);
            Array.Copy(slotHoursBackup, slotHours, slotHours.Length);
            Array.Copy(totalHoursBackup, totalHours, totalHours.Length);
            Array.Copy(shiftsPerDayBackup, shiftsPerDay, shiftsPerDay.Length);
            Array.Copy(fullDaysBackup, fullDaysCount, fullDaysCount.Length);
            return false;
        }

        private static List<ExactDayOption> BuildExactDayOptions(
            int emp,
            int day,
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            double[] minHours,
            double[] desiredHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int stride,
            int n)
        {
            var options = new List<ExactDayOption>(capacity: 4)
            {
                new ExactDayOption(
                    stateCode: 0,
                    shift1Slots: 0,
                    shift2Slots: 0,
                    coverage1: 0,
                    coverage2: 0,
                    addedHours: 0,
                    hardGain: 0,
                    softGain: 0,
                    fullDayCount: 0)
            };

            if (TryGetSingleShiftOption(day, emp, shifts[0], unavailable, availStartMin, availEndMin, n, out var shift1Hours, out var shift1CoverageMinutes)
                && CanAssignExactDayState(
                    schedule,
                    emp,
                    day,
                    addedShiftCount: 1,
                    addedHours: shift1Hours,
                    minHours,
                    totalHours,
                    shiftsPerDay,
                    fullDaysCount,
                    explicitFullDayAvailability,
                    daysInMonth,
                    shiftCount,
                    stride))
            {
                options.Add(BuildExactDayOption(
                    stateCode: 1,
                    shift1Slots: 1,
                    shift2Slots: 0,
                    coverage1: shift1CoverageMinutes,
                    coverage2: shiftCount > 1
                        ? ComputeForwardBridgeCoverageMinutes(day, emp, shifts[1], availStartMin, availEndMin, n)
                        : 0,
                    addedHours: shift1Hours,
                    emp,
                    minHours,
                    desiredHours,
                    totalHours));
            }

            if (shiftCount > 1
                && TryGetSingleShiftOption(day, emp, shifts[1], unavailable, availStartMin, availEndMin, n, out var shift2Hours, out var shift2CoverageMinutes)
                && CanAssignExactDayState(
                    schedule,
                    emp,
                    day,
                    addedShiftCount: 1,
                    addedHours: shift2Hours,
                    minHours,
                    totalHours,
                    shiftsPerDay,
                    fullDaysCount,
                    explicitFullDayAvailability,
                    daysInMonth,
                    shiftCount,
                    stride))
            {
                options.Add(BuildExactDayOption(
                    stateCode: 2,
                    shift1Slots: 0,
                    shift2Slots: 1,
                    coverage1: ComputeBackwardBridgeCoverageMinutes(day, emp, shifts[0], shifts[1], availStartMin, availEndMin, n),
                    coverage2: shift2CoverageMinutes,
                    addedHours: shift2Hours,
                    emp,
                    minHours,
                    desiredHours,
                    totalHours));
            }

            if (shiftCount > 1
                && TryGetDoubleShiftOption(day, emp, shifts[0], shifts[1], unavailable, availStartMin, availEndMin, n, out var fullDayHours, out var fullShift1CoverageMinutes, out var fullShift2CoverageMinutes)
                && CanAssignExactDayState(
                    schedule,
                    emp,
                    day,
                    addedShiftCount: 2,
                    addedHours: fullDayHours,
                    minHours,
                    totalHours,
                    shiftsPerDay,
                    fullDaysCount,
                    explicitFullDayAvailability,
                    daysInMonth,
                    shiftCount,
                    stride))
            {
                options.Add(BuildExactDayOption(
                    stateCode: 3,
                    shift1Slots: 1,
                    shift2Slots: 1,
                    coverage1: fullShift1CoverageMinutes,
                    coverage2: fullShift2CoverageMinutes,
                    addedHours: fullDayHours,
                    emp,
                    minHours,
                    desiredHours,
                    totalHours,
                    fullDayCount: 1));
            }

            return options;
        }

        private static int ComputeBackwardBridgeCoverageMinutes(
            int day,
            int emp,
            ShiftTemplate shift1,
            ShiftTemplate shift2,
            int[] availStartMin,
            int[] availEndMin,
            int n)
        {
            const int maxBridgeMinutes = 90;

            var idx = day * n + emp;
            var from = Math.Max(shift1.StartMin, Math.Max(shift2.StartMin - maxBridgeMinutes, availStartMin[idx]));
            var to = Math.Min(shift1.EndMin, Math.Min(shift2.StartMin, availEndMin[idx]));
            return Math.Max(0, to - from);
        }

        private static int ComputeForwardBridgeCoverageMinutes(
            int day,
            int emp,
            ShiftTemplate shift2,
            int[] availStartMin,
            int[] availEndMin,
            int n)
        {
            const int maxBridgeMinutes = 90;

            var idx = day * n + emp;
            var from = Math.Max(shift2.StartMin, availStartMin[idx]);
            var to = Math.Min(shift2.EndMin, Math.Min(shift2.StartMin + maxBridgeMinutes, availEndMin[idx]));
            return Math.Max(0, to - from);
        }

        private static ExactDayOption BuildExactDayOption(
            int stateCode,
            int shift1Slots,
            int shift2Slots,
            int coverage1,
            int coverage2,
            double addedHours,
            int emp,
            double[] minHours,
            double[] desiredHours,
            double[] totalHours,
            int fullDayCount = 0)
        {
            var hardGap = Math.Max(0.0, minHours[emp] - totalHours[emp]);
            var softGap = Math.Max(0.0, desiredHours[emp] - totalHours[emp]);

            return new ExactDayOption(
                stateCode,
                shift1Slots,
                shift2Slots,
                coverage1,
                coverage2,
                addedHours,
                Math.Min(addedHours, hardGap),
                Math.Min(addedHours, softGap),
                fullDayCount);
        }

        private static bool IsBetterExactDayPlan(
            int usedShift1,
            int usedShift2,
            ExactDayScore score,
            int bestShift1,
            int bestShift2,
            ExactDayScore bestScore,
            int peoplePerShift,
            int shift1RequiredCoverage,
            int shift2RequiredCoverage,
            bool hasSecondShift)
        {
            var coverageGap = Math.Max(0, shift1RequiredCoverage - score.Coverage1)
                + (hasSecondShift ? Math.Max(0, shift2RequiredCoverage - score.Coverage2) : 0);
            var bestCoverageGap = Math.Max(0, shift1RequiredCoverage - bestScore.Coverage1)
                + (hasSecondShift ? Math.Max(0, shift2RequiredCoverage - bestScore.Coverage2) : 0);
            if (coverageGap != bestCoverageGap)
                return coverageGap < bestCoverageGap;

            var minCoverage = hasSecondShift ? Math.Min(score.Coverage1, score.Coverage2) : score.Coverage1;
            var bestMinCoverage = hasSecondShift ? Math.Min(bestScore.Coverage1, bestScore.Coverage2) : bestScore.Coverage1;
            if (minCoverage != bestMinCoverage)
                return minCoverage > bestMinCoverage;

            var unfilledSlots = Math.Max(0, peoplePerShift - usedShift1) + Math.Max(0, peoplePerShift - usedShift2);
            var bestUnfilledSlots = Math.Max(0, peoplePerShift - bestShift1) + Math.Max(0, peoplePerShift - bestShift2);
            if (unfilledSlots != bestUnfilledSlots)
                return unfilledSlots < bestUnfilledSlots;

            if (score.HardGain > bestScore.HardGain + EPS)
                return true;

            if (score.HardGain < bestScore.HardGain - EPS)
                return false;

            if (score.SoftGain > bestScore.SoftGain + EPS)
                return true;

            if (score.SoftGain < bestScore.SoftGain - EPS)
                return false;

            if (score.FullDayCount != bestScore.FullDayCount)
                return score.FullDayCount < bestScore.FullDayCount;

            if (score.AddedHours > bestScore.AddedHours + EPS)
                return true;

            if (score.AddedHours < bestScore.AddedHours - EPS)
                return false;

            return false;
        }

        private static DayCoverageMetrics ComputeDayCoverageMetrics(
            int day,
            List<ShiftTemplate> shifts,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int shiftCount,
            int pps,
            int n)
        {
            var filled1 = 0;
            var filled2 = 0;
            var totalDayHours = 0.0;

            for (var shiftIdx = 0; shiftIdx < shiftCount; shiftIdx++)
            {
                for (var slotIdx = 0; slotIdx < pps; slotIdx++)
                {
                    var pos = SlotIndex(day, shiftIdx, slotIdx, pps, shiftCount);
                    var emp = assigned[pos];
                    if (emp < 0)
                        continue;

                    totalDayHours += slotHours[pos];
                    if (shiftIdx == 0)
                        filled1++;
                    else
                        filled2++;
                }
            }

            var shift1GapMinutes = ComputeShiftCoverageGapMinutes(
                day,
                shifts[0],
                assigned,
                slotFromMin,
                slotToMin,
                shiftCount,
                pps,
                out var coverage1Minutes);

            var shift2GapMinutes = 0;
            var coverage2Minutes = coverage1Minutes;
            if (shiftCount > 1)
            {
                shift2GapMinutes = ComputeShiftCoverageGapMinutes(
                    day,
                    shifts[1],
                    assigned,
                    slotFromMin,
                    slotToMin,
                    shiftCount,
                    pps,
                    out coverage2Minutes);
            }
            else
            {
                filled2 = filled1;
            }

            var unfilledSlots = Math.Max(0, pps - filled1);
            if (shiftCount > 1)
                unfilledSlots += Math.Max(0, pps - filled2);

            return new DayCoverageMetrics(
                coverage1Minutes,
                coverage2Minutes,
                shift1GapMinutes,
                shift2GapMinutes,
                filled1,
                filled2,
                unfilledSlots,
                totalDayHours,
                hasSecondShift: shiftCount > 1);
        }

        private static int ComputeShiftCoverageGapMinutes(
            int day,
            ShiftTemplate shift,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            int shiftCount,
            int pps,
            out int coveredWorkerMinutes)
        {
            var requiredWorkerMinutes = Math.Max(0, shift.EndMin - shift.StartMin) * pps;
            if (requiredWorkerMinutes <= 0)
            {
                coveredWorkerMinutes = 0;
                return 0;
            }

            var intervals = new List<(int From, int To)>(capacity: Math.Max(pps * shiftCount, 1));
            var boundaries = new List<int>(capacity: Math.Max(pps * shiftCount * 2 + 2, 4))
            {
                shift.StartMin,
                shift.EndMin
            };

            for (var innerShiftIdx = 0; innerShiftIdx < shiftCount; innerShiftIdx++)
            {
                for (var slotIdx = 0; slotIdx < pps; slotIdx++)
                {
                    var pos = SlotIndex(day, innerShiftIdx, slotIdx, pps, shiftCount);
                    if (assigned[pos] < 0)
                        continue;

                    var from = Math.Max(shift.StartMin, slotFromMin[pos]);
                    var to = Math.Min(shift.EndMin, slotToMin[pos]);
                    if (to <= from)
                        continue;

                    intervals.Add((from, to));
                    boundaries.Add(from);
                    boundaries.Add(to);
                }
            }

            if (intervals.Count == 0)
            {
                coveredWorkerMinutes = 0;
                return requiredWorkerMinutes;
            }

            boundaries.Sort();

            var uniqueCount = 1;
            for (var i = 1; i < boundaries.Count; i++)
            {
                if (boundaries[i] == boundaries[uniqueCount - 1])
                    continue;

                boundaries[uniqueCount++] = boundaries[i];
            }

            var gapMinutes = 0;
            for (var i = 1; i < uniqueCount; i++)
            {
                var segmentFrom = boundaries[i - 1];
                var segmentTo = boundaries[i];
                if (segmentTo <= segmentFrom)
                    continue;

                var active = 0;
                for (var intervalIndex = 0; intervalIndex < intervals.Count; intervalIndex++)
                {
                    var interval = intervals[intervalIndex];
                    if (interval.From <= segmentFrom && interval.To >= segmentTo)
                        active++;
                }

                gapMinutes += Math.Max(0, pps - active) * (segmentTo - segmentFrom);
            }

            coveredWorkerMinutes = Math.Max(0, requiredWorkerMinutes - gapMinutes);
            return gapMinutes;
        }

        private static void ClearDayAssignments(
            int day,
            List<ShiftTemplate> shifts,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int shiftCount,
            int pps,
            int stride,
            int n)
        {
            var removedShiftCounts = new int[n];

            for (var shiftIdx = 0; shiftIdx < shiftCount; shiftIdx++)
            {
                var shift = shifts[shiftIdx];
                for (var slotIdx = 0; slotIdx < pps; slotIdx++)
                {
                    var pos = SlotIndex(day, shiftIdx, slotIdx, pps, shiftCount);
                    var emp = assigned[pos];
                    if (emp >= 0)
                    {
                        totalHours[emp] -= slotHours[pos];
                        removedShiftCounts[emp]++;
                    }

                    assigned[pos] = -1;
                    slotFromMin[pos] = shift.StartMin;
                    slotToMin[pos] = shift.EndMin;
                    slotHours[pos] = 0;
                }
            }

            for (var emp = 0; emp < n; emp++)
            {
                var removed = removedShiftCounts[emp];
                if (removed <= 0)
                    continue;

                shiftsPerDay[emp * stride + day] -= removed;
                if (shiftCount >= 2 && removed >= 2)
                    fullDaysCount[emp] -= 1;
            }
        }

        private static bool TryGetSingleShiftOption(
            int day,
            int emp,
            ShiftTemplate shift,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int n,
            out double hours,
            out int coveredMinutes)
        {
            hours = 0;
            coveredMinutes = 0;

            var idx = day * n + emp;
            if (unavailable[idx])
                return false;

            var aStart = availStartMin[idx];
            var aEnd = availEndMin[idx];
            var from = Math.Max(shift.StartMin, aStart);
            var to = Math.Min(shift.EndMin, aEnd);
            if (to <= from)
                return false;

            hours = (to - from) / 60d;
            coveredMinutes = to - from;
            return true;
        }

        private static bool TryGetDoubleShiftOption(
            int day,
            int emp,
            ShiftTemplate shift1,
            ShiftTemplate shift2,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int n,
            out double hours,
            out int coveredMinutesShift1,
            out int coveredMinutesShift2)
        {
            hours = 0;
            coveredMinutesShift1 = 0;
            coveredMinutesShift2 = 0;

            var idx = day * n + emp;
            if (unavailable[idx])
                return false;

            if (!TryResolveShiftPairIntervals(
                    day,
                    emp,
                    emp,
                    availStartMin,
                    availEndMin,
                    shift1,
                    shift2,
                    n,
                    out var fromShift1,
                    out var toShift1,
                    out var fromShift2,
                    out var toShift2))
            {
                return false;
            }

            hours = ((toShift1 - fromShift1) + (toShift2 - fromShift2)) / 60d;
            coveredMinutesShift1 = toShift1 - fromShift1;
            coveredMinutesShift2 = toShift2 - fromShift2;
            return true;
        }

        private static bool CanAssignExactDayState(
            ScheduleModel schedule,
            int emp,
            int day,
            int addedShiftCount,
            double addedHours,
            double[] minHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            bool[] explicitFullDayAvailability,
            int daysInMonth,
            int shiftCount,
            int stride)
        {
            if (addedShiftCount <= 0)
                return true;

            if (schedule.MaxHoursPerEmpMonth > 0
                && totalHours[emp] + addedHours > schedule.MaxHoursPerEmpMonth + EPS)
            {
                return false;
            }

            var currentShifts = shiftsPerDay[emp * stride + day];
            var nextShifts = currentShifts + addedShiftCount;
            if (nextShifts > shiftCount)
                return false;

            if (schedule.MaxConsecutiveDays > 0 && currentShifts == 0)
            {
                var streak = 1
                             + CountLeft(emp, day, shiftsPerDay, stride, value => value > 0)
                             + CountRight(emp, day, daysInMonth, shiftsPerDay, stride, value => value > 0);

                if (streak > schedule.MaxConsecutiveDays)
                    return false;
            }

            if (shiftCount >= 2 && nextShifts >= 2 && currentShifts < 2)
            {
                var exceedsFullPerMonth = schedule.MaxFullPerMonth > 0 && fullDaysCount[emp] + 1 > schedule.MaxFullPerMonth;
                var exceedsConsecutiveFull = false;

                if (schedule.MaxConsecutiveFull > 0)
                {
                    var fullStreak = 1
                                     + CountLeft(emp, day, shiftsPerDay, stride, value => value >= 2)
                                     + CountRight(emp, day, daysInMonth, shiftsPerDay, stride, value => value >= 2);

                    exceedsConsecutiveFull = fullStreak > schedule.MaxConsecutiveFull;
                }

                if ((exceedsFullPerMonth || exceedsConsecutiveFull)
                    && !CanUseExplicitFullDayOverride(
                        emp,
                        day,
                        minHours,
                        totalHours,
                        explicitFullDayAvailability,
                        shiftCount))
                {
                    return false;
                }
            }

            return true;
        }

        private static void MaterializeExactDayPlan(
            int day,
            int[] availableToday,
            int[] chosenStates,
            List<ShiftTemplate> shifts,
            int[] availStartMin,
            int[] availEndMin,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int shiftCount,
            int pps,
            int stride,
            int n)
        {
            var bothShiftEmployees = new List<int>(pps);
            var shift1OnlyEmployees = new List<int>(pps);
            var shift2OnlyEmployees = new List<int>(pps);

            for (var i = 0; i < availableToday.Length; i++)
            {
                var emp = availableToday[i];
                switch (chosenStates[i])
                {
                    case 1:
                        shift1OnlyEmployees.Add(emp);
                        break;
                    case 2:
                        shift2OnlyEmployees.Add(emp);
                        break;
                    case 3:
                        bothShiftEmployees.Add(emp);
                        break;
                }
            }

            if (shiftCount > 1)
            {
                bothShiftEmployees.Sort((left, right) =>
                    Math.Max(shifts[1].StartMin, availStartMin[day * n + left])
                        .CompareTo(Math.Max(shifts[1].StartMin, availStartMin[day * n + right])));

                shift1OnlyEmployees.Sort((left, right) =>
                    availEndMin[day * n + left].CompareTo(availEndMin[day * n + right]));

                shift2OnlyEmployees.Sort((left, right) =>
                    Math.Max(shifts[1].StartMin, availStartMin[day * n + left])
                        .CompareTo(Math.Max(shifts[1].StartMin, availStartMin[day * n + right])));
            }
            else
            {
                shift1OnlyEmployees.Sort();
            }

            var slot = 0;
            for (var i = 0; i < bothShiftEmployees.Count && slot < pps; i++, slot++)
            {
                var emp = bothShiftEmployees[i];
                AssignToEmptySlot(day, 0, slot, emp, assigned, slotFromMin, slotToMin, slotHours, availStartMin, availEndMin, shifts, totalHours, shiftsPerDay, fullDaysCount, shiftCount, stride, pps, n);
                if (shiftCount > 1)
                    AssignToEmptySlot(day, 1, slot, emp, assigned, slotFromMin, slotToMin, slotHours, availStartMin, availEndMin, shifts, totalHours, shiftsPerDay, fullDaysCount, shiftCount, stride, pps, n);
            }

            var pairedCount = shiftCount > 1 ? Math.Min(shift1OnlyEmployees.Count, shift2OnlyEmployees.Count) : 0;
            for (var i = 0; i < pairedCount && slot < pps; i++, slot++)
            {
                AssignToEmptySlot(day, 0, slot, shift1OnlyEmployees[i], assigned, slotFromMin, slotToMin, slotHours, availStartMin, availEndMin, shifts, totalHours, shiftsPerDay, fullDaysCount, shiftCount, stride, pps, n);
                AssignToEmptySlot(day, 1, slot, shift2OnlyEmployees[i], assigned, slotFromMin, slotToMin, slotHours, availStartMin, availEndMin, shifts, totalHours, shiftsPerDay, fullDaysCount, shiftCount, stride, pps, n);
            }

            var shift1Index = pairedCount;
            var shift2Index = pairedCount;
            while (slot < pps && (shift1Index < shift1OnlyEmployees.Count || shift2Index < shift2OnlyEmployees.Count))
            {
                if (shift1Index < shift1OnlyEmployees.Count)
                {
                    AssignToEmptySlot(day, 0, slot, shift1OnlyEmployees[shift1Index++], assigned, slotFromMin, slotToMin, slotHours, availStartMin, availEndMin, shifts, totalHours, shiftsPerDay, fullDaysCount, shiftCount, stride, pps, n);
                }

                if (shiftCount > 1 && shift2Index < shift2OnlyEmployees.Count)
                {
                    AssignToEmptySlot(day, 1, slot, shift2OnlyEmployees[shift2Index++], assigned, slotFromMin, slotToMin, slotHours, availStartMin, availEndMin, shifts, totalHours, shiftsPerDay, fullDaysCount, shiftCount, stride, pps, n);
                }

                slot++;
            }
        }

        private static int FindBestOrderIndependent(
            int[] availableToday,
            int day,
            int shiftIdx,
            int slotIdx,
            bool restrictSecondShift,
            ScheduleModel schedule,
            double[] minHours,
            double[] desiredHours,
            double[] remainingPotentialHours,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int[] scarcityDays,
            int[] unavailableBoundaryPressure,
            List<ShiftTemplate> shifts,
            int n,
            int[] assignedStamp,
            int stamp,
            int rrCursor)
        {
            var best = -1;
            CandidatePriority? bestPriority = null;

            for (var k = 0; k < availableToday.Length; k++)
            {
                var emp = availableToday[k];

                if (assignedStamp[emp] == stamp)
                    continue;

                if (unavailable[day * n + emp])
                    continue;

                var cur = shiftsPerDay[emp * stride + day];
                if (cur >= shiftCount)
                    continue;

                if (restrictSecondShift && cur != 0)
                    continue;

                if (IsEmployeeInShift(assigned, day, shiftIdx, pps, shiftCount, emp))
                    continue;

                if (!TryPredictAssignmentImpact(
                        day,
                        shiftIdx,
                        slotIdx,
                        emp,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        availStartMin,
                        availEndMin,
                        shifts,
                        n,
                        pps,
                        shiftCount,
                        out var empHours,
                        out var pairedEmp,
                        out var pairedDelta))
                    continue;

                if (empHours <= EPS)
                    continue;

                // max hours for candidate and paired (if boundary extension increases paired hours)
                if (schedule.MaxHoursPerEmpMonth > 0)
                {
                    if (totalHours[emp] + empHours > schedule.MaxHoursPerEmpMonth + EPS)
                        continue;

                    if (pairedEmp >= 0 && pairedDelta > EPS)
                    {
                        if (totalHours[pairedEmp] + pairedDelta > schedule.MaxHoursPerEmpMonth + EPS)
                            continue;
                    }
                }

                if (!CanAddShiftOrderIndependent(
                        schedule, emp, day, empHours,
                        minHours,
                        totalHours, shiftsPerDay, fullDaysCount,
                        explicitFullDayAvailability,
                        daysInMonth, shiftCount, stride))
                    continue;

                var pairsOtherShiftSameSlot = IsEmployeeInOtherShiftSameSlot(assigned, day, shiftIdx, slotIdx, pps, shiftCount, emp);
                var blocksOtherShiftPair = BlocksOtherShiftPair(assigned, day, shiftIdx, slotIdx, pps, shiftCount, emp);

                var priority = BuildCandidatePriority(
                    pairsOtherShiftSameSlot,
                    blocksOtherShiftPair,
                    emp,
                    day,
                    empHours,
                    minHours,
                    desiredHours,
                    remainingPotentialHours,
                    totalHours,
                    fullDaysCount,
                    scarcityDays,
                    unavailableBoundaryPressure,
                    rrCursor,
                    n,
                    stride);

                if (bestPriority.HasValue && !IsBetterCandidate(priority, bestPriority.Value))
                    continue;

                best = emp;
                bestPriority = priority;
            }

            return best;
        }

        // =========================
        // Phase 2B: Strict MinHours, minimize conflicts
        // =========================
        private static void StrictMinHoursRepairMinConflicts(
            ScheduleModel schedule,
            List<ShiftTemplate> shifts,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] availStartMin,
            int[] availEndMin,
            double[] minHours,
            double[] desiredHours,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            double[] remainingPotentialHours,
            int[] unavailableBoundaryPressure,
            int n,
            CancellationToken ct)
        {
            // list deficits
            var deficit = new List<int>(n);
            for (var i = 0; i < n; i++)
                if (minHours[i] - totalHours[i] > EPS)
                    deficit.Add(i);

            if (deficit.Count == 0)
                return;

            // sort by biggest deficit first
            deficit.Sort((a, b) => (minHours[b] - totalHours[b]).CompareTo(minHours[a] - totalHours[a]));

            // cap to keep runtime predictable
            var maxOps = daysInMonth * shiftCount * pps * 6;
            var ops = 0;
            var generatedOffRunPressure = BuildGeneratedOffRunPressure(
                unavailable,
                shiftsPerDay,
                daysInMonth,
                stride,
                n);

            // Multiple passes can help after swaps
            for (var pass = 0; pass < 3; pass++)
            {
                generatedOffRunPressure = BuildGeneratedOffRunPressure(
                    unavailable,
                    shiftsPerDay,
                    daysInMonth,
                    stride,
                    n);
                var improvedPass = false;

                for (var di = 0; di < deficit.Count; di++)
                {
                    ct.ThrowIfCancellationRequested();

                    var emp = deficit[di];
                    if (minHours[emp] - totalHours[emp] <= EPS)
                        continue;

                    while (minHours[emp] - totalHours[emp] > EPS)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (ops++ >= maxOps) return;

                        // (A) first try to use explicitly declared full-day availability before generic repairs.
                        if (TrySeedExplicitFullDayForEmployee(emp) ||
                            TryPromoteExplicitFullDayForEmployee(emp) ||
                            TryFillEmptyForEmployee(emp, requireNewDay: true) ||
                            TryFillEmptyForEmployee(emp, requireNewDay: false))
                        {
                            generatedOffRunPressure = BuildGeneratedOffRunPressure(
                                unavailable,
                                shiftsPerDay,
                                daysInMonth,
                                stride,
                                n);
                            improvedPass = true;
                            continue;
                        }

                        // (B) swap
                        if (TrySwapForEmployee(emp, requireNewDay: true) ||
                            TrySwapForEmployee(emp, requireNewDay: false))
                        {
                            generatedOffRunPressure = BuildGeneratedOffRunPressure(
                                unavailable,
                                shiftsPerDay,
                                daysInMonth,
                                stride,
                                n);
                            improvedPass = true;
                            continue;
                        }

                        break; // nothing else possible for this employee
                    }
                }

                if (!improvedPass)
                    break;
            }

            bool TrySeedExplicitFullDayForEmployee(int emp)
            {
                if (shiftCount < 2 || shifts.Count < 2)
                    return false;

                var bestDay = -1;
                var bestSlot = -1;
                var bestUnifiedDonor = int.MinValue;
                var bestShift1Donor = -1;
                var bestShift2Donor = -1;
                var bestHardCriticality = -1.0;
                var bestDonorSoftSurplus = double.NegativeInfinity;
                var bestDonorHardSurplus = double.NegativeInfinity;
                var bestFullDayHours = -1.0;

                for (var day = 1; day <= daysInMonth; day++)
                {
                    if (shiftsPerDay[emp * stride + day] != 0)
                        continue;

                    if (!CanUseExplicitFullDayOverride(
                            emp,
                            day,
                            minHours,
                            totalHours,
                            explicitFullDayAvailability,
                            shiftCount))
                    {
                        continue;
                    }

                    if (!TryGetDoubleShiftOption(
                            day,
                            emp,
                            shifts[0],
                            shifts[1],
                            unavailable,
                            availStartMin,
                            availEndMin,
                            n,
                            out var fullDayHours,
                            out _,
                            out _))
                    {
                        continue;
                    }

                    if (!CanAssignExactDayState(
                            schedule,
                            emp,
                            day,
                            addedShiftCount: 2,
                            addedHours: fullDayHours,
                            minHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            explicitFullDayAvailability,
                            daysInMonth,
                            shiftCount,
                            stride))
                    {
                        continue;
                    }

                    for (var slot = 0; slot < pps; slot++)
                    {
                        var pos1 = SlotIndex(day, 0, slot, pps, shiftCount);
                        var pos2 = SlotIndex(day, 1, slot, pps, shiftCount);
                        var donor1 = assigned[pos1];
                        var donor2 = assigned[pos2];

                        if (donor1 >= 0 && donor2 >= 0 && donor1 != donor2)
                            continue;

                        var donorSoftSurplus = double.PositiveInfinity;
                        var donorHardSurplus = double.PositiveInfinity;

                        if (donor1 >= 0 || donor2 >= 0)
                        {
                            var donor = donor1 >= 0 ? donor1 : donor2;
                            var removedHours = slotHours[pos1] + slotHours[pos2];
                            if (totalHours[donor] - removedHours < minHours[donor] - EPS)
                                continue;

                            donorSoftSurplus = totalHours[donor] - removedHours - desiredHours[donor];
                            donorHardSurplus = totalHours[donor] - removedHours - minHours[donor];
                        }

                        var hardCriticality = ComputeHardCriticality(emp, day, minHours, totalHours, remainingPotentialHours, stride);
                        var currentUsesEmptyPair = donor1 < 0 && donor2 < 0;
                        var bestUsesEmptyPair = bestUnifiedDonor == int.MinValue;

                        if (bestDay < 0
                            || (currentUsesEmptyPair && !bestUsesEmptyPair)
                            || (currentUsesEmptyPair == bestUsesEmptyPair && hardCriticality > bestHardCriticality + EPS)
                            || (currentUsesEmptyPair == bestUsesEmptyPair && Math.Abs(hardCriticality - bestHardCriticality) <= EPS && donorSoftSurplus > bestDonorSoftSurplus + EPS)
                            || (currentUsesEmptyPair == bestUsesEmptyPair && Math.Abs(hardCriticality - bestHardCriticality) <= EPS && Math.Abs(donorSoftSurplus - bestDonorSoftSurplus) <= EPS && donorHardSurplus > bestDonorHardSurplus + EPS)
                            || (currentUsesEmptyPair == bestUsesEmptyPair && Math.Abs(hardCriticality - bestHardCriticality) <= EPS && Math.Abs(donorSoftSurplus - bestDonorSoftSurplus) <= EPS && Math.Abs(donorHardSurplus - bestDonorHardSurplus) <= EPS && fullDayHours > bestFullDayHours + EPS))
                        {
                            bestDay = day;
                            bestSlot = slot;
                            bestUnifiedDonor = donor1 >= 0 ? donor1 : donor2;
                            bestShift1Donor = donor1;
                            bestShift2Donor = donor2;
                            bestHardCriticality = hardCriticality;
                            bestDonorSoftSurplus = donorSoftSurplus;
                            bestDonorHardSurplus = donorHardSurplus;
                            bestFullDayHours = fullDayHours;
                        }
                    }
                }

                if (bestDay < 0 || bestSlot < 0)
                    return false;

                if (bestShift1Donor >= 0)
                {
                    SwapSlot(
                        bestDay,
                        0,
                        bestSlot,
                        bestShift1Donor,
                        emp,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        availStartMin,
                        availEndMin,
                        shifts,
                        totalHours,
                        shiftsPerDay,
                        fullDaysCount,
                        pps,
                        shiftCount,
                        stride,
                        n);
                }
                else
                {
                    AssignToEmptySlot(
                        bestDay,
                        0,
                        bestSlot,
                        emp,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        availStartMin,
                        availEndMin,
                        shifts,
                        totalHours,
                        shiftsPerDay,
                        fullDaysCount,
                        shiftCount,
                        stride,
                        pps,
                        n);
                }

                if (bestShift2Donor >= 0)
                {
                    SwapSlot(
                        bestDay,
                        1,
                        bestSlot,
                        bestShift2Donor,
                        emp,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        availStartMin,
                        availEndMin,
                        shifts,
                        totalHours,
                        shiftsPerDay,
                        fullDaysCount,
                        pps,
                        shiftCount,
                        stride,
                        n);
                }
                else
                {
                    AssignToEmptySlot(
                        bestDay,
                        1,
                        bestSlot,
                        emp,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        availStartMin,
                        availEndMin,
                        shifts,
                        totalHours,
                        shiftsPerDay,
                        fullDaysCount,
                        shiftCount,
                        stride,
                        pps,
                        n);
                }

                return true;
            }

            bool TryPromoteExplicitFullDayForEmployee(int emp)
            {
                if (shiftCount < 2 || shifts.Count < 2)
                    return false;

                var bestDay = -1;
                var bestShift = -1;
                var bestSlot = -1;
                var bestDonor = int.MinValue;
                var bestEffectiveHours = -1.0;
                var bestHardCriticality = -1.0;
                var bestDonorSoftSurplus = double.NegativeInfinity;
                var bestDonorHardSurplus = double.NegativeInfinity;

                for (var day = 1; day <= daysInMonth; day++)
                {
                    if (shiftsPerDay[emp * stride + day] != 1)
                        continue;

                    if (!CanUseExplicitFullDayOverride(
                            emp,
                            day,
                            minHours,
                            totalHours,
                            explicitFullDayAvailability,
                            shiftCount))
                    {
                        continue;
                    }

                    var existingShift = -1;
                    var existingSlot = -1;
                    var missingShift = -1;

                    for (var s = 0; s < shiftCount; s++)
                    {
                        var slotIndex = FindEmployeeSlotIndexInShift(assigned, day, s, pps, shiftCount, emp);
                        if (slotIndex >= 0)
                        {
                            if (existingShift >= 0)
                            {
                                existingShift = -2;
                                break;
                            }

                            existingShift = s;
                            existingSlot = slotIndex;
                        }
                        else if (missingShift < 0)
                        {
                            missingShift = s;
                        }
                    }

                    if (existingShift < 0 || existingSlot < 0 || missingShift < 0)
                        continue;

                    var pos = SlotIndex(day, missingShift, existingSlot, pps, shiftCount);
                    var donor = assigned[pos];
                    if (donor == emp)
                        continue;

                    if (!TryPredictAssignmentImpact(
                            day,
                            missingShift,
                            existingSlot,
                            emp,
                            assigned,
                            slotFromMin,
                            slotToMin,
                            slotHours,
                            availStartMin,
                            availEndMin,
                            shifts,
                            n,
                            pps,
                            shiftCount,
                            out var empHours,
                            out var pairedEmp,
                            out var pairedDelta))
                    {
                        continue;
                    }

                    var effectiveAddedHours = empHours;
                    if (pairedEmp == emp)
                        effectiveAddedHours += pairedDelta;

                    if (effectiveAddedHours <= EPS)
                        continue;

                    if (schedule.MaxHoursPerEmpMonth > 0
                        && totalHours[emp] + effectiveAddedHours > schedule.MaxHoursPerEmpMonth + EPS)
                    {
                        continue;
                    }

                    if (!CanAddShiftOrderIndependent(
                            schedule,
                            emp,
                            day,
                            effectiveAddedHours,
                            minHours,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            explicitFullDayAvailability,
                            daysInMonth,
                            shiftCount,
                            stride))
                    {
                        continue;
                    }

                    var donorSoftSurplus = double.PositiveInfinity;
                    var donorHardSurplus = double.PositiveInfinity;
                    if (donor >= 0)
                    {
                        var donorRemovedHours = slotHours[pos];
                        if (totalHours[donor] - donorRemovedHours < minHours[donor] - EPS)
                            continue;

                        donorSoftSurplus = totalHours[donor] - donorRemovedHours - desiredHours[donor];
                        donorHardSurplus = totalHours[donor] - donorRemovedHours - minHours[donor];
                    }

                    var hardCriticality = ComputeHardCriticality(emp, day, minHours, totalHours, remainingPotentialHours, stride);
                    var candidateUsesEmptySlot = donor < 0;
                    var bestUsesEmptySlot = bestDonor < 0;

                    if (bestDay < 0
                        || (candidateUsesEmptySlot && !bestUsesEmptySlot)
                        || (candidateUsesEmptySlot == bestUsesEmptySlot && hardCriticality > bestHardCriticality + EPS)
                        || (candidateUsesEmptySlot == bestUsesEmptySlot && Math.Abs(hardCriticality - bestHardCriticality) <= EPS && donorSoftSurplus > bestDonorSoftSurplus + EPS)
                        || (candidateUsesEmptySlot == bestUsesEmptySlot && Math.Abs(hardCriticality - bestHardCriticality) <= EPS && Math.Abs(donorSoftSurplus - bestDonorSoftSurplus) <= EPS && donorHardSurplus > bestDonorHardSurplus + EPS)
                        || (candidateUsesEmptySlot == bestUsesEmptySlot && Math.Abs(hardCriticality - bestHardCriticality) <= EPS && Math.Abs(donorSoftSurplus - bestDonorSoftSurplus) <= EPS && Math.Abs(donorHardSurplus - bestDonorHardSurplus) <= EPS && effectiveAddedHours > bestEffectiveHours + EPS))
                    {
                        bestDay = day;
                        bestShift = missingShift;
                        bestSlot = existingSlot;
                        bestDonor = donor;
                        bestEffectiveHours = effectiveAddedHours;
                        bestHardCriticality = hardCriticality;
                        bestDonorSoftSurplus = donorSoftSurplus;
                        bestDonorHardSurplus = donorHardSurplus;
                    }
                }

                if (bestDay < 0)
                    return false;

                if (bestDonor >= 0)
                {
                    SwapSlot(
                        bestDay,
                        bestShift,
                        bestSlot,
                        bestDonor,
                        emp,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        availStartMin,
                        availEndMin,
                        shifts,
                        totalHours,
                        shiftsPerDay,
                        fullDaysCount,
                        pps,
                        shiftCount,
                        stride,
                        n);
                }
                else
                {
                    AssignToEmptySlot(
                        bestDay,
                        bestShift,
                        bestSlot,
                        emp,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        availStartMin,
                        availEndMin,
                        shifts,
                        totalHours,
                        shiftsPerDay,
                        fullDaysCount,
                        shiftCount,
                        stride,
                        pps,
                        n);
                }

                return true;
            }

            bool TryFillEmptyForEmployee(int emp, bool requireNewDay)
            {
                var bestDay = -1;
                var bestShift = -1;
                var bestSlot = -1;
                var bestHours = 0.0;
                var bestCreatesFull = true;
                var bestHardCriticality = -1.0;
                var bestUsefulGain = -1.0;
                var bestRestPressure = -1;

                for (var day = 1; day <= daysInMonth; day++)
                {
                    if (unavailable[day * n + emp])
                        continue;

                    var cur = shiftsPerDay[emp * stride + day];
                    if (cur >= shiftCount)
                        continue;

                    if (requireNewDay && cur != 0)
                        continue;

                    for (var s = 0; s < shiftCount; s++)
                    {
                        if (IsEmployeeInShift(assigned, day, s, pps, shiftCount, emp))
                            continue;

                        for (var slot = 0; slot < pps; slot++)
                        {
                            var pos = SlotIndex(day, s, slot, pps, shiftCount);
                            if (assigned[pos] >= 0)
                                continue; // not empty

                            if (!TryPredictAssignmentImpact(
                                    day,
                                    s,
                                    slot,
                                    emp,
                                    assigned,
                                    slotFromMin,
                                    slotToMin,
                                    slotHours,
                                    availStartMin,
                                    availEndMin,
                                    shifts,
                                    n,
                                    pps,
                                    shiftCount,
                                    out var empHours,
                                    out var pairedEmp,
                                    out var pairedDelta))
                                continue;

                            if (empHours <= EPS)
                                continue;

                            // max hours for candidate and paired
                            if (schedule.MaxHoursPerEmpMonth > 0)
                            {
                                if (totalHours[emp] + empHours > schedule.MaxHoursPerEmpMonth + EPS)
                                    continue;

                                if (pairedEmp >= 0 && pairedDelta > EPS)
                                {
                                    if (totalHours[pairedEmp] + pairedDelta > schedule.MaxHoursPerEmpMonth + EPS)
                                        continue;
                                }
                            }

                            if (!CanAddShiftOrderIndependent(
                                    schedule, emp, day, empHours,
                                    minHours,
                                    totalHours, shiftsPerDay, fullDaysCount,
                                    explicitFullDayAvailability,
                                    daysInMonth, shiftCount, stride))
                                continue;

                            var createsFull = (shiftCount >= 2 && cur == 1); // would make day full (1->2)
                            var hardGap = Math.Max(0.0, minHours[emp] - totalHours[emp]);
                            var usefulGain = Math.Min(empHours, hardGap > EPS ? hardGap : Math.Max(0.0, desiredHours[emp] - totalHours[emp]));
                            var hardCriticality = ComputeHardCriticality(emp, day, minHours, totalHours, remainingPotentialHours, stride);
                            var generatedRunPressure = generatedOffRunPressure[emp * stride + day];
                            var boundaryPressure = unavailableBoundaryPressure[emp * stride + day];

                            // Prefer the most fragile deficit first, then avoid unnecessary full days.
                            if (bestDay < 0
                                || hardCriticality > bestHardCriticality + EPS
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure > bestRestPressure)
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure > unavailableBoundaryPressure[emp * stride + bestDay])
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure == unavailableBoundaryPressure[emp * stride + bestDay] && !createsFull && bestCreatesFull)
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure == unavailableBoundaryPressure[emp * stride + bestDay] && createsFull == bestCreatesFull && usefulGain > bestUsefulGain + EPS)
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure == unavailableBoundaryPressure[emp * stride + bestDay] && createsFull == bestCreatesFull && Math.Abs(usefulGain - bestUsefulGain) <= EPS && empHours > bestHours + EPS))
                            {
                                bestDay = day;
                                bestShift = s;
                                bestSlot = slot;
                                bestHours = empHours;
                                bestCreatesFull = createsFull;
                                bestHardCriticality = hardCriticality;
                                bestUsefulGain = usefulGain;
                                bestRestPressure = generatedRunPressure;
                            }
                        }
                    }
                }

                if (bestDay < 0)
                    return false;

                AssignToEmptySlot(bestDay, bestShift, bestSlot, emp,
                    assigned,
                    slotFromMin, slotToMin, slotHours,
                    availStartMin, availEndMin,
                    shifts,
                    totalHours,
                    shiftsPerDay,
                    fullDaysCount,
                    shiftCount,
                    stride,
                    pps,
                    n);

                return true;
            }

            bool TrySwapForEmployee(int emp, bool requireNewDay)
            {
                var bestDay = -1;
                var bestShift = -1;
                var bestSlot = -1;
                var bestReceiverHours = 0.0;
                var bestDonor = -1;
                var bestDonorHardSurplus = -1.0;
                var bestDonorSoftSurplus = double.NegativeInfinity;
                var bestCreatesFull = true;
                var bestHardCriticality = -1.0;
                var bestUsefulGain = -1.0;
                var bestRestPressure = -1;

                for (var day = 1; day <= daysInMonth; day++)
                {
                    if (unavailable[day * n + emp])
                        continue;

                    var cur = shiftsPerDay[emp * stride + day];
                    if (cur >= shiftCount)
                        continue;

                    if (requireNewDay && cur != 0)
                        continue;

                    for (var s = 0; s < shiftCount; s++)
                    {
                        if (IsEmployeeInShift(assigned, day, s, pps, shiftCount, emp))
                            continue;

                        for (var slot = 0; slot < pps; slot++)
                        {
                            var pos = SlotIndex(day, s, slot, pps, shiftCount);
                            var donor = assigned[pos];
                            if (donor < 0) continue;
                            if (donor == emp) continue;

                            // donor must remain >= its MinHours after giving away this slot (using ACTUAL hours)
                            var donorRemovedHours = slotHours[pos];
                            if (totalHours[donor] - donorRemovedHours < minHours[donor] - EPS)
                                continue;

                            if (!TryPredictAssignmentImpact(
                                    day,
                                    s,
                                    slot,
                                    emp,
                                    assigned,
                                    slotFromMin,
                                    slotToMin,
                                    slotHours,
                                    availStartMin,
                                    availEndMin,
                                    shifts,
                                    n,
                                    pps,
                                    shiftCount,
                                    out var empHours,
                                    out var pairedEmp,
                                    out var pairedDelta))
                                continue;

                            if (empHours <= EPS)
                                continue;

                            // max hours for receiver and paired
                            if (schedule.MaxHoursPerEmpMonth > 0)
                            {
                                if (totalHours[emp] + empHours > schedule.MaxHoursPerEmpMonth + EPS)
                                    continue;

                                if (pairedEmp >= 0 && pairedDelta > EPS)
                                {
                                    if (totalHours[pairedEmp] + pairedDelta > schedule.MaxHoursPerEmpMonth + EPS)
                                        continue;
                                }
                            }

                            if (!CanAddShiftOrderIndependent(
                                    schedule, emp, day, empHours,
                                    minHours,
                                    totalHours, shiftsPerDay, fullDaysCount,
                                    explicitFullDayAvailability,
                                    daysInMonth, shiftCount, stride))
                                continue;

                            var hardGap = Math.Max(0.0, minHours[emp] - totalHours[emp]);
                            var usefulGain = Math.Min(empHours, hardGap > EPS ? hardGap : Math.Max(0.0, desiredHours[emp] - totalHours[emp]));
                            var hardCriticality = ComputeHardCriticality(emp, day, minHours, totalHours, remainingPotentialHours, stride);
                            var donorHardSurplus = totalHours[donor] - donorRemovedHours - minHours[donor];
                            var donorSoftSurplus = totalHours[donor] - donorRemovedHours - desiredHours[donor];
                            var createsFull = (shiftCount >= 2 && cur == 1);
                            var generatedRunPressure = generatedOffRunPressure[emp * stride + day];
                            var boundaryPressure = unavailableBoundaryPressure[emp * stride + day];

                            if (bestDay < 0
                                || hardCriticality > bestHardCriticality + EPS
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure > bestRestPressure)
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure > unavailableBoundaryPressure[emp * stride + bestDay])
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure == unavailableBoundaryPressure[emp * stride + bestDay] && donorSoftSurplus > bestDonorSoftSurplus + EPS)
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure == unavailableBoundaryPressure[emp * stride + bestDay] && Math.Abs(donorSoftSurplus - bestDonorSoftSurplus) <= EPS && donorHardSurplus > bestDonorHardSurplus + EPS)
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure == unavailableBoundaryPressure[emp * stride + bestDay] && Math.Abs(donorSoftSurplus - bestDonorSoftSurplus) <= EPS && Math.Abs(donorHardSurplus - bestDonorHardSurplus) <= EPS && !createsFull && bestCreatesFull)
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure == unavailableBoundaryPressure[emp * stride + bestDay] && Math.Abs(donorSoftSurplus - bestDonorSoftSurplus) <= EPS && Math.Abs(donorHardSurplus - bestDonorHardSurplus) <= EPS && createsFull == bestCreatesFull && usefulGain > bestUsefulGain + EPS)
                                || (Math.Abs(hardCriticality - bestHardCriticality) <= EPS && generatedRunPressure == bestRestPressure && boundaryPressure == unavailableBoundaryPressure[emp * stride + bestDay] && Math.Abs(donorSoftSurplus - bestDonorSoftSurplus) <= EPS && Math.Abs(donorHardSurplus - bestDonorHardSurplus) <= EPS && createsFull == bestCreatesFull && Math.Abs(usefulGain - bestUsefulGain) <= EPS && empHours > bestReceiverHours + EPS))
                            {
                                bestDay = day;
                                bestShift = s;
                                bestSlot = slot;
                                bestReceiverHours = empHours;
                                bestDonor = donor;
                                bestDonorHardSurplus = donorHardSurplus;
                                bestDonorSoftSurplus = donorSoftSurplus;
                                bestCreatesFull = createsFull;
                                bestHardCriticality = hardCriticality;
                                bestUsefulGain = usefulGain;
                                bestRestPressure = generatedRunPressure;
                            }
                        }
                    }
                }

                if (bestDay < 0 || bestDonor < 0)
                    return false;

                SwapSlot(bestDay, bestShift, bestSlot, bestDonor, emp,
                    assigned,
                    slotFromMin, slotToMin, slotHours,
                    availStartMin, availEndMin,
                    shifts,
                    totalHours,
                    shiftsPerDay,
                    fullDaysCount,
                    pps,
                    shiftCount,
                    stride,
                    n);

                return true;
            }
        }

        // =========================
        // Order-independent constraints (scan left/right)
        // =========================
        private static bool CanAddShiftOrderIndependent(
            ScheduleModel schedule,
            int emp,
            int day,
            double addedHours,
            double[] minHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            bool[] explicitFullDayAvailability,
            int daysInMonth,
            int shiftCount,
            int stride)
        {
            // Max hours per month
            if (schedule.MaxHoursPerEmpMonth > 0)
            {
                if (totalHours[emp] + addedHours > schedule.MaxHoursPerEmpMonth + EPS)
                    return false;
            }

            var cur = shiftsPerDay[emp * stride + day];
            if (cur >= shiftCount)
                return false;

            // Max consecutive working days (only if day becomes working: 0 -> 1)
            if (schedule.MaxConsecutiveDays > 0 && cur == 0)
            {
                var streak = 1
                             + CountLeft(emp, day, shiftsPerDay, stride, v => v > 0)
                             + CountRight(emp, day, daysInMonth, shiftsPerDay, stride, v => v > 0);

                if (streak > schedule.MaxConsecutiveDays)
                    return false;
            }

            // Full day constraints (only when day becomes full: 1 -> 2)
            if (shiftCount >= 2 && cur == 1 && (schedule.MaxFullPerMonth > 0 || schedule.MaxConsecutiveFull > 0))
            {
                var exceedsFullPerMonth = schedule.MaxFullPerMonth > 0 && fullDaysCount[emp] + 1 > schedule.MaxFullPerMonth;
                var exceedsConsecutiveFull = false;

                if (schedule.MaxConsecutiveFull > 0)
                {
                    var fullStreak = 1
                                     + CountLeft(emp, day, shiftsPerDay, stride, v => v >= 2)
                                     + CountRight(emp, day, daysInMonth, shiftsPerDay, stride, v => v >= 2);

                    exceedsConsecutiveFull = fullStreak > schedule.MaxConsecutiveFull;
                }

                if ((exceedsFullPerMonth || exceedsConsecutiveFull)
                    && !CanUseExplicitFullDayOverride(
                        emp,
                        day,
                        minHours,
                        totalHours,
                        explicitFullDayAvailability,
                        shiftCount))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountLeft(int emp, int day, int[] shiftsPerDay, int stride, Func<int, bool> predicate)
        {
            var cnt = 0;
            for (var d = day - 1; d >= 1; d--)
            {
                if (predicate(shiftsPerDay[emp * stride + d])) cnt++;
                else break;
            }
            return cnt;
        }

        private static int CountRight(int emp, int day, int daysInMonth, int[] shiftsPerDay, int stride, Func<int, bool> predicate)
        {
            var cnt = 0;
            for (var d = day + 1; d <= daysInMonth; d++)
            {
                if (predicate(shiftsPerDay[emp * stride + d])) cnt++;
                else break;
            }
            return cnt;
        }

        private static void BuildPotentialHours(
            int daysInMonth,
            List<ShiftTemplate> shifts,
            bool[] unavailable,
            int[] availStartMin,
            int[] availEndMin,
            int stride,
            int n,
            double[] remainingPotentialHours,
            double[] totalPotentialHours)
        {
            var dailyPotentialHours = new double[n * stride];

            for (var day = 1; day <= daysInMonth; day++)
            {
                for (var emp = 0; emp < n; emp++)
                {
                    var idx = day * n + emp;
                    if (unavailable[idx])
                        continue;

                    var aStart = availStartMin[idx];
                    var aEnd = availEndMin[idx];
                    var total = 0.0;

                    for (var s = 0; s < shifts.Count; s++)
                    {
                        var shift = shifts[s];
                        var from = Math.Max(shift.StartMin, aStart);
                        var to = Math.Min(shift.EndMin, aEnd);
                        if (to > from)
                            total += (to - from) / 60d;
                    }

                    dailyPotentialHours[emp * stride + day] = total;
                }
            }

            for (var emp = 0; emp < n; emp++)
            {
                var suffix = 0.0;
                for (var day = daysInMonth; day >= 1; day--)
                {
                    suffix += dailyPotentialHours[emp * stride + day];
                    remainingPotentialHours[emp * stride + day] = suffix;
                }

                totalPotentialHours[emp] = suffix;
            }
        }

        private static double[] BuildDesiredHours(
            double[] minHours,
            double[] totalPotentialHours,
            int maxHoursPerEmpMonth,
            List<ShiftTemplate> shifts,
            int daysInMonth,
            int pps)
        {
            var n = minHours.Length;
            var desired = new double[n];
            var cappedPotential = new double[n];
            var totalDemand = 0.0;

            for (var i = 0; i < shifts.Count; i++)
                totalDemand += shifts[i].Hours * daysInMonth * pps;

            for (var emp = 0; emp < n; emp++)
            {
                var cap = totalPotentialHours[emp];
                if (maxHoursPerEmpMonth > 0)
                    cap = Math.Min(cap, maxHoursPerEmpMonth);

                cappedPotential[emp] = Math.Max(0.0, cap);
                desired[emp] = Math.Min(Math.Max(0.0, minHours[emp]), cappedPotential[emp]);
                totalDemand -= desired[emp];
            }

            if (totalDemand <= EPS)
                return desired;

            while (totalDemand > EPS)
            {
                var activeEmployees = 0;
                for (var emp = 0; emp < n; emp++)
                {
                    if (cappedPotential[emp] - desired[emp] > EPS)
                        activeEmployees++;
                }

                if (activeEmployees <= 0)
                    break;

                var fairShare = totalDemand / activeEmployees;
                var distributed = 0.0;
                for (var emp = 0; emp < n; emp++)
                {
                    var headroom = Math.Max(0.0, cappedPotential[emp] - desired[emp]);
                    if (headroom <= EPS)
                        continue;

                    var add = Math.Min(headroom, fairShare);
                    if (add <= EPS)
                        continue;

                    desired[emp] += add;
                    distributed += add;
                }

                if (distributed <= EPS)
                    break;

                totalDemand -= distributed;
            }

            return desired;
        }

        private static int[] BuildUnavailableBoundaryPressure(
            int daysInMonth,
            bool[] unavailable,
            int stride,
            int n)
        {
            var pressure = new int[n * stride];

            for (var emp = 0; emp < n; emp++)
            {
                var leftRun = 0;
                for (var day = 1; day <= daysInMonth; day++)
                {
                    if (unavailable[day * n + emp])
                    {
                        leftRun++;
                        continue;
                    }

                    if (leftRun > 0)
                        pressure[emp * stride + day] += leftRun;

                    leftRun = 0;
                }

                var rightRun = 0;
                for (var day = daysInMonth; day >= 1; day--)
                {
                    if (unavailable[day * n + emp])
                    {
                        rightRun++;
                        continue;
                    }

                    if (rightRun > 0)
                        pressure[emp * stride + day] += rightRun;

                    rightRun = 0;
                }
            }

            return pressure;
        }

        private static CandidatePriority BuildCandidatePriority(
            bool pairsOtherShiftSameSlot,
            bool blocksOtherShiftPair,
            int emp,
            int day,
            double gainHours,
            double[] minHours,
            double[] desiredHours,
            double[] remainingPotentialHours,
            double[] totalHours,
            int[] fullDaysCount,
            int[] scarcityDays,
            int[] unavailableBoundaryPressure,
            int rrCursor,
            int n,
            int stride)
        {
            var hardTarget = Math.Max(0.0, minHours[emp]);
            var hardDeficit = Math.Max(0.0, hardTarget - totalHours[emp]);
            var remainingPotential = Math.Max(EPS, remainingPotentialHours[emp * stride + day]);
            var hardCoverage = hardTarget > EPS ? totalHours[emp] / hardTarget : 1.0;
            var hardSlack = remainingPotential - hardDeficit;
            var isCriticalHardNeed = hardDeficit > EPS && hardSlack <= gainHours + EPS;
            var hardCriticality = hardDeficit > EPS ? hardDeficit / remainingPotential : 0.0;

            var softTarget = Math.Max(0.0, desiredHours[emp]);
            var softGap = Math.Max(0.0, softTarget - totalHours[emp]);
            var softCoverage = softTarget > EPS
                ? totalHours[emp] / softTarget
                : (totalHours[emp] > EPS ? double.MaxValue : 0.0);

            var total = totalHours[emp];
            var full = fullDaysCount[emp];
            var scar = scarcityDays[emp] <= 0 ? int.MaxValue : scarcityDays[emp];
            var availabilityPressure = unavailableBoundaryPressure[emp * stride + day];
            var rrDist = emp >= rrCursor ? (emp - rrCursor) : (emp + n - rrCursor);

            return new CandidatePriority(
                pairsOtherShiftSameSlot: pairsOtherShiftSameSlot,
                blocksOtherShiftPair: blocksOtherShiftPair,
                hasHardNeed: hardDeficit > EPS,
                isCriticalHardNeed: isCriticalHardNeed,
                hardSlack: hardSlack,
                hardCriticality: hardCriticality,
                hardCoverage: hardCoverage,
                hardDeficit: hardDeficit,
                softGap: softGap,
                softCoverage: softCoverage,
                gainHours: gainHours,
                totalHours: total,
                fullDays: full,
                scarcity: scar,
                availabilityPressure: availabilityPressure,
                roundRobinDistance: rrDist);
        }

        private static bool IsBetterCandidate(CandidatePriority candidate, CandidatePriority best)
        {
            if (candidate.PairsOtherShiftSameSlot != best.PairsOtherShiftSameSlot)
                return candidate.PairsOtherShiftSameSlot;

            if (candidate.BlocksOtherShiftPair != best.BlocksOtherShiftPair)
                return !candidate.BlocksOtherShiftPair;

            if (candidate.HasHardNeed != best.HasHardNeed)
                return candidate.HasHardNeed;

            if (candidate.IsCriticalHardNeed != best.IsCriticalHardNeed)
                return candidate.IsCriticalHardNeed;

            if (candidate.HasHardNeed && candidate.IsCriticalHardNeed)
            {
                if (candidate.HardSlack < best.HardSlack - EPS)
                    return true;

                if (Math.Abs(candidate.HardSlack - best.HardSlack) <= EPS
                    && candidate.HardCriticality > best.HardCriticality + EPS)
                    return true;

                if (Math.Abs(candidate.HardSlack - best.HardSlack) <= EPS
                    && Math.Abs(candidate.HardCriticality - best.HardCriticality) <= EPS
                    && candidate.HardCoverage < best.HardCoverage - EPS)
                    return true;
            }

            if (candidate.HasHardNeed)
            {
                if (candidate.HardCoverage < best.HardCoverage - EPS)
                    return true;

                if (Math.Abs(candidate.HardCoverage - best.HardCoverage) <= EPS
                    && candidate.HardCriticality > best.HardCriticality + EPS)
                    return true;

                if (Math.Abs(candidate.HardCoverage - best.HardCoverage) <= EPS
                    && Math.Abs(candidate.HardCriticality - best.HardCriticality) <= EPS
                    && candidate.HardDeficit > best.HardDeficit + EPS)
                    return true;
            }

            if (candidate.AvailabilityPressure != best.AvailabilityPressure)
                return candidate.AvailabilityPressure > best.AvailabilityPressure;

            if (candidate.SoftCoverage < best.SoftCoverage - EPS)
                return true;

            if (candidate.SoftCoverage > best.SoftCoverage + EPS)
                return false;

            if (candidate.SoftGap > best.SoftGap + EPS)
                return true;

            if (Math.Abs(candidate.SoftGap - best.SoftGap) <= EPS
                && candidate.GainHours > best.GainHours + EPS)
                return true;

            if (Math.Abs(candidate.SoftGap - best.SoftGap) <= EPS
                && Math.Abs(candidate.GainHours - best.GainHours) <= EPS
                && candidate.TotalHours < best.TotalHours - EPS)
                return true;

            if (Math.Abs(candidate.SoftGap - best.SoftGap) <= EPS
                && Math.Abs(candidate.GainHours - best.GainHours) <= EPS
                && Math.Abs(candidate.TotalHours - best.TotalHours) <= EPS
                && candidate.FullDays < best.FullDays)
                return true;

            if (Math.Abs(candidate.SoftGap - best.SoftGap) <= EPS
                && Math.Abs(candidate.GainHours - best.GainHours) <= EPS
                && Math.Abs(candidate.TotalHours - best.TotalHours) <= EPS
                && candidate.FullDays == best.FullDays
                && candidate.Scarcity < best.Scarcity)
                return true;

            if (Math.Abs(candidate.SoftGap - best.SoftGap) <= EPS
                && Math.Abs(candidate.GainHours - best.GainHours) <= EPS
                && Math.Abs(candidate.TotalHours - best.TotalHours) <= EPS
                && candidate.FullDays == best.FullDays
                && candidate.Scarcity == best.Scarcity
                && candidate.RoundRobinDistance < best.RoundRobinDistance)
                return true;

            return false;
        }

        private static double ComputeHardCriticality(
            int emp,
            int day,
            double[] minHours,
            double[] totalHours,
            double[] remainingPotentialHours,
            int stride)
        {
            var hardDeficit = Math.Max(0.0, minHours[emp] - totalHours[emp]);
            if (hardDeficit <= EPS)
                return 0.0;

            var remainingPotential = Math.Max(EPS, remainingPotentialHours[emp * stride + day]);
            return hardDeficit / remainingPotential;
        }

        // =========================
        // Phase 1: fast candidate selection + constraints (O(1))
        // =========================
        private static int PickCandidatePhase1Fast(
            int[] availableToday,
            int day,
            int shiftIdx,
            int slotIdx,
            bool preferNoSecondShift,
            ScheduleModel schedule,
            double[] minHours,
            double[] desiredHours,
            double[] remainingPotentialHours,
            int[] scarcityDays,
            int[] unavailableBoundaryPressure,
            double[] totalHours,
            int[] fullDaysCount,
            int[] shiftsToday,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
            int stride,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            int n,
            int pps,
            int[] assignedStamp,
            int stamp,
            int rrCursor)
        {
            var best = FindBestPhase1Fast(
                availableToday,
                day,
                shiftIdx,
                slotIdx,
                restrictSecondShift: preferNoSecondShift,
                schedule,
                minHours,
                desiredHours,
                remainingPotentialHours,
                scarcityDays,
                unavailableBoundaryPressure,
                totalHours,
                fullDaysCount,
                shiftsToday,
                lastWorkedDay,
                consecutiveDays,
                lastFullDay,
                consecutiveFullDays,
                shiftCount,
                stride,
                unavailable,
                explicitFullDayAvailability,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                availStartMin,
                availEndMin,
                shifts,
                n,
                pps,
                assignedStamp,
                stamp,
                rrCursor);

            if (best >= 0)
                return best;

            if (preferNoSecondShift)
            {
                best = FindBestPhase1Fast(
                    availableToday,
                    day,
                    shiftIdx,
                    slotIdx,
                    restrictSecondShift: false,
                    schedule,
                    minHours,
                    desiredHours,
                    remainingPotentialHours,
                    scarcityDays,
                    unavailableBoundaryPressure,
                    totalHours,
                    fullDaysCount,
                    shiftsToday,
                    lastWorkedDay,
                    consecutiveDays,
                    lastFullDay,
                    consecutiveFullDays,
                    shiftCount,
                    stride,
                    unavailable,
                    explicitFullDayAvailability,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    availStartMin,
                    availEndMin,
                    shifts,
                    n,
                    pps,
                    assignedStamp,
                    stamp,
                    rrCursor);
            }

            return best;
        }

        private static int FindBestPhase1Fast(
            int[] availableToday,
            int day,
            int shiftIdx,
            int slotIdx,
            bool restrictSecondShift,
            ScheduleModel schedule,
            double[] minHours,
            double[] desiredHours,
            double[] remainingPotentialHours,
            int[] scarcityDays,
            int[] unavailableBoundaryPressure,
            double[] totalHours,
            int[] fullDaysCount,
            int[] shiftsToday,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
            int stride,
            bool[] unavailable,
            bool[] explicitFullDayAvailability,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            int n,
            int pps,
            int[] assignedStamp,
            int stamp,
            int rrCursor)
        {
            var best = -1;
            CandidatePriority? bestPriority = null;

            for (var k = 0; k < availableToday.Length; k++)
            {
                var emp = availableToday[k];

                if (assignedStamp[emp] == stamp)
                    continue;

                var st = shiftsToday[emp];
                if (st >= shiftCount)
                    continue;

                if (restrictSecondShift && st != 0)
                    continue;

                if (!TryPredictAssignmentImpact(
                        day,
                        shiftIdx,
                        slotIdx,
                        emp,
                        assigned,
                        slotFromMin,
                        slotToMin,
                        slotHours,
                        availStartMin,
                        availEndMin,
                        shifts,
                        n,
                        pps,
                        shiftCount,
                        out var empHours,
                        out var pairedEmp,
                        out var pairedDelta))
                    continue;

                if (empHours <= EPS)
                    continue;

                // max hours for candidate and paired
                if (schedule.MaxHoursPerEmpMonth > 0)
                {
                    if (totalHours[emp] + empHours > schedule.MaxHoursPerEmpMonth + EPS)
                        continue;

                    if (pairedEmp >= 0 && pairedDelta > EPS)
                    {
                        if (totalHours[pairedEmp] + pairedDelta > schedule.MaxHoursPerEmpMonth + EPS)
                            continue;
                    }
                }

                if (!CanAssignPhase1Fast(
                        emp,
                        day,
                        empHours,
                        st,
                        schedule,
                        minHours,
                        totalHours,
                        fullDaysCount,
                        lastWorkedDay,
                        consecutiveDays,
                        lastFullDay,
                        consecutiveFullDays,
                        shiftCount,
                        explicitFullDayAvailability))
                    continue;

                var pairsOtherShiftSameSlot = IsEmployeeInOtherShiftSameSlot(assigned, day, shiftIdx, slotIdx, pps, shiftCount, emp);
                var blocksOtherShiftPair = BlocksOtherShiftPair(assigned, day, shiftIdx, slotIdx, pps, shiftCount, emp);

                var priority = BuildCandidatePriority(
                    pairsOtherShiftSameSlot,
                    blocksOtherShiftPair,
                    emp,
                    day,
                    empHours,
                    minHours,
                    desiredHours,
                    remainingPotentialHours,
                    totalHours,
                    fullDaysCount,
                    scarcityDays,
                    unavailableBoundaryPressure,
                    rrCursor,
                    n,
                    stride);

                if (bestPriority.HasValue && !IsBetterCandidate(priority, bestPriority.Value))
                    continue;

                best = emp;
                bestPriority = priority;
            }

            return best;
        }

        private static bool CanAssignPhase1Fast(
            int emp,
            int day,
            double addedHours,
            int shiftsAlreadyToday,
            ScheduleModel schedule,
            double[] minHours,
            double[] totalHours,
            int[] fullDaysCount,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
            bool[] explicitFullDayAvailability)
        {
            // Max hours
            if (schedule.MaxHoursPerEmpMonth > 0)
            {
                if (totalHours[emp] + addedHours > schedule.MaxHoursPerEmpMonth + EPS)
                    return false;
            }

            // Max consecutive working days (only when adding first shift of day)
            if (schedule.MaxConsecutiveDays > 0 && shiftsAlreadyToday == 0)
            {
                var newConsec = (lastWorkedDay[emp] == day - 1) ? (consecutiveDays[emp] + 1) : 1;
                if (newConsec > schedule.MaxConsecutiveDays)
                    return false;
            }

            // Full-day constraints (only when adding 2nd shift of day)
            if (shiftCount >= 2 && shiftsAlreadyToday == 1 &&
                (schedule.MaxFullPerMonth > 0 || schedule.MaxConsecutiveFull > 0))
            {
                var exceedsFullPerMonth = schedule.MaxFullPerMonth > 0 && fullDaysCount[emp] + 1 > schedule.MaxFullPerMonth;
                var exceedsConsecutiveFull = false;

                if (schedule.MaxConsecutiveFull > 0)
                {
                    var newFullConsec = (lastFullDay[emp] == day - 1) ? (consecutiveFullDays[emp] + 1) : 1;
                    exceedsConsecutiveFull = newFullConsec > schedule.MaxConsecutiveFull;
                }

                if ((exceedsFullPerMonth || exceedsConsecutiveFull)
                    && !CanUseExplicitFullDayOverride(
                        emp,
                        day,
                        minHours,
                        totalHours,
                        explicitFullDayAvailability,
                        shiftCount))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssignPhase1Fast(
            int day,
            int shiftIdx,
            int slotIdx,
            int emp,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int[] shiftsToday,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
            int stride,
            int pps,
            int n,
            int[] assignedStamp,
            int stamp)
        {
            var pos = SlotIndex(day, shiftIdx, slotIdx, pps, shiftCount);
            if (assigned[pos] >= 0) return;

            assigned[pos] = emp;
            assignedStamp[emp] = stamp;

            // We'll recompute hours from slotHours[pos] (currently 0) -> actual.

            var prevToday = shiftsToday[emp];
            var newToday = prevToday + 1;

            shiftsToday[emp] = newToday;
            shiftsPerDay[emp * stride + day] = newToday;

            // first shift of day -> working streak
            if (prevToday == 0)
            {
                if (lastWorkedDay[emp] == day - 1) consecutiveDays[emp] += 1;
                else consecutiveDays[emp] = 1;

                lastWorkedDay[emp] = day;
            }

            // becomes full day
            if (shiftCount >= 2 && prevToday == 1 && newToday == 2)
            {
                fullDaysCount[emp] += 1;

                if (lastFullDay[emp] == day - 1) consecutiveFullDays[emp] += 1;
                else consecutiveFullDays[emp] = 1;

                lastFullDay[emp] = day;
            }

            // Apply timing and hour deltas (also adjusts paired shift if needed)
            RecomputeAfterChange(day, shiftIdx, slotIdx,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                availStartMin,
                availEndMin,
                shifts,
                totalHours,
                n,
                pps,
                shiftCount);
        }

        // =========================
        // Mutations for phase2
        // =========================
        private static void AssignToEmptySlot(
            int day,
            int shiftIdx,
            int slotIdx,
            int emp,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int shiftCount,
            int stride,
            int pps,
            int n)
        {
            var pos = SlotIndex(day, shiftIdx, slotIdx, pps, shiftCount);
            if (assigned[pos] >= 0) return;

            assigned[pos] = emp;

            var p = emp * stride + day;
            var before = shiftsPerDay[p];
            var after = before + 1;
            shiftsPerDay[p] = after;

            if (shiftCount >= 2 && before == 1 && after == 2)
                fullDaysCount[emp] += 1;

            RecomputeAfterChange(day, shiftIdx, slotIdx,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                availStartMin,
                availEndMin,
                shifts,
                totalHours,
                n,
                pps,
                shiftCount);
        }

        private static void SwapSlot(
            int day,
            int shiftIdx,
            int slotIdx,
            int donor,
            int receiver,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int pps,
            int shiftCount,
            int stride,
            int n)
        {
            var pos = SlotIndex(day, shiftIdx, slotIdx, pps, shiftCount);
            if (assigned[pos] != donor)
                return;

            // remove donor hours
            totalHours[donor] -= slotHours[pos];
            slotHours[pos] = 0;

            // update donor day counts
            var dPos = donor * stride + day;
            var dBefore = shiftsPerDay[dPos];
            var dAfter = dBefore - 1;
            shiftsPerDay[dPos] = dAfter;

            if (shiftCount >= 2 && dBefore == 2 && dAfter == 1)
                fullDaysCount[donor] -= 1;

            // set receiver
            assigned[pos] = receiver;

            // update receiver day counts
            var rPos = receiver * stride + day;
            var rBefore = shiftsPerDay[rPos];
            var rAfter = rBefore + 1;
            shiftsPerDay[rPos] = rAfter;

            if (shiftCount >= 2 && rBefore == 1 && rAfter == 2)
                fullDaysCount[receiver] += 1;

            RecomputeAfterChange(day, shiftIdx, slotIdx,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                availStartMin,
                availEndMin,
                shifts,
                totalHours,
                n,
                pps,
                shiftCount);
        }

        // =========================
        // Time-aware prediction + recompute
        // =========================
        private static bool TryResolveShiftPairIntervals(
            int day,
            int emp1,
            int emp2,
            int[] availStartMin,
            int[] availEndMin,
            ShiftTemplate sh1,
            ShiftTemplate sh2,
            int n,
            out int from1,
            out int to1,
            out int from2,
            out int to2)
        {
            from1 = sh1.StartMin;
            to1 = sh1.EndMin;
            from2 = sh2.StartMin;
            to2 = sh2.EndMin;

            if (emp1 < 0 && emp2 < 0)
                return false;

            if (emp1 >= 0 && emp2 < 0)
            {
                var idx1 = day * n + emp1;
                from1 = Math.Max(sh1.StartMin, availStartMin[idx1]);
                to1 = Math.Min(sh1.EndMin, availEndMin[idx1]);
                return to1 > from1;
            }

            if (emp2 >= 0 && emp1 < 0)
            {
                var idx2 = day * n + emp2;
                from2 = Math.Max(sh2.StartMin, availStartMin[idx2]);
                to2 = Math.Min(sh2.EndMin, availEndMin[idx2]);
                return to2 > from2;
            }

            var idxEmp1 = day * n + emp1;
            var a1Start = availStartMin[idxEmp1];
            var a1End = availEndMin[idxEmp1];

            var idxEmp2 = day * n + emp2;
            var a2Start = availStartMin[idxEmp2];
            var a2End = availEndMin[idxEmp2];

            var lowerBoundary = Math.Max(sh1.StartMin, a2Start);
            var upperBoundary = Math.Min(sh2.EndMin, a1End);
            if (upperBoundary < lowerBoundary)
                return false;

            var boundary = Clamp(sh2.StartMin, lowerBoundary, upperBoundary);

            from1 = Math.Max(sh1.StartMin, a1Start);
            to1 = Math.Min(boundary, a1End);

            from2 = boundary;
            to2 = Math.Min(sh2.EndMin, a2End);

            return to1 > from1 && to2 > from2;
        }

        private static bool TryPredictAssignmentImpact(
            int day,
            int shiftIdx,
            int slotIdx,
            int candidateEmp,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            int n,
            int pps,
            int shiftCount,
            out double candidateHours,
            out int pairedEmp,
            out double pairedDelta)
        {
            candidateHours = 0;
            pairedEmp = -1;
            pairedDelta = 0;

            if (candidateEmp < 0) return false;

            if (shiftCount > 1)
            {
                var otherShiftIdx = shiftIdx == 0 ? 1 : 0;
                var otherShiftSlotIdx = FindEmployeeSlotIndexInShift(assigned, day, otherShiftIdx, pps, shiftCount, candidateEmp);
                if (otherShiftSlotIdx >= 0 && otherShiftSlotIdx != slotIdx)
                    return false;
            }

            var idx = day * n + candidateEmp;
            var aStart = availStartMin[idx];
            var aEnd = availEndMin[idx];
            if (aEnd <= aStart)
                return false;

            if (shiftCount <= 1)
            {
                var sh = shifts[0];
                var from = Math.Max(sh.StartMin, aStart);
                var to = Math.Min(sh.EndMin, aEnd);
                if (to <= from) return false;
                candidateHours = (to - from) / 60d;
                pairedEmp = -1;
                pairedDelta = 0;
                return true;
            }

            var sh1 = shifts[0];
            var sh2 = shifts[1];
            var pos1 = SlotIndex(day, 0, slotIdx, pps, shiftCount);
            var pos2 = SlotIndex(day, 1, slotIdx, pps, shiftCount);

            if (shiftIdx == 0)
            {
                var emp2 = assigned[pos2];
                if (emp2 >= 0)
                {
                    if (!TryResolveShiftPairIntervals(
                            day,
                            candidateEmp,
                            emp2,
                            availStartMin,
                            availEndMin,
                            sh1,
                            sh2,
                            n,
                            out var pairFrom1,
                            out var pairTo1,
                            out var pairFrom2,
                            out var pairTo2))
                    {
                        return false;
                    }

                    candidateHours = (pairTo1 - pairFrom1) / 60d;
                    pairedEmp = emp2;
                    pairedDelta = ((pairTo2 - pairFrom2) / 60d) - slotHours[pos2];
                    return candidateHours > EPS;
                }

                var from = Math.Max(sh1.StartMin, aStart);
                var to = Math.Min(sh1.EndMin, aEnd);
                if (to <= from) return false;

                candidateHours = (to - from) / 60d;
                pairedEmp = -1;
                pairedDelta = 0;
                return true;
            }

            var emp1 = assigned[pos1];
            if (emp1 >= 0)
            {
                if (!TryResolveShiftPairIntervals(
                        day,
                        emp1,
                        candidateEmp,
                        availStartMin,
                        availEndMin,
                        sh1,
                        sh2,
                        n,
                        out var pairFrom1,
                        out var pairTo1,
                        out var pairFrom2,
                        out var pairTo2))
                {
                    return false;
                }

                candidateHours = (pairTo2 - pairFrom2) / 60d;
                pairedEmp = emp1;
                pairedDelta = ((pairTo1 - pairFrom1) / 60d) - slotHours[pos1];
                return candidateHours > EPS;
            }

            var boundary2 = Math.Max(sh2.StartMin, aStart);
            var to2 = Math.Min(sh2.EndMin, aEnd);
            if (to2 <= boundary2) return false;

            candidateHours = (to2 - boundary2) / 60d;
            pairedEmp = -1;
            pairedDelta = 0;
            return true;
        }

        private static void RecomputeAfterChange(
            int day,
            int shiftIdx,
            int slotIdx,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            List<ShiftTemplate> shifts,
            double[] totalHours,
            int n,
            int pps,
            int shiftCount)
        {
            if (shiftCount <= 1)
            {
                RecomputeSingleShiftSlot(day, slotIdx,
                    assigned,
                    slotFromMin,
                    slotToMin,
                    slotHours,
                    availStartMin,
                    availEndMin,
                    shifts[0],
                    totalHours,
                    n,
                    pps);
                return;
            }

            // two shifts: recompute pair for this slot index
            RecomputeTwoShiftPair(day, slotIdx,
                assigned,
                slotFromMin,
                slotToMin,
                slotHours,
                availStartMin,
                availEndMin,
                shifts[0],
                shifts[1],
                totalHours,
                n,
                pps,
                shiftCount);
        }

        private static void RecomputeSingleShiftSlot(
            int day,
            int slotIdx,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            ShiftTemplate sh,
            double[] totalHours,
            int n,
            int pps)
        {
            var pos = SlotIndex(day, 0, slotIdx, pps, 1);
            var emp = assigned[pos];

            // base times (for output when assigned)
            var baseFrom = sh.StartMin;
            var baseTo = sh.EndMin;

            if (emp < 0)
            {
                slotFromMin[pos] = baseFrom;
                slotToMin[pos] = baseTo;
                slotHours[pos] = 0;
                return;
            }

            var idx = day * n + emp;
            var aStart = availStartMin[idx];
            var aEnd = availEndMin[idx];

            var from = Math.Max(baseFrom, aStart);
            var to = Math.Min(baseTo, aEnd);
            var newH = to > from ? (to - from) / 60d : 0;

            totalHours[emp] += newH - slotHours[pos];

            slotFromMin[pos] = from;
            slotToMin[pos] = to;
            slotHours[pos] = newH;
        }

        private static void RecomputeTwoShiftPair(
            int day,
            int slotIdx,
            int[] assigned,
            int[] slotFromMin,
            int[] slotToMin,
            double[] slotHours,
            int[] availStartMin,
            int[] availEndMin,
            ShiftTemplate sh1,
            ShiftTemplate sh2,
            double[] totalHours,
            int n,
            int pps,
            int shiftCount)
        {
            var pos1 = SlotIndex(day, 0, slotIdx, pps, shiftCount);
            var pos2 = SlotIndex(day, 1, slotIdx, pps, shiftCount);

            var emp1 = assigned[pos1];
            var emp2 = assigned[pos2];

            // default template times
            slotFromMin[pos1] = sh1.StartMin;
            slotToMin[pos1] = sh1.EndMin;
            slotFromMin[pos2] = sh2.StartMin;
            slotToMin[pos2] = sh2.EndMin;

            // If empty: hours = 0
            if (emp1 < 0) slotHours[pos1] = 0;
            if (emp2 < 0) slotHours[pos2] = 0;

            if (emp1 < 0 && emp2 < 0)
                return;

            var hasIntervals = TryResolveShiftPairIntervals(
                day,
                emp1,
                emp2,
                availStartMin,
                availEndMin,
                sh1,
                sh2,
                n,
                out var from1Resolved,
                out var to1Resolved,
                out var from2Resolved,
                out var to2Resolved);

            if (emp1 >= 0)
            {
                var newHours1 = hasIntervals ? Math.Max(0.0, (to1Resolved - from1Resolved) / 60d) : 0.0;
                totalHours[emp1] += newHours1 - slotHours[pos1];
                slotFromMin[pos1] = newHours1 > EPS ? from1Resolved : sh1.StartMin;
                slotToMin[pos1] = newHours1 > EPS ? to1Resolved : sh1.EndMin;
                slotHours[pos1] = newHours1;
            }

            if (emp2 >= 0)
            {
                var newHours2 = hasIntervals ? Math.Max(0.0, (to2Resolved - from2Resolved) / 60d) : 0.0;
                totalHours[emp2] += newHours2 - slotHours[pos2];
                slotFromMin[pos2] = newHours2 > EPS ? from2Resolved : sh2.StartMin;
                slotToMin[pos2] = newHours2 > EPS ? to2Resolved : sh2.EndMin;
                slotHours[pos2] = newHours2;
            }
        }

        // =========================
        // Shift-level duplicates + stamp utils
        // =========================
        private static int NextStamp(int[] stampArr, int stamp)
        {
            stamp++;
            if (stamp == int.MaxValue)
            {
                Array.Clear(stampArr, 0, stampArr.Length);
                stamp = 1;
            }
            return stamp;
        }

        private static void PremarkAssignedInShift(
            int[] assigned,
            int day,
            int shiftIdx,
            int pps,
            int shiftCount,
            int[] assignedStamp,
            int stamp)
        {
            var basePos = SlotBase(day, shiftIdx, pps, shiftCount);
            for (var i = 0; i < pps; i++)
            {
                var emp = assigned[basePos + i];
                if (emp >= 0)
                    assignedStamp[emp] = stamp;
            }
        }

        private static bool IsEmployeeInShift(int[] assigned, int day, int shiftIdx, int pps, int shiftCount, int emp)
        {
            var basePos = SlotBase(day, shiftIdx, pps, shiftCount);
            for (var i = 0; i < pps; i++)
            {
                if (assigned[basePos + i] == emp)
                    return true;
            }
            return false;
        }

        private static bool IsEmployeeInOtherShiftSameSlot(
            int[] assigned,
            int day,
            int shiftIdx,
            int slotIdx,
            int pps,
            int shiftCount,
            int emp)
        {
            if (shiftCount <= 1 || emp < 0)
                return false;

            var otherShiftIdx = shiftIdx == 0 ? 1 : 0;
            var otherPos = SlotIndex(day, otherShiftIdx, slotIdx, pps, shiftCount);
            return assigned[otherPos] == emp;
        }

        private static bool BlocksOtherShiftPair(
            int[] assigned,
            int day,
            int shiftIdx,
            int slotIdx,
            int pps,
            int shiftCount,
            int emp)
        {
            if (shiftCount <= 1)
                return false;

            var otherShiftIdx = shiftIdx == 0 ? 1 : 0;
            var otherPos = SlotIndex(day, otherShiftIdx, slotIdx, pps, shiftCount);
            var otherEmp = assigned[otherPos];

            return otherEmp >= 0 && otherEmp != emp;
        }

        private static int FindEmployeeSlotIndexInShift(int[] assigned, int day, int shiftIdx, int pps, int shiftCount, int emp)
        {
            var basePos = SlotBase(day, shiftIdx, pps, shiftCount);
            for (var i = 0; i < pps; i++)
            {
                if (assigned[basePos + i] == emp)
                    return i;
            }

            return -1;
        }

        // =========================
        // Slot indexing
        // =========================
        private static int SlotBase(int day, int shiftIdx, int pps, int shiftCount)
            => ((day - 1) * shiftCount + shiftIdx) * pps;

        private static int SlotIndex(int day, int shiftIdx, int slotIdx, int pps, int shiftCount)
            => SlotBase(day, shiftIdx, pps, shiftCount) + slotIdx;

        private static void InitSlotBaseTimes(
            int daysInMonth,
            int shiftCount,
            int pps,
            List<ShiftTemplate> shifts,
            int[] slotFromMin,
            int[] slotToMin)
        {
            for (var day = 1; day <= daysInMonth; day++)
            {
                for (var s = 0; s < shiftCount; s++)
                {
                    var sh = shifts[s];
                    var basePos = SlotBase(day, s, pps, shiftCount);
                    for (var slot = 0; slot < pps; slot++)
                    {
                        var pos = basePos + slot;
                        slotFromMin[pos] = sh.StartMin;
                        slotToMin[pos] = sh.EndMin;
                    }
                }
            }
        }

        // =========================
        // MinHours parsing
        // =========================
        private static double ReadMinHours(ScheduleEmployeeModel e)
        {
            try
            {
                var d = Convert.ToDouble(e.MinHoursMonth);
                return d < 0 ? 0 : d;
            }
            catch
            {
                return 0;
            }
        }

        // =========================
        // Shift parsing
        // =========================
        private static List<ShiftTemplate> GetShiftTemplates(ScheduleModel schedule)
        {
            var list = new List<ShiftTemplate>(capacity: 2);

            if (TryCreateShiftTemplate(schedule.Shift1Time, 1, out var t1))
                list.Add(t1);

            if (TryCreateShiftTemplate(schedule.Shift2Time, 2, out var t2))
                list.Add(t2);

            return list;
        }

        private static bool TryCreateShiftTemplate(string? shiftText, int index, out ShiftTemplate template)
        {
            template = null!;

            if (string.IsNullOrWhiteSpace(shiftText))
                return false;

            var cleaned = shiftText.Replace(" ", string.Empty);
            var parts = cleaned.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                throw new InvalidOperationException(
                    $"Invalid shift format '{shiftText}'. Expected 'HH:mm-HH:mm' or 'HH:mm - HH:mm'.");

            if (!TimeSpan.TryParse(parts[0], CultureInfo.InvariantCulture, out var start) ||
                !TimeSpan.TryParse(parts[1], CultureInfo.InvariantCulture, out var end))
                throw new InvalidOperationException(
                    $"Invalid shift time '{shiftText}'. Cannot parse start/end as time.");

            if (end <= start)
                throw new InvalidOperationException(
                    $"Invalid shift time '{shiftText}'. End time must be after start time within the same day.");

            template = new ShiftTemplate
            {
                Index = index,
                From = start.ToString(@"hh\:mm"),
                To = end.ToString(@"hh\:mm"),
                StartMin = (int)start.TotalMinutes,
                EndMin = (int)end.TotalMinutes,
                Hours = (end - start).TotalHours
            };

            return true;
        }

        // =========================
        // Availability window parsing (reflection-based)
        // =========================
        private static bool TryReadAvailabilityWindow(object dayModel, out int fromMin, out int toMin)
        {
            fromMin = 0;
            toMin = 24 * 60;

            // 1) Common packed interval properties ("09:00 - 15:00")
            if (TryReadIntervalText(dayModel,
                    new[] { "IntervalStr", "IntervalString", "Interval", "AvailabilityInterval", "TimeInterval" },
                    out var iFrom,
                    out var iTo))
            {
                fromMin = iFrom;
                toMin = iTo;
                return true;
            }

            // 2) Split time properties (string "HH:mm", TimeSpan, numeric)
            var hasFrom = TryReadTime(dayModel,
                new[] { "FromTime", "StartTime", "From", "Start", "AvailableFrom", "TimeFrom" },
                out var fm);

            var hasTo = TryReadTime(dayModel,
                new[] { "ToTime", "EndTime", "To", "End", "AvailableTo", "TimeTo" },
                out var tm);

            // 3) Also support pairs like FromHour/FromMinute etc.
            if (!hasFrom)
                hasFrom = TryReadHourMinute(dayModel,
                    new[] { "FromHour", "StartHour" },
                    new[] { "FromMinute", "StartMinute" },
                    out fm);

            if (!hasTo)
                hasTo = TryReadHourMinute(dayModel,
                    new[] { "ToHour", "EndHour" },
                    new[] { "ToMinute", "EndMinute" },
                    out tm);

            if (!hasFrom && !hasTo)
                return false;

            if (hasFrom) fromMin = fm;
            if (hasTo) toMin = tm;

            return true;
        }

        private static bool AvailabilityKindRequiresWindow(object? kind)
        {
            if (kind is null) return false;

            var s = kind.ToString();
            if (string.IsNullOrWhiteSpace(s)) return false;

            s = s.Trim();

            // "INT", "INTERVAL", etc. => requires a parsable window.
            if (string.Equals(s, "INT", StringComparison.OrdinalIgnoreCase))
                return true;

            if (s.IndexOf("INTERVAL", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // "ANY" / "NONE" do not require a window.
            return false;
        }

        private static bool TryReadIntervalText(object obj, string[] names, out int fromMin, out int toMin)
        {
            fromMin = 0;
            toMin = 0;

            var t = obj.GetType();

            foreach (var name in names)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p is null) continue;

                var v = p.GetValue(obj);
                if (v is null) continue;

                if (v is string s && TryParseIntervalRange(s, out fromMin, out toMin))
                    return true;
            }

            return false;
        }

        private static bool TryParseIntervalRange(string text, out int fromMin, out int toMin)
        {
            fromMin = 0;
            toMin = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var s = text.Trim();

            // Normalize common separators and dashes.
            s = s.Replace("–", "-").Replace("—", "-");

            var parts = s.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return false;

            if (!TryConvertToMinutes(parts[0], out fromMin))
                return false;

            if (!TryConvertToMinutes(parts[1], out toMin))
                return false;

            return true;
        }

        private static bool TryReadTime(object obj, string[] names, out int minutes)
        {
            minutes = 0;
            var t = obj.GetType();

            foreach (var name in names)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p is null) continue;

                var v = p.GetValue(obj);
                if (v is null) continue;

                if (TryConvertToMinutes(v, out minutes))
                    return true;
            }

            return false;
        }

        private static bool TryReadHourMinute(object obj, string[] hourNames, string[] minuteNames, out int minutes)
        {
            minutes = 0;
            var t = obj.GetType();

            int? h = null;
            int? m = null;

            foreach (var hn in hourNames)
            {
                var p = t.GetProperty(hn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p is null) continue;
                var v = p.GetValue(obj);
                if (v is null) continue;
                if (TryToInt(v, out var hi)) { h = hi; break; }
            }

            foreach (var mn in minuteNames)
            {
                var p = t.GetProperty(mn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p is null) continue;
                var v = p.GetValue(obj);
                if (v is null) continue;
                if (TryToInt(v, out var mi)) { m = mi; break; }
            }

            if (h is null && m is null)
                return false;

            var hh = h ?? 0;
            var mm = m ?? 0;
            minutes = Clamp(hh, 0, 23) * 60 + Clamp(mm, 0, 59);
            return true;
        }

        private static bool TryConvertToMinutes(object value, out int minutes)
        {
            minutes = 0;

            switch (value)
            {
                case TimeSpan ts:
                    minutes = (int)Math.Round(ts.TotalMinutes);
                    return true;

                case string s:
                    s = s.Trim();
                    if (string.IsNullOrEmpty(s)) return false;

                    // try HH:mm
                    if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts2))
                    {
                        minutes = (int)Math.Round(ts2.TotalMinutes);
                        return true;
                    }

                    // try minutes as number
                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var dmins))
                    {
                        // Heuristic: if value <= 24, treat as hours; else minutes
                        if (dmins <= 24.0 + 1e-6)
                            minutes = (int)Math.Round(dmins * 60.0);
                        else
                            minutes = (int)Math.Round(dmins);
                        return true;
                    }

                    return false;

                default:
                    if (value is IConvertible)
                    {
                        try
                        {
                            var d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                            if (d <= 24.0 + 1e-6)
                                minutes = (int)Math.Round(d * 60.0);
                            else
                                minutes = (int)Math.Round(d);
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }

                    return false;
            }
        }

        private static bool TryToInt(object value, out int i)
        {
            i = 0;
            try
            {
                i = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // =========================
        // Formatting + utilities
        // =========================
        private static int Clamp(int v, int lo, int hi)
            => v < lo ? lo : (v > hi ? hi : v);

        private static string MinutesToHHmm(int minutes)
        {
            minutes = Clamp(minutes, 0, 24 * 60);
            var ts = TimeSpan.FromMinutes(minutes);
            return ts.ToString(@"hh\:mm");
        }
    }
}


