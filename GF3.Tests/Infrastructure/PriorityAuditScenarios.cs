using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;

namespace GF3.Tests.Infrastructure;

internal sealed record PriorityAuditScenario(string Id, ScheduleModel Schedule, ScheduleEmployeeModel[] Employees,
    AvailabilityGroupModel[] Groups, ScheduleSlotModel[] External, int Hours, bool Respect, bool Weekends,
    int[] Factors);

internal static class PriorityAuditScenarios
{
    internal static readonly int[] Levels = [6, 4, 5, 4, 2, 2, 6, 4, 4, 4, 4];
    internal static readonly int[][] PairwiseRows = Cover();

    private static int[][] Cover()
    {
        var missing = new HashSet<(int, int, int, int)>();
        for (var a = 0; a < Levels.Length; a++)
        for (var b = a + 1; b < Levels.Length; b++)
        for (var x = 0; x < Levels[a]; x++)
        for (var y = 0; y < Levels[b]; y++) missing.Add((a, b, x, y));
        var random = new Random(350112026);
        var rows = new List<int[]>();
        while (missing.Count > 0)
        {
            int[]? best = null;
            var bestScore = -1;
            var first = missing.OrderBy(p => p.Item1).ThenBy(p => p.Item2).ThenBy(p => p.Item3).ThenBy(p => p.Item4).First();
            for (var trial = 0; trial < 600; trial++)
            {
                var row = Levels.Select(level => random.Next(level)).ToArray();
                row[first.Item1] = first.Item3; row[first.Item2] = first.Item4;
                var score = 0;
                for (var a = 0; a < row.Length; a++)
                for (var b = a + 1; b < row.Length; b++) if (missing.Contains((a, b, row[a], row[b]))) score++;
                if (score <= bestScore) continue;
                best = row; bestScore = score;
            }
            rows.Add(best!);
            for (var a = 0; a < best!.Length; a++)
            for (var b = a + 1; b < best.Length; b++) missing.Remove((a, b, best[a], best[b]));
        }
        return rows.ToArray();
    }

    internal static PriorityAuditScenario Build(int index)
    {
        var f = PairwiseRows[index];
        var count = new[] { 1, 2, 4, 8, 12, 20 }[f[0]];
        var staffing = f[1] + 1;
        var priorities = new[] { 0, 1, Math.Min(2, count), Math.Max(1, count / 2), count }[f[2]];
        var hours = new[] { 6, 8, 10, 12 }[f[3]];
        var (year, month) = new[] { (2026, 2), (2028, 2), (2026, 11), (2026, 7) }[f[8]];
        var first = new[] { "09:00 - 15:00", "09:00 - 16:00", "08:00 - 14:00", "09:00 - 13:00" }[f[9]];
        var second = new[] { "15:00 - 21:00", "15:00 - 22:00", "14:00 - 20:00", "15:00 - 21:00" }[f[9]];
        var days = DateTime.DaysInMonth(year, month);
        var schedule = new ScheduleModel { Id = 1, Year = year, Month = month, PeoplePerShift = staffing,
            Shift1Time = first, Shift2Time = second, MaxHoursPerEmpMonth = 200, MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 2, MaxFullPerMonth = 5 };
        var roster = Enumerable.Range(1, count).Select(id => new ScheduleEmployeeModel { EmployeeId = id,
            IsPriority = id <= priorities, MinHoursMonth = new[] { 0, 80, 168, 240 }[f[7]], DisplayOrder = id }).ToArray();
        var random = new Random(3500 + index);
        var group = new AvailabilityGroupModel { Id = 1, Year = year, Month = month };
        foreach (var employee in roster)
        {
            var member = new AvailabilityGroupMemberModel { EmployeeId = employee.EmployeeId };
            for (var day = 1; day <= days; day++)
            {
                var pattern = f[6] == 3 ? random.Next(6) : f[6];
                member.Days.Add(new() { DayOfMonth = day,
                    Kind = pattern == 4 ? AvailabilityKind.NONE : pattern == 5 ? AvailabilityKind.ANY : AvailabilityKind.INT,
                    IntervalStr = pattern switch { 0 => "08:00 - 13:00", 1 => "17:00 - 22:00", 2 => "08:00 - 22:00",
                        3 => day % 2 == 0 ? "09:00 - 14:00" : "13:00 - 21:00", _ => null } });
            }
            group.Members.Add(member);
        }
        var external = new List<ScheduleSlotModel>();
        foreach (var employee in roster)
        for (var day = 1; day <= days; day++)
        {
            if (f[10] == 0 || f[10] == 1 && (day + employee.EmployeeId) % 7 != 0 || f[10] == 2 && day % 3 == 0) continue;
            external.Add(new() { DayOfMonth = day, EmployeeId = employee.EmployeeId, Status = SlotStatus.ASSIGNED,
                FromTime = f[10] == 3 ? "08:00" : "09:00", ToTime = f[10] == 3 ? "22:00" : "13:00" });
        }
        return new($"matrix-{index:000}", schedule, roster, [group], external.ToArray(), hours, f[4] == 0, f[5] == 0, f);
    }

