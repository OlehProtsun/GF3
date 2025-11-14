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
    }
}
