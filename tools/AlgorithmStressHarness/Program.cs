using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Globalization;
using System.Net.Http.Json;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;

var apiBase = args.Length > 0 ? args[0] : "http://localhost:54295";
var containerId = args.Length > 1 && int.TryParse(args[1], out var parsedContainerId) ? parsedContainerId : 2;
var graphId = args.Length > 2 && int.TryParse(args[2], out var parsedGraphId) ? parsedGraphId : 5;
var availabilityGroupId = args.Length > 3 && int.TryParse(args[3], out var parsedAvailabilityId) ? parsedAvailabilityId : 2;

using var client = new HttpClient
{
    BaseAddress = new Uri(apiBase, UriKind.Absolute),
    Timeout = TimeSpan.FromMinutes(2)
};

var graph = await client.GetFromJsonAsync<GraphDto>($"api/containers/{containerId}/graphs/{graphId}")
    ?? throw new InvalidOperationException("Graph payload was empty.");
var graphEmployees = await client.GetFromJsonAsync<List<GraphEmployeeDto>>($"api/containers/{containerId}/graphs/{graphId}/employees")
    ?? throw new InvalidOperationException("Graph employees payload was empty.");
var availabilityGroup = await client.GetFromJsonAsync<AvailabilityGroupDto>($"api/availability-groups/{availabilityGroupId}")
    ?? throw new InvalidOperationException("Availability group payload was empty.");
var availabilityItems = await client.GetFromJsonAsync<List<AvailabilityGroupItemDto>>($"api/availability-groups/{availabilityGroupId}/items")
    ?? throw new InvalidOperationException("Availability items payload was empty.");
var employeeDirectory = await client.GetFromJsonAsync<List<EmployeeDto>>("api/employees")
    ?? new List<EmployeeDto>();

var schedule = new ScheduleModel
{
    Id = graph.Id,
    ContainerId = graph.ContainerId,
    ShopId = graph.ShopId,
    Name = graph.Name,
    Year = graph.Year,
    Month = graph.Month,
    PeoplePerShift = graph.PeoplePerShift,
    Shift1Time = graph.Shift1Time,
    Shift2Time = graph.Shift2Time,
    MaxHoursPerEmpMonth = graph.MaxHoursPerEmpMonth,
    MaxConsecutiveDays = graph.MaxConsecutiveDays,
    MaxConsecutiveFull = graph.MaxConsecutiveFull,
    MaxFullPerMonth = graph.MaxFullPerMonth,
    Note = graph.Note,
    AvailabilityGroupId = graph.AvailabilityGroupId
};

var employees = graphEmployees
    .OrderBy(x => x.DisplayOrder)
    .ThenBy(x => x.Id)
    .Select(x => new ScheduleEmployeeModel
    {
        Id = x.Id,
        ScheduleId = x.ScheduleId,
        EmployeeId = x.EmployeeId,
        MinHoursMonth = x.MinHoursMonth,
        DisplayOrder = x.DisplayOrder
    })
    .ToList();

var employeeNames = employeeDirectory
    .GroupBy(x => x.Id)
    .ToDictionary(
        x => x.Key,
        x =>
        {
            var employee = x.First();
            var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? $"Employee {employee.Id}" : fullName;
        });

var baseAvailability = BuildAvailabilityGroupModel(availabilityGroup, availabilityItems);
var variants = BuildVariants(baseAvailability, schedule);

var generator = new ScheduleGenerator();

Console.WriteLine($"Scenario set for container {containerId}, graph {graphId}, availability {availabilityGroupId}");
Console.WriteLine("name\tconflictDays\tcoverageGapHours\tminDef\tminSq\tdesiredDef\tdesiredSq\trestPenalty\tmaxExtRun\tworstExtEmp\tassignedHours");