    internal static PriorityAuditScenario Screenshot(int target = 170)
    {
        var schedule = new ScheduleModel { Id = 1, Year = 2026, Month = 11, PeoplePerShift = 4,
            Shift1Time = "09:00 - 15:00", Shift2Time = "15:00 - 21:00", MaxHoursPerEmpMonth = 200,
            MaxConsecutiveDays = 5, MaxConsecutiveFull = 2, MaxFullPerMonth = 5 };
        var roster = Enumerable.Range(1, 12).Select(id => new ScheduleEmployeeModel { EmployeeId = id,
            IsPriority = id == 1, MinHoursMonth = id == 1 ? target : new[] { 40, 80, 120, 160, 180 }[(id - 2) % 5] }).ToArray();
        return new($"screenshot-config-target-{target}", schedule, roster, [], [], 8, false, true, []);
    }

    internal static PriorityAuditScenario Handover(string ordinaryWindow, bool blocked = false)
    {
        var s = Screenshot(8); s.Schedule.Month = 4; s.Schedule.PeoplePerShift = 1; s.Schedule.MaxHoursPerEmpMonth = 8;
        var roster = s.Employees.Take(2).ToArray(); roster[1].MinHoursMonth = 4;
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 4,
            Members = roster.Select(e => new AvailabilityGroupMemberModel { EmployeeId = e.EmployeeId,
                Days = Enumerable.Range(1, 30).Select(day => new AvailabilityGroupDayModel { DayOfMonth = day,
                    Kind = day > 1 ? AvailabilityKind.NONE : e.IsPriority || ordinaryWindow == "ANY" ? AvailabilityKind.ANY : AvailabilityKind.INT,
                    IntervalStr = day == 1 && !e.IsPriority && ordinaryWindow != "ANY" ? ordinaryWindow : null }).ToList() }).ToList() };
        return new(blocked ? "external-blocked" : ordinaryWindow == "ANY" ? "equivalent" : ordinaryWindow.StartsWith("09") ? "late-better" : "early-better",
            s.Schedule, roster, [group], blocked ? [new() { EmployeeId = 1, DayOfMonth = 1, FromTime = "17:00", ToTime = "21:00", Status = SlotStatus.ASSIGNED }] : [], 8, true, true, []);
    }

    internal static PriorityAuditScenario CalendarTrap()
    {
        var s = Screenshot(48); s.Schedule.Month = 4; s.Schedule.PeoplePerShift = 1;
        s.Schedule.MaxConsecutiveDays = 2; s.Schedule.MaxHoursPerEmpMonth = 48;
        var roster = s.Employees.Take(1).ToArray();
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 4,
            Members = [new() { EmployeeId = 1, Days = Enumerable.Range(9, 22).Select(day => new AvailabilityGroupDayModel {
                DayOfMonth = day, Kind = AvailabilityKind.NONE }).ToList() }] };
        return new("calendar-trap", s.Schedule, roster, [group], [], 8, true, false, []);
    }
}
