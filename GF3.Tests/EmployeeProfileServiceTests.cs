using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;

namespace GF3.Tests;

public sealed class EmployeeProfileServiceTests
{
    [Fact]
    public async Task GetAsync_MapsEmployeeAndAccountToProfile()
    {
        var employeeService = new RecordingEmployeeService
        {
            Employee = new EmployeeModel
            {
                Id = 5,
                FirstName = " Ada ",
                LastName = " Lovelace ",
                Email = "ada@example.com",
                Phone = "123",
            },
        };
        var accountService = new RecordingEmployeeAccountService
        {
            Account = new EmployeeAccountModel
            {
                EmployeeId = 5,
                Username = "ada",
                PasswordHash = "hash",
            },
        };
        var service = new EmployeeProfileService(employeeService, accountService, new RecordingEmailSender());

        var profile = await service.GetAsync(5);

        Assert.Equal(5, profile.EmployeeId);
        Assert.Equal("ada", profile.Username);
        Assert.Equal("Ada Lovelace", profile.DisplayName);
        Assert.Equal("ada@example.com", profile.RecoveryEmail);
        Assert.Equal("123", profile.Phone);
    }

    [Fact]
    public async Task GetAsync_ThrowsValidation_WhenEmployeeOrAccountIsMissing()
    {
        var accountService = new RecordingEmployeeAccountService
        {
            Account = new EmployeeAccountModel
            {
                EmployeeId = 5,
                Username = "ada",
                PasswordHash = "hash",
            },
        };
        var missingEmployeeService = new EmployeeProfileService(
            new RecordingEmployeeService(),
            accountService,
            new RecordingEmailSender());

        var missingEmployee = await Assert.ThrowsAsync<ValidationException>(() => missingEmployeeService.GetAsync(5));

        var missingAccountService = new EmployeeProfileService(
            new RecordingEmployeeService
            {
                Employee = new EmployeeModel { Id = 5, FirstName = "", LastName = "", Email = "ada@example.com" },
            },
            new RecordingEmployeeAccountService(),
            new RecordingEmailSender());

        var missingAccount = await Assert.ThrowsAsync<ValidationException>(() => missingAccountService.GetAsync(5));

        Assert.Equal("The employee profile could not be found.", missingEmployee.Message);
        Assert.Equal("The login account for this employee could not be found.", missingAccount.Message);
    }

