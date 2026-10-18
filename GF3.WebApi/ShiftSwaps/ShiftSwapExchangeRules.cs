using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using WebApi.Contracts.ShiftSwaps;
using static WebApi.ShiftSwaps.ShiftSwapRules;

namespace WebApi.ShiftSwaps;

internal static class ShiftSwapExchangeRules
{
    internal sealed record ExchangeInput(
        int OwnerId, int TargetId, ScheduleSlotModel Source, ShiftSwapPeriod SourcePeriod,
        ScheduleSlotModel Return, ShiftSwapPeriod ReturnPeriod,
        IReadOnlyList<ScheduleSlotModel> Assignments, IReadOnlyList<ShiftSwapRequestModel> OpenOffers,
        bool SourceLocked = false, bool ReturnLocked = false, int? RequestId = null,
        ShiftSwapReturnCandidateDto? ExpectedSource = null, ShiftSwapReturnCandidateDto? ExpectedReturn = null);

    internal static int DurationMinutes(ShiftSwapPeriod period)
        => ParseTimeMinutes(period.ToTime)!.Value - ParseTimeMinutes(period.FromTime)!.Value;

    internal static string? GetExchangeConflictReason(ExchangeInput input)
    {
        var owner = input.OwnerId; var target = input.TargetId;
        var source = input.Source; var give = input.SourcePeriod;
        var back = input.Return; var receive = input.ReturnPeriod;
        var assignments = input.Assignments; var offers = input.OpenOffers;
        var sourceLocked = input.SourceLocked; var returnLocked = input.ReturnLocked; var requestId = input.RequestId;
        if (owner <= 0 || target <= 0 || owner == target) return "Choose another employee for the exchange.";
        if (source.Id == back.Id) return "Choose a different return shift.";
        if ((input.ExpectedSource is { } expectedSource && !Matches(expectedSource, source)) ||
            (input.ExpectedReturn is { } expectedReturn && !Matches(expectedReturn, back)))
            return "This shift changed after the exchange was offered. Refresh and try again.";
        if (source.EmployeeId != owner || back.EmployeeId != target ||
            source.Status != SlotStatus.ASSIGNED || back.Status != SlotStatus.ASSIGNED)
            return "This shift is no longer assigned to the selected employee.";
        if (source.Schedule.ContainerId != back.Schedule.ContainerId)
            return "The return shift must belong to the same container.";
        if (source.Schedule.PublicationStatus != SchedulePublicationStatus.Public ||
            back.Schedule.PublicationStatus != SchedulePublicationStatus.Public ||
            !source.Schedule.AllowSwap || !back.Schedule.AllowSwap) return "Shift unavailable.";
        if (sourceLocked || returnLocked) return "Schedule is locked while a manager is editing it.";
        try
        {
            NormalizePeriod(give.FromTime, give.ToTime, "FromTime", "ToTime");
            NormalizePeriod(receive.FromTime, receive.ToTime, "ReturnFromTime", "ReturnToTime");
            EnsurePeriodWithinSlot(source, give);
            EnsurePeriodWithinSlot(back, receive);
            _ = Date(source);
            _ = Date(back);
        }
        catch (ValidationException exception) { return exception.Message; }
        catch (ArgumentOutOfRangeException) { return "The selected shift has an invalid date."; }
        if (DurationMinutes(give) != DurationMinutes(receive)) return "Exchange periods must have exactly equal duration.";
        if (source.ScheduleId == back.ScheduleId && Date(source) == Date(back) && give == receive)
            return "Choose a different shift period for the exchange.";
        if (offers.Any(offer => offer.Id != requestId && offer.Status == ShiftSwapStatus.Open &&
            (offer.ScheduleSlotId == source.Id || offer.ScheduleSlotId == back.Id ||
             offer.ReturnScheduleSlotId == source.Id || offer.ReturnScheduleSlotId == back.Id)))
            return "This shift already has an open swap offer. Refresh and choose another shift.";
        var remainingOwner = Project(assignments, owner, source, give, null, null);
        var remainingTarget = Project(assignments, target, back, receive, null, null);
        if (Overlaps(remainingOwner, back, receive) || Overlaps(remainingTarget, source, give))
            return "Already working at this time.";
        return null;
    }

