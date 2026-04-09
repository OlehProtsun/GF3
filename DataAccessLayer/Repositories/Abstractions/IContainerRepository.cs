using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for containers and container-specific lookup rules.
/// </summary>
public interface IContainerRepository : IBaseRepository<ContainerModel>
{
    /// <summary>
    /// Searches containers by user-facing fields used in the frontend.
    /// </summary>
    Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Returns whether another container already uses the same normalized name.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Returns whether any schedule still references the container.
    /// </summary>
    Task<bool> HasScheduleReferencesAsync(int containerId, CancellationToken ct = default);
}
