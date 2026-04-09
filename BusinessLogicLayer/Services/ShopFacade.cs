using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Contracts.Shops;
using BusinessLogicLayer.Services.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Application-facing adapter for shop use cases.
/// The facade keeps transport contracts isolated from the service layer and gives controllers
/// a compact interface that already speaks in DTO/request terms.
/// </summary>
public sealed class ShopFacade : IShopFacade
{
    private readonly IShopService _shopService;

    public ShopFacade(IShopService shopService)
    {
        _shopService = shopService;
    }

    public async Task<IReadOnlyList<ShopDto>> GetAllAsync(CancellationToken ct = default)
        => (await _shopService.GetAllAsync(ct).ConfigureAwait(false)).Select(MapToDto).ToList();

    public async Task<IReadOnlyList<ShopDto>> GetByValueAsync(string value, CancellationToken ct = default)
        => (await _shopService.GetByValueAsync(value, ct).ConfigureAwait(false)).Select(MapToDto).ToList();

    public async Task<ShopDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var model = await _shopService.GetAsync(id, ct).ConfigureAwait(false);
        return model is null ? null : MapToDto(model);
    }

    public async Task<ShopDto> CreateAsync(SaveShopRequest request, CancellationToken ct = default)
    {
        var created = await _shopService.CreateAsync(MapToModel(request), ct).ConfigureAwait(false);
        return MapToDto(created);
    }

    public Task UpdateAsync(SaveShopRequest request, CancellationToken ct = default)
        => _shopService.UpdateAsync(MapToModel(request), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _shopService.DeleteAsync(id, ct);

    public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
        => _shopService.TryDeleteAsync(id, ct);

    private static ShopDto MapToDto(ShopModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Address = model.Address,
        Description = model.Description
    };

    private static ShopModel MapToModel(SaveShopRequest request) => new()
    {
        Id = request.Id,
        Name = request.Name,
        Address = request.Address,
        Description = request.Description
    };
}
