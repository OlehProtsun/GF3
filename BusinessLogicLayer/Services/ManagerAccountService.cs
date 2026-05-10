using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Managers;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Security;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using DalManagerAccountModel = DataAccessLayer.Models.ManagerAccountModel;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Encapsulates manager account validation, default account bootstrapping, profile updates, and password recovery.
/// </summary>
public sealed class ManagerAccountService : IManagerAccountService
{
    private const string DefaultManagerUserName = "manager";
    private const string DefaultManagerPassword = "123";
    private const string DefaultManagerDisplayName = "Manager";
    private static readonly TimeSpan PasswordResetCodeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetRequestThrottle = TimeSpan.FromSeconds(45);
    private readonly IManagerAccountRepository _managerAccountRepository;
    private readonly IEmployeeAccountRepository _employeeAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;

    public ManagerAccountService(
        IManagerAccountRepository managerAccountRepository,
        IEmployeeAccountRepository employeeAccountRepository,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender)
    {
        _managerAccountRepository = managerAccountRepository;
        _employeeAccountRepository = employeeAccountRepository;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
    }

    public async Task<ManagerAccountModel?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var normalizedUsername = NormalizeUsername(username);
        if (string.IsNullOrWhiteSpace(normalizedUsername) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        await EnsureDefaultManagerAsync(ct).ConfigureAwait(false);

        var account = await _managerAccountRepository.GetByUsernameAsync(normalizedUsername, ct).ConfigureAwait(false);
        if (account is null || !_passwordHasher.VerifyPassword(password, account.PasswordHash))
        {
            return null;
        }

        return account.ToContract();
    }

