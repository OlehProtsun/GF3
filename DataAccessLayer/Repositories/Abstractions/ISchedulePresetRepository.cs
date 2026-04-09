using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for schedule presets.
/// </summary>
public interface ISchedulePresetRepository : IBaseRepository<SchedulePresetModel>
{
    /// <summary>
    /// Returns presets that belong to one container.
    /// </summary>
    Task<List<SchedulePresetModel>> GetByContainerAsync(int containerId, CancellationToken ct = default);

    /// <summary>
    /// Returns whether another preset in the same container already uses the same normalized name.
    /// </summary>
    Task<bool> ExistsByNameAsync(int containerId, string name, int? excludeId = null, CancellationToken ct = default);
}
