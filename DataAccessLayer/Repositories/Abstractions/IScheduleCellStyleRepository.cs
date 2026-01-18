using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IScheduleCellStyleRepository : IBaseRepository<ScheduleCellStyleModel>
    {
        Task<List<ScheduleCellStyleModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default);
    }
}
