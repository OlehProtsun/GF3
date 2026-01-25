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
        Task<bool> ExistsByNameAsync(string name, int year, int month, int? excludeId = null, CancellationToken ct = default);

    }
}
