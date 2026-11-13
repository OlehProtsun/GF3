using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Business operations for key/value binds.
/// </summary>
public interface IBindService : IBaseService<BindModel>
{
    Task<BindModel?> GetAsync(int id, int managerAccountId, CancellationToken ct = default);

    Task<List<BindModel>> GetAllAsync(int managerAccountId, CancellationToken ct = default);

    Task<BindModel> CreateAsync(BindModel entity, int managerAccountId, CancellationToken ct = default);

    Task UpdateAsync(BindModel entity, int managerAccountId, CancellationToken ct = default);

    Task DeleteAsync(int id, int managerAccountId, CancellationToken ct = default);

    /// <summary>
    /// Returns only active binds.
    /// </summary>
    Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default);

    Task<List<BindModel>> GetActiveAsync(int managerAccountId, CancellationToken ct = default);

    /// <summary>
    /// Returns one bind by its normalized key or <see langword="null"/> when it does not exist.
    /// </summary>
    Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a bind using the key as the natural identifier.
    /// </summary>
    Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default);
}
