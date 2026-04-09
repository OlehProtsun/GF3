using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Minimal reusable CRUD service for entities that do not require extra orchestration.
/// Specialized services can inherit from this base and override only the operations where business
/// rules are more complex than a straight repository call.
/// </summary>
public class GenericService<TEntity> : IBaseService<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Exposed to derived services so they can reuse repository access while layering extra rules.
    /// </summary>
    protected IBaseRepository<TEntity> Repository { get; }

    public GenericService(IBaseRepository<TEntity> repository)
    {
        Repository = repository;
    }

    public Task<TEntity?> GetAsync(int id, CancellationToken ct = default) =>
        Repository.GetByIdAsync(id, ct);

    public Task<List<TEntity>> GetAllAsync(CancellationToken ct = default) =>
        Repository.GetAllAsync(ct);

    /// <summary>
    /// Creates and persists the entity immediately. The repository owns the actual save boundary.
    /// </summary>
    public virtual Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default) =>
        Repository.AddAsync(entity, ct);

    public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default) =>
        Repository.UpdateAsync(entity, ct);

    public virtual Task DeleteAsync(int id, CancellationToken ct = default) =>
        Repository.DeleteAsync(id, ct);
}
