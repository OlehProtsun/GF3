using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;

namespace BusinessLogicLayer.Generators
{
    public sealed class ScheduleGenerator : IScheduleGenerator
    {
        private const double EPS = 1e-9;

        private sealed class ShiftTemplate
        {
            public int Index { get; init; }             // 1 => Shift1, 2 => Shift2
            public string From { get; init; } = null!;  // "HH:mm"
            public string To { get; init; } = null!;    // "HH:mm"
            public double Hours { get; init; }          // duration in hours
        }

        public Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress = null,
            CancellationToken ct = default)
            => Task.Run(() => GenerateCore(schedule, availabilities, employees, progress, ct), ct);

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

            // 2) Unavailable matrix: unavailable[day*n + emp] = true only if AvailabilityKind.NONE
            var unavailable = new bool[(daysInMonth + 1) * n];

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

                        // only NONE blocks
                        if (d.Kind == AvailabilityKind.NONE)
                            unavailable[day * n + empIdx] = true;
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

            // 4) Internal schedule storage: assigned slot -> empIdx or -1
            var totalSlots = daysInMonth * shiftCount * pps;
            var assigned = new int[totalSlots];
            Array.Fill(assigned, -1);

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

                    // mark already assigned in this shift (none initially, but keep consistent)
                    PremarkAssignedInShift(assigned, day, s, pps, shiftCount, assignedStamp, stamp);

                    var sh = shifts[s];
                    var preferNoSecondShift = (shiftCount >= 2 && sh.Index == 2);

