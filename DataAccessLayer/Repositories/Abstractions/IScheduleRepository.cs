using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for saved schedule graphs.
/// </summary>
public interface IScheduleRepository : IBaseRepository<ScheduleModel>
{
    /// <summary>
    /// Searches schedules across graph, container, shop, and date-related fields.
    /// </summary>
    Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Returns schedules that belong to one container, optionally filtered by a search term.
    /// </summary>
    Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default);

    /// <summary>
    /// Returns a schedule together with the related data required by edit/export flows.
    /// </summary>
    Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default);
}
