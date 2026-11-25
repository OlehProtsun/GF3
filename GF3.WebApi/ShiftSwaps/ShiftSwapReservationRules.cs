using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using static WebApi.ShiftSwaps.ShiftSwapRules;

namespace WebApi.ShiftSwaps;

internal readonly record struct SlotReservation(int RequestId, int SlotId, ShiftSwapPeriod Period, bool IsReturnSide);

internal static class ShiftSwapReservationRules
{
    internal static IReadOnlyList<SlotReservation> ForSlot(IEnumerable<ShiftSwapRequestModel> openOffers,
        ScheduleSlotModel slot, int? excludedRequestId = null)
    {
        var reservations = new List<SlotReservation>();
        foreach (var offer in openOffers)
        {
            if (offer.Status != ShiftSwapStatus.Open || offer.Id == excludedRequestId)
                continue;

            if (offer.ScheduleSlotId == slot.Id)
                reservations.Add(new(offer.Id, slot.Id, GetPeriod(offer, slot, isReturnSide: false), false));
            if (offer.ReturnScheduleSlotId == slot.Id)
                reservations.Add(new(offer.Id, slot.Id, GetPeriod(offer, slot, isReturnSide: true), true));
        }

        return reservations;
    }

    internal static bool Overlaps(ShiftSwapPeriod left, ShiftSwapPeriod right)
        => TimesOverlap(left.FromTime, left.ToTime, right.FromTime, right.ToTime);

    internal static bool HasConflict(IEnumerable<ShiftSwapRequestModel> openOffers, ScheduleSlotModel slot,
        ShiftSwapPeriod candidate, int? excludedRequestId = null)
        => ForSlot(openOffers, slot, excludedRequestId).Any(reservation => Overlaps(reservation.Period, candidate));

    internal static IReadOnlyList<ShiftSwapPeriod> GetFreeWindows(ScheduleSlotModel slot,
        IEnumerable<SlotReservation> reservations)
    {
        if (ParseTimeMinutes(slot.FromTime) is not int start || ParseTimeMinutes(slot.ToTime) is not int end || start >= end)
            return [];

        var windows = new List<ShiftSwapPeriod>();
        var cursor = slot.FromTime;
        var fullSlot = new ShiftSwapPeriod(slot.FromTime, slot.ToTime);
        foreach (var period in reservations.Where(r => r.SlotId == slot.Id && Overlaps(r.Period, fullSlot))
                     .Select(r => r.Period).OrderBy(p => p.FromTime, StringComparer.Ordinal))
        {
            if (string.CompareOrdinal(cursor, period.FromTime) < 0)
                windows.Add(new(cursor, period.FromTime));
            if (string.CompareOrdinal(period.ToTime, cursor) > 0)
                cursor = period.ToTime;
        }

        if (string.CompareOrdinal(cursor, slot.ToTime) < 0)
            windows.Add(new(cursor, slot.ToTime));
        return windows;
    }

    private static ShiftSwapPeriod GetPeriod(ShiftSwapRequestModel offer, ScheduleSlotModel slot, bool isReturnSide)
    {
        var fullSlot = new ShiftSwapPeriod(slot.FromTime, slot.ToTime);
        var from = isReturnSide ? offer.ReturnFromTime : offer.OfferedFromTime;
        var to = isReturnSide ? offer.ReturnToTime : offer.OfferedToTime;
        if (offer.IsManagerCreated || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return fullSlot;

        try
        {
            var period = isReturnSide
                ? NormalizePeriod(from, to, nameof(offer.ReturnFromTime), nameof(offer.ReturnToTime))
                : GetSwapPeriod(offer, slot);
            EnsurePeriodWithinSlot(slot, period);
            return period;
        }
        catch (ValidationException)
        {
            return fullSlot;
        }
    }
}
