using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Contracts.Employees;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Login use cases for manager and employee sessions.
/// </summary>
public interface IAuthService
{
    Task<AuthenticatedSessionDto?> AuthenticateAsync(string username, string password, CancellationToken ct = default);

    Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default);

    Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default);
}
