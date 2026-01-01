using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Repositories
{
    public class AvailabilityGroupRepository
        : GenericRepository<AvailabilityGroupModel>, IAvailabilityGroupRepository
    {
        public AvailabilityGroupRepository(AppDbContext db) : base(db) { }

        public override async Task<List<AvailabilityGroupModel>> GetAllAsync(CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(g => g.Members)
                    .ThenInclude(m => m.Employee)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public async Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(value))
                return await GetAllAsync(ct).ConfigureAwait(false);

            value = value.ToLower().Trim();
            bool hasInt = int.TryParse(value, out var intValue);

            var query = _set
                .AsNoTracking()
                .Include(g => g.Members)
                    .ThenInclude(m => m.Employee)
                .Where(g =>
                    g.Name.ToLower().Contains(value) ||
                    g.Members.Any(m =>
                        m.Employee.FirstName.ToLower().Contains(value) ||
                        m.Employee.LastName.ToLower().Contains(value)
                    ) ||
                    (hasInt && (g.Year == intValue || g.Month == intValue || g.Id == intValue))
                );

            return await query.ToListAsync(ct).ConfigureAwait(false);
        }

        public async Task<AvailabilityGroupModel?> GetFullByIdAsync(int id, CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(g => g.Members).ThenInclude(m => m.Employee)
                .Include(g => g.Members).ThenInclude(m => m.Days)
                .SingleOrDefaultAsync(g => g.Id == id, ct)
                .ConfigureAwait(false);
        }
    }
}
