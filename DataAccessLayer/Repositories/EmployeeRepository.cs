using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class EmployeeRepository : GenericRepository<EmployeeModel>, IEmployeeRepository
    {
        public EmployeeRepository(AppDbContext db) : base(db) { }

        public async Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(value))
                return await _set.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

            value = value.ToLower().Trim();

            return await _set
                .AsNoTracking()
                .Where(emp =>
                    emp.FirstName.ToLower().Contains(value) ||
                    emp.LastName.ToLower().Contains(value) ||
                    (emp.Email != null && emp.Email.ToLower().Contains(value)) ||
                    (emp.Phone != null && emp.Phone.ToLower().Contains(value)))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public Task<bool> ExistsByNameAsync(
            string firstName,
            string lastName,
            int? excludeId = null,
            CancellationToken ct = default)
        {
            var normalizedFirst = (firstName ?? string.Empty).Trim().ToLower();
            var normalizedLast = (lastName ?? string.Empty).Trim().ToLower();

            return _set.AsNoTracking().AnyAsync(emp =>
                (excludeId == null || emp.Id != excludeId.Value) &&
                emp.FirstName.ToLower().Trim() == normalizedFirst &&
                emp.LastName.ToLower().Trim() == normalizedLast, ct);
        }

        public Task<bool> HasAvailabilityReferencesAsync(int employeeId, CancellationToken ct = default)
        {
            return _db.Set<AvailabilityGroupMemberModel>()
                .AsNoTracking()
                .AnyAsync(member => member.EmployeeId == employeeId, ct);
        }

        public async Task<bool> HasScheduleReferencesAsync(int employeeId, CancellationToken ct = default)
        {
            var hasScheduleEmployees = await _db.Set<ScheduleEmployeeModel>()
                .AsNoTracking()
                .AnyAsync(se => se.EmployeeId == employeeId, ct)
                .ConfigureAwait(false);

            if (hasScheduleEmployees)
                return true;

            return await _db.Set<ScheduleSlotModel>()
                .AsNoTracking()
                .AnyAsync(slot => slot.EmployeeId == employeeId, ct)
                .ConfigureAwait(false);
        }
    }
}