    public async Task<ManagerAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        await EnsureDefaultManagerAsync(ct).ConfigureAwait(false);
        var account = await _managerAccountRepository.GetByUsernameAsync(NormalizeUsername(username), ct).ConfigureAwait(false);
        return account?.ToContract();
    }

    public async Task<ManagerProfileDto> GetProfileAsync(int? managerId, string? userName, CancellationToken ct = default)
    {
        await EnsureDefaultManagerAsync(ct).ConfigureAwait(false);
        var account = await ResolveManagerAccountAsync(managerId, userName, ct).ConfigureAwait(false);
        return account.ToProfileDto();
    }

    public async Task<IReadOnlyList<ManagerProfileDto>> ListProfilesAsync(CancellationToken ct = default)
    {
        await EnsureDefaultManagerAsync(ct).ConfigureAwait(false);
        var accounts = await _managerAccountRepository.GetAllAsync(ct).ConfigureAwait(false);
        return accounts.Select(account => account.ToProfileDto()).ToList();
    }

    public async Task<ManagerProfileDto> CreateAsync(CreateManagerAccountRequest request, CancellationToken ct = default)
    {
        var normalizedUsername = NormalizeUsername(request.UserName);
        var normalizedDisplayName = NormalizeDisplayName(request.DisplayName, normalizedUsername);
        var normalizedPassword = NormalizePassword(request.Password);
        var normalizedRecoveryEmail = NormalizeEmail(request.RecoveryEmail);

        ValidateUsername(normalizedUsername);
        ValidateDisplayName(normalizedDisplayName);
        ValidatePassword(normalizedPassword, "password");
        await EnsureUsernameIsAvailableAsync(normalizedUsername, excludeManagerId: null, ct).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var created = await _managerAccountRepository.AddAsync(
                new DalManagerAccountModel
                {
                    Username = normalizedUsername,
                    DisplayName = normalizedDisplayName,
                    RecoveryEmail = normalizedRecoveryEmail,
                    PasswordHash = _passwordHasher.HashPassword(normalizedPassword),
                    PasswordUpdatedAtUtc = now,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                },
                ct)
            .ConfigureAwait(false);

        return created.ToProfileDto();
    }

    public async Task<ManagerProfileDto> UpdateProfileAsync(
        int? managerId,
        string? userName,
        UpdateManagerProfileRequest request,
        CancellationToken ct = default)
    {
        await EnsureDefaultManagerAsync(ct).ConfigureAwait(false);
        var account = await ResolveManagerAccountAsync(managerId, userName, ct).ConfigureAwait(false);
        var normalizedUsername = NormalizeUsername(request.UserName);
        var normalizedDisplayName = NormalizeDisplayName(request.DisplayName, normalizedUsername);
        var normalizedRecoveryEmail = NormalizeEmail(request.RecoveryEmail);
        var normalizedNewPassword = NormalizePassword(request.NewPassword);
        var isChangingPassword = !string.IsNullOrWhiteSpace(normalizedNewPassword);
        var isChangingRecoveryEmail = !string.Equals(
            NormalizeNullable(account.RecoveryEmail),
            NormalizeNullable(normalizedRecoveryEmail),
            StringComparison.OrdinalIgnoreCase);

        ValidateUsername(normalizedUsername);
        ValidateDisplayName(normalizedDisplayName);
        await EnsureUsernameIsAvailableAsync(normalizedUsername, account.Id, ct).ConfigureAwait(false);

        if (isChangingPassword)
        {
            ValidatePassword(normalizedNewPassword, "newPassword");
            account.PasswordHash = _passwordHasher.HashPassword(normalizedNewPassword);
            account.PasswordUpdatedAtUtc = DateTimeOffset.UtcNow;
            ClearPasswordResetChallengeFields(account);
        }

        if (isChangingRecoveryEmail)
        {
            ClearPasswordResetChallengeFields(account);
        }

        account.Username = normalizedUsername;
        account.DisplayName = normalizedDisplayName;
        account.RecoveryEmail = normalizedRecoveryEmail;
        account.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _managerAccountRepository.UpdateAsync(account, ct).ConfigureAwait(false);
        return account.ToProfileDto();
    }

    public async Task<ManagerProfileDto> DeleteAsync(
        int managerId,
        int? currentManagerId,
        string? currentUserName,
        CancellationToken ct = default)
    {
        await EnsureDefaultManagerAsync(ct).ConfigureAwait(false);

        if (managerId <= 0)
        {
            throw ValidationException.ForField("managerId", "Choose a manager account to delete.");
        }

        var currentAccount = await ResolveManagerAccountAsync(currentManagerId, currentUserName, ct).ConfigureAwait(false);
        if (currentAccount.Id == managerId)
        {
            throw ValidationException.ForField("managerId", "You cannot delete your own manager account.");
        }

        var account = await _managerAccountRepository.GetByIdAsync(managerId, ct).ConfigureAwait(false);
        if (account is null)
        {
            throw ValidationException.ForField("managerId", "This manager account could not be found.");
        }

        var deletedProfile = account.ToProfileDto();
        await _managerAccountRepository.DeleteAsync(managerId, ct).ConfigureAwait(false);
        return deletedProfile;
    }

    public Task MarkLoginSucceededAsync(int managerId, CancellationToken ct = default)
        => _managerAccountRepository.RecordSuccessfulLoginAsync(managerId, DateTimeOffset.UtcNow, ct);

    public async Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default)
    {
        await EnsureDefaultManagerAsync(ct).ConfigureAwait(false);
        var account = await ResolveManagerByUsernameForRecoveryAsync(username, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(account.RecoveryEmail))
        {
            throw ValidationException.ForField("recoveryEmail", "Add a recovery email before requesting a password code.");
        }

        var challenge = await CreatePasswordResetChallengeAsync(account, ct).ConfigureAwait(false);

        try
        {
            await _emailSender.SendAsync(
                    account.RecoveryEmail,
                    account.DisplayName,
                    "GF3 password reset code",
                    BuildPasswordResetBody(account.DisplayName, challenge.Code, challenge.ExpiresAtUtc),
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
                await ClearPasswordResetChallengeAsync(account.Username, ct).ConfigureAwait(false);
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
            DeliveryHint = MaskEmail(account.RecoveryEmail),
            ExpiresAtUtc = challenge.ExpiresAtUtc,
        };
    }

    public async Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default)
    {
        await EnsureDefaultManagerAsync(ct).ConfigureAwait(false);
        var account = await ResolveManagerByUsernameForRecoveryAsync(username, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(account.PasswordResetCodeHash) || !account.PasswordResetExpiresAtUtc.HasValue)
        {
            throw ValidationException.ForField("code", "Request a new password code before changing the password.");
        }

        var normalizedCode = NormalizeResetCode(code);
        if (DateTimeOffset.UtcNow > account.PasswordResetExpiresAtUtc.Value)
        {
            ClearPasswordResetChallengeFields(account);
            await _managerAccountRepository.UpdateAsync(account, ct).ConfigureAwait(false);
            throw ValidationException.ForField("code", "This password code expired. Request a new one.");
        }

        if (!_passwordHasher.VerifyPassword(normalizedCode, account.PasswordResetCodeHash))
        {
            throw ValidationException.ForField("code", "The password code is invalid.");
        }

        var normalizedPassword = NormalizePassword(newPassword);
        if (string.IsNullOrWhiteSpace(normalizedPassword))
        {
            throw ValidationException.ForField("newPassword", "New password is required.");
        }

        ValidatePassword(normalizedPassword, "newPassword");

        account.PasswordHash = _passwordHasher.HashPassword(normalizedPassword);
        account.PasswordUpdatedAtUtc = DateTimeOffset.UtcNow;
        account.UpdatedAtUtc = DateTimeOffset.UtcNow;
        ClearPasswordResetChallengeFields(account);

        await _managerAccountRepository.UpdateAsync(account, ct).ConfigureAwait(false);
    }

    private async Task<EmployeePasswordResetChallengeDto> CreatePasswordResetChallengeAsync(DalManagerAccountModel account, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (account.PasswordResetRequestedAtUtc.HasValue &&
            now < account.PasswordResetRequestedAtUtc.Value.Add(PasswordResetRequestThrottle))
        {
            throw new ValidationException("Please wait a moment before requesting another password code.");
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        account.PasswordResetCodeHash = _passwordHasher.HashPassword(code);
        account.PasswordResetRequestedAtUtc = now;
        account.PasswordResetExpiresAtUtc = now.Add(PasswordResetCodeLifetime);
        account.UpdatedAtUtc = now;

        await _managerAccountRepository.UpdateAsync(account, ct).ConfigureAwait(false);

        return new EmployeePasswordResetChallengeDto
        {
            Code = code,
            ExpiresAtUtc = account.PasswordResetExpiresAtUtc.Value,
        };
    }

    private async Task ClearPasswordResetChallengeAsync(string username, CancellationToken ct)
    {
        var account = await _managerAccountRepository.GetByUsernameAsync(username, ct).ConfigureAwait(false);
        if (account is null ||
            string.IsNullOrWhiteSpace(account.PasswordResetCodeHash) &&
            !account.PasswordResetExpiresAtUtc.HasValue &&
            !account.PasswordResetRequestedAtUtc.HasValue)
        {
            return;
        }

        ClearPasswordResetChallengeFields(account);
        account.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _managerAccountRepository.UpdateAsync(account, ct).ConfigureAwait(false);
    }

    private async Task EnsureDefaultManagerAsync(CancellationToken ct)
    {
        if (await _managerAccountRepository.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        await _managerAccountRepository.AddAsync(
                new DalManagerAccountModel
                {
                    Username = DefaultManagerUserName,
                    DisplayName = DefaultManagerDisplayName,
                    PasswordHash = _passwordHasher.HashPassword(DefaultManagerPassword),
                    PasswordUpdatedAtUtc = now,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                },
                ct)
            .ConfigureAwait(false);
    }

    private async Task<DalManagerAccountModel> ResolveManagerAccountAsync(int? managerId, string? userName, CancellationToken ct)
    {
        DalManagerAccountModel? account = null;

        if (managerId is > 0)
        {
            account = await _managerAccountRepository.GetByIdAsync(managerId.Value, ct).ConfigureAwait(false);
        }

        if (account is null && !string.IsNullOrWhiteSpace(userName))
        {
            account = await _managerAccountRepository.GetByUsernameAsync(userName, ct).ConfigureAwait(false);
        }

        return account ?? throw new ValidationException("The current manager account could not be found.");
    }

    private async Task<DalManagerAccountModel> ResolveManagerByUsernameForRecoveryAsync(string? username, CancellationToken ct)
    {
        var normalizedUsername = NormalizeUsername(username);
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            throw ValidationException.ForField("username", "Enter the username for this account.");
        }

        return await _managerAccountRepository.GetByUsernameAsync(normalizedUsername, ct).ConfigureAwait(false)
            ?? throw ValidationException.ForField("username", "No account was found for this username.");
    }

    private async Task EnsureUsernameIsAvailableAsync(string username, int? excludeManagerId, CancellationToken ct)
    {
        if (await _managerAccountRepository.ExistsByUsernameAsync(username, excludeManagerId, ct).ConfigureAwait(false))
        {
            throw ValidationException.ForField("userName", "This username is already in use.");
        }

        if (await _employeeAccountRepository.GetByUsernameAsync(username, ct).ConfigureAwait(false) is not null)
        {
            throw ValidationException.ForField("userName", "This username is already in use.");
        }
    }

    private static void ValidateUsername(string username)
    {
        if (username.Length < 3 || username.Length > 100)
        {
            throw ValidationException.ForField("userName", "Username must be between 3 and 100 characters long.");
        }

        if (!username.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-'))
        {
            throw ValidationException.ForField("userName", "Username may contain only letters, numbers, dots, underscores, and dashes.");
        }
    }

    private static void ValidateDisplayName(string displayName)
    {
        if (displayName.Length < 2 || displayName.Length > 160)
        {
            throw ValidationException.ForField("displayName", "Display name must be between 2 and 160 characters long.");
        }
    }

    private static void ValidatePassword(string password, string fieldName)
    {
        if (password.Length < 6 || password.Length > 200)
        {
            throw ValidationException.ForField(fieldName, "Password must be between 6 and 200 characters long.");
        }
    }

    private static string NormalizeUsername(string? username)
        => (username ?? string.Empty).Trim();

    private static string NormalizeDisplayName(string? displayName, string fallbackUsername)
    {
        var normalizedDisplayName = displayName?.Trim();
        return string.IsNullOrWhiteSpace(normalizedDisplayName) ? fallbackUsername : normalizedDisplayName;
    }

    private static string NormalizePassword(string? password)
        => password?.Trim() ?? string.Empty;

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmedEmail = email.Trim();
        if (trimmedEmail.Length > 254)
        {
            throw ValidationException.ForField("recoveryEmail", "Recovery email is too long.");
        }

        try
        {
            return new MailAddress(trimmedEmail).Address;
        }
        catch (FormatException)
        {
            throw ValidationException.ForField("recoveryEmail", "Enter a valid recovery email address.");
        }
    }

    private static string NormalizeResetCode(string? code)
    {
        var normalizedCode = code?.Trim() ?? string.Empty;
        if (normalizedCode.Length != 6 || !normalizedCode.All(char.IsDigit))
        {
            throw ValidationException.ForField("code", "Enter the 6-digit code from your email.");
        }

        return normalizedCode;
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

    private static void ClearPasswordResetChallengeFields(DalManagerAccountModel account)
    {
        account.PasswordResetCodeHash = null;
        account.PasswordResetExpiresAtUtc = null;
        account.PasswordResetRequestedAtUtc = null;
    }
}
