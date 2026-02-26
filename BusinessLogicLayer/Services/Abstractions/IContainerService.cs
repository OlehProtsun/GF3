using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IContainerService : IBaseService<ContainerModel>
    {
        Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default);

        Task<List<ScheduleModel>> GetGraphsAsync(int containerId, CancellationToken ct = default);
        Task<ScheduleModel?> GetGraphByIdAsync(int containerId, int graphId, CancellationToken ct = default);
        Task<ScheduleModel> CreateGraphAsync(int containerId, ScheduleModel model, CancellationToken ct = default);
        Task UpdateGraphAsync(int containerId, int graphId, ScheduleModel model, CancellationToken ct = default);
        Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default);

        Task<List<ScheduleSlotModel>> GetGraphSlotsAsync(int containerId, int graphId, CancellationToken ct = default);
        Task<ScheduleSlotModel> CreateGraphSlotAsync(int containerId, int graphId, ScheduleSlotModel model, CancellationToken ct = default);
        Task UpdateGraphSlotAsync(int containerId, int graphId, int slotId, ScheduleSlotModel model, CancellationToken ct = default);
        Task DeleteGraphSlotAsync(int containerId, int graphId, int slotId, CancellationToken ct = default);

        Task<List<ScheduleEmployeeModel>> GetGraphEmployeesAsync(int containerId, int graphId, CancellationToken ct = default);
        Task<ScheduleEmployeeModel> AddGraphEmployeeAsync(int containerId, int graphId, ScheduleEmployeeModel model, CancellationToken ct = default);
        Task UpdateGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, ScheduleEmployeeModel model, CancellationToken ct = default);
        Task RemoveGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, CancellationToken ct = default);

        Task<List<ScheduleCellStyleModel>> GetGraphCellStylesAsync(int containerId, int graphId, CancellationToken ct = default);
        Task<ScheduleCellStyleModel> UpsertGraphCellStyleAsync(int containerId, int graphId, ScheduleCellStyleModel model, CancellationToken ct = default);
        Task DeleteGraphCellStyleAsync(int containerId, int graphId, int styleId, CancellationToken ct = default);
    }
}
