using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// API-facing employee facade that translates between HTTP-layer contracts and domain services.
/// </summary>
public interface IEmployeeFacade
{
    /// <summary>
    /// Returns all employees in facade DTO form.
    /// </summary>
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches employees using the frontend search term.
    /// </summary>
    Task<IReadOnlyList<EmployeeDto>> GetByValueAsync(string value, CancellationToken ct = default);

    /// <summary>
    /// Returns one employee by id or <see langword="null"/> when it does not exist.
    /// </summary>
    Task<EmployeeDto?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new employee from the supplied save request.
    /// </summary>
    Task<EmployeeDto> CreateAsync(SaveEmployeeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing employee from the supplied save request.
    /// </summary>
    Task UpdateAsync(SaveEmployeeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes an employee and throws on failure.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Attempts to delete an employee without throwing for expected business-rule failures.
    /// </summary>
    Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default);
}
