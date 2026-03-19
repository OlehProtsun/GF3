using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class SchedulePresetRepository : GenericRepository<SchedulePresetModel>, ISchedulePresetRepository
    {
        public SchedulePresetRepository(AppDbContext db) : base(db) { }

        public override async Task<List<SchedulePresetModel>> GetAllAsync(CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(x => x.Employees)
                .OrderBy(x => x.Name)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public async Task<List<SchedulePresetModel>> GetByContainerAsync(int containerId, CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(x => x.Employees)
                .Where(x => x.ContainerId == containerId)
                .OrderBy(x => x.Name)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public async Task<bool> ExistsByNameAsync(int containerId, string name, int? excludeId = null, CancellationToken ct = default)
        {
            var normalizedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return false;
            }

            return await _set
                .AsNoTracking()
                .AnyAsync(
                    x => x.ContainerId == containerId
                        && x.Name == normalizedName
                        && (!excludeId.HasValue || x.Id != excludeId.Value),
                    ct)
                .ConfigureAwait(false);
        }
    }
}