    [Fact]
    public async Task UpdateContactAsync_ClearsPasswordResetChallenge_WhenRecoveryEmailChanges()
    {
        var employeeService = new RecordingEmployeeService
        {
            Employee = new EmployeeModel
            {
                Id = 5,
                FirstName = "Ada",
                LastName = "Lovelace",
                Email = "old@example.com",
                Phone = "111",
            },
        };
        var accountService = new RecordingEmployeeAccountService
        {
            Account = new EmployeeAccountModel
            {
                EmployeeId = 5,
                Username = "ada",
                PasswordHash = "hash",
            },
        };
        var service = new EmployeeProfileService(employeeService, accountService, new RecordingEmailSender());

        var profile = await service.UpdateContactAsync(5, "new@example.com", "222");

        Assert.Equal("new@example.com", profile.RecoveryEmail);
        Assert.Equal("222", profile.Phone);
        Assert.Equal((5, "new@example.com", "222"), employeeService.LastContactUpdate);
        Assert.Equal([5], accountService.ClearedChallenges);
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_SendsMaskedEmailAndDelegatesConfirmation()
    {
        var expiresAtUtc = new DateTimeOffset(2026, 5, 13, 18, 30, 0, TimeSpan.Zero);
        var emailSender = new RecordingEmailSender();
        var accountService = new RecordingEmployeeAccountService
        {
            Account = new EmployeeAccountModel
            {
                EmployeeId = 5,
                Username = "ada",
                PasswordHash = "hash",
            },
            Challenge = new EmployeePasswordResetChallengeDto
            {
                Code = "123456",
                ExpiresAtUtc = expiresAtUtc,
            },
        };
        var service = new EmployeeProfileService(
            new RecordingEmployeeService
            {
                Employee = new EmployeeModel
                {
                    Id = 5,
                    FirstName = "Ada",
                    LastName = "Lovelace",
                    Email = "ada@example.com",
                },
            },
            accountService,
            emailSender);

        var result = await service.SendPasswordResetCodeAsync(5);
        await service.ConfirmPasswordResetAsync(5, "123456", "new-password");

        Assert.Equal("ad*@example.com", result.DeliveryHint);
        Assert.Equal(expiresAtUtc, result.ExpiresAtUtc);
        Assert.Equal("ada@example.com", emailSender.LastMessage!.Value.ToEmail);
        Assert.Equal("Ada Lovelace", emailSender.LastMessage.Value.ToName);
        Assert.Equal("GF3 password reset code", emailSender.LastMessage.Value.Subject);
        Assert.Contains("123456", emailSender.LastMessage.Value.TextBody);
        Assert.Contains("2026-05-13 18:30 UTC", emailSender.LastMessage.Value.TextBody);
        Assert.Equal((5, "123456", "new-password"), accountService.LastCompletedReset);
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_RequiresRecoveryEmailAndClearsChallengeOnDeliveryFailure()
    {
        var accountService = new RecordingEmployeeAccountService
        {
            Account = new EmployeeAccountModel
            {
                EmployeeId = 5,
                Username = "ada",
                PasswordHash = "hash",
            },
            Challenge = new EmployeePasswordResetChallengeDto
            {
                Code = "123456",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
            },
        };
        var missingEmailService = new EmployeeProfileService(
            new RecordingEmployeeService
            {
                Employee = new EmployeeModel { Id = 5, FirstName = "Ada", LastName = "Lovelace", Email = "" },
            },
            accountService,
            new RecordingEmailSender());

        var missingEmail = await Assert.ThrowsAsync<ValidationException>(() => missingEmailService.SendPasswordResetCodeAsync(5));

        var failingEmailService = new EmployeeProfileService(
            new RecordingEmployeeService
            {
                Employee = new EmployeeModel
                {
                    Id = 5,
                    FirstName = "Ada",
                    LastName = "Lovelace",
                    Email = "ada@example.com",
                },
            },
            accountService,
            new RecordingEmailSender { Failure = new InvalidOperationException("SMTP unavailable.") });

        var deliveryFailure = await Assert.ThrowsAsync<ValidationException>(() => failingEmailService.SendPasswordResetCodeAsync(5));

        Assert.Equal(["Add a recovery email before requesting a password code."], missingEmail.Errors["recoveryEmail"]);
        Assert.Equal("The password reset email could not be delivered. SMTP unavailable.", deliveryFailure.Message);
        Assert.Equal([5], accountService.ClearedChallenges);
    }

    private sealed class RecordingEmployeeService : IEmployeeService
    {
        public EmployeeModel? Employee { get; set; }
        public (int EmployeeId, string? Email, string? Phone)? LastContactUpdate { get; private set; }

        public Task<EmployeeModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(Employee?.Id == id ? Employee : null);

        public Task<EmployeeModel> UpdateContactAsync(int employeeId, string? email, string? phone, CancellationToken ct = default)
        {
            LastContactUpdate = (employeeId, email, phone);
            Employee ??= new EmployeeModel { Id = employeeId };
            Employee.Email = email;
            Employee.Phone = phone;
            return Task.FromResult(Employee);
        }

        public Task<EmployeeModel> CreateAsync(EmployeeModel entity, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<EmployeeModel>> GetAllAsync(CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(EmployeeModel entity, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingEmployeeAccountService : IEmployeeAccountService
    {
        public EmployeeAccountModel? Account { get; set; }
        public EmployeePasswordResetChallengeDto Challenge { get; set; } = new();
        public List<int> ClearedChallenges { get; } = [];
        public (int EmployeeId, string Code, string NewPassword)? LastCompletedReset { get; private set; }

        public Task<EmployeeAccountModel?> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
            => Task.FromResult(Account?.EmployeeId == employeeId ? Account : null);

        public Task<EmployeePasswordResetChallengeDto> CreatePasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
            => Task.FromResult(Challenge);

        public Task ClearPasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
        {
            ClearedChallenges.Add(employeeId);
            return Task.CompletedTask;
        }

        public Task CompletePasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)
        {
            LastCompletedReset = (employeeId, code, newPassword);
            return Task.CompletedTask;
        }

        public Task<EmployeeAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<int, EmployeeAccountModel>> GetByEmployeeIdsAsync(IEnumerable<int> employeeIds, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task MarkLoginSucceededAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task TouchLastSeenAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<EmployeeAccountModel?> UpsertForEmployeeAsync(int employeeId, string? username, string? password, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public Exception? Failure { get; init; }
        public (string ToEmail, string? ToName, string Subject, string TextBody)? LastMessage { get; private set; }

        public Task SendAsync(
            string toEmail,
            string? toName,
            string subject,
            string textBody,
            CancellationToken ct = default)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            LastMessage = (toEmail, toName, subject, textBody);
            return Task.CompletedTask;
        }
    }
}
