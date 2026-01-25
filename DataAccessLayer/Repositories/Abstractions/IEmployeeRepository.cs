using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Abstractions
{
    public interface IEmployeeRepository : IBaseRepository<EmployeeModel> 
    {
        Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(string firstName, string lastName, int? excludeId = null, CancellationToken ct = default);
        Task<bool> HasAvailabilityReferencesAsync(int employeeId, CancellationToken ct = default);
        Task<bool> HasScheduleReferencesAsync(int employeeId, CancellationToken ct = default);
    }

}
