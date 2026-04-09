using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for employees and employee-specific lookup rules.
/// </summary>
public interface IEmployeeRepository : IBaseRepository<EmployeeModel>
{
    /// <summary>
    /// Searches employees across the fields exposed in the UI.
    /// </summary>
    Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Returns whether another employee already uses the same normalized first/last name pair.
    /// </summary>
    Task<bool> ExistsByNameAsync(string firstName, string lastName, int? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// Returns whether any availability-group member still references the employee.
    /// </summary>
    Task<bool> HasAvailabilityReferencesAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Returns whether any saved schedule data still references the employee.
    /// </summary>
    Task<bool> HasScheduleReferencesAsync(int employeeId, CancellationToken ct = default);
}
