using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories.Abstractions;

/// <summary>
/// Persistence contract for employee login accounts.
/// </summary>
public interface IEmployeeAccountRepository : IBaseRepository<EmployeeAccountModel>
{
    /// <summary>
    /// Returns the account attached to one employee, if it exists.
    /// </summary>
    Task<EmployeeAccountModel?> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Returns accounts for a specific employee id set.
    /// </summary>
    Task<List<EmployeeAccountModel>> GetByEmployeeIdsAsync(IEnumerable<int> employeeIds, CancellationToken ct = default);

    /// <summary>
    /// Returns one account by username using case-insensitive matching.
    /// </summary>
    Task<EmployeeAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Checks whether another employee already uses the same username.
    /// </summary>
    Task<bool> ExistsByUsernameAsync(string username, int? excludeEmployeeId = null, CancellationToken ct = default);

    /// <summary>
    /// Persists a successful login timestamp for the employee account.
    /// </summary>
    Task RecordSuccessfulLoginAsync(int employeeId, DateTimeOffset occurredAtUtc, CancellationToken ct = default);

    /// <summary>
    /// Updates the last-seen timestamp when the employee is active, throttled by a minimum interval.
    /// </summary>
    Task TouchLastSeenAsync(int employeeId, DateTimeOffset seenAtUtc, TimeSpan minInterval, CancellationToken ct = default);
}
