using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
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
        var slotList = PrepareSlotBatch(scheduleId, slots);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        if (overwrite)
        {
            await DeleteExistingScheduleSlotsAsync(scheduleId, ct).ConfigureAwait(false);
        }

        if (slotList.Count > 0)
        {
            await _set.AddRangeAsync(slotList, ct).ConfigureAwait(false);
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
}
