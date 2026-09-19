using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Security;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using System.Globalization;
using System.Security.Cryptography;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Encapsulates employee-account validation plus create/update behavior.
/// </summary>
public sealed class EmployeeAccountService : IEmployeeAccountService
{
    private static readonly TimeSpan ActivityWriteThrottle = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PasswordResetCodeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetRequestThrottle = TimeSpan.FromSeconds(45);
    private readonly IEmployeeAccountRepository _accountRepository;
    private readonly IManagerAccountRepository _managerAccountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public EmployeeAccountService(
        IEmployeeAccountRepository accountRepository,
        IManagerAccountRepository managerAccountRepository,
        IPasswordHasher passwordHasher)
    {
        _accountRepository = accountRepository;
        _managerAccountRepository = managerAccountRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<EmployeeAccountModel?> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByEmployeeIdAsync(employeeId, ct).ConfigureAwait(false);
        return account?.ToContract();
    }

    public async Task<IReadOnlyDictionary<int, EmployeeAccountModel>> GetByEmployeeIdsAsync(IEnumerable<int> employeeIds, CancellationToken ct = default)
    {
        var accounts = await _accountRepository.GetByEmployeeIdsAsync(employeeIds, ct).ConfigureAwait(false);
        return accounts
            .Select(account => account.ToContract())
            .ToDictionary(account => account.EmployeeId);
    }