    internal static IReadOnlyList<ShiftSwapPeriod> FindEligibleReturnWindows(ExchangeInput input, ScheduleSlotModel candidate)
    {
        var duration = DurationMinutes(input.SourcePeriod);
        var start = ParseTimeMinutes(candidate.FromTime);
        var end = ParseTimeMinutes(candidate.ToTime);
        if (start is null || end is null || duration <= 0 || end - start < duration) return [];
        var windows = new List<ShiftSwapPeriod>();
        // Minute precision also supports legacy shifts that are not aligned to the UI's 15-minute steps.
        for (var minute = start.Value; minute + duration <= end; minute++)
        {
            var period = new ShiftSwapPeriod(Time(minute), Time(minute + duration));
            if (GetExchangeConflictReason(input with { Return = candidate, ReturnPeriod = period }) is null)
                windows.Add(period);
        }
        return windows;
    }

    internal static IReadOnlyList<ScheduleSlotModel> ApplyExchangeTransferToSlot(
        ScheduleSlotModel slot, int previousOwnerId, int nextOwnerId, ShiftSwapPeriod transferred,
        IReadOnlyCollection<ScheduleSlotModel> slotUniverse)
        => ApplyAcceptedSwapPeriod(slot, previousOwnerId, nextOwnerId, transferred, slotUniverse);

    internal static List<ShiftSwapPreviewMonthStatsDto> CalculateProjectedOwnerStats(
        IReadOnlyList<ScheduleSlotModel> assignments, int employeeId,
        ScheduleSlotModel outgoing, ShiftSwapPeriod outgoingPeriod,
        ScheduleSlotModel? incoming = null, ShiftSwapPeriod? incomingPeriod = null,
        IReadOnlyList<(int Year, int Month)>? extraMonths = null)
    {
        var before = assignments.Where(slot => slot.EmployeeId == employeeId && slot.Status == SlotStatus.ASSIGNED)
            .Select(slot => new Interval(Date(slot), ParseTimeMinutes(slot.FromTime)!.Value, ParseTimeMinutes(slot.ToTime)!.Value)).ToList();
        var after = Project(assignments, employeeId, outgoing, outgoingPeriod, incoming, incomingPeriod);
        var months = new[] { Date(outgoing), incoming is null ? Date(outgoing) : Date(incoming) }
            .Select(date => (date.Year, date.Month)).Concat(extraMonths ?? []).Distinct().OrderBy(month => month.Year).ThenBy(month => month.Month);
        return months.Select(month =>
        {
            var old = before.Where(item => item.Date.Year == month.Year && item.Date.Month == month.Month).ToList();
            var next = after.Where(item => item.Date.Year == month.Year && item.Date.Month == month.Month).ToList();
            var oldDays = old.Select(item => item.Date).Distinct().Count();
            var newDays = next.Select(item => item.Date).Distinct().Count();
            var count = DateTime.DaysInMonth(month.Year, month.Month);
            return new ShiftSwapPreviewMonthStatsDto(month.Year, month.Month,
                old.Sum(item => item.End - item.Start) / 60d, next.Sum(item => item.End - item.Start) / 60d,
                oldDays, newDays, count - oldDays, count - newDays);
        }).ToList();
    }

