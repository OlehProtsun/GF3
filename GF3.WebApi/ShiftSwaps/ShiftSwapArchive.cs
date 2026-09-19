using System.Text.Json;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ShiftSwaps;

namespace WebApi.ShiftSwaps;

internal static class ShiftSwapArchive
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ShiftSwapDto ReadView(string json, int employeeId)
    {
        var views = JsonSerializer.Deserialize<Dictionary<int, ShiftSwapDto>>(json, JsonOptions)
            ?? throw new InvalidOperationException("The archived swap is invalid.");
        return views.TryGetValue(employeeId, out var view) ? view : views[0];
    }

    // Called inside the checkout transaction, before slots are removed.
    public static async Task PreserveCompletedAsync(AppDbContext db, int scheduleId, CancellationToken ct)
    {
        var swaps = await db.ShiftSwapRequests
            .Include(swap => swap.Schedule).ThenInclude(schedule => schedule.Container)
            .Include(swap => swap.Schedule).ThenInclude(schedule => schedule.Shop)
            .Include(swap => swap.Schedule).ThenInclude(schedule => schedule.Slots)
            .Include(swap => swap.ScheduleSlot)
            .Include(swap => swap.FromEmployee)
            .Include(swap => swap.TargetEmployee)
            .Include(swap => swap.AcceptedByEmployee)
            .Where(swap => swap.ScheduleId == scheduleId && swap.Status != ShiftSwapStatus.Open && swap.ArchivedViewsJson == null)
            .ToListAsync(ct).ConfigureAwait(false);
        if (swaps.Count == 0) return;

        var employeeIds = swaps.SelectMany(swap => new[] { swap.FromEmployeeId, swap.AcceptedByEmployeeId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var schedule = swaps[0].Schedule;
        var monthSlots = await db.ScheduleSlots.AsNoTracking().Include(slot => slot.Schedule)
            .Where(slot => slot.EmployeeId.HasValue && employeeIds.Contains(slot.EmployeeId.Value) &&
                slot.Schedule.Year == schedule.Year && slot.Schedule.Month == schedule.Month &&
                slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public)
            .ToListAsync(ct).ConfigureAwait(false);
        foreach (var swap in swaps)
        {
            var views = new Dictionary<int, ShiftSwapDto> { [0] = ShiftSwapRules.ToDto(swap, 0, false, []) };
            foreach (var id in new[] { swap.FromEmployeeId, swap.AcceptedByEmployeeId }.Where(id => id.HasValue).Select(id => id!.Value).Distinct())
                views[id] = ShiftSwapRules.ToDto(swap, id, false, monthSlots.Where(slot => slot.EmployeeId == id).ToList());
            swap.ArchivedViewsJson = JsonSerializer.Serialize(views, JsonOptions);
            swap.ScheduleSlotId = null;
            swap.ScheduleSlot = null;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}