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
    public class ScheduleCellStyleRepository : GenericRepository<ScheduleCellStyleModel>, IScheduleCellStyleRepository
    {
        public ScheduleCellStyleRepository(AppDbContext db) : base(db) { }

        public async Task<List<ScheduleCellStyleModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Where(cs => cs.ScheduleId == scheduleId)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
    }
}
