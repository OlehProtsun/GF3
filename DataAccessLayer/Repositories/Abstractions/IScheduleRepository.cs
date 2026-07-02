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
    /// Returns public schedules assigned to one employee.
    /// </summary>
    Task<List<ScheduleModel>> GetPublishedForEmployeeAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Returns every other saved schedule in the requested month together with its concrete slots.
    /// Generation uses this read model to keep one employee from receiving overlapping work in
    /// separate schedules.
    /// </summary>
    Task<List<ScheduleModel>> GetByMonthWithSlotsAsync(
        int year,
        int month,
        int? excludeScheduleId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a schedule together with the related data required by edit/export flows.
    /// </summary>
    Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default);
}
