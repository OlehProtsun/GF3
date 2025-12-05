using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class ShopRepository : GenericRepository<ShopModel>, IShopRepository
    {
        public ShopRepository(AppDbContext db) : base(db) { }

        public async Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(value))
                return await _set.AsNoTracking().ToListAsync(ct);

            value = value.ToLower().Trim();

            return await _set
                .AsNoTracking()
                .Where(shop =>
                    shop.Name.ToLower().Contains(value) ||
                    (shop.Description != null && shop.Description.ToLower().Contains(value)))
                .ToListAsync(ct);
        }
    }
}