using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Repositories
{
    public class AvailabilityGroupMemberRepository
        : GenericRepository<AvailabilityGroupMemberModel>, IAvailabilityGroupMemberRepository
    {
        public AvailabilityGroupMemberRepository(AppDbContext db) : base(db) { }

        public override async Task<List<AvailabilityGroupMemberModel>> GetAllAsync(CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(m => m.Employee)
                .Include(m => m.AvailabilityGroup)
                .ToListAsync(ct);
        }

        public async Task<List<AvailabilityGroupMemberModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(m => m.Employee)
                .Where(m => m.AvailabilityGroupId == groupId)
                .ToListAsync(ct);
        }

        public async Task<AvailabilityGroupMemberModel?> GetByGroupAndEmployeeAsync(int groupId, int employeeId, CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .SingleOrDefaultAsync(m =>
                    m.AvailabilityGroupId == groupId &&
                    m.EmployeeId == employeeId, ct);
        }
    }
}
