using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using static WebApi.ShiftSwaps.ShiftSwapRules;

namespace WebApi.ShiftSwaps;

internal sealed class ContractShiftConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

internal static class EmploymentSwapRules
{
    internal const string EightHourReason = "Employment contract requires a whole shift of exactly eight hours.";

    internal static bool IsWholeEightHourShift(ScheduleSlotModel slot, ShiftSwapPeriod period)
        => GetTimeRangeDurationHours(slot.FromTime, slot.ToTime) == 8 &&
            GetTimeRangeDurationHours(period.FromTime, period.ToTime) == 8 &&
            period.FromTime == slot.FromTime && period.ToTime == slot.ToTime;

    internal static void ValidateWholeEightHourShift(ScheduleSlotModel slot, ShiftSwapPeriod period, int participantId)
    {
        EnsurePeriodWithinSlot(slot, period);
        if (participantId <= 0 || !IsWholeEightHourShift(slot, period))
            throw new ValidationException(EightHourReason);
    }

    internal sealed record MinimumCheck(MinimumHoursResult Minimum, int ProjectedMinutes)
    {
        public bool RequiresException => !Minimum.IsConfigured || ProjectedMinutes < Minimum.MinimumMinutes;
    }

    internal static async Task<MinimumCheck?> CheckOutgoingMinimumAsync(int ownerId, ScheduleModel outgoingSchedule,
        ShiftSwapPeriod outgoing, ScheduleModel? incomingSchedule, ShiftSwapPeriod? incoming,
        IEmploymentContractPolicyService policy, CancellationToken ct)
    {
        if (!await policy.IsRuleActiveAsync(ownerId, EmploymentContractRule.RequireBelowMinimumGiveApproval, ct)) return null;
        var outgoingMinutes = checked((int)Math.Round(GetTimeRangeDurationHours(outgoing.FromTime, outgoing.ToTime) * 60));
        if (outgoingMinutes <= 0) throw new ValidationException("Invalid shift period.");
        var delta = -outgoingMinutes;
        if (incomingSchedule is not null && incoming is { } back && incomingSchedule.ShopId == outgoingSchedule.ShopId &&
            incomingSchedule.Year == outgoingSchedule.Year && incomingSchedule.Month == outgoingSchedule.Month)
        {
            var incomingMinutes = checked((int)Math.Round(GetTimeRangeDurationHours(back.FromTime, back.ToTime) * 60));
            if (incomingMinutes <= 0) throw new ValidationException("Invalid shift period.");
            delta += incomingMinutes;
        }
        if (delta >= 0) return null;
        var minimum = await policy.GetShopMonthMinimumAsync(ownerId, outgoingSchedule.ShopId,
            outgoingSchedule.Year, outgoingSchedule.Month, ct);
        return new(minimum, checked(minimum.CurrentMinutes + delta));
    }

    internal static ContractShiftConflictException GiveApprovalRequired()
        => new("CONTRACT_GIVE_APPROVAL_REQUIRED", "Request a manager exception before giving this shift. Set MinHoursMonth for this graph/shop if no minimum is configured.");
    internal static ContractShiftConflictException ClaimApprovalRequired()
        => new("CONTRACT_CLAIM_APPROVAL_REQUIRED", "Request manager approval to accept this shift.");
}
