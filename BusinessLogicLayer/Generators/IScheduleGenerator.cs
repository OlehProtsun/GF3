using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace BusinessLogicLayer.Generators
{
    public interface IScheduleGenerator
    {
        Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress = null,
            CancellationToken ct = default);
    }
}
