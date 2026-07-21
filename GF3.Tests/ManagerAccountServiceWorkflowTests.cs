using System.Text.RegularExpressions;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Managers;
using BusinessLogicLayer.Security;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GF3.Tests;

public sealed class ManagerAccountServiceWorkflowTests
{
    [Fact]
    public async Task AuthenticateAsync_EmptyDatabase_CreatesUniversalManagerAndRecordsLogin()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var session = await service.AuthenticateAsync(" manager ", "123456");
        var wrongPassword = await service.AuthenticateAsync("manager", "999999");

        Assert.NotNull(session);
        Assert.Equal("manager", session!.UserName);
        Assert.Equal("Manager", session.DisplayName);
        Assert.Null(wrongPassword);

        await service.MarkLoginSucceededAsync(session.Id);

        var storedAccount = await context.ManagerAccounts.AsNoTracking().SingleAsync();
        Assert.Equal("manager", storedAccount.Username);
        Assert.NotNull(storedAccount.LastLoginAtUtc);
    }

    [Fact]
    public async Task CreateAsync_RejectsManagerUsernameThatAlreadyBelongsToEmployee()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var hasher = new PasswordHasher();
        var service = CreateService(context, passwordHasher: hasher);

        var employee = TestDataFactory.CreateDalEmployee("Casey", "Worker");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        context.EmployeeAccounts.Add(new EmployeeAccountModel
        {
            EmployeeId = employee.Id,
            Username = "shared.login",
            PasswordHash = hasher.HashPassword("444444"),
            PasswordUpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateManagerAccountRequest
            {
                DisplayName = "Shared Login",
                UserName = " shared.login ",
                Password = "333333",
            }));

        Assert.Equal(["This username is already in use."], exception.Errors["userName"]);
    }

    [Fact]
    public async Task UpdateProfileAsync_ChangesPasswordWithoutCurrentPassword_AndClearsRecoveryChallenge()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var emailSender = new CapturingEmailSender();
        var service = CreateService(context, emailSender: emailSender);
        var manager = await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Primary Manager",
            UserName = "primary",
            Password = "555555",
            RecoveryEmail = "primary@example.com",
        });

        await service.SendPasswordResetCodeAsync("primary");
        var challengedAccount = await context.ManagerAccounts.AsNoTracking().SingleAsync(account => account.Id == manager.Id);
        Assert.NotNull(challengedAccount.PasswordResetCodeHash);

        var updated = await service.UpdateProfileAsync(
            manager.Id,
            null,
            new UpdateManagerProfileRequest
            {
                DisplayName = " Updated Manager ",
                UserName = " updated.manager ",
                RecoveryEmail = " updated@example.com ",
                NewPassword = "666666",
            });

        var oldLogin = await service.AuthenticateAsync("primary", "555555");
        var newLogin = await service.AuthenticateAsync("updated.manager", "666666");
        var storedAccount = await context.ManagerAccounts.AsNoTracking().SingleAsync(account => account.Id == manager.Id);

        Assert.Equal("Updated Manager", updated.DisplayName);
        Assert.Equal("updated.manager", updated.UserName);
        Assert.Equal("updated@example.com", updated.RecoveryEmail);
        Assert.Null(oldLogin);
        Assert.NotNull(newLogin);
        Assert.Null(storedAccount.PasswordResetCodeHash);
        Assert.Null(storedAccount.PasswordResetExpiresAtUtc);
        Assert.Null(storedAccount.PasswordResetRequestedAtUtc);
    }

    [Fact]
    public async Task PasswordRecovery_SendsCodeMasksEmailAndResetsManagerPassword()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var emailSender = new CapturingEmailSender();
        var service = CreateService(context, emailSender: emailSender);
        await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Recovery Manager",
            UserName = "recovery.manager",
            Password = "555555",
            RecoveryEmail = "recovery.manager@example.com",
        });

        var dispatch = await service.SendPasswordResetCodeAsync(" recovery.manager ");
        var sentEmail = Assert.Single(emailSender.Messages);
        var code = ExtractResetCode(sentEmail.TextBody);

        var invalidCodeException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ConfirmPasswordResetAsync("recovery.manager", "000000", "666666"));
        await service.ConfirmPasswordResetAsync("recovery.manager", code, "666666");

        var oldLogin = await service.AuthenticateAsync("recovery.manager", "555555");
        var newLogin = await service.AuthenticateAsync("recovery.manager", "666666");
        var storedAccount = await context.ManagerAccounts.AsNoTracking().SingleAsync();

        Assert.Contains("*", dispatch.DeliveryHint);
        Assert.EndsWith("@example.com", dispatch.DeliveryHint);
        Assert.Equal("recovery.manager@example.com", sentEmail.ToEmail);
        Assert.Equal("GF3 password reset code", sentEmail.Subject);
        Assert.Equal(["The password code is invalid."], invalidCodeException.Errors["code"]);
        Assert.Null(oldLogin);
        Assert.NotNull(newLogin);
        Assert.Null(storedAccount.PasswordResetCodeHash);
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_RequiresRecoveryEmailAndThrottlesRepeatedRequests()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "No Recovery",
            UserName = "no.recovery",
            Password = "777777",
        });
        await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "With Recovery",
            UserName = "with.recovery",
            Password = "777777",
            RecoveryEmail = "with.recovery@example.com",
        });

        var missingEmailException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SendPasswordResetCodeAsync("no.recovery"));
        await service.SendPasswordResetCodeAsync("with.recovery");
        var throttledException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SendPasswordResetCodeAsync("with.recovery"));

        Assert.Equal(["Add a recovery email before requesting a password code."], missingEmailException.Errors["recoveryEmail"]);
        Assert.Equal("Please wait a moment before requesting another password code.", throttledException.Message);
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_ClearsChallengeWhenEmailDeliveryFails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context, emailSender: new FailingEmailSender("SMTP down."));
        await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Recovery Manager",
            UserName = "delivery.failure",
            Password = "777777",
            RecoveryEmail = "delivery.failure@example.com",
        });

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SendPasswordResetCodeAsync("delivery.failure"));
        var storedAccount = await context.ManagerAccounts.AsNoTracking().SingleAsync();

        Assert.Equal("The password reset email could not be delivered. SMTP down.", exception.Message);
        Assert.Null(storedAccount.PasswordResetCodeHash);
        Assert.Null(storedAccount.PasswordResetExpiresAtUtc);
        Assert.Null(storedAccount.PasswordResetRequestedAtUtc);
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_ValidatesChallengeCodeExpiryAndNewPassword()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var emailSender = new CapturingEmailSender();
        var service = CreateService(context, emailSender: emailSender);
        await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Recovery Manager",
            UserName = "confirm.manager",
            Password = "555555",
            RecoveryEmail = "confirm.manager@example.com",
        });

        var missingChallenge = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ConfirmPasswordResetAsync("confirm.manager", "123456", "666666"));

        await service.SendPasswordResetCodeAsync("confirm.manager");
        var code = ExtractResetCode(Assert.Single(emailSender.Messages).TextBody);

        var malformedCode = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ConfirmPasswordResetAsync("confirm.manager", "12x", "666666"));
        var blankPassword = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ConfirmPasswordResetAsync("confirm.manager", code, " "));
        var weakPassword = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ConfirmPasswordResetAsync("confirm.manager", code, "1234567"));

        var challengedAccount = await context.ManagerAccounts.SingleAsync();
        challengedAccount.PasswordResetExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var expiredCode = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ConfirmPasswordResetAsync("confirm.manager", code, "666666"));
        var storedAccount = await context.ManagerAccounts.AsNoTracking().SingleAsync();

        Assert.Equal(["Request a new password code before changing the password."], missingChallenge.Errors["code"]);
        Assert.Equal(["Enter the 6-digit code from your email."], malformedCode.Errors["code"]);
        Assert.Equal(["New password is required."], blankPassword.Errors["newPassword"]);
        Assert.Equal(["Password must contain exactly 6 digits."], weakPassword.Errors["newPassword"]);
        Assert.Equal(["This password code expired. Request a new one."], expiredCode.Errors["code"]);
        Assert.Null(storedAccount.PasswordResetCodeHash);
        Assert.Null(storedAccount.PasswordResetExpiresAtUtc);
        Assert.Null(storedAccount.PasswordResetRequestedAtUtc);
    }

    [Fact]
    public async Task CreateAndUpdateProfileAsync_ValidateFieldShapesAndDuplicateUsernames()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var shortUsername = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateManagerAccountRequest
            {
                DisplayName = "Valid Name",
                UserName = "ab",
                Password = "777777",
            }));
        var badUsername = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateManagerAccountRequest
            {
                DisplayName = "Valid Name",
                UserName = "bad user",
                Password = "777777",
            }));
        var badEmail = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateManagerAccountRequest
            {
                DisplayName = "Valid Name",
                UserName = "valid.user",
                Password = "777777",
                RecoveryEmail = "not-an-email",
            }));
        var weakPassword = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateManagerAccountRequest
            {
                DisplayName = "Valid Name",
                UserName = "weak.password",
                Password = "123",
            }));
        var nonNumericPassword = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateManagerAccountRequest
            {
                DisplayName = "Valid Name",
                UserName = "numeric.password",
                Password = "123abc",
            }));

        var first = await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "First Manager",
            UserName = "first.manager",
            Password = "777777",
            RecoveryEmail = "first@example.com",
        });
        var second = await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Second Manager",
            UserName = "second.manager",
            Password = "777777",
        });

        var duplicateUsername = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateProfileAsync(
                second.Id,
                null,
                new UpdateManagerProfileRequest
                {
                    DisplayName = "Second Manager",
                    UserName = " first.manager ",
                }));

        var updatedByUsername = await service.UpdateProfileAsync(
            null,
            first.UserName,
            new UpdateManagerProfileRequest
            {
                DisplayName = " ",
                UserName = " renamed.manager ",
                RecoveryEmail = " renamed@example.com ",
            });

        Assert.Equal(["Username must be between 3 and 100 characters long."], shortUsername.Errors["userName"]);
        Assert.Equal(["Username may contain only letters, numbers, dots, underscores, and dashes."], badUsername.Errors["userName"]);
        Assert.Equal(["Enter a valid recovery email address."], badEmail.Errors["recoveryEmail"]);
        Assert.Equal(["Password must contain exactly 6 digits."], weakPassword.Errors["password"]);
        Assert.Equal(["Password must contain exactly 6 digits."], nonNumericPassword.Errors["password"]);
        Assert.Equal(["This username is already in use."], duplicateUsername.Errors["userName"]);
        Assert.Equal("renamed.manager", updatedByUsername.UserName);
        Assert.Equal("renamed.manager", updatedByUsername.DisplayName);
        Assert.Equal("renamed@example.com", updatedByUsername.RecoveryEmail);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAnotherManagerButPreventsSelfDeletion()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var owner = await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Owner Manager",
            UserName = "owner.manager",
            Password = "777777",
        });
        var teammate = await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Team Manager",
            UserName = "team.manager",
            Password = "777777",
        });

        var selfDeleteException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeleteAsync(owner.Id, owner.Id, owner.UserName));
        var deleted = await service.DeleteAsync(teammate.Id, owner.Id, owner.UserName);
        var profiles = await service.ListProfilesAsync();

        Assert.Equal(["You cannot delete your own manager account."], selfDeleteException.Errors["managerId"]);
        Assert.Equal(teammate.Id, deleted.Id);
        Assert.DoesNotContain(profiles, profile => profile.Id == teammate.Id);
        Assert.Contains(profiles, profile => profile.Id == owner.Id);
    }

    [Fact]
    public async Task DeleteAsync_RejectsMissingTargetAndMissingCurrentManager()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var owner = await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Owner Manager",
            UserName = "owner.manager",
            Password = "777777",
        });

        var invalidTarget = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeleteAsync(0, owner.Id, owner.UserName));
        var missingTarget = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeleteAsync(999, owner.Id, owner.UserName));
        var missingCurrent = await Assert.ThrowsAsync<ValidationException>(() =>
            service.DeleteAsync(owner.Id, null, "missing.manager"));

        Assert.Equal(["Choose a manager account to delete."], invalidTarget.Errors["managerId"]);
        Assert.Equal(["This manager account could not be found."], missingTarget.Errors["managerId"]);
        Assert.Equal("The current manager account could not be found.", missingCurrent.Message);
    }

    private static ManagerAccountService CreateService(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        IPasswordHasher? passwordHasher = null,
        IEmailSender? emailSender = null)
    {
        var resolvedHasher = passwordHasher ?? new PasswordHasher();
        return new ManagerAccountService(
            new ManagerAccountRepository(context),
            new EmployeeAccountRepository(context),
            resolvedHasher,
            emailSender ?? new CapturingEmailSender());
    }

    private static string ExtractResetCode(string body)
    {
        var match = Regex.Match(body, @"\b\d{6}\b");
        Assert.True(match.Success, "The reset email should contain a 6-digit code.");
        return match.Value;
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<SentEmail> Messages { get; } = [];

        public Task SendAsync(
            string toEmail,
            string? toName,
            string subject,
            string textBody,
            CancellationToken ct = default)
        {
            Messages.Add(new SentEmail(toEmail, toName, subject, textBody));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingEmailSender(string message) : IEmailSender
    {
        public Task SendAsync(
            string toEmail,
            string? toName,
            string subject,
            string textBody,
            CancellationToken ct = default)
            => Task.FromException(new InvalidOperationException(message));
    }

    private sealed record SentEmail(
        string ToEmail,
        string? ToName,
        string Subject,
        string TextBody);
}
