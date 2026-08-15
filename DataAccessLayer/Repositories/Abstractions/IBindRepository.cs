using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for key/value binds used by configuration-like backend flows.
/// </summary>
public interface IBindRepository : IBaseRepository<BindModel>
{
    Task<BindModel?> GetByIdAsync(int id, int managerAccountId, CancellationToken ct = default);

    Task<List<BindModel>> GetAllAsync(int managerAccountId, CancellationToken ct = default);

    /// <summary>
    /// Returns one bind by its normalized key or <see langword="null"/> when it does not exist.
    /// </summary>
    Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default);

    Task<BindModel?> GetByKeyAsync(string key, int managerAccountId, CancellationToken ct = default);

    /// <summary>
    /// Returns only active binds ordered by key.
    /// </summary>
    Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default);

    Task<List<BindModel>> GetActiveAsync(int managerAccountId, CancellationToken ct = default);

    Task DeleteAsync(int id, int managerAccountId, CancellationToken ct = default);

    Task<bool> IsColorKeyInUseAsync(int managerAccountId, string key, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a bind using the key as the natural identifier.
    /// </summary>
    Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default);
}