foreach (var variant in variants)
{
    var slots = await generator.GenerateAsync(
        schedule,
        new[] { variant.Group },
        employees,
        progress: null,
        ct: CancellationToken.None);

    var metrics = ComputeMetrics(schedule, slots, employees, variant.Group, employeeNames);
    Console.WriteLine(
        string.Join(
            "\t",
            variant.Name,
            metrics.ConflictDays.ToString(CultureInfo.InvariantCulture),
            metrics.CoverageGapHours.ToString("0.##", CultureInfo.InvariantCulture),
            metrics.MinHourDeficit.ToString("0.##", CultureInfo.InvariantCulture),
            metrics.MinHourSquaredDeficit.ToString("0.##", CultureInfo.InvariantCulture),
            metrics.DesiredHourDeficit.ToString("0.##", CultureInfo.InvariantCulture),
            metrics.DesiredHourSquaredDeficit.ToString("0.##", CultureInfo.InvariantCulture),
            metrics.RestPenalty.ToString("0.##", CultureInfo.InvariantCulture),
            metrics.MaxExtendedOffRun.ToString(CultureInfo.InvariantCulture),
            metrics.WorstExtendedOffRunEmployee,
            metrics.AssignedHours.ToString("0.##", CultureInfo.InvariantCulture)));

    if (variant.Name == "base")
    {
        Console.WriteLine("employee\tassigned\tmin\tdesired\tpotential\tminDef\tavailableFullDays\tscheduledFullDays");
        foreach (var employeeMetric in metrics.EmployeeMetrics
                     .OrderByDescending(x => x.MinDeficit)
                     .ThenBy(x => x.Name, StringComparer.Ordinal))
        {
            Console.WriteLine(
                string.Join(
                    "\t",
                    employeeMetric.Name,
                    employeeMetric.AssignedHours.ToString("0.##", CultureInfo.InvariantCulture),
                    employeeMetric.MinHours.ToString("0.##", CultureInfo.InvariantCulture),
                    employeeMetric.DesiredHours.ToString("0.##", CultureInfo.InvariantCulture),
                    employeeMetric.PotentialHours.ToString("0.##", CultureInfo.InvariantCulture),
                    employeeMetric.MinDeficit.ToString("0.##", CultureInfo.InvariantCulture),
                    employeeMetric.AvailableFullDays.ToString(CultureInfo.InvariantCulture),
                    employeeMetric.ScheduledFullDays.ToString(CultureInfo.InvariantCulture)));
        }

        Console.WriteLine("underMinAssignments");
        foreach (var employeeMetric in metrics.EmployeeMetrics
                     .Where(x => x.MinDeficit > 0.0)
                     .OrderByDescending(x => x.MinDeficit)
                     .ThenBy(x => x.Name, StringComparer.Ordinal))
        {
            Console.WriteLine($"{employeeMetric.Name}\t{BuildAssignmentSummary(slots, employeeMetric.EmployeeId)}");
        }

        Console.WriteLine();
    }
}

static AvailabilityGroupModel BuildAvailabilityGroupModel(
    AvailabilityGroupDto group,
    List<AvailabilityGroupItemDto> items)
{
    var memberMap = new Dictionary<int, AvailabilityGroupMemberModel>();

    foreach (var item in items.OrderBy(x => x.DisplayOrder).ThenBy(x => x.EmployeeId).ThenBy(x => x.DayOfMonth))
    {
        if (!memberMap.TryGetValue(item.MemberId, out var member))
        {
            member = new AvailabilityGroupMemberModel
            {
                Id = item.MemberId,
                AvailabilityGroupId = group.Id,
                EmployeeId = item.EmployeeId,
                DisplayOrder = item.DisplayOrder
            };
            memberMap[item.MemberId] = member;
        }

        member.Days.Add(new AvailabilityGroupDayModel
        {
            Id = item.DayId,
            AvailabilityGroupMemberId = item.MemberId,
            DayOfMonth = item.DayOfMonth,
            Kind = item.Kind,
            IntervalStr = item.IntervalStr
        });
    }

    return new AvailabilityGroupModel
    {
        Id = group.Id,
        Name = group.Name,
        Year = group.Year,
        Month = group.Month,
        Members = memberMap.Values
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.EmployeeId)
            .ToList()
    };
}

static List<AvailabilityVariant> BuildVariants(AvailabilityGroupModel source, ScheduleModel schedule)
{
    var variants = new List<AvailabilityVariant>
    {
        new("base", CloneGroup(source)),
        new("early_leave_bridge", CloneGroup(source)),
        new("late_start_bridge", CloneGroup(source)),
        new("boundary_crossfade", CloneGroup(source)),
        new("weekend_partial", CloneGroup(source)),
        new("mixed_partial_stress", CloneGroup(source))
    };

    ApplyEarlyLeaveBridge(variants[1].Group, schedule);
    ApplyLateStartBridge(variants[2].Group, schedule);
    ApplyBoundaryCrossfade(variants[3].Group, schedule);
    ApplyWeekendPartial(variants[4].Group, schedule);
    ApplyEarlyLeaveBridge(variants[5].Group, schedule);
    ApplyLateStartBridge(variants[5].Group, schedule);
    ApplyBoundaryCrossfade(variants[5].Group, schedule, stronger: true);
    ApplyWeekendPartial(variants[5].Group, schedule, stronger: true);

    return variants;
}