                    for (var slot = 0; slot < pps; slot++)
                    {
                        var pos = SlotIndex(day, s, slot, pps, shiftCount);
                        if (assigned[pos] >= 0) continue;

                        var chosen = PickCandidatePhase1Fast(
                            availableToday,
                            day,
                            sh.Hours,
                            preferNoSecondShift,
                            schedule,
                            minHours,
                            scarcityDays,
                            totalHours,
                            fullDaysCount,
                            shiftsToday,
                            lastWorkedDay,
                            consecutiveDays,
                            lastFullDay,
                            consecutiveFullDays,
                            shiftCount,
                            assignedStamp,
                            stamp,
                            rrCursor);

                        if (chosen >= 0)
                        {
                            rrCursor = (chosen + 1) % n;
                            AssignPhase1Fast(
                                day, s, slot, chosen, sh.Hours,
                                assigned,
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
                schedule, shifts, availableByDay, unavailable,
                assigned, totalHours, shiftsPerDay, fullDaysCount,
                daysInMonth, shiftCount, pps, stride, scarcityDays,
                ref rrCursor, ct);

            progress?.Report(85);

            StrictMinHoursRepairMinConflicts(
                schedule, shifts, unavailable, minHours,
                assigned, totalHours, shiftsPerDay, fullDaysCount,
                daysInMonth, shiftCount, pps, stride,
                ct);

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
                        var empIdx = assigned[basePos + (slotNo - 1)];

                        var slotModel = new ScheduleSlotModel
                        {
                            DayOfMonth = day,
                            SlotNo = slotNo,
                            FromTime = sh.From,
                            ToTime = sh.To,
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
            bool[] unavailable,
            int[] assigned,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int[] scarcityDays,
            ref int rrCursor,
            CancellationToken ct)
        {
            // Greedy fill: for each empty slot choose best candidate (deficit-first, then fairness).
            // Uses order-independent constraint check.

            var n = totalHours.Length;
            var assignedStamp = new int[n];
            var stamp = 1;

            for (var day = 1; day <= daysInMonth; day++)
            {
                ct.ThrowIfCancellationRequested();

                var availableToday = availableByDay[day];
                if (availableToday.Length == 0)
                    continue;

                for (var s = 0; s < shiftCount; s++)
                {
                    stamp = NextStamp(assignedStamp, stamp);
                    PremarkAssignedInShift(assigned, day, s, pps, shiftCount, assignedStamp, stamp);

                    var sh = shifts[s];
                    var preferNoSecondShift = (shiftCount >= 2 && sh.Index == 2);

                    var basePos = SlotBase(day, s, pps, shiftCount);

                    for (var slot = 0; slot < pps; slot++)
                    {
                        var pos = basePos + slot;
                        if (assigned[pos] >= 0) continue;

                        // two-pass for Shift2 to avoid full-day, then allow
                        var chosen = FindBestOrderIndependent(
                            availableToday,
                            day,
                            sh.Hours,
                            preferNoSecondShift,
                            schedule,
                            unavailable,
                            assigned,
                            totalHours,
                            shiftsPerDay,
                            fullDaysCount,
                            daysInMonth,
                            shiftCount,
                            pps,
                            stride,
                            scarcityDays,
                            assignedStamp,
                            stamp,
                            rrCursor);

                        if (chosen < 0 && preferNoSecondShift)
                        {
                            chosen = FindBestOrderIndependent(
                                availableToday,
                                day,
                                sh.Hours,
                                restrictSecondShift: false,
                                schedule,
                                unavailable,
                                assigned,
                                totalHours,
                                shiftsPerDay,
                                fullDaysCount,
                                daysInMonth,
                                shiftCount,
                                pps,
                                stride,
                                scarcityDays,
                                assignedStamp,
                                stamp,
                                rrCursor);
                        }

                        if (chosen >= 0)
                        {
                            rrCursor = (chosen + 1) % n;
                            AssignToEmptySlot(day, s, slot, chosen, sh.Hours,
                                assigned, totalHours, shiftsPerDay, fullDaysCount,
                                shiftCount, stride, pps);
                            assignedStamp[chosen] = stamp;
                        }
                    }
                }
            }
        }

        private static int FindBestOrderIndependent(
            int[] availableToday,
            int day,
            double shiftHours,
            bool restrictSecondShift,
            ScheduleModel schedule,
            bool[] unavailable,
            int[] assigned,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            int[] scarcityDays,
            int[] assignedStamp,
            int stamp,
            int rrCursor)
        {
            var n = totalHours.Length;

            var best = -1;
            var bestNeed = -1.0;
            var bestTotal = double.MaxValue;
            var bestFull = int.MaxValue;
            var bestScar = int.MaxValue;
            var bestRR = int.MaxValue;

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

                if (IsEmployeeInShift(assigned, day, /*shiftIdx*/ 0, /*ignored*/ 0, /*ignored*/ 0, emp))
                {
                    // This overload isn't used; real check below with shiftIdx+pps+shiftCount
                }

                // must not already be in THIS shift (we rely on assignedStamp + premark, but keep safe)
                // The caller already premarked existing employees in shift into assignedStamp.
                // So this is redundant but harmless.

                if (!CanAddShiftOrderIndependent(
                        schedule, emp, day, shiftHours,
                        totalHours, shiftsPerDay, fullDaysCount,
                        daysInMonth, shiftCount, stride))
                    continue;

                // need = how much under MinHoursMonth (we don't have minHours here in phase2A),
                // so phase2A is purely fairness-based (hours + full days + scarcity + RR).
                // (MinHours strict handled in phase2B)
                var need = 0.0;

                var total = totalHours[emp];
                var full = fullDaysCount[emp];
                var scar = scarcityDays[emp] <= 0 ? int.MaxValue : scarcityDays[emp];
                var rrDist = emp >= rrCursor ? (emp - rrCursor) : (emp + n - rrCursor);

                var better =
                    best < 0
                    || need > bestNeed + EPS
                    || (Math.Abs(need - bestNeed) <= EPS && total < bestTotal - EPS)
                    || (Math.Abs(need - bestNeed) <= EPS && Math.Abs(total - bestTotal) <= EPS && full < bestFull)
                    || (Math.Abs(need - bestNeed) <= EPS && Math.Abs(total - bestTotal) <= EPS && full == bestFull && scar < bestScar)
                    || (Math.Abs(need - bestNeed) <= EPS && Math.Abs(total - bestTotal) <= EPS && full == bestFull && scar == bestScar && rrDist < bestRR);

                if (!better) continue;

                best = emp;
                bestNeed = need;
                bestTotal = total;
                bestFull = full;
                bestScar = scar;
                bestRR = rrDist;
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
            double[] minHours,
            int[] assigned,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int pps,
            int stride,
            CancellationToken ct)
        {
            var n = totalHours.Length;

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

            // Multiple passes can help after swaps
            for (var pass = 0; pass < 3; pass++)
            {
                var improvedPass = false;

                for (var di = 0; di < deficit.Count; di++)
                {
                    ct.ThrowIfCancellationRequested();

                    var emp = deficit[di];
                    if (minHours[emp] - totalHours[emp] <= EPS)
                        continue;

                    // While deficit exists, attempt:
                    // 1) Take UNFURNISHED slot (reduces conflicts)
                    // 2) If none, swap with donor (donor must keep >= its MinHours)
                    while (minHours[emp] - totalHours[emp] > EPS)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (ops++ >= maxOps) return;

                        // (A) try fill empty slot, prefer new day (avoid full-day), then allow
                        if (TryFillEmptyForEmployee(emp, requireNewDay: true) ||
                            TryFillEmptyForEmployee(emp, requireNewDay: false))
                        {
                            improvedPass = true;
                            continue;
                        }

                        // (B) swap
                        if (TrySwapForEmployee(emp, requireNewDay: true) ||
                            TrySwapForEmployee(emp, requireNewDay: false))
                        {
                            improvedPass = true;
                            continue;
                        }

                        break; // nothing else possible for this employee
                    }
                }

                if (!improvedPass)
                    break;
            }

            bool TryFillEmptyForEmployee(int emp, bool requireNewDay)
            {
                var bestDay = -1;
                var bestShift = -1;
                var bestSlot = -1;
                var bestHours = 0.0;
                var bestCreatesFull = true;

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

                        var sh = shifts[s];

                        if (!CanAddShiftOrderIndependent(
                                schedule, emp, day, sh.Hours,
                                totalHours, shiftsPerDay, fullDaysCount,
                                daysInMonth, shiftCount, stride))
                            continue;

                        var basePos = SlotBase(day, s, pps, shiftCount);

                        for (var slot = 0; slot < pps; slot++)
                        {
                            if (assigned[basePos + slot] >= 0)
                                continue; // not empty

                            var createsFull = (shiftCount >= 2 && cur == 1); // would make day full (1->2)
                            // prefer: not creating full day, then longer hours
                            if (bestDay < 0
                                || (!createsFull && bestCreatesFull)
                                || (createsFull == bestCreatesFull && sh.Hours > bestHours + EPS))
                            {
                                bestDay = day;
                                bestShift = s;
                                bestSlot = slot;
                                bestHours = sh.Hours;
                                bestCreatesFull = createsFull;
                            }
                        }
                    }
                }

                if (bestDay < 0)
                    return false;

                AssignToEmptySlot(bestDay, bestShift, bestSlot, emp, bestHours,
                    assigned, totalHours, shiftsPerDay, fullDaysCount,
                    shiftCount, stride, pps);

                return true;
            }