    internal static void PopulateProjectedStats(ShiftSwapDto dto, ShiftSwapRequestModel swap, int employeeId,
        ScheduleSlotModel? back, IReadOnlyList<ScheduleSlotModel> assignments)
    {
        if (back is not null && swap.ScheduleSlot is { } offered && (employeeId == swap.FromEmployeeId || employeeId == swap.TargetEmployeeId))
        {
            var owner = employeeId == swap.FromEmployeeId;
            var outgoing = owner ? offered : back;
            var incoming = owner ? back : offered;
            var outgoingPeriod = owner ? ShiftSwapRules.GetSwapPeriod(swap, offered) : new ShiftSwapPeriod(swap.ReturnFromTime!, swap.ReturnToTime!);
            var incomingPeriod = owner ? new ShiftSwapPeriod(swap.ReturnFromTime!, swap.ReturnToTime!) : ShiftSwapRules.GetSwapPeriod(swap, offered);
            var originalMonths = new[] { (Year: dto.Year, Month: dto.Month),
                (Year: dto.ReturnYear ?? dto.Year, Month: dto.ReturnMonth ?? dto.Month) };
            var stats = swap.Status == ShiftSwapStatus.Accepted
                ? ShiftSwapExchangeRules.CalculateProjectedOwnerStats(assignments, employeeId, incoming, incomingPeriod, outgoing, outgoingPeriod, originalMonths)
                    .Select(s => new ShiftSwapPreviewMonthStatsDto(s.Year, s.Month, s.HoursAfter, s.HoursBefore, s.WorkDaysAfter, s.WorkDaysBefore, s.FreeDaysAfter, s.FreeDaysBefore)).ToList()
                : ShiftSwapExchangeRules.CalculateProjectedOwnerStats(assignments, employeeId, outgoing, outgoingPeriod,
                    incoming, incomingPeriod, originalMonths);
            if (swap.Status == ShiftSwapStatus.Cancelled)
                stats = stats.Select(s => s with { HoursAfter = s.HoursBefore, WorkDaysAfter = s.WorkDaysBefore, FreeDaysAfter = s.FreeDaysBefore }).ToList();
            dto.CurrentEmployeeProjectedMonths = stats;
            var month = stats.First(s => s.Year == dto.Year && s.Month == dto.Month);
            dto.CurrentEmployeeHoursBefore = month.HoursBefore; dto.CurrentEmployeeHoursAfter = month.HoursAfter;
            dto.CurrentEmployeeWorkDaysBefore = month.WorkDaysBefore; dto.CurrentEmployeeWorkDaysAfter = month.WorkDaysAfter;
            dto.CurrentEmployeeFreeDaysBefore = month.FreeDaysBefore; dto.CurrentEmployeeFreeDaysAfter = month.FreeDaysAfter;
        }
    }

    private sealed record Interval(DateOnly Date, int Start, int End);
    private static bool Matches(ShiftSwapReturnCandidateDto expected, ScheduleSlotModel slot)
        => expected.SlotId == slot.Id && expected.ScheduleId == slot.ScheduleId && expected.Year == slot.Schedule.Year &&
            expected.Month == slot.Schedule.Month && expected.DayOfMonth == slot.DayOfMonth &&
            expected.FromTime == slot.FromTime && expected.ToTime == slot.ToTime;
    private static DateOnly Date(ScheduleSlotModel slot) => new(slot.Schedule.Year, slot.Schedule.Month, slot.DayOfMonth);
    private static string Time(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";
    private static bool Overlaps(IEnumerable<Interval> remaining, ScheduleSlotModel incoming, ShiftSwapPeriod period)
        => remaining.Any(item => item.Date == Date(incoming) && item.Start < ParseTimeMinutes(period.ToTime) &&
            ParseTimeMinutes(period.FromTime) < item.End);

    private static List<Interval> Project(IReadOnlyList<ScheduleSlotModel> assignments, int employeeId,
        ScheduleSlotModel outgoing, ShiftSwapPeriod outgoingPeriod, ScheduleSlotModel? incoming, ShiftSwapPeriod? incomingPeriod)
    {
        var result = new List<Interval>();
        foreach (var slot in assignments.Where(slot => slot.EmployeeId == employeeId && slot.Status == SlotStatus.ASSIGNED))
        {
            var start = ParseTimeMinutes(slot.FromTime)!.Value;
            var end = ParseTimeMinutes(slot.ToTime)!.Value;
            if (slot.Id != outgoing.Id)
                result.Add(new(Date(slot), start, end));
            else
            {
                var from = ParseTimeMinutes(outgoingPeriod.FromTime)!.Value;
                var to = ParseTimeMinutes(outgoingPeriod.ToTime)!.Value;
                if (start < from) result.Add(new(Date(slot), start, from));
                if (to < end) result.Add(new(Date(slot), to, end));
            }
        }
        if (incoming is not null && incomingPeriod is { } period)
            result.Add(new(Date(incoming), ParseTimeMinutes(period.FromTime)!.Value, ParseTimeMinutes(period.ToTime)!.Value));
        return result;
    }
}
