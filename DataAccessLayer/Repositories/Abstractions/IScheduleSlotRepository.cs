using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IScheduleSlotRepository : IBaseRepository<ScheduleSlotModel>
    {
        Task<List<ScheduleSlotModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default);
        Task<int> ReplaceForScheduleAsync(int scheduleId, IEnumerable<ScheduleSlotModel> slots, bool overwrite, CancellationToken ct = default);
    }
}