static AvailabilityGroupModel CloneGroup(AvailabilityGroupModel source)
{
    return new AvailabilityGroupModel
    {
        Id = source.Id,
        Name = source.Name,
        Year = source.Year,
        Month = source.Month,
        Members = source.Members
            .Select(member => new AvailabilityGroupMemberModel
            {
                Id = member.Id,
                AvailabilityGroupId = member.AvailabilityGroupId,
                EmployeeId = member.EmployeeId,
                DisplayOrder = member.DisplayOrder,
                Days = member.Days
                    .Select(day => new AvailabilityGroupDayModel
                    {
                        Id = day.Id,
                        AvailabilityGroupMemberId = day.AvailabilityGroupMemberId,
                        DayOfMonth = day.DayOfMonth,
                        Kind = day.Kind,
                        IntervalStr = day.IntervalStr
                    })
                    .OrderBy(day => day.DayOfMonth)
                    .ToList()
            })
            .OrderBy(member => member.DisplayOrder)
            .ThenBy(member => member.EmployeeId)
            .ToList()
    };
}

static void ApplyEarlyLeaveBridge(AvailabilityGroupModel group, ScheduleModel schedule)
{
    var shifts = GetShiftTemplates(schedule);
    if (shifts.Count == 0)
        return;

    var shift1 = shifts[0];
    var minimumEnd = Math.Min(shift1.EndMin - 30, shift1.StartMin + 180);
    if (minimumEnd <= shift1.StartMin)
        return;

    foreach (var member in group.Members)
    {
        foreach (var day in member.Days)
        {
            if (day.Kind != AvailabilityKind.ANY)
                continue;

            var selector = StablePercent(member.DisplayOrder, day.DayOfMonth, salt: 11);
            if (selector >= 18)
                continue;

            var trimMinutes = selector < 6 ? 60 : 90;
            var end = Math.Max(minimumEnd, shift1.EndMin - trimMinutes);
            day.Kind = AvailabilityKind.INT;
            day.IntervalStr = FormatInterval(shift1.StartMin, end);
        }
    }
}

static void ApplyLateStartBridge(AvailabilityGroupModel group, ScheduleModel schedule)
{
    var shifts = GetShiftTemplates(schedule);
    if (shifts.Count < 2)
        return;

    var shift1 = shifts[0];
    var shift2 = shifts[1];

    foreach (var member in group.Members)
    {
        foreach (var day in member.Days)
        {
            if (day.Kind != AvailabilityKind.ANY)
                continue;

            var selector = StablePercent(member.DisplayOrder, day.DayOfMonth, salt: 17);
            if (selector >= 18)
                continue;

            var leadMinutes = selector < 6 ? 60 : 90;
            var from = Math.Max(shift1.StartMin + 60, shift2.StartMin - leadMinutes);
            day.Kind = AvailabilityKind.INT;
            day.IntervalStr = FormatInterval(from, shift2.EndMin);
        }
    }
}

static void ApplyBoundaryCrossfade(AvailabilityGroupModel group, ScheduleModel schedule, bool stronger = false)
{
    var shifts = GetShiftTemplates(schedule);
    if (shifts.Count < 2)
        return;

    var shift1 = shifts[0];
    var shift2 = shifts[1];
    var hotDays = stronger ? new[] { 6, 7, 8, 12, 13, 14, 20, 21 } : new[] { 7, 8, 13, 14, 21 };

    foreach (var member in group.Members)
    {
        foreach (var day in member.Days.Where(day => hotDays.Contains(day.DayOfMonth)))
        {
            if (day.Kind == AvailabilityKind.NONE)
                continue;

            var selector = StablePercent(member.DisplayOrder, day.DayOfMonth, salt: stronger ? 29 : 23);
            if (selector < (stronger ? 10 : 6))
            {
                day.Kind = AvailabilityKind.NONE;
                day.IntervalStr = null;
            }
            else if ((member.DisplayOrder + day.DayOfMonth) % 2 == 0)
            {
                day.Kind = AvailabilityKind.INT;
                day.IntervalStr = FormatInterval(shift1.StartMin, Math.Max(shift1.StartMin + 180, shift1.EndMin - 60));
            }
            else
            {
                day.Kind = AvailabilityKind.INT;
                day.IntervalStr = FormatInterval(Math.Max(shift1.StartMin + 60, shift2.StartMin - 60), shift2.EndMin);
            }
        }
    }
}

