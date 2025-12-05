using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IScheduleRepository : IBaseRepository<ScheduleModel>
    {
        Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default);
        Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default);
        Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default);
    }
}
