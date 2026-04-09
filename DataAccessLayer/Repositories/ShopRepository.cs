using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Shop-specific queries and dependency checks.
/// The repository keeps UI search behavior and delete-guard lookups in one place so services
/// can focus on orchestration instead of repeating persistence details.
/// </summary>
public class ShopRepository : GenericRepository<ShopModel>, IShopRepository
{
    public ShopRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <summary>
    /// Performs a case-insensitive shop search across the fields exposed in the frontend.
    /// Empty input returns the full detached list so callers do not need a separate branch.
    /// </summary>
    public async Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return await _set.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        }

        var normalized = NormalizeSearchValue(value);

        return await _set
            .AsNoTracking()
            .Where(shop =>
                shop.Name.ToLower().Contains(normalized) ||
                shop.Address.ToLower().Contains(normalized) ||
                (shop.Description != null && shop.Description.ToLower().Contains(normalized)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Checks whether another shop already uses the same normalized name.
    /// </summary>
    public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = NormalizeSearchValue(name);

        return _set.AsNoTracking().AnyAsync(shop =>
            (!excludeId.HasValue || shop.Id != excludeId.Value) &&
            shop.Name.ToLower().Trim() == normalized,
            ct);
    }

    /// <summary>
    /// Returns whether any saved schedule still points to the shop.
    /// </summary>
    public Task<bool> HasScheduleReferencesAsync(int shopId, CancellationToken ct = default)
    {
        return _db.Set<ScheduleModel>()
            .AsNoTracking()
            .AnyAsync(schedule => schedule.ShopId == shopId, ct);
    }

    /// <summary>
    /// Returns whether any saved schedule preset still points to the shop.
    /// </summary>
    public Task<bool> HasSchedulePresetReferencesAsync(int shopId, CancellationToken ct = default)
    {
        return _db.Set<SchedulePresetModel>()
            .AsNoTracking()
            .AnyAsync(schedulePreset => schedulePreset.ShopId == shopId, ct);
    }

    private static string NormalizeSearchValue(string? value)
        => (value ?? string.Empty).Trim().ToLower();
}
