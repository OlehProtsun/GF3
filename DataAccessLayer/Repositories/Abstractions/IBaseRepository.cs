namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Minimal CRUD contract shared by concrete repositories.
/// Domain-specific repositories are free to extend this surface with richer query methods,
/// but these operations define the common persistence baseline for one aggregate root.
/// </summary>
public interface IBaseRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Returns one entity by its integer primary key or <see langword="null"/> when it does not exist.
    /// </summary>
    Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns all entities of the current type as a detached read model list.
    /// </summary>
    Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Persists a new entity instance and returns the same entity after the save completed.
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Persists modifications of an existing entity instance.
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Deletes an entity by primary key when it exists.
    /// Missing rows are treated as a no-op to keep delete flows idempotent.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