static void ApplyWeekendPartial(AvailabilityGroupModel group, ScheduleModel schedule, bool stronger = false)
{
    var shifts = GetShiftTemplates(schedule);
    if (shifts.Count == 0)
        return;

    var shift1 = shifts[0];
    var shift2 = shifts.Count > 1 ? shifts[1] : shifts[0];

    foreach (var member in group.Members)
    {
        foreach (var day in member.Days)
        {
            if (!IsWeekend(schedule.Year, schedule.Month, day.DayOfMonth))
                continue;

            var selector = StablePercent(member.DisplayOrder, day.DayOfMonth, salt: stronger ? 41 : 31);
            if (day.Kind == AvailabilityKind.ANY && selector < (stronger ? 30 : 18))
            {
                if (selector < (stronger ? 8 : 5))
                {
                    day.Kind = AvailabilityKind.NONE;
                    day.IntervalStr = null;
                }
                else if (selector % 2 == 0)
                {
                    day.Kind = AvailabilityKind.INT;
                    day.IntervalStr = FormatInterval(shift1.StartMin, Math.Max(shift1.StartMin + 180, shift1.EndMin - 60));
                }
                else
                {
                    day.Kind = AvailabilityKind.INT;
                    day.IntervalStr = FormatInterval(Math.Max(shift1.StartMin + 60, shift2.StartMin - 60), shift2.EndMin);
                }
            }
        }
    }
}

