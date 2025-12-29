using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Repositories
{
    public class AvailabilityGroupDayRepository
        : GenericRepository<AvailabilityGroupDayModel>, IAvailabilityGroupDayRepository
    {
        public AvailabilityGroupDayRepository(AppDbContext db) : base(db) { }

        public async Task<List<AvailabilityGroupDayModel>> GetByMemberIdAsync(int memberId, CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Where(d => d.AvailabilityGroupMemberId == memberId)
                .OrderBy(d => d.DayOfMonth)
                .ToListAsync(ct);
        }

        public async Task DeleteByMemberIdAsync(int memberId, CancellationToken ct = default)
        {
            // швидко і без GetAllAsync:
            var rows = await _set.Where(d => d.AvailabilityGroupMemberId == memberId).ToListAsync(ct);
            if (rows.Count == 0) return;

            _set.RemoveRange(rows);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<AvailabilityGroupDayModel>> GetByGroupIdAsync(int groupId, CancellationToken ct = default)
        {
            // Підтягуємо всі days для всіх members, що належать групі
            // Робимо join з таблицею members, щоб не було N+1
            return await (
                from d in _set.AsNoTracking()
                join m in _db.Set<AvailabilityGroupMemberModel>().AsNoTracking()
                    on d.AvailabilityGroupMemberId equals m.Id
                where m.AvailabilityGroupId == groupId
                orderby m.EmployeeId, d.DayOfMonth
                select d
            ).ToListAsync(ct);
        }

        public async Task AddRangeAsync(IEnumerable<AvailabilityGroupDayModel> days, CancellationToken ct = default)
        {
            await _set.AddRangeAsync(days, ct);
            await _db.SaveChangesAsync(ct);
        }


    }
}
