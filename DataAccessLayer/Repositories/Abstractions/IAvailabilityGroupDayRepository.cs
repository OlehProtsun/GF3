using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IAvailabilityGroupDayRepository : IBaseRepository<AvailabilityGroupDayModel>
    {
        Task<List<AvailabilityGroupDayModel>> GetByMemberIdAsync(int memberId, CancellationToken ct = default);
        Task DeleteByMemberIdAsync(int memberId, CancellationToken ct = default);
        Task<List<AvailabilityGroupDayModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<AvailabilityGroupDayModel> days, CancellationToken ct = default);

    }
}
