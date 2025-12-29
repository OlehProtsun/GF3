using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IAvailabilityGroupRepository : IBaseRepository<AvailabilityGroupModel>
    {
        Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default);
        Task<AvailabilityGroupModel?> GetFullByIdAsync(int id, CancellationToken ct = default);

    }
}
