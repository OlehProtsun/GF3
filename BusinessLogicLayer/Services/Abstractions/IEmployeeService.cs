using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Business operations for employees.
/// </summary>
public interface IEmployeeService : IBaseService<EmployeeModel>
{
    /// <summary>
    /// Searches employees by user-facing fields such as name, email, and phone.
    /// </summary>
    Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Updates only the employee-owned contact fields exposed in the self-service profile.
    /// </summary>
    Task<EmployeeModel> UpdateContactAsync(int employeeId, string? email, string? phone, CancellationToken ct = default);

    /// <summary>
    /// Attempts to delete an employee without throwing for expected dependency-related failures.
    /// </summary>
    Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default);
}