static Metrics ComputeMetrics(
    ScheduleModel schedule,
    IList<ScheduleSlotModel> slots,
    List<ScheduleEmployeeModel> employees,
    AvailabilityGroupModel availabilityGroup,
    Dictionary<int, string> employeeNames)
{
    var shifts = GetShiftTemplates(schedule);
    var daysInMonth = DateTime.DaysInMonth(schedule.Year, schedule.Month);
    var employeeIds = employees.Select(x => x.EmployeeId).ToArray();
    var n = employeeIds.Length;
    var stride = daysInMonth + 1;
    var employeeIndexById = employeeIds
        .Select((employeeId, index) => new { employeeId, index })
        .ToDictionary(x => x.employeeId, x => x.index);

    var intervalsByEmployeeDay = new Dictionary<(int EmployeeId, int Day), List<Interval>>();
    var hoursByEmployee = new double[n];
    var workingDays = new bool[n * stride];
    var assignedHours = 0.0;

    foreach (var slot in slots)
    {
        if (slot.EmployeeId is not int employeeId || employeeId <= 0)
            continue;

        if (!employeeIndexById.TryGetValue(employeeId, out var emp))
            continue;

        var from = ParseTime(slot.FromTime);
        var to = ParseTime(slot.ToTime);
        var duration = ComputeIntervalHours(from, to);
        assignedHours += duration;
        hoursByEmployee[emp] += duration;
        workingDays[emp * stride + slot.DayOfMonth] = true;

        var key = (employeeId, slot.DayOfMonth);
        if (!intervalsByEmployeeDay.TryGetValue(key, out var intervals))
        {
            intervals = new List<Interval>();
            intervalsByEmployeeDay[key] = intervals;
        }

        intervals.Add(new Interval(from, to));
    }

    var conflictDays = 0;
    var coverageGapMinutes = 0;

    for (var day = 1; day <= daysInMonth; day++)
    {
        var dayHasConflict = false;
        var hasOverlap = false;

        for (var emp = 0; emp < n && !hasOverlap; emp++)
        {
            if (!intervalsByEmployeeDay.TryGetValue((employeeIds[emp], day), out var intervals) || intervals.Count < 2)
                continue;

            var ordered = intervals
                .OrderBy(interval => interval.FromMin)
                .ThenBy(interval => interval.ToMin)
                .ToList();

            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i - 1].FromMin < ordered[i].ToMin && ordered[i].FromMin < ordered[i - 1].ToMin)
                {
                    hasOverlap = true;
                    break;
                }
            }
        }

        if (hasOverlap)
            dayHasConflict = true;

        foreach (var shift in shifts)
        {
            var shiftGapMinutes = ComputeShiftCoverageGapMinutes(
                day,
                shift,
                schedule.PeoplePerShift,
                employeeIds,
                intervalsByEmployeeDay);

            coverageGapMinutes += shiftGapMinutes;
            if (shiftGapMinutes > 0)
                dayHasConflict = true;
        }

        if (dayHasConflict)
            conflictDays++;
    }

    var minHours = employees
        .Select(employee => employee.MinHoursMonth is > 0 ? (double)employee.MinHoursMonth.Value : 0.0)
        .ToArray();

    var potential = BuildPotentialHours(schedule, availabilityGroup, employeeIds);
    var desiredHours = BuildDesiredHours(minHours, potential.TotalPotentialHours, schedule.MaxHoursPerEmpMonth, shifts, daysInMonth, schedule.PeoplePerShift);
    var availableFullDays = BuildAvailableFullDayCounts(schedule, availabilityGroup, employeeIds);
    var scheduledFullDays = BuildScheduledFullDayCounts(daysInMonth, employeeIds, intervalsByEmployeeDay);

    var minHourDeficit = 0.0;
    var minHourSquaredDeficit = 0.0;
    var desiredHourDeficit = 0.0;
    var desiredHourSquaredDeficit = 0.0;

    for (var emp = 0; emp < n; emp++)
    {
        var hardDeficit = Math.Max(0.0, minHours[emp] - hoursByEmployee[emp]);
        if (hardDeficit > 0)
        {
            minHourDeficit += hardDeficit;
            minHourSquaredDeficit += hardDeficit * hardDeficit;
        }

        var softDeficit = Math.Max(0.0, desiredHours[emp] - hoursByEmployee[emp]);
        if (softDeficit > 0)
        {
            desiredHourDeficit += softDeficit;
            desiredHourSquaredDeficit += softDeficit * softDeficit;
        }
    }

    var restMetrics = ComputeRestMetrics(
        potential.Unavailable,
        workingDays,
        employeeIds,
        employeeNames,
        daysInMonth,
        stride,
        n);

    var employeeMetrics = new List<EmployeeMetrics>(n);
    for (var emp = 0; emp < n; emp++)
    {
        employeeMetrics.Add(new EmployeeMetrics(
            employeeIds[emp],
            employeeNames.TryGetValue(employeeIds[emp], out var name) ? name : $"Employee {employeeIds[emp]}",
            hoursByEmployee[emp],
            minHours[emp],
            desiredHours[emp],
            potential.TotalPotentialHours[emp],
            Math.Max(0.0, minHours[emp] - hoursByEmployee[emp]),
            availableFullDays[emp],
            scheduledFullDays[emp]));
    }

    return new Metrics(
        conflictDays,
        coverageGapMinutes / 60d,
        minHourDeficit,
        minHourSquaredDeficit,
        desiredHourDeficit,
        desiredHourSquaredDeficit,
        restMetrics.RestPenalty,
        restMetrics.MaxExtendedOffRun,
        restMetrics.WorstEmployeeName,
        assignedHours,
        employeeMetrics);
}

static int[] BuildAvailableFullDayCounts(
    ScheduleModel schedule,
    AvailabilityGroupModel availabilityGroup,
    int[] employeeIds)
{
    var shifts = GetShiftTemplates(schedule);
    var counts = new int[employeeIds.Length];
    var membersByEmployeeId = availabilityGroup.Members.ToDictionary(member => member.EmployeeId);

    if (shifts.Count < 2)
        return counts;

    for (var emp = 0; emp < employeeIds.Length; emp++)
    {
        if (!membersByEmployeeId.TryGetValue(employeeIds[emp], out var member))
            continue;

        foreach (var dayModel in member.Days)
        {
            if (dayModel.Kind is AvailabilityKind.ANY or AvailabilityKind.NONE)
                continue;

            if (!TryGetAvailabilityWindow(dayModel, out var fromMin, out var toMin))
                continue;

            if (fromMin <= shifts[0].StartMin
                && toMin >= shifts[0].EndMin
                && fromMin <= shifts[1].StartMin
                && toMin >= shifts[1].EndMin)
            {
                counts[emp]++;
            }
        }
    }

    return counts;
}

