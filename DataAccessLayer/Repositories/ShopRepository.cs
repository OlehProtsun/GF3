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
    public class ShopRepository : GenericRepository<ShopModel>, IShopRepository
    {
        public ShopRepository(AppDbContext db) : base(db) { }

        public async Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(value))
                return await _set.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

            value = value.Trim().ToLower();

            return await _set
                .AsNoTracking()
                .Where(s =>
                    s.Name.ToLower().Contains(value) ||
                    s.Address.ToLower().Contains(value) ||
                    (s.Description != null && s.Description.ToLower().Contains(value)))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
        {
            var normalized = (name ?? string.Empty).Trim().ToLower();

            return _set.AsNoTracking().AnyAsync(shop =>
                (excludeId == null || shop.Id != excludeId.Value) &&
                shop.Name.ToLower().Trim() == normalized, ct);
        }

        public Task<bool> HasScheduleReferencesAsync(int shopId, CancellationToken ct = default)
        {
            return _db.Set<ScheduleModel>()
                .AsNoTracking()
                .AnyAsync(s => s.ShopId == shopId, ct);
        }
    }
}
