using BusinessLogicLayer.Contracts.Employees;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Employee self-service operations for the profile screen.
/// </summary>
public interface IEmployeeProfileService
{
    Task<EmployeeProfileDto> GetAsync(int employeeId, CancellationToken ct = default);

    Task<EmployeeProfileDto> UpdateContactAsync(int employeeId, string? recoveryEmail, string? phone, CancellationToken ct = default);

    Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(int employeeId, CancellationToken ct = default);

    Task ConfirmPasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default);
}