static int[] BuildScheduledFullDayCounts(
    int daysInMonth,
    int[] employeeIds,
    Dictionary<(int EmployeeId, int Day), List<Interval>> intervalsByEmployeeDay)
{
    var counts = new int[employeeIds.Length];

    for (var emp = 0; emp < employeeIds.Length; emp++)
    {
        for (var day = 1; day <= daysInMonth; day++)
        {
            if (!intervalsByEmployeeDay.TryGetValue((employeeIds[emp], day), out var intervals))
                continue;

            if (intervals.Count >= 2)
                counts[emp]++;
        }
    }

    return counts;
}

static string BuildAssignmentSummary(
    IEnumerable<ScheduleSlotModel> slots,
    int employeeId)
{
    var fragments = slots
        .Where(slot => slot.EmployeeId == employeeId)
        .GroupBy(slot => slot.DayOfMonth)
        .OrderBy(group => group.Key)
        .Select(group =>
        {
            var intervals = group
                .Select(slot => $"{slot.FromTime}-{slot.ToTime}")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            return $"{group.Key:00}:{string.Join("|", intervals)}";
        })
        .ToArray();

    return fragments.Length == 0 ? "-" : string.Join(", ", fragments);
}

static int ComputeShiftCoverageGapMinutes(
    int day,
    ShiftTemplate shift,
    int peoplePerShift,
    int[] employeeIds,
    Dictionary<(int EmployeeId, int Day), List<Interval>> intervalsByEmployeeDay)
{
    var requiredWorkerMinutes = Math.Max(0, shift.EndMin - shift.StartMin) * peoplePerShift;
    if (requiredWorkerMinutes <= 0)
        return 0;

    var clippedIntervals = new List<Interval>(capacity: Math.Max(employeeIds.Length, 1));
    var boundaries = new List<int>(capacity: Math.Max(employeeIds.Length * 2 + 2, 4))
    {
        shift.StartMin,
        shift.EndMin
    };

    for (var emp = 0; emp < employeeIds.Length; emp++)
    {
        if (!intervalsByEmployeeDay.TryGetValue((employeeIds[emp], day), out var intervals))
            continue;

        foreach (var interval in intervals)
        {
            var from = Math.Max(shift.StartMin, interval.FromMin);
            var to = Math.Min(shift.EndMin, interval.ToMin);
            if (to <= from)
                continue;

            clippedIntervals.Add(new Interval(from, to));
            boundaries.Add(from);
            boundaries.Add(to);
        }
    }

    if (clippedIntervals.Count == 0)
        return requiredWorkerMinutes;

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
        foreach (var interval in clippedIntervals)
        {
            if (interval.FromMin <= segmentFrom && interval.ToMin >= segmentTo)
                active++;
        }

        gapMinutes += Math.Max(0, peoplePerShift - active) * (segmentTo - segmentFrom);
    }

    return gapMinutes;
}

static PotentialMetrics BuildPotentialHours(
    ScheduleModel schedule,
    AvailabilityGroupModel availabilityGroup,
    int[] employeeIds)
{
    var shifts = GetShiftTemplates(schedule);
    var daysInMonth = DateTime.DaysInMonth(schedule.Year, schedule.Month);
    var stride = daysInMonth + 1;
    var n = employeeIds.Length;
    var dailyPotentialHours = new double[n * stride];
    var unavailable = new bool[n * stride];
    var membersByEmployeeId = availabilityGroup.Members.ToDictionary(member => member.EmployeeId);

    for (var emp = 0; emp < n; emp++)
    {
        var employeeId = employeeIds[emp];
        membersByEmployeeId.TryGetValue(employeeId, out var member);

        for (var day = 1; day <= daysInMonth; day++)
        {
            var dayModel = member?.Days.FirstOrDefault(item => item.DayOfMonth == day);
            if (dayModel is null)
                continue;

            if (!TryGetAvailabilityWindow(dayModel, out var fromMin, out var toMin))
            {
                unavailable[emp * stride + day] = true;
                continue;
            }

            var total = 0.0;
            foreach (var shift in shifts)
            {
                var from = Math.Max(shift.StartMin, fromMin);
                var to = Math.Min(shift.EndMin, toMin);
                if (to > from)
                    total += (to - from) / 60d;
            }

            dailyPotentialHours[emp * stride + day] = total;
            unavailable[emp * stride + day] = total <= 0.0;
        }
    }

    var totalPotentialHours = new double[n];
    for (var emp = 0; emp < n; emp++)
    {
        var total = 0.0;
        for (var day = 1; day <= daysInMonth; day++)
            total += dailyPotentialHours[emp * stride + day];

        totalPotentialHours[emp] = total;
    }

    return new PotentialMetrics(totalPotentialHours, unavailable);
}

