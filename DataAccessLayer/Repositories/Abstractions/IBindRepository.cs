using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for key/value binds used by configuration-like backend flows.
/// </summary>
public interface IBindRepository : IBaseRepository<BindModel>
{
    /// <summary>
    /// Returns one bind by its normalized key or <see langword="null"/> when it does not exist.
    /// </summary>
    Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Returns only active binds ordered by key.
    /// </summary>
    Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a bind using the key as the natural identifier.
    /// </summary>
    Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default);
}
