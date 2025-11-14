using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class AvailabilityMonthService : GenericService<AvailabilityMonthModel>, IAvailabilityMonthService
    {
        private readonly IAvailabilityMonthRepository _repo;
        public AvailabilityMonthService(IAvailabilityMonthRepository repo) : base(repo) 
        {
            _repo = repo;
        }

        public async Task<List<AvailabilityMonthModel>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            return await _repo.GetByValueAsync(value, ct);
        }
    }
}
