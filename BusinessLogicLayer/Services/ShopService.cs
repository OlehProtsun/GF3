using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class ShopService : GenericService<ShopModel>, IShopService
    {
        private readonly IShopRepository _repo;

        public ShopService(IShopRepository repo) : base(repo)
            => _repo = repo;

        public override Task<ShopModel> CreateAsync(ShopModel entity, CancellationToken ct = default)
        {
            entity.Name = (entity.Name ?? string.Empty).Trim();
            entity.Description = entity.Description?.Trim();
            return base.CreateAsync(entity, ct);
        }

        public Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => _repo.GetByValueAsync(value, ct);
    }
}
