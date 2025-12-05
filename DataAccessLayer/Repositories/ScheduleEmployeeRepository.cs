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
    public class ScheduleEmployeeRepository : GenericRepository<ScheduleEmployeeModel>, IScheduleEmployeeRepository
    {
        public ScheduleEmployeeRepository(AppDbContext db) : base(db) { }

        public async Task<List<ScheduleEmployeeModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(se => se.Employee)
                .Where(se => se.ScheduleId == scheduleId)
                .ToListAsync(ct);
        }
    }
}
