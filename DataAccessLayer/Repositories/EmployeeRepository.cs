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
                return await _set.AsNoTracking().ToListAsync(ct);

            value = value.ToLower().Trim();

            return await _set
                .AsNoTracking()
                .Where(emp =>
                    emp.FirstName.ToLower().Contains(value) ||
                    emp.LastName.ToLower().Contains(value) ||
                    (emp.Email != null && emp.Email.ToLower().Contains(value)) ||
                    (emp.Phone != null && emp.Phone.ToLower().Contains(value)))
                .ToListAsync(ct);
        }
    }
}
