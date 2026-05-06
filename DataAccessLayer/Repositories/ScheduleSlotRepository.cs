using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for concrete schedule slots.
/// Slot replacement is implemented as one transactional batch because edit flows usually replace
/// the full slot collection for a schedule in one operation.
/// </summary>
public class ScheduleSlotRepository : GenericRepository<ScheduleSlotModel>, IScheduleSlotRepository
{
    public ScheduleSlotRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public async Task<List<ScheduleSlotModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(slot => slot.Employee)
            .Where(slot => slot.ScheduleId == scheduleId)
            .OrderBy(slot => slot.DayOfMonth)
            .ThenBy(slot => slot.SlotNo)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<int> ReplaceForScheduleAsync(int scheduleId, IEnumerable<ScheduleSlotModel> slots, bool overwrite, CancellationToken ct = default)
    {
        var incomingSlots = slots?.ToList() ?? [];

        await using var transaction = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        if (overwrite)
        {
            var protectedSlots = await GetProtectedManagerManualOfferSlotsAsync(scheduleId, ct).ConfigureAwait(false);
            if (protectedSlots.Count > 0)
            {
                incomingSlots = ExcludeProtectedOpenOfferSlots(incomingSlots, protectedSlots);
                var slotList = PrepareSlotBatch(scheduleId, incomingSlots);
                ReserveProtectedSlotNumbers(slotList, protectedSlots);
                await DeleteExistingScheduleSlotsAsync(scheduleId, protectedSlots.Select(slot => slot.Id).ToList(), ct)
                    .ConfigureAwait(false);

                if (slotList.Count > 0)
                {
                    await _set.AddRangeAsync(slotList, ct).ConfigureAwait(false);
                }
            }
            else
            {
                var slotList = PrepareSlotBatch(scheduleId, incomingSlots);
                await DeleteExistingScheduleSlotsAsync(scheduleId, ct).ConfigureAwait(false);

                if (slotList.Count > 0)
                {
                    await _set.AddRangeAsync(slotList, ct).ConfigureAwait(false);
                }
            }
        }
        else
        {
            var slotList = PrepareSlotBatch(scheduleId, incomingSlots);
            if (slotList.Count > 0)
            {
                await _set.AddRangeAsync(slotList, ct).ConfigureAwait(false);
            }
        }

        var written = await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);

        // Batch replacement can leave many tracked entries in the DbContext.
        // Clearing the tracker keeps the context lightweight for subsequent operations.
        _db.ChangeTracker.Clear();
        return written;
    }

    private async Task DeleteExistingScheduleSlotsAsync(int scheduleId, CancellationToken ct)
    {
        await _set
            .Where(slot => slot.ScheduleId == scheduleId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task DeleteExistingScheduleSlotsAsync(int scheduleId, IReadOnlyCollection<int> protectedSlotIds, CancellationToken ct)
    {
        await _set
            .Where(slot => slot.ScheduleId == scheduleId && !protectedSlotIds.Contains(slot.Id))
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<List<ScheduleSlotModel>> GetProtectedManagerManualOfferSlotsAsync(int scheduleId, CancellationToken ct)
    {
        return await _db.ShiftSwapRequests
            .AsNoTracking()
            .Where(request =>
                request.ScheduleId == scheduleId &&
                request.IsManagerCreated &&
                request.Status == ShiftSwapStatus.Open &&
                request.ScheduleSlot.EmployeeId == null)
            .Select(request => new ScheduleSlotModel
            {
                Id = request.ScheduleSlot.Id,
                ScheduleId = request.ScheduleSlot.ScheduleId,
                DayOfMonth = request.ScheduleSlot.DayOfMonth,
                SlotNo = request.ScheduleSlot.SlotNo,
                EmployeeId = request.ScheduleSlot.EmployeeId,
                Status = request.ScheduleSlot.Status,
                FromTime = request.ScheduleSlot.FromTime,
                ToTime = request.ScheduleSlot.ToTime,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static List<ScheduleSlotModel> PrepareSlotBatch(int scheduleId, IEnumerable<ScheduleSlotModel> slots)
    {
        var slotList = slots?.ToList() ?? [];
        foreach (var slot in slotList)
        {
            slot.Id = 0;
            slot.ScheduleId = scheduleId;
        }

        return slotList;
    }

    private static List<ScheduleSlotModel> ExcludeProtectedOpenOfferSlots(
        IEnumerable<ScheduleSlotModel> slots,
        IReadOnlyCollection<ScheduleSlotModel> protectedSlots)
    {
        var protectedSlotIds = protectedSlots.Select(slot => slot.Id).ToHashSet();

        return slots
            .Where(slot => slot.Id <= 0 || !protectedSlotIds.Contains(slot.Id) || slot.EmployeeId is > 0)
            .ToList();
    }

    private static void ReserveProtectedSlotNumbers(List<ScheduleSlotModel> slots, IReadOnlyCollection<ScheduleSlotModel> protectedSlots)
    {
        if (slots.Count == 0 || protectedSlots.Count == 0)
        {
            return;
        }

        var occupiedByTime = protectedSlots
            .GroupBy(slot => new SlotTimeKey(slot.DayOfMonth, slot.FromTime, slot.ToTime))
            .ToDictionary(
                group => group.Key,
                group => group.Select(slot => slot.SlotNo).ToHashSet());

        foreach (var group in slots.GroupBy(slot => new SlotTimeKey(slot.DayOfMonth, slot.FromTime, slot.ToTime)))
        {
            if (!occupiedByTime.TryGetValue(group.Key, out var occupiedSlotNumbers))
            {
                continue;
            }

            foreach (var slot in group.OrderBy(slot => slot.SlotNo).ThenBy(slot => slot.EmployeeId ?? int.MaxValue))
            {
                while (occupiedSlotNumbers.Contains(slot.SlotNo))
                {
                    slot.SlotNo++;
                }

                occupiedSlotNumbers.Add(slot.SlotNo);
            }
        }
    }

    private readonly record struct SlotTimeKey(int DayOfMonth, string FromTime, string ToTime);
}
