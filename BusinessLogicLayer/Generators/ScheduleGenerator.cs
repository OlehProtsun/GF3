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
                            continue;
                        }

                        // Optional: time window inside a day (e.g. 15:30-24:00 or "09:00 - 15:00")
                        if (TryReadAvailabilityWindow(d, out var fromMin, out var toMin))
                        {
                            fromMin = Clamp(fromMin, 0, 24 * 60);
                            toMin = Clamp(toMin, 0, 24 * 60);
                            if (toMin < fromMin)
                                (fromMin, toMin) = (toMin, fromMin);

                            // intersection
                            if (fromMin > availStartMin[idx]) availStartMin[idx] = fromMin;
                            if (toMin < availEndMin[idx]) availEndMin[idx] = toMin;

                            if (availEndMin[idx] <= availStartMin[idx])
                            {
                                unavailable[idx] = true;
                                availStartMin[idx] = 24 * 60;
                                availEndMin[idx] = 0;
                            }
                        }
                        else if (AvailabilityKindRequiresWindow(d.Kind))
                        {
                            // Strict mode: interval-like availability without a readable interval
                            // is treated as not schedulable rather than "ANY" (0..24).
                            unavailable[idx] = true;
                            availStartMin[idx] = 24 * 60;
                            availEndMin[idx] = 0;
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
                            scarcityDays,
                            totalHours,
                            fullDaysCount,
                            shiftsToday,
                            lastWorkedDay,
                            consecutiveDays,
                            lastFullDay,
                            consecutiveFullDays,
                            shiftCount,
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
                unavailable,
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
                ref rrCursor,
                ct);

            progress?.Report(85);

            StrictMinHoursRepairMinConflicts(
                schedule,
                shifts,
                unavailable,
                availStartMin,
                availEndMin,
                minHours,
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
            bool[] unavailable,
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
                            unavailable,
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
                                unavailable,
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

        private static int FindBestOrderIndependent(
            int[] availableToday,
            int day,
            int shiftIdx,
            int slotIdx,
            bool restrictSecondShift,
            ScheduleModel schedule,
            bool[] unavailable,
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
            List<ShiftTemplate> shifts,
            int n,
            int[] assignedStamp,
            int stamp,
            int rrCursor)
        {
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
                        totalHours, shiftsPerDay, fullDaysCount,
                        daysInMonth, shiftCount, stride))
                    continue;

                // phase2A is purely fairness-based (hours + full days + scarcity + RR).
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
            int[] availStartMin,
            int[] availEndMin,
            double[] minHours,
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
                                    totalHours, shiftsPerDay, fullDaysCount,
                                    daysInMonth, shiftCount, stride))
                                continue;

                            var createsFull = (shiftCount >= 2 && cur == 1); // would make day full (1->2)

                            // prefer: not creating full day, then longer hours
                            if (bestDay < 0
                                || (!createsFull && bestCreatesFull)
                                || (createsFull == bestCreatesFull && empHours > bestHours + EPS))
                            {
                                bestDay = day;
                                bestShift = s;
                                bestSlot = slot;
                                bestHours = empHours;
                                bestCreatesFull = createsFull;
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
                                    totalHours, shiftsPerDay, fullDaysCount,
                                    daysInMonth, shiftCount, stride))
                                continue;

                            var donorSurplus = totalHours[donor] - minHours[donor];
                            var createsFull = (shiftCount >= 2 && cur == 1);

                            if (bestDay < 0
                                || donorSurplus > bestDonorSurplus + EPS
                                || (Math.Abs(donorSurplus - bestDonorSurplus) <= EPS && (!createsFull && bestCreatesFull))
                                || (Math.Abs(donorSurplus - bestDonorSurplus) <= EPS && createsFull == bestCreatesFull && empHours > bestReceiverHours + EPS))
                            {
                                bestDay = day;
                                bestShift = s;
                                bestSlot = slot;
                                bestReceiverHours = empHours;
                                bestDonor = donor;
                                bestDonorSurplus = donorSurplus;
                                bestCreatesFull = createsFull;
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
            int shiftIdx,
            int slotIdx,
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
                scarcityDays,
                totalHours,
                fullDaysCount,
                shiftsToday,
                lastWorkedDay,
                consecutiveDays,
                lastFullDay,
                consecutiveFullDays,
                shiftCount,
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
                    scarcityDays,
                    totalHours,
                    fullDaysCount,
                    shiftsToday,
                    lastWorkedDay,
                    consecutiveDays,
                    lastFullDay,
                    consecutiveFullDays,
                    shiftCount,
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
            int[] scarcityDays,
            double[] totalHours,
            int[] fullDaysCount,
            int[] shiftsToday,
            int[] lastWorkedDay,
            int[] consecutiveDays,
            int[] lastFullDay,
            int[] consecutiveFullDays,
            int shiftCount,
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
                        totalHours,
                        fullDaysCount,
                        lastWorkedDay,
                        consecutiveDays,
                        lastFullDay,
                        consecutiveFullDays,
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
            double addedHours,
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
                // Assigning to shift1. If shift2 already assigned in this slot, shift1 must reach shift2 actual start.
                var emp2 = assigned[pos2];
                var boundary = sh1.EndMin;
                if (emp2 >= 0)
                {
                    // shift2 actual start already computed as slotFromMin[pos2]
                    boundary = Math.Max(boundary, slotFromMin[pos2]);
                }

                var from = Math.Max(sh1.StartMin, aStart);
                var to = Math.Min(boundary, aEnd);
                if (to <= from) return false;

                // Also, if shift2 exists and is assigned, ensure continuity (shift1 must be able to reach boundary)
                if (emp2 >= 0 && aEnd < boundary)
                    return false;

                candidateHours = (to - from) / 60d;
                pairedEmp = -1;
                pairedDelta = 0;
                return true;
            }

            // Assigning to shift2.
            var boundary2 = Math.Max(sh2.StartMin, aStart); // shift2 starts no earlier than template
            var to2 = Math.Min(sh2.EndMin, aEnd);
            if (to2 <= boundary2) return false;

            candidateHours = (to2 - boundary2) / 60d;

            var emp1 = assigned[pos1];
            if (emp1 >= 0)
            {
                // shift1 must be able to extend until boundary2
                var idx1 = day * n + emp1;
                var a1Start = availStartMin[idx1];
                var a1End = availEndMin[idx1];
                if (a1End < boundary2)
                    return false;

                var from1 = Math.Max(sh1.StartMin, a1Start);
                if (boundary2 <= from1)
                    return false;

                var newH1 = (boundary2 - from1) / 60d;
                pairedEmp = emp1;
                pairedDelta = newH1 - slotHours[pos1];
            }

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

            // Only shift1 assigned
            if (emp1 >= 0 && emp2 < 0)
            {
                var idx1 = day * n + emp1;
                var a1Start = availStartMin[idx1];
                var a1End = availEndMin[idx1];

                var from1 = Math.Max(sh1.StartMin, a1Start);
                var to1 = Math.Min(sh1.EndMin, a1End);
                var h1 = to1 > from1 ? (to1 - from1) / 60d : 0;

                totalHours[emp1] += h1 - slotHours[pos1];
                slotFromMin[pos1] = from1;
                slotToMin[pos1] = to1;
                slotHours[pos1] = h1;
                return;
            }

            // Only shift2 assigned
            if (emp2 >= 0 && emp1 < 0)
            {
                var idx2 = day * n + emp2;
                var a2Start = availStartMin[idx2];
                var a2End = availEndMin[idx2];

                var from2 = Math.Max(sh2.StartMin, a2Start);
                var to2 = Math.Min(sh2.EndMin, a2End);
                var h2 = to2 > from2 ? (to2 - from2) / 60d : 0;

                totalHours[emp2] += h2 - slotHours[pos2];
                slotFromMin[pos2] = from2;
                slotToMin[pos2] = to2;
                slotHours[pos2] = h2;
                return;
            }

            if (emp1 < 0 && emp2 < 0)
                return;

            // Both assigned: enforce continuity by moving the boundary to shift2 actual start (>= template)
            var idxEmp1 = day * n + emp1;
            var a1Start2 = availStartMin[idxEmp1];
            var a1End2 = availEndMin[idxEmp1];

            var idxEmp2 = day * n + emp2;
            var a2Start2 = availStartMin[idxEmp2];
            var a2End2 = availEndMin[idxEmp2];

            // shift2 actual start
            var boundary = Math.Max(sh2.StartMin, a2Start2);

            // If shift1 cannot reach boundary (due to availability), best-effort: cap at their end.
            // Candidate selection tries to avoid this situation.
            var from1b = Math.Max(sh1.StartMin, a1Start2);
            var to1b = Math.Min(boundary, a1End2);

            var from2b = boundary;
            var to2b = Math.Min(sh2.EndMin, a2End2);

            var h1b = to1b > from1b ? (to1b - from1b) / 60d : 0;
            var h2b = to2b > from2b ? (to2b - from2b) / 60d : 0;

            totalHours[emp1] += h1b - slotHours[pos1];
            totalHours[emp2] += h2b - slotHours[pos2];

            slotFromMin[pos1] = from1b;
            slotToMin[pos1] = to1b;
            slotHours[pos1] = h1b;

            slotFromMin[pos2] = from2b;
            slotToMin[pos2] = to2b;
            slotHours[pos2] = h2b;
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


