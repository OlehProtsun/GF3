using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;

namespace GF3.Tests.Infrastructure;

// Independent scoring/enumeration: no production generator, candidate or scoring helpers.
internal static class LegacyScheduleReference
{
    internal static (int From, int To) Range(string text)
    {
        var parts = text.Replace('–', '-').Split('-', StringSplitOptions.TrimEntries);
        int Minutes(string value)
        {
            var clock = value.Split(':');
            if (clock.Length != 2 || !int.TryParse(clock[0], out var hour) || !int.TryParse(clock[1], out var minute) ||
                hour < 0 || hour > 24 || minute < 0 || minute > 59 || hour == 24 && minute != 0) throw new FormatException("Invalid clock interval");
            return hour * 60 + minute;
        }
        return (Minutes(parts[0]), Minutes(parts[1]));
    }

    internal static (int From, int To) Window(ScheduleModel schedule, IEnumerable<AvailabilityGroupModel> groups, int employee, int day)
    {
        var from = 0; var to = 1440;
        foreach (var d in groups.Where(g => g.Year == schedule.Year && g.Month == schedule.Month)
                     .SelectMany(g => g.Members).Where(m => m.EmployeeId == employee).SelectMany(m => m.Days).Where(d => d.DayOfMonth == day))
        {
            if (d.Kind == AvailabilityKind.NONE) return (0, 0);
            if (string.IsNullOrWhiteSpace(d.IntervalStr)) { if (d.Kind == AvailabilityKind.INT) return (0, 0); continue; }
            try { var w = Range(d.IntervalStr); from = Math.Max(from, w.From); to = Math.Min(to, w.To); }
            catch (Exception e) when (e is FormatException or IndexOutOfRangeException) { if (d.Kind == AvailabilityKind.INT) return (0, 0); }
        }
        return (from, Math.Max(from, to));
    }

    internal static double[] Score(ScheduleModel schedule, IReadOnlyList<ScheduleEmployeeModel> employees,
        IReadOnlyList<AvailabilityGroupModel> groups, IReadOnlyCollection<ScheduleSlotModel> slots)
    {
        var templates = new[] { schedule.Shift1Time, schedule.Shift2Time }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(Range).ToArray();
        var days = DateTime.DaysInMonth(schedule.Year, schedule.Month);
        var assigned = slots.Where(s => s.Status == SlotStatus.ASSIGNED && s.EmployeeId.HasValue)
            .Select(s => (Day: s.DayOfMonth, Employee: s.EmployeeId!.Value, Time: Range(s.FromTime + "-" + s.ToTime))).ToArray();
        var worked = employees.Select(e => assigned.Where(s => s.Employee == e.EmployeeId).Sum(s => s.Time.To - s.Time.From) / 60d).ToArray();
        var capacity = employees.Select(e => Math.Min(schedule.MaxHoursPerEmpMonth > 0 ? schedule.MaxHoursPerEmpMonth : double.MaxValue,
            Enumerable.Range(1, days).Sum(day =>
            {
                var w = Window(schedule, groups, e.EmployeeId, day);
                return templates.Sum(t => Math.Max(0, Math.Min(w.To, t.To) - Math.Max(w.From, t.From))) / 60d;
            }))).ToArray();
        var minimum = employees.Select(e => Math.Max(0, e.MinHoursMonth ?? 0)).ToArray();
        var floor = minimum.Select((target, i) => Math.Min(target, capacity[i])).ToArray();
        var desiredSum = Math.Min(capacity.Sum(), Math.Max(floor.Sum(), templates.Sum(t => (t.To - t.From) / 60d) * days * schedule.PeoplePerShift));
        var low = 0d; var high = capacity.DefaultIfEmpty().Max();
        for (var iteration = 0; iteration < 64; iteration++)
        {
            var level = (low + high) / 2;
            if (capacity.Select((cap, i) => Math.Min(cap, floor[i] + level)).Sum() < desiredSum) low = level; else high = level;
        }
        var hard = minimum.Select((target, i) => Math.Max(0, target - worked[i])).ToArray();
        var soft = capacity.Select((cap, i) => Math.Max(0, Math.Min(cap, floor[i] + low) - worked[i])).ToArray();
        var conflicts = 0; var gap = 0; var overlap = 0;
        for (var day = 1; day <= days; day++)
        {
            var own = assigned.Where(s => s.Day == day).ToArray(); var dayGap = 0;
            foreach (var template in templates)
            {
                var endpoints = own.SelectMany(s => new[] { Math.Clamp(s.Time.From, template.From, template.To), Math.Clamp(s.Time.To, template.From, template.To) })
                    .Append(template.From).Append(template.To).Distinct().Order().ToArray();
                for (var j = 1; j < endpoints.Length; j++) dayGap += (endpoints[j] - endpoints[j - 1]) * Math.Max(0,
                    schedule.PeoplePerShift - own.Where(s => s.Time.From <= endpoints[j - 1] && s.Time.To >= endpoints[j]).Select(s => s.Employee).Distinct().Count());
            }
            var hasOverlap = own.GroupBy(s => s.Employee).Any(g => g.Any(a => g.Any(b => a != b && a.Time.From < b.Time.To && b.Time.From < a.Time.To)));
            gap += dayGap; if (hasOverlap) overlap++; if (dayGap > 0 || hasOverlap) conflicts++;
        }
        double rest = 0;
        foreach (var employee in employees)
        {
            var working = assigned.Where(s => s.Employee == employee.EmployeeId).Select(s => s.Day).ToHashSet();
            for (var day = 1; day <= days;)
            {
                if (working.Contains(day)) { day++; continue; }
                var length = 0; var blocked = 0;
                while (day <= days && !working.Contains(day)) { var w = Window(schedule, groups, employee.EmployeeId, day++); if (w.To <= w.From) blocked++; length++; }
                var available = length - blocked;
                if (available > 0) rest += available * available + blocked * available + Math.Max(0, length - 4) * .5;
            }
        }
        return [conflicts, gap, hard.Sum(), hard.Sum(x => x*x), soft.Sum(), soft.Sum(x => x*x), rest,
            slots.Count(s => s.Status == SlotStatus.UNFURNISHED), overlap];
    }

