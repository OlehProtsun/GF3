using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Generators
{
    public interface IScheduleGenerator
    {
        Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityMonthModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Placeholder generator; implementation will be provided later.
    /// </summary>
    public class ScheduleGenerator : IScheduleGenerator
    {
        public Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityMonthModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            CancellationToken ct = default)
        {
            // TODO: implement generation logic based on availability
            return Task.FromResult<IList<ScheduleSlotModel>>(new List<ScheduleSlotModel>());
        }
    }
}
