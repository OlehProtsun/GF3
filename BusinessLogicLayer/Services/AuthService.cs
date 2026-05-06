using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Security;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Validates login credentials for the static manager account and employee accounts stored in the database.
/// </summary>
public sealed class AuthService : IAuthService
{
    public const string ManagerRole = "manager";
    public const string EmployeeRole = "employee";
    private const string StaticManagerUserName = "manager";
    private const string StaticManagerPassword = "123";
    private readonly IEmployeeAccountService _employeeAccountService;
    private readonly IEmployeeProfileService _employeeProfileService;
    private readonly IEmployeeService _employeeService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IEmployeeAccountService employeeAccountService,
        IEmployeeProfileService employeeProfileService,
        IEmployeeService employeeService,
        IPasswordHasher passwordHasher)
    {
        _employeeAccountService = employeeAccountService;
        _employeeProfileService = employeeProfileService;
        _employeeService = employeeService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticatedSessionDto?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var normalizedUserName = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedUserName) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        if (string.Equals(normalizedUserName, StaticManagerUserName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(password, StaticManagerPassword, StringComparison.Ordinal))
        {
            return new AuthenticatedSessionDto
            {
                Role = ManagerRole,
                UserName = StaticManagerUserName,
                DisplayName = "Manager",
            };
        }

        var account = await _employeeAccountService.GetByUsernameAsync(normalizedUserName, ct).ConfigureAwait(false);
        if (account is null || !_passwordHasher.VerifyPassword(password, account.PasswordHash))
        {
            return null;
        }

        var employee = await _employeeService.GetAsync(account.EmployeeId, ct).ConfigureAwait(false);
        if (employee is null)
        {
            return null;
        }

        await _employeeAccountService.MarkLoginSucceededAsync(employee.Id, ct).ConfigureAwait(false);

        var displayName = string.Join(
            " ",
            new[] { employee.FirstName, employee.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
            .Trim();

        return new AuthenticatedSessionDto
        {
            Role = EmployeeRole,
            UserName = account.Username,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? account.Username : displayName,
            EmployeeId = employee.Id,
        };
    }

    public async Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default)
    {
        var account = await ResolveRecoverableEmployeeAccountAsync(username, ct).ConfigureAwait(false);
        return await _employeeProfileService.SendPasswordResetCodeAsync(account.EmployeeId, ct).ConfigureAwait(false);
    }

    public async Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default)
    {
        var account = await ResolveRecoverableEmployeeAccountAsync(username, ct).ConfigureAwait(false);
        await _employeeProfileService.ConfirmPasswordResetAsync(account.EmployeeId, code, newPassword, ct).ConfigureAwait(false);
    }

    private async Task<EmployeeAccountModel> ResolveRecoverableEmployeeAccountAsync(string? username, CancellationToken ct)
    {
        var normalizedUserName = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            throw ValidationException.ForField("username", "Enter the username for this account.");
        }

        if (string.Equals(normalizedUserName, StaticManagerUserName, StringComparison.OrdinalIgnoreCase))
        {
            throw ValidationException.ForField("username", "Password recovery is available only for employee accounts.");
        }

        var account = await _employeeAccountService.GetByUsernameAsync(normalizedUserName, ct).ConfigureAwait(false);
        if (account is null)
        {
            throw ValidationException.ForField("username", "No employee account was found for this username.");
        }

        var employee = await _employeeService.GetAsync(account.EmployeeId, ct).ConfigureAwait(false);
        if (employee is null)
        {
            throw ValidationException.ForField("username", "The employee profile for this account could not be found.");
        }

        if (string.IsNullOrWhiteSpace(employee.Email))
        {
            throw ValidationException.ForField(
                "recoveryEmail",
                "This account does not have a recovery email. Ask a manager to add one before using password recovery.");
        }

        return account;
    }
}