    internal static int Compare(double[] a, double[] b)
    {
        for (var i = 0; i < a.Length; i++) if (Math.Abs(a[i] - b[i]) > 1e-7) return a[i] < b[i] ? -1 : 1;
        return 0;
    }

    internal static (double[] Score, ScheduleSlotModel[] Slots, int States) Enumerate(ScheduleModel schedule,
        ScheduleEmployeeModel[] employees, AvailabilityGroupModel[] groups, int activeDays, int bound = 200_000)
    {
        var templates = new[] { schedule.Shift1Time, schedule.Shift2Time }.Select(Range).ToArray();
        var slots = Enumerable.Range(1, DateTime.DaysInMonth(schedule.Year, schedule.Month)).SelectMany(day => templates.Select(t =>
            new ScheduleSlotModel { DayOfMonth = day, SlotNo = 1, FromTime = Time(t.From), ToTime = Time(t.To), Status = SlotStatus.UNFURNISHED })).ToArray();
        double[]? best = null; ScheduleSlotModel[]? witness = null; var states = 0;
        void Visit(int position)
        {
            if (++states > bound) throw new InvalidOperationException("INCONCLUSIVE: independent reference state bound exhausted");
            if (position == activeDays * 2)
            {
                var quality = Score(schedule, employees, groups, slots);
                if (best is null || Compare(quality, best) < 0) { best = quality; witness = System.Text.Json.JsonSerializer.Deserialize<ScheduleSlotModel[]>(System.Text.Json.JsonSerializer.Serialize(slots)); }
                return;
            }
            Visit(position + 1);
            var slot = slots[position]; var day = position / 2 + 1; var template = templates[position % 2];
            foreach (var employee in employees)
            {
                var w = Window(schedule, groups, employee.EmployeeId, day); var from = Math.Max(w.From, template.From); var to = Math.Min(w.To, template.To);
                if (to <= from) continue;
                var own = slots.Take(position).Where(s => s.EmployeeId == employee.EmployeeId).ToArray();
                var worked = own.Sum(s => Range(s.FromTime + "-" + s.ToTime).To - Range(s.FromTime + "-" + s.ToTime).From);
                var remaining = schedule.MaxHoursPerEmpMonth > 0 ? schedule.MaxHoursPerEmpMonth * 60 - worked : int.MaxValue;
                if (remaining <= 0) continue;
                var duration = Math.Min(to - from, remaining);
                foreach (var start in new[] { from, to - duration }.Distinct())
                {
                    var workDays = own.Select(s => s.DayOfMonth).Append(day);
                    var fullDays = own.GroupBy(s => s.DayOfMonth).Where(g => g.Count() == 2).Select(g => g.Key).ToList();
                    if (own.Any(s => s.DayOfMonth == day)) fullDays.Add(day);
                    if (schedule.MaxConsecutiveDays > 0 && Run(workDays) > schedule.MaxConsecutiveDays ||
                        schedule.MaxFullPerMonth > 0 && fullDays.Count > schedule.MaxFullPerMonth ||
                        schedule.MaxConsecutiveFull > 0 && Run(fullDays) > schedule.MaxConsecutiveFull) continue;
                    slot.EmployeeId = employee.EmployeeId; slot.Status = SlotStatus.ASSIGNED; slot.FromTime = Time(start); slot.ToTime = Time(start + duration);
                    Visit(position + 1); slot.EmployeeId = null; slot.Status = SlotStatus.UNFURNISHED; slot.FromTime = Time(template.From); slot.ToTime = Time(template.To);
                }
            }
        }
        Visit(0); return (best!, witness!, states);
    }

    private static string Time(int value) => $"{value / 60:00}:{value % 60:00}";
    private static int Run(IEnumerable<int> dates)
    {
        var longest = 0; var run = 0; var previous = -2;
        foreach (var day in dates.Distinct().Order()) { run = day == previous + 1 ? run + 1 : 1; longest = Math.Max(longest, run); previous = day; }
        return longest;
    }
}
