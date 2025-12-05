using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IAvailabilityMonthService : IBaseService<AvailabilityMonthModel> 
    {
        Task<List<AvailabilityMonthModel>> GetByValueAsync(string value, CancellationToken ct = default);

        Task SaveWithDaysAsync(
            AvailabilityMonthModel month,
            IList<AvailabilityDayModel> days,
            CancellationToken ct = default);

        Task<List<AvailabilityDayModel>> GetDaysForMonthAsync(
            int availabilityMonthId,
            CancellationToken ct = default);
    }
}