static bool TryGetAvailabilityWindow(AvailabilityGroupDayModel dayModel, out int fromMin, out int toMin)
{
    if (dayModel.Kind == AvailabilityKind.NONE)
    {
        fromMin = 24 * 60;
        toMin = 0;
        return false;
    }

    if (dayModel.Kind == AvailabilityKind.ANY)
    {
        fromMin = 0;
        toMin = 24 * 60;
        return true;
    }

    if (TryParseInterval(dayModel.IntervalStr, out fromMin, out toMin))
        return toMin > fromMin;

    fromMin = 24 * 60;
    toMin = 0;
    return false;
}

static double[] BuildDesiredHours(
    double[] minHours,
    double[] totalPotentialHours,
    int maxHoursPerEmpMonth,
    List<ShiftTemplate> shifts,
    int daysInMonth,
    int peoplePerShift)
{
    var n = minHours.Length;
    var desired = new double[n];
    var cappedPotential = new double[n];
    var totalDemand = shifts.Sum(shift => shift.Hours) * daysInMonth * peoplePerShift;

    for (var emp = 0; emp < n; emp++)
    {
        var cap = totalPotentialHours[emp];
        if (maxHoursPerEmpMonth > 0)
            cap = Math.Min(cap, maxHoursPerEmpMonth);

        cappedPotential[emp] = Math.Max(0.0, cap);
        desired[emp] = Math.Min(Math.Max(0.0, minHours[emp]), cappedPotential[emp]);
        totalDemand -= desired[emp];
    }

    const double eps = 1e-9;
    while (totalDemand > eps)
    {
        var activeEmployees = 0;
        for (var emp = 0; emp < n; emp++)
        {
            if (cappedPotential[emp] - desired[emp] > eps)
                activeEmployees++;
        }

        if (activeEmployees <= 0)
            break;

        var fairShare = totalDemand / activeEmployees;
        var distributed = 0.0;
        for (var emp = 0; emp < n; emp++)
        {
            var headroom = Math.Max(0.0, cappedPotential[emp] - desired[emp]);
            if (headroom <= eps)
                continue;

            var add = Math.Min(headroom, fairShare);
            if (add <= eps)
                continue;

            desired[emp] += add;
            distributed += add;
        }

        if (distributed <= eps)
            break;

        totalDemand -= distributed;
    }

    return desired;
}

static RestMetrics ComputeRestMetrics(
    bool[] unavailable,
    bool[] workingDays,
    int[] employeeIds,
    Dictionary<int, string> employeeNames,
    int daysInMonth,
    int stride,
    int n)
{
    var restPenalty = 0.0;
    var maxExtendedOffRun = 0;
    var worstEmployeeName = "-";

    for (var emp = 0; emp < n; emp++)
    {
        var day = 1;
        while (day <= daysInMonth)
        {
            var isWorking = workingDays[emp * stride + day];
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
                isWorking = workingDays[emp * stride + day];
                if (isWorking)
                    break;

                runLength++;
                if (unavailable[emp * stride + day])
                    blockedDays++;
                else
                    availableOffDays++;

                day++;
            }

            if (availableOffDays <= 0)
                continue;

            restPenalty += availableOffDays * availableOffDays;
            if (blockedDays > 0)
                restPenalty += blockedDays * availableOffDays;

            if (runLength > 4)
                restPenalty += (runLength - 4) * 0.5;

            if (blockedDays > 0 && runLength > maxExtendedOffRun)
            {
                maxExtendedOffRun = runLength;
                worstEmployeeName = employeeNames.TryGetValue(employeeIds[emp], out var name)
                    ? name
                    : $"Employee {employeeIds[emp]}";
            }
        }
    }

    return new RestMetrics(restPenalty, maxExtendedOffRun, worstEmployeeName);
}

static List<ShiftTemplate> GetShiftTemplates(ScheduleModel schedule)
{
    var shifts = new List<ShiftTemplate>(capacity: 2);

    if (TryCreateShiftTemplate(schedule.Shift1Time, out var shift1))
        shifts.Add(shift1);

    if (TryCreateShiftTemplate(schedule.Shift2Time, out var shift2))
        shifts.Add(shift2);

    return shifts;
}

