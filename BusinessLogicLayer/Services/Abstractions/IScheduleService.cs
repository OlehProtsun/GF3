using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IScheduleService : IBaseService<ScheduleModel>
    {
        Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default);
        Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default);
        Task SaveWithDetailsAsync(
            ScheduleModel schedule,
            IEnumerable<ScheduleEmployeeModel> employees,
            IEnumerable<ScheduleSlotModel> slots,
            IEnumerable<ScheduleCellStyleModel> cellStyles,
            CancellationToken ct = default);
        Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default);
    }
}
