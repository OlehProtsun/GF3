using System.Text.Json;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ShiftSwaps;

namespace WebApi.ShiftSwaps;

internal static class ShiftSwapArchive
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal sealed record ExchangeReservationSnapshot(ShiftSwapReturnCandidateDto Source, ShiftSwapReturnCandidateDto Return);

    internal static bool IsArchived(ShiftSwapRequestModel swap)
        => swap.Status != ShiftSwapStatus.Open && swap.ScheduleSlotId is null && swap.ArchivedViewsJson is not null;

    internal static string CaptureReservation(ScheduleSlotModel source, ScheduleSlotModel back)
    {
        static ShiftSwapReturnCandidateDto Summary(ScheduleSlotModel slot) => new()
        {
            ScheduleId = slot.ScheduleId, SlotId = slot.Id, ScheduleName = slot.Schedule.Name,
            ShopName = slot.Schedule.Shop?.Name ?? string.Empty, Year = slot.Schedule.Year, Month = slot.Schedule.Month,
            DayOfMonth = slot.DayOfMonth, FromTime = slot.FromTime, ToTime = slot.ToTime,
        };
        return JsonSerializer.Serialize(new ExchangeReservationSnapshot(Summary(source), Summary(back)), JsonOptions);
    }

    internal static ExchangeReservationSnapshot? ReadReservation(ShiftSwapRequestModel swap)
    {
        if (swap.RequestKind != ShiftSwapRequestKind.Exchange || swap.ArchivedViewsJson is null || IsArchived(swap)) return null;
        try
        {
            var snapshot = JsonSerializer.Deserialize<ExchangeReservationSnapshot>(swap.ArchivedViewsJson, JsonOptions);
            return snapshot is { Source: not null, Return: not null } ? snapshot : null;
        }
        catch (JsonException) { return null; }
    }

    public static ShiftSwapDto ReadView(string json, int employeeId)
    {
        var views = JsonSerializer.Deserialize<Dictionary<int, ShiftSwapDto>>(json, JsonOptions)
            ?? throw new InvalidOperationException("The archived swap is invalid.");
        return views.TryGetValue(employeeId, out var view) ? view : views[0];
    }

    // Called inside the completion or checkout transaction, before slot references can disappear.
    public static async Task PreserveCompletedAsync(AppDbContext db, int scheduleId, CancellationToken ct, int? requestId = null)
    {
        var swaps = await db.ShiftSwapRequests
            .Include(swap => swap.Schedule).ThenInclude(schedule => schedule.Container)
            .Include(swap => swap.Schedule).ThenInclude(schedule => schedule.Shop)
            .Include(swap => swap.Schedule).ThenInclude(schedule => schedule.Slots)
            .Include(swap => swap.ScheduleSlot)
            .Include(swap => swap.FromEmployee)
            .Include(swap => swap.TargetEmployee)
            .Include(swap => swap.AcceptedByEmployee)
            .Where(swap => (swap.ScheduleId == scheduleId || swap.ReturnScheduleId == scheduleId) && swap.Status != ShiftSwapStatus.Open && swap.ScheduleSlotId != null)
            .Where(swap => !requestId.HasValue || swap.Id == requestId.Value)
            .ToListAsync(ct).ConfigureAwait(false);
        if (swaps.Count == 0) return;

        var employeeIds = swaps.SelectMany(swap => new[] { swap.FromEmployeeId, swap.TargetEmployeeId, swap.AcceptedByEmployeeId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var periods = swaps.Select(s => s.Schedule.Year * 100 + s.Schedule.Month).ToHashSet();
        foreach (var swap in swaps)
            if (ReadReservation(swap) is { } original)
            {
                periods.Add(original.Source.Year * 100 + original.Source.Month);
                periods.Add(original.Return.Year * 100 + original.Return.Month);
            }
        var returnIds = swaps.Where(s => s.ReturnScheduleId.HasValue).Select(s => s.ReturnScheduleId!.Value).ToArray();
        var returns = await db.ScheduleSlots.AsNoTracking().Include(s => s.Schedule).ThenInclude(s => s.Shop)
            .Where(s => returnIds.Contains(s.ScheduleId)).ToListAsync(ct);
        foreach (var slot in returns) periods.Add(slot.Schedule.Year * 100 + slot.Schedule.Month);
        var periodKeys = periods.ToArray();
        var ids = swaps.Select(s => s.Id).ToArray();
        var histories = await db.ShiftSwapHistories.AsNoTracking().Where(h => ids.Contains(h.SourceShiftSwapRequestId)).ToListAsync(ct);
        var monthSlots = await db.ScheduleSlots.AsNoTracking().Include(slot => slot.Schedule)
            .Where(slot => slot.EmployeeId.HasValue && employeeIds.Contains(slot.EmployeeId.Value) &&
                periodKeys.Contains(slot.Schedule.Year * 100 + slot.Schedule.Month) &&
                slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public)
            .ToListAsync(ct).ConfigureAwait(false);
        foreach (var swap in swaps)
        {
            var views = new Dictionary<int, ShiftSwapDto> { [0] = ShiftSwapRules.ToDto(swap, 0, false, []) };
            foreach (var id in new[] { swap.FromEmployeeId, swap.TargetEmployeeId, swap.AcceptedByEmployeeId }.Where(id => id.HasValue).Select(id => id!.Value).Distinct())
                views[id] = ShiftSwapRules.ToDto(swap, id, false, monthSlots.Where(slot => slot.EmployeeId == id &&
                    slot.Schedule.Year == swap.Schedule.Year && slot.Schedule.Month == swap.Schedule.Month).ToList());
            if (swap.RequestKind == ShiftSwapRequestKind.Exchange)
                foreach (var (employeeId, view) in views)
                {
                    ShiftSwapRules.PopulateReturnSummary(view, swap, returns.FirstOrDefault(s => s.Id == swap.ReturnScheduleSlotId),
                        histories.FirstOrDefault(h => h.SourceShiftSwapRequestId == swap.Id));
                    ShiftSwapExchangeRules.PopulateProjectedStats(view, swap, employeeId, returns.FirstOrDefault(s => s.Id == swap.ReturnScheduleSlotId), monthSlots);
                }
            swap.ArchivedViewsJson = JsonSerializer.Serialize(views, JsonOptions);
            swap.ScheduleSlotId = null;
            swap.ScheduleSlot = null;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
