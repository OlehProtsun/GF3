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
    public class ContainerRepository : GenericRepository<ContainerModel>, IContainerRepository
    {
        public ContainerRepository(AppDbContext db) : base(db) { }

        public override async Task<List<ContainerModel>> GetAllAsync(CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(c => c.Schedules)
                .ToListAsync(ct);
        }

        public async Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(value))
                return await GetAllAsync(ct);

            value = value.Trim().ToLower();
            return await _set
                .AsNoTracking()
                .Include(c => c.Schedules)
                .Where(c =>
                    c.Name.ToLower().Contains(value) ||
                    (c.Note != null && c.Note.ToLower().Contains(value)))
                .ToListAsync(ct);
        }
    }
}
