using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Security;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Validates login credentials for manager and employee accounts stored in the database.
/// </summary>
public sealed class AuthService : IAuthService
{
    public const string ManagerRole = "manager";
    public const string EmployeeRole = "employee";
    private readonly IManagerAccountService _managerAccountService;
    private readonly IEmployeeAccountService _employeeAccountService;
    private readonly IEmployeeProfileService _employeeProfileService;
    private readonly IEmployeeService _employeeService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IManagerAccountService managerAccountService,
        IEmployeeAccountService employeeAccountService,
        IEmployeeProfileService employeeProfileService,
        IEmployeeService employeeService,
        IPasswordHasher passwordHasher)
    {
        _managerAccountService = managerAccountService;
        _employeeAccountService = employeeAccountService;
        _employeeProfileService = employeeProfileService;
        _employeeService = employeeService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticatedSessionDto?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var normalizedUserName = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedUserName) || !NumericPasswordPolicy.IsValid(password))
        {
            return null;
        }

        var managerAccount = await _managerAccountService.AuthenticateAsync(normalizedUserName, password, ct).ConfigureAwait(false);
        if (managerAccount is not null)
        {
            await _managerAccountService.MarkLoginSucceededAsync(managerAccount.Id, ct).ConfigureAwait(false);
            return new AuthenticatedSessionDto
            {
                Role = ManagerRole,
                IsSystemManager = managerAccount.IsSystem,
                UserName = managerAccount.UserName,
                DisplayName = managerAccount.DisplayName,
                ManagerId = managerAccount.Id,
                CredentialVersion = managerAccount.PasswordUpdatedAtUtc.UtcTicks,
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
            SessionVersion = account.SessionVersion,
        };
    }

    public async Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default)
    {
        var normalizedUserName = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            throw ValidationException.ForField("username", "Enter the username for this account.");
        }

        if (await _managerAccountService.GetByUsernameAsync(normalizedUserName, ct).ConfigureAwait(false) is not null)
        {
            return await _managerAccountService.SendPasswordResetCodeAsync(normalizedUserName, ct).ConfigureAwait(false);
        }

        var account = await ResolveRecoverableEmployeeAccountAsync(username, ct).ConfigureAwait(false);
        return await _employeeProfileService.SendPasswordResetCodeAsync(account.EmployeeId, ct).ConfigureAwait(false);
    }

    public async Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default)
    {
        var normalizedUserName = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            throw ValidationException.ForField("username", "Enter the username for this account.");
        }

        if (await _managerAccountService.GetByUsernameAsync(normalizedUserName, ct).ConfigureAwait(false) is not null)
        {
            await _managerAccountService.ConfirmPasswordResetAsync(normalizedUserName, code, newPassword, ct).ConfigureAwait(false);
            return;
        }

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

        var account = await _employeeAccountService.GetByUsernameAsync(normalizedUserName, ct).ConfigureAwait(false);
        if (account is null)
        {
            throw ValidationException.ForField("username", "No account was found for this username.");
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
