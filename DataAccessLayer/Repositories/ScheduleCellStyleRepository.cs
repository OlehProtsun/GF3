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

        public Task<List<ScheduleCellStyleModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)
            => _db.ScheduleCellStyles
                .AsNoTracking()
                .Where(s => s.ScheduleId == scheduleId)
                .ToListAsync(ct);
    }
}