static bool TryCreateShiftTemplate(string? value, out ShiftTemplate shift)
{
    shift = default;
    if (!TryParseInterval(value, out var fromMin, out var toMin))
        return false;

    shift = new ShiftTemplate(fromMin, toMin, (toMin - fromMin) / 60d);
    return true;
}

static bool TryParseInterval(string? value, out int fromMin, out int toMin)
{
    fromMin = 0;
    toMin = 0;

    if (string.IsNullOrWhiteSpace(value))
        return false;

    var cleaned = value.Replace("–", "-", StringComparison.Ordinal).Replace("—", "-", StringComparison.Ordinal);
    var parts = cleaned.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length != 2)
        return false;

    if (!TimeOnly.TryParseExact(parts[0], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
        return false;

    if (!TimeOnly.TryParseExact(parts[1], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
        return false;

    fromMin = ParseTime(from.ToString("HH:mm", CultureInfo.InvariantCulture));
    toMin = ParseTime(to.ToString("HH:mm", CultureInfo.InvariantCulture));
    return toMin > fromMin;
}

static double ComputeIntervalHours(int fromMin, int toMin)
{
    var minutes = toMin - fromMin;
    if (minutes < 0)
        minutes += (int)TimeSpan.FromDays(1).TotalMinutes;

    return minutes / 60.0;
}

static int ParseTime(string value)
{
    var time = TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture);
    return (int)time.ToTimeSpan().TotalMinutes;
}

static string FormatInterval(int fromMin, int toMin)
{
    return $"{MinutesToText(fromMin)} - {MinutesToText(toMin)}";
}

static string MinutesToText(int minutes)
{
    var normalized = ((minutes % (24 * 60)) + (24 * 60)) % (24 * 60);
    var time = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(normalized));
    return time.ToString("HH:mm", CultureInfo.InvariantCulture);
}

static bool IsWeekend(int year, int month, int day)
{
    var date = new DateOnly(year, month, day);
    return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}

static int StablePercent(int displayOrder, int dayOfMonth, int salt)
{
    var value = (displayOrder + 3) * 97 + dayOfMonth * 53 + salt * 31;
    value ^= value << 7;
    value ^= value >> 9;
    return Math.Abs(value % 100);
}

internal readonly record struct ShiftTemplate(int StartMin, int EndMin, double Hours);

internal readonly record struct Interval(int FromMin, int ToMin);

internal sealed record AvailabilityVariant(string Name, AvailabilityGroupModel Group);

internal sealed record Metrics(
    int ConflictDays,
    double CoverageGapHours,
    double MinHourDeficit,
    double MinHourSquaredDeficit,
    double DesiredHourDeficit,
    double DesiredHourSquaredDeficit,
    double RestPenalty,
    int MaxExtendedOffRun,
    string WorstExtendedOffRunEmployee,
    double AssignedHours,
    IReadOnlyList<EmployeeMetrics> EmployeeMetrics);

internal sealed record EmployeeMetrics(
    int EmployeeId,
    string Name,
    double AssignedHours,
    double MinHours,
    double DesiredHours,
    double PotentialHours,
    double MinDeficit,
    int AvailableFullDays,
    int ScheduledFullDays);

internal sealed record PotentialMetrics(double[] TotalPotentialHours, bool[] Unavailable);

internal sealed record RestMetrics(double RestPenalty, int MaxExtendedOffRun, string WorstEmployeeName);

internal sealed class GraphDto
{
    public int Id { get; set; }
    public int ContainerId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int PeoplePerShift { get; set; }
    public string Shift1Time { get; set; } = string.Empty;
    public string Shift2Time { get; set; } = string.Empty;
    public int MaxHoursPerEmpMonth { get; set; }
    public int MaxConsecutiveDays { get; set; }
    public int MaxConsecutiveFull { get; set; }
    public int MaxFullPerMonth { get; set; }
    public string? Note { get; set; }
    public int? AvailabilityGroupId { get; set; }
}

internal sealed class GraphEmployeeDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int EmployeeId { get; set; }
    public int? MinHoursMonth { get; set; }
    public int DisplayOrder { get; set; }
}

internal sealed class AvailabilityGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
}

internal sealed class AvailabilityGroupItemDto
{
    public int MemberId { get; set; }
    public int EmployeeId { get; set; }
    public int DisplayOrder { get; set; }
    public int DayId { get; set; }
    public int DayOfMonth { get; set; }
    public AvailabilityKind Kind { get; set; }
    public string? IntervalStr { get; set; }
}

internal sealed class EmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
