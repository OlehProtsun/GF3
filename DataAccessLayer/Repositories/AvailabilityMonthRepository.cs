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
    public class AvailabilityMonthRepository : GenericRepository<AvailabilityMonthModel>, IAvailabilityMonthRepository
    {
        public AvailabilityMonthRepository(AppDbContext db) : base(db) { }

        public override async Task<List<AvailabilityMonthModel>> GetAllAsync(CancellationToken ct = default)
        {
            return await _set
                .AsNoTracking()
                .Include(am => am.Employee)
                .ToListAsync(ct);
        }

        public Task<List<AvailabilityMonthModel>> GetByValueAsync(string value, CancellationToken ct = default)
        {
            // якщо нічого не ввели – просто повертаємо всі
            if (string.IsNullOrWhiteSpace(value))
            {
                return _set
                    .AsNoTracking()
                    .Include(am => am.Employee)
                    .ToListAsync(ct);
            }

            value = value.ToLower().Trim();
            bool hasInt = int.TryParse(value, out var intValue);

            var query = _set
                .AsNoTracking()
                .Include(am => am.Employee)
                .Where(am =>
                    // пошук по назві
                    am.Name.ToLower().Contains(value) ||

                    // пошук по імені/прізвищу працівника
                    am.Employee.FirstName.ToLower().Contains(value) ||
                    am.Employee.LastName.ToLower().Contains(value) ||

                    // якщо ввели число – шукаємо ще й по числових полях
                    (hasInt && (
                        am.Year == intValue ||
                        am.Month == intValue ||
                        am.EmployeeId == intValue ||
                        am.Id == intValue
                    ))
                );

            return query.ToListAsync(ct);
        }

    }
}
