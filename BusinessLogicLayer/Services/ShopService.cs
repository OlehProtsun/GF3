using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class ShopService : GenericService<ShopModel>, IShopService
    {
        private readonly IShopRepository _repo;

        public ShopService(IShopRepository repo) : base(repo)
            => _repo = repo;

        public override async Task<ShopModel> CreateAsync(ShopModel entity, CancellationToken ct = default)
        {
            entity.Name = (entity.Name ?? string.Empty).Trim();
            entity.Address = (entity.Address ?? string.Empty).Trim();
            entity.Description = string.IsNullOrWhiteSpace(entity.Description)
                ? null
                : entity.Description.Trim();

            if (string.IsNullOrWhiteSpace(entity.Name))
                throw new ValidationException("Назва є обов'язковою.");
            if (string.IsNullOrWhiteSpace(entity.Address))
                throw new ValidationException("Адреса є обов'язковою.");
            if (entity.Name.Length > 200 || entity.Address.Length > 200)
                throw new ValidationException("Назва/адреса занадто довгі.");

            return await base.CreateAsync(entity, ct).ConfigureAwait(false);
        }

        public Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => _repo.GetByValueAsync(value, ct);
    }
}
