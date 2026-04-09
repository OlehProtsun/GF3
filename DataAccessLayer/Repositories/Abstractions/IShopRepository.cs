using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for shops and shop-specific lookup rules.
/// </summary>
public interface IShopRepository : IBaseRepository<ShopModel>
{
    /// <summary>
    /// Searches shops across the fields exposed in the UI.
    /// </summary>
    Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Returns whether another shop already uses the same normalized name.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Returns whether any saved schedule still references the shop.
    /// </summary>
    Task<bool> HasScheduleReferencesAsync(int shopId, CancellationToken ct = default);

    /// <summary>
    /// Returns whether any schedule preset still references the shop.
    /// </summary>
    Task<bool> HasSchedulePresetReferencesAsync(int shopId, CancellationToken ct = default);
}
