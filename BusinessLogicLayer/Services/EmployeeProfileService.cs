using System.Text;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Coordinates employee self-service profile edits and password recovery.
/// </summary>
public sealed class EmployeeProfileService : IEmployeeProfileService
{
    private readonly IEmployeeService _employeeService;
    private readonly IEmployeeAccountService _employeeAccountService;
    private readonly IEmailSender _emailSender;

    public EmployeeProfileService(
        IEmployeeService employeeService,
        IEmployeeAccountService employeeAccountService,
        IEmailSender emailSender)
    {
        _employeeService = employeeService;
        _employeeAccountService = employeeAccountService;
        _emailSender = emailSender;
    }

    public async Task<EmployeeProfileDto> GetAsync(int employeeId, CancellationToken ct = default)
    {
        var employeeTask = _employeeService.GetAsync(employeeId, ct);
        var accountTask = _employeeAccountService.GetByEmployeeIdAsync(employeeId, ct);

        await Task.WhenAll(employeeTask, accountTask).ConfigureAwait(false);

        var employee = await employeeTask.ConfigureAwait(false)
            ?? throw new ValidationException("The employee profile could not be found.");
        var account = await accountTask.ConfigureAwait(false)
            ?? throw new ValidationException("The login account for this employee could not be found.");

        return MapToProfile(employee, account);
    }

    public async Task<EmployeeProfileDto> UpdateContactAsync(int employeeId, string? recoveryEmail, string? phone, CancellationToken ct = default)
    {
        var currentProfile = await GetAsync(employeeId, ct).ConfigureAwait(false);
        var updatedEmployee = await _employeeService.UpdateContactAsync(employeeId, recoveryEmail, phone, ct).ConfigureAwait(false);

        if (!string.Equals(
                NormalizeNullable(currentProfile.RecoveryEmail),
                NormalizeNullable(updatedEmployee.Email),
                StringComparison.OrdinalIgnoreCase))
        {
            await _employeeAccountService.ClearPasswordResetChallengeAsync(employeeId, ct).ConfigureAwait(false);
        }

        var account = await _employeeAccountService.GetByEmployeeIdAsync(employeeId, ct).ConfigureAwait(false)
            ?? throw new ValidationException("The login account for this employee could not be found.");

        return MapToProfile(updatedEmployee, account);
    }

    public async Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(int employeeId, CancellationToken ct = default)
    {
        var profile = await GetAsync(employeeId, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(profile.RecoveryEmail))
        {
            throw ValidationException.ForField("recoveryEmail", "Add a recovery email before requesting a password code.");
        }

        var challenge = await _employeeAccountService.CreatePasswordResetChallengeAsync(employeeId, ct).ConfigureAwait(false);

        try
        {
            await _emailSender.SendAsync(
                    profile.RecoveryEmail,
                    profile.DisplayName,
                    "GF3 password reset code",
                    BuildPasswordResetBody(profile.DisplayName, challenge.Code, challenge.ExpiresAtUtc),
                    ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            try
            {
                await _employeeAccountService.ClearPasswordResetChallengeAsync(employeeId, ct).ConfigureAwait(false);
            }
            catch
            {
                // Best effort: the primary failure is still the email delivery problem.
            }

            var reason = string.IsNullOrWhiteSpace(exception.Message)
                ? "Check SMTP settings and try again."
                : exception.Message;
            throw new ValidationException($"The password reset email could not be delivered. {reason}");
        }

        return new PasswordResetDispatchResult
        {
            DeliveryHint = MaskEmail(profile.RecoveryEmail),
            ExpiresAtUtc = challenge.ExpiresAtUtc,
        };
    }

    public Task ConfirmPasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)
        => _employeeAccountService.CompletePasswordResetAsync(employeeId, code, newPassword, ct);

    private static EmployeeProfileDto MapToProfile(EmployeeModel employee, EmployeeAccountModel account) => new()
    {
        EmployeeId = employee.Id,
        Username = account.Username,
        DisplayName = BuildDisplayName(employee, account.Username),
        RecoveryEmail = employee.Email,
        Phone = employee.Phone,
    };

    private static string BuildDisplayName(EmployeeModel employee, string fallbackUserName)
    {
        var displayName = string.Join(
            " ",
            new[] { employee.FirstName, employee.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()))
            .Trim();

        return string.IsNullOrWhiteSpace(displayName) ? fallbackUserName : displayName;
    }

    private static string BuildPasswordResetBody(string displayName, string code, DateTimeOffset expiresAtUtc)
    {
        var safeDisplayName = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName.Trim();
        var body = new StringBuilder();
        body.AppendLine($"Hello {safeDisplayName},");
        body.AppendLine();
        body.AppendLine("Use this GF3 code to change your password:");
        body.AppendLine(code);
        body.AppendLine();
        body.AppendLine($"This code expires at {expiresAtUtc:yyyy-MM-dd HH:mm} UTC.");
        body.AppendLine("If you did not request a password change, you can ignore this email.");
        return body.ToString();
    }

    private static string MaskEmail(string email)
    {
        var trimmedEmail = email.Trim();
        var separatorIndex = trimmedEmail.IndexOf('@');
        if (separatorIndex <= 1 || separatorIndex == trimmedEmail.Length - 1)
        {
            return trimmedEmail;
        }

        var localPart = trimmedEmail[..separatorIndex];
        var domainPart = trimmedEmail[separatorIndex..];
        var visiblePrefix = localPart[..Math.Min(2, localPart.Length)];
        return $"{visiblePrefix}{new string('*', Math.Max(1, localPart.Length - visiblePrefix.Length))}{domainPart}";
    }

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
