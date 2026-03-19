using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface ISchedulePresetRepository : IBaseRepository<SchedulePresetModel>
    {
        Task<List<SchedulePresetModel>> GetByContainerAsync(int containerId, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(int containerId, string name, int? excludeId = null, CancellationToken ct = default);
    }
}