    public async Task<EmployeeAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByUsernameAsync(username, ct).ConfigureAwait(false);
        return account?.ToContract();
    }

    public async Task<EmployeeAccountModel?> UpsertForEmployeeAsync(int employeeId, string? username, string? password, CancellationToken ct = default)
    {
        var existingAccount = await _accountRepository.GetByEmployeeIdAsync(employeeId, ct).ConfigureAwait(false);
        var normalizedUsername = NormalizeUsername(username);
        var normalizedPassword = NormalizePassword(password);

        if (existingAccount is null)
        {
            if (string.IsNullOrWhiteSpace(normalizedUsername) && string.IsNullOrWhiteSpace(normalizedPassword))
            {
                return null;
            }

            ValidateNewAccount(employeeId, normalizedUsername, normalizedPassword);

            await EnsureUsernameIsAvailableAsync(normalizedUsername!, employeeId, ct).ConfigureAwait(false);

            var created = await _accountRepository.AddAsync(
                    new DataAccessLayer.Models.EmployeeAccountModel
                    {
                        EmployeeId = employeeId,
                        Username = normalizedUsername!,
                        PasswordHash = _passwordHasher.HashPassword(normalizedPassword!),
                        PasswordUpdatedAtUtc = DateTimeOffset.UtcNow,
                    },
                    ct)
                .ConfigureAwait(false);

            return created.ToContract();
        }

        ValidateExistingAccount(existingAccount, normalizedUsername, normalizedPassword);

        await EnsureUsernameIsAvailableAsync(normalizedUsername!, employeeId, ct).ConfigureAwait(false);

        existingAccount.Username = normalizedUsername!;
        if (!string.IsNullOrWhiteSpace(normalizedPassword))
        {
            existingAccount.PasswordHash = _passwordHasher.HashPassword(normalizedPassword);
            existingAccount.PasswordUpdatedAtUtc = DateTimeOffset.UtcNow;
            existingAccount.SessionVersion++;
            ClearPasswordResetChallengeFields(existingAccount);
        }

        await _accountRepository.UpdateAsync(existingAccount, ct).ConfigureAwait(false);
        return existingAccount.ToContract();
    }

    public Task MarkLoginSucceededAsync(int employeeId, CancellationToken ct = default)
        => _accountRepository.RecordSuccessfulLoginAsync(employeeId, DateTimeOffset.UtcNow, ct);

    public Task TouchLastSeenAsync(int employeeId, CancellationToken ct = default)
        => _accountRepository.TouchLastSeenAsync(employeeId, DateTimeOffset.UtcNow, ActivityWriteThrottle, ct);

    public Task<bool> RevokeSessionsAsync(int employeeId, CancellationToken ct = default)
        => _accountRepository.IncrementSessionVersionAsync(employeeId, ct);

    public async Task<EmployeePasswordResetChallengeDto> CreatePasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByEmployeeIdAsync(employeeId, ct).ConfigureAwait(false);
        if (account is null)
        {
            throw new ValidationException("The login account for this employee could not be found.");
        }

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

        await _accountRepository.UpdateAsync(account, ct).ConfigureAwait(false);

        return new EmployeePasswordResetChallengeDto
        {
            Code = code,
            ExpiresAtUtc = account.PasswordResetExpiresAtUtc.Value,
        };
    }

    public async Task CompletePasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByEmployeeIdAsync(employeeId, ct).ConfigureAwait(false);
        if (account is null)
        {
            throw new ValidationException("The login account for this employee could not be found.");
        }

        if (string.IsNullOrWhiteSpace(account.PasswordResetCodeHash) || !account.PasswordResetExpiresAtUtc.HasValue)
        {
            throw ValidationException.ForField("code", "Request a new password code before changing the password.");
        }

        var normalizedCode = NormalizeResetCode(code);
        if (DateTimeOffset.UtcNow > account.PasswordResetExpiresAtUtc.Value)
        {
            ClearPasswordResetChallengeFields(account);
            await _accountRepository.UpdateAsync(account, ct).ConfigureAwait(false);
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

        ValidatePassword(normalizedPassword);

        account.PasswordHash = _passwordHasher.HashPassword(normalizedPassword);
        account.PasswordUpdatedAtUtc = DateTimeOffset.UtcNow;
        account.SessionVersion++;
        ClearPasswordResetChallengeFields(account);

        await _accountRepository.UpdateAsync(account, ct).ConfigureAwait(false);
    }

    public async Task ClearPasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByEmployeeIdAsync(employeeId, ct).ConfigureAwait(false);
        if (account is null ||
            string.IsNullOrWhiteSpace(account.PasswordResetCodeHash) &&
            !account.PasswordResetExpiresAtUtc.HasValue &&
            !account.PasswordResetRequestedAtUtc.HasValue)
        {
            return;
        }

        ClearPasswordResetChallengeFields(account);
        await _accountRepository.UpdateAsync(account, ct).ConfigureAwait(false);
    }

    private static void ValidateNewAccount(int employeeId, string? username, string? password)
    {
        if (employeeId <= 0)
        {
            throw new ValidationException("A valid employee id is required before creating an account.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ValidationException("Username is required when creating a login account.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required when creating a login account.");
        }

        ValidateUsername(username);
        ValidatePassword(password);
    }

    private static void ValidateExistingAccount(DataAccessLayer.Models.EmployeeAccountModel existingAccount, string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ValidationException("Username is required for employees that already have a login account.");
        }

        ValidateUsername(username);

        if (!string.IsNullOrWhiteSpace(password))
        {
            ValidatePassword(password);
        }

        if (existingAccount.EmployeeId <= 0)
        {
            throw new ValidationException("The employee login account is linked to an invalid employee.");
        }
    }

    private static void ValidateUsername(string username)
    {
        if (username.Length < 3 || username.Length > 100)
        {
            throw new ValidationException("Username must be between 3 and 100 characters long.");
        }

        if (!username.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-'))
        {
            throw new ValidationException("Username may contain only letters, numbers, dots, underscores, and dashes.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (!NumericPasswordPolicy.IsValid(password))
        {
            throw new ValidationException(NumericPasswordPolicy.ValidationMessage);
        }
    }

    private async Task EnsureUsernameIsAvailableAsync(string username, int employeeId, CancellationToken ct)
    {
        if (await _accountRepository.ExistsByUsernameAsync(username, excludeEmployeeId: employeeId, ct).ConfigureAwait(false))
        {
            throw new ValidationException("This username is already in use.");
        }

        if (await _managerAccountRepository.GetByUsernameAsync(username, ct).ConfigureAwait(false) is not null)
        {
            throw new ValidationException("This username is already in use.");
        }
    }

    private static string? NormalizeUsername(string? username)
        => string.IsNullOrWhiteSpace(username) ? null : username.Trim();

    private static string? NormalizePassword(string? password)
        => string.IsNullOrWhiteSpace(password) ? null : password.Trim();

    private static string NormalizeResetCode(string? code)
    {
        var normalizedCode = code?.Trim() ?? string.Empty;
        if (normalizedCode.Length != 6 || !normalizedCode.All(char.IsDigit))
        {
            throw ValidationException.ForField("code", "Enter the 6-digit code from your email.");
        }

        return normalizedCode;
    }

    private static void ClearPasswordResetChallengeFields(DataAccessLayer.Models.EmployeeAccountModel account)
    {
        account.PasswordResetCodeHash = null;
        account.PasswordResetExpiresAtUtc = null;
        account.PasswordResetRequestedAtUtc = null;
    }
}
