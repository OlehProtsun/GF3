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
    public class ScheduleRepository : GenericRepository<ScheduleModel>, IScheduleRepository
    {
        public ScheduleRepository(AppDbContext db) : base(db) { }

        public override async Task<List<ScheduleModel>> GetAllAsync(CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(s => s.Container)
                .Include(s => s.Shop)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public async Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default)
        {
            var query = _set
                .AsNoTracking()
                .Include(s => s.Container)
                .Include(s => s.Shop)
                .Where(s => s.ContainerId == containerId);

            if (!string.IsNullOrWhiteSpace(value))
            {
                value = value.Trim().ToLower();
                bool hasInt = int.TryParse(value, out var intVal);

                query = query.Where(s =>
                    s.Name.ToLower().Contains(value) ||
                    (s.Note != null && s.Note.ToLower().Contains(value)) ||
                    (hasInt && (s.Year == intVal || s.Month == intVal)));
            }

            return await query.ToListAsync(ct).ConfigureAwait(false);
        }

        public async Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(value))
                return await GetAllAsync(ct).ConfigureAwait(false);

            value = value.Trim().ToLower();
            bool hasInt = int.TryParse(value, out var intVal);

            return await _set
                .AsNoTracking()
                .Include(s => s.Container)
                .Include(s => s.Shop)
                .Where(s =>
                    s.Name.ToLower().Contains(value) ||
                    (s.Note != null && s.Note.ToLower().Contains(value)) ||
                    s.Container.Name.ToLower().Contains(value) ||
                    s.Shop.Name.ToLower().Contains(value) ||
                    (hasInt && (s.Year == intVal || s.Month == intVal)))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public async Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(s => s.Container)
                .Include(s => s.Shop)
                .Include(s => s.Slots)
                .Include(s => s.Employees)
                .ThenInclude(e => e.Employee)
                .FirstOrDefaultAsync(s => s.Id == id, ct)
                .ConfigureAwait(false);
        }
    }
}
