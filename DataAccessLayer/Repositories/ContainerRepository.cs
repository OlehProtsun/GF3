using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Container-specific queries and delete-guard lookups.
/// Container read models always include schedules because most backend and frontend flows present
/// the container together with its graphs.
/// </summary>
public class ContainerRepository : GenericRepository<ContainerModel>, IContainerRepository
{
    public ContainerRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public override async Task<List<ContainerModel>> GetAllAsync(CancellationToken ct = default)
        => await CreateDetailedQuery()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<ContainerModel>> GetSummariesAsync(CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return await GetAllAsync(ct).ConfigureAwait(false);
        }

        var normalized = NormalizeSearchValue(value);

        return await CreateDetailedQuery()
            .Where(container =>
                container.Name.ToLower().Contains(normalized) ||
                (container.Note != null && container.Note.ToLower().Contains(normalized)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<ContainerModel>> GetSummariesByValueAsync(string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return await GetSummariesAsync(ct).ConfigureAwait(false);
        }

        var normalized = NormalizeSearchValue(value);

        return await _set
            .AsNoTracking()
            .Where(container =>
                container.Name.ToLower().Contains(normalized) ||
                (container.Note != null && container.Note.ToLower().Contains(normalized)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = NormalizeSearchValue(name);

        return _set.AsNoTracking().AnyAsync(container =>
            (!excludeId.HasValue || container.Id != excludeId.Value) &&
            container.Name.ToLower().Trim() == normalized,
            ct);
    }

    /// <inheritdoc />
    public Task<bool> HasScheduleReferencesAsync(int containerId, CancellationToken ct = default)
    {
        return _db.Set<ScheduleModel>()
            .AsNoTracking()
            .AnyAsync(schedule => schedule.ContainerId == containerId, ct);
    }

    private IQueryable<ContainerModel> CreateDetailedQuery()
        => _set
            .AsNoTracking()
            .Include(container => container.Schedules);

    private static string NormalizeSearchValue(string? value)
        => (value ?? string.Empty).Trim().ToLower();
}
