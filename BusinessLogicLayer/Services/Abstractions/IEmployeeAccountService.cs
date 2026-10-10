using BusinessLogicLayer.Contracts.Employees;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Business operations for employee login accounts.
/// </summary>
public interface IEmployeeAccountService
{
    Task<EmployeeAccountModel?> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, EmployeeAccountModel>> GetByEmployeeIdsAsync(IEnumerable<int> employeeIds, CancellationToken ct = default);

    Task<EmployeeAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<EmployeeAccountModel?> UpsertForEmployeeAsync(int employeeId, string? username, string? password, CancellationToken ct = default);

    Task MarkLoginSucceededAsync(int employeeId, CancellationToken ct = default);

    Task TouchLastSeenAsync(int employeeId, CancellationToken ct = default);

    Task<bool> RevokeSessionsAsync(int employeeId, CancellationToken ct = default)
        => Task.FromResult(false);

    Task<EmployeePasswordResetChallengeDto> CreatePasswordResetChallengeAsync(int employeeId, CancellationToken ct = default);

    Task CompletePasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default);

    Task ClearPasswordResetChallengeAsync(int employeeId, CancellationToken ct = default);
}
