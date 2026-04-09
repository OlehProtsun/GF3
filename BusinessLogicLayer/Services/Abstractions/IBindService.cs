using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Business operations for key/value binds.
/// </summary>
public interface IBindService : IBaseService<BindModel>
{
    /// <summary>
    /// Returns only active binds.
    /// </summary>
    Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns one bind by its normalized key or <see langword="null"/> when it does not exist.
    /// </summary>
    Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a bind using the key as the natural identifier.
    /// </summary>
    Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default);
}