            bool TrySwapForEmployee(int emp, bool requireNewDay)
            {
                var bestDay = -1;
                var bestShift = -1;
                var bestSlot = -1;
                var bestHours = 0.0;
                var bestDonor = -1;
                var bestDonorSurplus = -1.0;
                var bestCreatesFull = true;

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

                        var sh = shifts[s];

                        if (!CanAddShiftOrderIndependent(
                                schedule, emp, day, sh.Hours,
                                totalHours, shiftsPerDay, fullDaysCount,
                                daysInMonth, shiftCount, stride))
                            continue;

                        var basePos = SlotBase(day, s, pps, shiftCount);

                        for (var slot = 0; slot < pps; slot++)
                        {
                            var donor = assigned[basePos + slot];
                            if (donor < 0) continue;
                            if (donor == emp) continue;

                            // donor must remain >= its MinHours after giving away this shift
                            if (totalHours[donor] - sh.Hours < minHours[donor] - EPS)
                                continue;

                            var donorSurplus = totalHours[donor] - minHours[donor];
                            var createsFull = (shiftCount >= 2 && cur == 1);

                            // prioritize:
                            // 1) donor with largest surplus (safe)
                            // 2) avoid creating full day if possible
                            // 3) longer shift hours
                            if (bestDay < 0
                                || donorSurplus > bestDonorSurplus + EPS
                                || (Math.Abs(donorSurplus - bestDonorSurplus) <= EPS && (!createsFull && bestCreatesFull))
                                || (Math.Abs(donorSurplus - bestDonorSurplus) <= EPS && createsFull == bestCreatesFull && sh.Hours > bestHours + EPS))
                            {
                                bestDay = day;
                                bestShift = s;
                                bestSlot = slot;
                                bestHours = sh.Hours;
                                bestDonor = donor;
                                bestDonorSurplus = donorSurplus;
                                bestCreatesFull = createsFull;
                            }
                        }
                    }
                }

                if (bestDay < 0 || bestDonor < 0)
                    return false;

                SwapSlot(bestDay, bestShift, bestSlot, bestDonor, emp, bestHours,
                    assigned, totalHours, shiftsPerDay, fullDaysCount,
                    pps, shiftCount, stride);

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
            double shiftHours,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int daysInMonth,
            int shiftCount,
            int stride)
        {
            // Max hours per month
            if (schedule.MaxHoursPerEmpMonth > 0)
            {
                if (totalHours[emp] + shiftHours > schedule.MaxHoursPerEmpMonth + EPS)
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
                if (schedule.MaxFullPerMonth > 0 && fullDaysCount[emp] + 1 > schedule.MaxFullPerMonth)
                    return false;

                if (schedule.MaxConsecutiveFull > 0)
                {
                    var fullStreak = 1
                                     + CountLeft(emp, day, shiftsPerDay, stride, v => v >= 2)
                                     + CountRight(emp, day, daysInMonth, shiftsPerDay, stride, v => v >= 2);

                    if (fullStreak > schedule.MaxConsecutiveFull)
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

        // =========================
        // Phase 1: fast candidate selection + constraints (O(1))
        // =========================
        private static int PickCandidatePhase1Fast(
            int[] availableToday,
            int day,
            double shiftHours,
            bool preferNoSecondShift,
            ScheduleModel schedule,
            double[] minHours,
            int[] scarcityDays,
            double[] totalHours,
            int[] fullDaysCount,
            int[] shiftsToday,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
            int[] assignedStamp,
            int stamp,
            int rrCursor)
        {
            var best = FindBestPhase1Fast(
                availableToday, day, shiftHours,
                restrictSecondShift: preferNoSecondShift,
                schedule, minHours, scarcityDays,
                totalHours, fullDaysCount,
                shiftsToday, lastWorkedDay, consecutiveDays,
                lastFullDay, consecutiveFullDays,
                shiftCount,
                assignedStamp, stamp, rrCursor);

            if (best >= 0)
                return best;

            if (preferNoSecondShift)
            {
                best = FindBestPhase1Fast(
                    availableToday, day, shiftHours,
                    restrictSecondShift: false,
                    schedule, minHours, scarcityDays,
                    totalHours, fullDaysCount,
                    shiftsToday, lastWorkedDay, consecutiveDays,
                    lastFullDay, consecutiveFullDays,
                    shiftCount,
                    assignedStamp, stamp, rrCursor);
            }

            return best;
        }

        private static int FindBestPhase1Fast(
            int[] availableToday,
            int day,
            double shiftHours,
            bool restrictSecondShift,
            ScheduleModel schedule,
            double[] minHours,
            int[] scarcityDays,
            double[] totalHours,
            int[] fullDaysCount,
            int[] shiftsToday,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
            int[] assignedStamp,
            int stamp,
            int rrCursor)
        {
            var n = totalHours.Length;

            var best = -1;
            var bestNeed = -1.0;
            var bestTotal = double.MaxValue;
            var bestFull = int.MaxValue;
            var bestScar = int.MaxValue;
            var bestRR = int.MaxValue;

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

                if (!CanAssignPhase1Fast(
                        emp, day, shiftHours, st,
                        schedule,
                        totalHours, fullDaysCount,
                        lastWorkedDay, consecutiveDays,
                        lastFullDay, consecutiveFullDays,
                        shiftCount))
                    continue;

                var need = minHours[emp] - totalHours[emp];
                if (need < 0) need = 0;

                var total = totalHours[emp];
                var full = fullDaysCount[emp];
                var scar = scarcityDays[emp] <= 0 ? int.MaxValue : scarcityDays[emp];
                var rrDist = emp >= rrCursor ? (emp - rrCursor) : (emp + n - rrCursor);

                var better =
                    best < 0
                    || need > bestNeed + EPS
                    || (Math.Abs(need - bestNeed) <= EPS && total < bestTotal - EPS)
                    || (Math.Abs(need - bestNeed) <= EPS && Math.Abs(total - bestTotal) <= EPS && full < bestFull)
                    || (Math.Abs(need - bestNeed) <= EPS && Math.Abs(total - bestTotal) <= EPS && full == bestFull && scar < bestScar)
                    || (Math.Abs(need - bestNeed) <= EPS && Math.Abs(total - bestTotal) <= EPS && full == bestFull && scar == bestScar && rrDist < bestRR);

                if (!better) continue;

                best = emp;
                bestNeed = need;
                bestTotal = total;
                bestFull = full;
                bestScar = scar;
                bestRR = rrDist;
            }

            return best;
        }

        private static bool CanAssignPhase1Fast(
            int emp,
            int day,
            double shiftHours,
            int shiftsAlreadyToday,
            ScheduleModel schedule,
            double[] totalHours,
            int[] fullDaysCount,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount)
        {
            // Max hours
            if (schedule.MaxHoursPerEmpMonth > 0)
            {
                if (totalHours[emp] + shiftHours > schedule.MaxHoursPerEmpMonth + EPS)
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
                if (schedule.MaxFullPerMonth > 0 && fullDaysCount[emp] + 1 > schedule.MaxFullPerMonth)
                    return false;

                if (schedule.MaxConsecutiveFull > 0)
                {
                    var newFullConsec = (lastFullDay[emp] == day - 1) ? (consecutiveFullDays[emp] + 1) : 1;
                    if (newFullConsec > schedule.MaxConsecutiveFull)
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
            double shiftHours,
            int[] assigned,
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
            int[] assignedStamp,
            int stamp)
        {
            var pos = SlotIndex(day, shiftIdx, slotIdx, pps, shiftCount);
            if (assigned[pos] >= 0) return;

            assigned[pos] = emp;
            assignedStamp[emp] = stamp;

            totalHours[emp] += shiftHours;

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
        }

        // =========================
        // Mutations for phase2
        // =========================
        private static void AssignToEmptySlot(
            int day,
            int shiftIdx,
            int slotIdx,
            int emp,
            double shiftHours,
            int[] assigned,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int shiftCount,
            int stride,
            int pps)
        {
            var pos = SlotIndex(day, shiftIdx, slotIdx, pps, shiftCount);
            if (assigned[pos] >= 0) return;

            assigned[pos] = emp;
            totalHours[emp] += shiftHours;

            var p = emp * stride + day;
            var before = shiftsPerDay[p];
            var after = before + 1;
            shiftsPerDay[p] = after;

            if (shiftCount >= 2 && before == 1 && after == 2)
                fullDaysCount[emp] += 1;
        }

        private static void SwapSlot(
            int day,
            int shiftIdx,
            int slotIdx,
            int donor,
            int receiver,
            double shiftHours,
            int[] assigned,
            double[] totalHours,
            int[] shiftsPerDay,
            int[] fullDaysCount,
            int pps,
            int shiftCount,
            int stride)
        {
            var pos = SlotIndex(day, shiftIdx, slotIdx, pps, shiftCount);
            if (assigned[pos] != donor)
                return;

            // remove donor
            totalHours[donor] -= shiftHours;
            var dPos = donor * stride + day;
            var dBefore = shiftsPerDay[dPos];
            var dAfter = dBefore - 1;
            shiftsPerDay[dPos] = dAfter;

            if (shiftCount >= 2 && dBefore == 2 && dAfter == 1)
                fullDaysCount[donor] -= 1;

            // add receiver
            assigned[pos] = receiver;

            totalHours[receiver] += shiftHours;
            var rPos = receiver * stride + day;
            var rBefore = shiftsPerDay[rPos];
            var rAfter = rBefore + 1;
            shiftsPerDay[rPos] = rAfter;

            if (shiftCount >= 2 && rBefore == 1 && rAfter == 2)
                fullDaysCount[receiver] += 1;
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

        // =========================
        // Slot indexing
        // =========================
        private static int SlotBase(int day, int shiftIdx, int pps, int shiftCount)
            => ((day - 1) * shiftCount + shiftIdx) * pps;

        private static int SlotIndex(int day, int shiftIdx, int slotIdx, int pps, int shiftCount)
            => SlotBase(day, shiftIdx, pps, shiftCount) + slotIdx;

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

            if (!TimeSpan.TryParse(parts[0], out var start) ||
                !TimeSpan.TryParse(parts[1], out var end))
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
                Hours = (end - start).TotalHours
            };

            return true;
        }
    }
}
