using System.Globalization;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Schedule;

/// <summary>
/// Converts assignments saved in other schedules into a conservative availability window.
/// The selected free segment maximizes target-shift coverage and can never overlap saved work.
/// </summary>
internal static class ExternalScheduleAvailabilityBuilder
{
    private readonly record struct MinuteInterval(int Start, int End)
    {
        public int Duration => Math.Max(0, End - Start);
    }

    public static AvailabilityGroupModel? Build(
        ScheduleModel schedule,
        IEnumerable<ScheduleSlotModel> savedSlots,
        IEnumerable<int> employeeIds)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(savedSlots);
        ArgumentNullException.ThrowIfNull(employeeIds);

        var targetShifts = ParseTargetShifts(schedule);
        var employeeIdSet = employeeIds.Where(id => id > 0).ToHashSet();
        if (targetShifts.Count == 0 || employeeIdSet.Count == 0)
        {
            return null;
        }

        var occupiedByEmployeeDay = savedSlots
            .Where(slot => slot.EmployeeId is int employeeId && employeeIdSet.Contains(employeeId))
            .Select(slot => new
            {
                EmployeeId = slot.EmployeeId!.Value,
                slot.DayOfMonth,
                Interval = TryParseTime(slot.FromTime, out var start) && TryParseTime(slot.ToTime, out var end)
                    ? new MinuteInterval(start, end)
                    : new MinuteInterval(0, 0),
            })
            .Where(item =>
                item.DayOfMonth is >= 1 and <= 31 &&
                item.Interval.End > item.Interval.Start &&
                targetShifts.Any(shift => Overlaps(item.Interval, shift)))
            .GroupBy(item => (item.EmployeeId, item.DayOfMonth));

        var members = new List<AvailabilityGroupMemberModel>();
        foreach (var employeeDay in occupiedByEmployeeDay)
        {
            var constraint = BuildConstraint(targetShifts, employeeDay.Select(item => item.Interval));
            if (constraint is null)
            {
                continue;
            }

            var member = members.FirstOrDefault(item => item.EmployeeId == employeeDay.Key.EmployeeId);
            if (member is null)
            {
                member = new AvailabilityGroupMemberModel
                {
                    EmployeeId = employeeDay.Key.EmployeeId,
                    DisplayOrder = members.Count,
                };
                members.Add(member);
            }

            member.Days.Add(new AvailabilityGroupDayModel
            {
                DayOfMonth = employeeDay.Key.DayOfMonth,
                Kind = constraint.Value.Duration > 0 ? AvailabilityKind.INT : AvailabilityKind.NONE,
                IntervalStr = constraint.Value.Duration > 0
                    ? $"{FormatTime(constraint.Value.Start)} - {FormatTime(constraint.Value.End)}"
                    : null,
            });
        }

        return members.Count == 0
            ? null
            : new AvailabilityGroupModel
            {
                Name = "Saved schedule commitments",
                Year = schedule.Year,
                Month = schedule.Month,
                Members = members,
            };
    }

    private static MinuteInterval? BuildConstraint(
        IReadOnlyList<MinuteInterval> targetShifts,
        IEnumerable<MinuteInterval> occupiedIntervals)
    {
        var targetStart = targetShifts.Min(shift => shift.Start);
        var targetEnd = targetShifts.Max(shift => shift.End);
        var occupied = MergeIntervals(occupiedIntervals
            .Select(interval => new MinuteInterval(
                Math.Max(targetStart, interval.Start),
                Math.Min(targetEnd, interval.End)))
            .Where(interval => interval.End > interval.Start));

        if (occupied.Count == 0)
        {
            return null;
        }

        var free = new List<MinuteInterval>();
        var cursor = targetStart;
        foreach (var interval in occupied)
        {
            if (interval.Start > cursor)
            {
                free.Add(new MinuteInterval(cursor, interval.Start));
            }
            cursor = Math.Max(cursor, interval.End);
        }
        if (cursor < targetEnd)
        {
            free.Add(new MinuteInterval(cursor, targetEnd));
        }

        return free.Count == 0
            ? new MinuteInterval(0, 0)
            : free
                .OrderByDescending(segment => targetShifts.Sum(shift => OverlapMinutes(segment, shift)))
                .ThenByDescending(segment => segment.Duration)
                .ThenBy(segment => segment.Start)
                .First();
    }

    private static List<MinuteInterval> MergeIntervals(IEnumerable<MinuteInterval> intervals)
    {
        var merged = new List<MinuteInterval>();
        foreach (var interval in intervals.OrderBy(item => item.Start).ThenBy(item => item.End))
        {
            if (merged.Count == 0 || interval.Start > merged[^1].End)
            {
                merged.Add(interval);
                continue;
            }

            var previous = merged[^1];
            merged[^1] = new MinuteInterval(previous.Start, Math.Max(previous.End, interval.End));
        }
        return merged;
    }

    private static List<MinuteInterval> ParseTargetShifts(ScheduleModel schedule)
    {
        var result = new List<MinuteInterval>(2);
        AddShift(schedule.Shift1Time, result);
        AddShift(schedule.Shift2Time, result);
        return result.OrderBy(item => item.Start).ThenBy(item => item.End).ToList();
    }

    private static void AddShift(string? value, ICollection<MinuteInterval> target)
    {
        var parts = (value ?? string.Empty).Replace(" ", string.Empty)
            .Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 &&
            TryParseTime(parts[0], out var start) &&
            TryParseTime(parts[1], out var end) &&
            end > start)
        {
            target.Add(new MinuteInterval(start, end));
        }
    }

    private static bool TryParseTime(string? value, out int minutes)
    {
        minutes = 0;
        if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }
        minutes = (int)parsed.TotalMinutes;
        return minutes is >= 0 and <= 24 * 60;
    }

    private static string FormatTime(int minutes)
        => TimeSpan.FromMinutes(minutes).ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private static bool Overlaps(MinuteInterval left, MinuteInterval right)
        => left.Start < right.End && right.Start < left.End;

    private static int OverlapMinutes(MinuteInterval left, MinuteInterval right)
        => Math.Max(0, Math.Min(left.End, right.End) - Math.Max(left.Start, right.Start));
}
