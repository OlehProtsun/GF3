using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace DataAccessLayer.Repositories
{
    public class ScheduleSlotRepository : GenericRepository<ScheduleSlotModel>, IScheduleSlotRepository
    {
        public ScheduleSlotRepository(AppDbContext db) : base(db) { }

        public async Task<List<ScheduleSlotModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(ss => ss.Employee)
                .Where(ss => ss.ScheduleId == scheduleId)
                .OrderBy(ss => ss.DayOfMonth)
                .ThenBy(ss => ss.SlotNo)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }


        public async Task<int> ReplaceForScheduleAsync(int scheduleId, IEnumerable<ScheduleSlotModel> slots, bool overwrite, CancellationToken ct = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            if (overwrite)
            {
                await _set
                    .Where(ss => ss.ScheduleId == scheduleId)
                    .ExecuteDeleteAsync(ct)
                    .ConfigureAwait(false);
            }

            var slotList = slots?.ToList() ?? new List<ScheduleSlotModel>();
            foreach (var slot in slotList)
            {
                slot.Id = 0;
                slot.ScheduleId = scheduleId;
            }

            if (slotList.Count > 0)
            {
                await _set.AddRangeAsync(slotList, ct).ConfigureAwait(false);
            }

            var written = await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            _db.ChangeTracker.Clear();
            return written;
        }
    }
}
