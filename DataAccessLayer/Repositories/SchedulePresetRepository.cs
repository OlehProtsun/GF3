using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Repository for schedule presets saved under a container.
/// Preset queries eagerly load employees because edit forms usually need the full assignment snapshot.
/// </summary>
public class SchedulePresetRepository : GenericRepository<SchedulePresetModel>, ISchedulePresetRepository
{
    public SchedulePresetRepository(AppDbContext db)
        : base(db)
    {
    }

    /// <inheritdoc />
    public override async Task<List<SchedulePresetModel>> GetAllAsync(CancellationToken ct = default)
        => await CreateDetailedQuery()
            .OrderBy(preset => preset.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<SchedulePresetModel>> GetByContainerAsync(int containerId, CancellationToken ct = default)
        => await CreateDetailedQuery()
            .Where(preset => preset.ContainerId == containerId)
            .OrderBy(preset => preset.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(int containerId, string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalizedName = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        return await _set
            .AsNoTracking()
            .AnyAsync(
                preset => preset.ContainerId == containerId
                    && preset.Name == normalizedName
                    && (!excludeId.HasValue || preset.Id != excludeId.Value),
                ct)
            .ConfigureAwait(false);
    }

    private IQueryable<SchedulePresetModel> CreateDetailedQuery()
        => _set
            .AsNoTracking()
            .Include(preset => preset.Employees);
}
