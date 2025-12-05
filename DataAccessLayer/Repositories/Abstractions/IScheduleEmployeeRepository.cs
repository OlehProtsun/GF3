using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IScheduleEmployeeRepository : IBaseRepository<ScheduleEmployeeModel>
    {
        Task<List<ScheduleEmployeeModel>> GetByScheduleAsync(int scheduleId, CancellationToken ct = default);
    }
}
