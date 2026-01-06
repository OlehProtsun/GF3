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
    }
}
