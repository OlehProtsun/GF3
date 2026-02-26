using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IContainerService : IBaseService<ContainerModel>
    {
        Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default);

        Task<List<ScheduleModel>> GetGraphsAsync(int containerId, CancellationToken ct = default);
        Task<ScheduleModel> CreateGraphAsync(int containerId, ScheduleModel model, CancellationToken ct = default);
        Task UpdateGraphAsync(int containerId, int graphId, ScheduleModel model, CancellationToken ct = default);
        Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default);
    }
}
