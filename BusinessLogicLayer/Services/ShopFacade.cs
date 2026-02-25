using BusinessLogicLayer.Contracts.Shops;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services
{
    public sealed class ShopFacade : IShopFacade
    {
        private readonly IShopService _shopService;

        public ShopFacade(IShopService shopService)
        {
            _shopService = shopService;
        }

        public async Task<IReadOnlyList<ShopDto>> GetAllAsync(CancellationToken ct = default)
            => (await _shopService.GetAllAsync(ct).ConfigureAwait(false)).Select(Map).ToList();

        public async Task<IReadOnlyList<ShopDto>> GetByValueAsync(string value, CancellationToken ct = default)
            => (await _shopService.GetByValueAsync(value, ct).ConfigureAwait(false)).Select(Map).ToList();

        public async Task<ShopDto?> GetAsync(int id, CancellationToken ct = default)
        {
            var model = await _shopService.GetAsync(id, ct).ConfigureAwait(false);
            return model is null ? null : Map(model);
        }

        public async Task<ShopDto> CreateAsync(SaveShopRequest request, CancellationToken ct = default)
        {
            var created = await _shopService.CreateAsync(Map(request), ct).ConfigureAwait(false);
            return Map(created);
        }

        public Task UpdateAsync(SaveShopRequest request, CancellationToken ct = default)
            => _shopService.UpdateAsync(Map(request), ct);

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => _shopService.DeleteAsync(id, ct);

        private static ShopDto Map(ShopModel model) => new()
        {
            Id = model.Id,
            Name = model.Name,
            Address = model.Address,
            Description = model.Description
        };

        private static ShopModel Map(SaveShopRequest request) => new()
        {
            Id = request.Id,
            Name = request.Name,
            Address = request.Address,
            Description = request.Description
        };
    }
}
