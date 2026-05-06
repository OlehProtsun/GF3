using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Business operations for containers and the graphs that belong to them.
/// This service is one of the main orchestration boundaries in the system: it owns container CRUD,
/// graph CRUD, graph generation, and related nested resources such as slots, employees, and styles.
/// </summary>
public interface IContainerService : IBaseService<ContainerModel>
{
    /// <summary>
    /// Searches containers by user-facing fields such as name and note.
    /// </summary>
    Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Attempts to delete a container without throwing for expected dependency-related failures.
    /// </summary>
    Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns all graphs that belong to one container.
    /// </summary>
    Task<List<ScheduleModel>?> GetGraphsAsync(int containerId, CancellationToken ct = default);

    /// <summary>
    /// Returns one graph that belongs to the specified container.
    /// </summary>
    Task<ScheduleModel?> GetGraphByIdAsync(int containerId, int graphId, CancellationToken ct = default);

    /// <summary>
    /// Returns public graphs assigned to one employee for the employee workflow.
    /// </summary>
    Task<List<ScheduleModel>> GetPublishedGraphsForEmployeeAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new graph inside the specified container.
    /// </summary>
    Task<ScheduleModel> CreateGraphAsync(int containerId, ScheduleModel model, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing graph inside the specified container.
    /// </summary>
    Task UpdateGraphAsync(int containerId, int graphId, ScheduleModel model, CancellationToken ct = default);

    /// <summary>
    /// Deletes one graph from the specified container.
    /// </summary>
    Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default);

    /// <summary>
    /// Returns all saved schedule presets of one container.
    /// </summary>
    Task<List<SchedulePresetModel>?> GetSchedulePresetsAsync(int containerId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new schedule preset inside the specified container.
    /// </summary>
    Task<SchedulePresetModel> CreateSchedulePresetAsync(int containerId, SchedulePresetModel model, CancellationToken ct = default);

    /// <summary>
    /// Generates slots for a persisted graph.
    /// </summary>
    Task<GenerateGraphResult> GenerateGraphAsync(
        int containerId,
        int graphId,
        bool overwrite,
        bool dryRun,
        IProgress<int>? progress,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a preview for a not-yet-persisted or client-edited graph snapshot.
    /// </summary>
    Task<GenerateGraphResult> GenerateGraphPreviewAsync(
        int containerId,
        ScheduleModel model,
        IEnumerable<ScheduleEmployeeModel> employees,
        IProgress<int>? progress,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all slots that belong to one graph.
    /// </summary>
    Task<List<ScheduleSlotModel>?> GetGraphSlotsAsync(int containerId, int graphId, CancellationToken ct = default);

    /// <summary>
    /// Replaces the entire slot collection of one graph.
    /// </summary>
    Task ReplaceGraphSlotsAsync(int containerId, int graphId, IEnumerable<ScheduleSlotModel> slots, CancellationToken ct = default);

    /// <summary>
    /// Creates one slot inside the specified graph.
    /// </summary>
    Task<ScheduleSlotModel> CreateGraphSlotAsync(int containerId, int graphId, ScheduleSlotModel model, CancellationToken ct = default);

    /// <summary>
    /// Updates one slot inside the specified graph.
    /// </summary>
    Task UpdateGraphSlotAsync(int containerId, int graphId, int slotId, ScheduleSlotModel model, CancellationToken ct = default);

    /// <summary>
    /// Deletes one slot from the specified graph.
    /// </summary>
    Task DeleteGraphSlotAsync(int containerId, int graphId, int slotId, CancellationToken ct = default);

    /// <summary>
    /// Returns all employee assignments of one graph.
    /// </summary>
    Task<List<ScheduleEmployeeModel>?> GetGraphEmployeesAsync(int containerId, int graphId, CancellationToken ct = default);

    /// <summary>
    /// Adds one employee assignment to the specified graph.
    /// </summary>
    Task<ScheduleEmployeeModel> AddGraphEmployeeAsync(int containerId, int graphId, ScheduleEmployeeModel model, CancellationToken ct = default);

    /// <summary>
    /// Updates one employee assignment in the specified graph.
    /// </summary>
    Task UpdateGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, ScheduleEmployeeModel model, CancellationToken ct = default);

    /// <summary>
    /// Removes one employee assignment from the specified graph.
    /// </summary>
    Task RemoveGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, CancellationToken ct = default);

    /// <summary>
    /// Returns all per-cell visual style overrides of one graph.
    /// </summary>
    Task<List<ScheduleCellStyleModel>?> GetGraphCellStylesAsync(int containerId, int graphId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates one per-cell style override for the specified graph.
    /// </summary>
    Task<ScheduleCellStyleModel> UpsertGraphCellStyleAsync(int containerId, int graphId, ScheduleCellStyleModel model, CancellationToken ct = default);

    /// <summary>
    /// Deletes one per-cell style override from the specified graph.
    /// </summary>
    Task DeleteGraphCellStyleAsync(int containerId, int graphId, int styleId, CancellationToken ct = default);
}
