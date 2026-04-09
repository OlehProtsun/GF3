using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Shared EF Core implementation for the repository baseline contract.
/// The repository deliberately keeps write operations small and explicit:
/// 1. load/attach the correct tracked entity,
/// 2. save changes,
/// 3. detach the entity again so long-lived contexts do not accumulate tracking state.
/// </summary>
public class GenericRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<TEntity> _set;

    public GenericRepository(AppDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        _db = db;
        _set = _db.Set<TEntity>();
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => EF.Property<int>(entity, "Id") == id, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _set.AddAsync(entity, ct).ConfigureAwait(false);
        await SaveChangesOrResetAsync(ct).ConfigureAwait(false);

        // We immediately detach newly created entities so later commands in the same request
        // do not accidentally reuse tracked state from a previous repository operation.
        DetachEntity(entity);
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var trackedOrAttachedEntity = await GetTrackedOrAttachForUpdateAsync(entity, ct).ConfigureAwait(false);
        await SaveChangesOrResetAsync(ct).ConfigureAwait(false);
        DetachEntity(trackedOrAttachedEntity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await FindEntityForDeleteAsync(id, ct).ConfigureAwait(false);
        if (entity is null)
        {
            return;
        }

        _set.Remove(entity);
        await SaveChangesOrResetAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds an already tracked entity for the same primary key when possible.
    /// Reusing the tracked instance avoids EF Core conflicts caused by attaching two instances
    /// that represent the same database row in one <see cref="DbContext"/>.
    /// </summary>
    private async Task<TEntity> GetTrackedOrAttachForUpdateAsync(TEntity entity, CancellationToken ct)
    {
        var keyValues = GetPrimaryKeyValues(entity);
        var trackedEntity = await _set.FindAsync(keyValues, ct).ConfigureAwait(false);

        if (trackedEntity is not null)
        {
            _db.Entry(trackedEntity).CurrentValues.SetValues(entity);
            return trackedEntity;
        }

        _set.Attach(entity);
        _db.Entry(entity).State = EntityState.Modified;
        return entity;
    }

    private async Task<TEntity?> FindEntityForDeleteAsync(int id, CancellationToken ct)
        => await _set.FindAsync([id], ct).ConfigureAwait(false);

    private object?[] GetPrimaryKeyValues(TEntity entity)
    {
        var entityType = GetEntityTypeMetadata();
        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity '{typeof(TEntity).Name}' does not define a primary key.");

        return primaryKey.Properties
            .Select(property => ReadKeyValue(entity, property))
            .ToArray();
    }

    private IEntityType GetEntityTypeMetadata()
        => _db.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity '{typeof(TEntity).Name}' is not part of the current DbContext model.");

    private static object? ReadKeyValue(TEntity entity, IProperty property)
        => property.PropertyInfo?.GetValue(entity)
            ?? throw new InvalidOperationException(
                $"Primary key property '{property.Name}' on entity '{typeof(TEntity).Name}' must be mapped to a CLR property.");

    private void DetachEntity(TEntity entity)
    {
        var entry = _db.Entry(entity);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
    }

    private async Task SaveChangesOrResetAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // When SaveChanges fails, the change tracker may contain half-updated entries.
            // Clearing it keeps the DbContext safe for the remainder of the request/test.
            _db.ChangeTracker.Clear();
            throw;
        }
    }
}
