using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Business operations for shops.
/// </summary>
public interface IShopService : IBaseService<ShopModel>
{
    /// <summary>
    /// Searches shops by user-facing fields such as name, address, and description.
    /// </summary>
    Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Attempts to delete a shop without throwing for expected dependency-related failures.
    /// </summary>
    Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default);
}
