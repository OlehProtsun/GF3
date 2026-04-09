using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Shops;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// API-facing shop facade that translates between HTTP-layer contracts and domain services.
/// </summary>
public interface IShopFacade
{
    /// <summary>
    /// Returns all shops in facade DTO form.
    /// </summary>
    Task<IReadOnlyList<ShopDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches shops using the frontend search term.
    /// </summary>
    Task<IReadOnlyList<ShopDto>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Returns one shop by id or <see langword="null"/> when it does not exist.
    /// </summary>
    Task<ShopDto?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new shop from the supplied save request.
    /// </summary>
    Task<ShopDto> CreateAsync(SaveShopRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing shop from the supplied save request.
    /// </summary>
    Task UpdateAsync(SaveShopRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a shop and throws on failure.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Attempts to delete a shop without throwing for expected business-rule failures.
    /// </summary>
    Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default);
}
