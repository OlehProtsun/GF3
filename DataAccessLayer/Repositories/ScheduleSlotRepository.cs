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
    }
}
