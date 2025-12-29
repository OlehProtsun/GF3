using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IAvailabilityGroupMemberRepository : IBaseRepository<AvailabilityGroupMemberModel>
    {
        Task<List<AvailabilityGroupMemberModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default);
        Task<AvailabilityGroupMemberModel?> GetByGroupAndEmployeeAsync(int groupId, int employeeId, CancellationToken ct = default);
    }
}
