using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using static WebApi.ShiftSwaps.ShiftSwapRules;

namespace WebApi.ShiftSwaps;

internal readonly record struct OriginalSlotSnapshot(int SlotId, int ScheduleId, int DayOfMonth,
    int OwnerEmployeeId, string FromTime, string ToTime);

internal static class ShiftSwapOpenOfferRebinder
{
    internal static OriginalSlotSnapshot Capture(ScheduleSlotModel slot)
        => new(slot.Id, slot.ScheduleId, slot.DayOfMonth, slot.EmployeeId!.Value, slot.FromTime, slot.ToTime);

    internal static async Task RebindAfterSplitAsync(AppDbContext db, OriginalSlotSnapshot before,
        ShiftSwapPeriod acceptedInterval, IReadOnlyList<ScheduleSlotModel> persistedRemainders,
        int completedRequestId, CancellationToken ct)
    {
        var offers = await db.ShiftSwapRequests.Where(offer => offer.Status == ShiftSwapStatus.Open &&
            offer.Id != completedRequestId && (offer.ScheduleSlotId == before.SlotId ||
                offer.ReturnScheduleSlotId == before.SlotId)).ToDictionaryAsync(offer => offer.Id, ct);
        if (offers.Count == 0)
            return;

        var original = new ScheduleSlotModel { Id = before.SlotId, FromTime = before.FromTime, ToTime = before.ToTime };
        var offerIds = offers.Keys.ToArray();
        var approvals = await db.ContractShiftApprovalRequests.Where(approval => approval.Status == "pending" &&
            approval.ShiftSwapRequestId.HasValue && offerIds.Contains(approval.ShiftSwapRequestId.Value)).ToListAsync(ct);
        var approvalsByOffer = approvals.ToLookup(approval => approval.ShiftSwapRequestId!.Value);
        foreach (var reservation in ShiftSwapReservationRules.ForSlot(offers.Values, original))
        {
            if (ShiftSwapReservationRules.Overlaps(reservation.Period, acceptedInterval))
                throw Changed();

            var matching = persistedRemainders.Where(slot => slot.Id > 0 && slot.ScheduleId == before.ScheduleId &&
                slot.DayOfMonth == before.DayOfMonth && slot.EmployeeId == before.OwnerEmployeeId &&
                string.CompareOrdinal(slot.FromTime, reservation.Period.FromTime) <= 0 &&
                string.CompareOrdinal(slot.ToTime, reservation.Period.ToTime) >= 0).ToList();
            if (matching.Count != 1)
                throw Changed();

            var remainder = matching[0];
            var offer = offers[reservation.RequestId];
            if (reservation.IsReturnSide)
            {
                offer.ReturnScheduleSlotId = remainder.Id;
                foreach (var approval in approvalsByOffer[offer.Id].Where(approval => approval.ReturnScheduleSlotId == before.SlotId))
                    approval.ReturnScheduleSlotId = remainder.Id;
            }
            else
            {
                offer.ScheduleSlot = remainder;
                offer.ScheduleSlotId = remainder.Id;
                foreach (var approval in approvalsByOffer[offer.Id].Where(approval => approval.ScheduleSlotId == before.SlotId))
                    approval.ScheduleSlotId = remainder.Id;
            }
        }
    }

    private static ValidationException Changed()
        => new("This swap changed during another request. Refresh and try again.");
}
