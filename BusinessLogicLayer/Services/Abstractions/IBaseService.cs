namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Minimal CRUD contract shared by business services that manage one primary entity type.
/// </summary>
public interface IBaseService<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Returns one entity by id or <see langword="null"/> when it does not exist.
    /// </summary>
    Task<TEntity?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns all entities of the current type.
    /// </summary>
    Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new entity and returns the canonical saved model.
    /// </summary>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Deletes an entity by id.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
