using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Business operations for persisted schedule graphs.
/// </summary>
public interface IScheduleService : IBaseService<ScheduleModel>
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
    /// Saves a schedule together with its nested employees, slots, and visual cell styles.
    /// </summary>
    Task SaveWithDetailsAsync(
        ScheduleModel schedule,
        IEnumerable<ScheduleEmployeeModel> employees,
        IEnumerable<ScheduleSlotModel> slots,
        IEnumerable<ScheduleCellStyleModel> cellStyles,
        CancellationToken ct = default);

    /// <summary>
    /// Returns one schedule with all related data required by edit/export flows.
    /// </summary>
    Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default);
}
