using BusinessLogicLayer.Contracts.Shops;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IShopFacade
    {
        Task<IReadOnlyList<ShopDto>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<ShopDto>> GetByValueAsync(string value, CancellationToken ct = default);
        Task<ShopDto?> GetAsync(int id, CancellationToken ct = default);

        Task<ShopDto> CreateAsync(SaveShopRequest request, CancellationToken ct = default);
        Task UpdateAsync(SaveShopRequest request, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}