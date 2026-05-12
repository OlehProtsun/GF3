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

        var session = await service.AuthenticateAsync(" manager ", "123");
        var wrongPassword = await service.AuthenticateAsync("manager", "wrong-password");

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
            PasswordHash = hasher.HashPassword("employee-password"),
            PasswordUpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateManagerAccountRequest
            {
                DisplayName = "Shared Login",
                UserName = " shared.login ",
                Password = "manager-password",
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
            Password = "old-password",
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
                NewPassword = "new-password",
            });

        var oldLogin = await service.AuthenticateAsync("primary", "old-password");
        var newLogin = await service.AuthenticateAsync("updated.manager", "new-password");
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
            Password = "old-password",
            RecoveryEmail = "recovery.manager@example.com",
        });

        var dispatch = await service.SendPasswordResetCodeAsync(" recovery.manager ");
        var sentEmail = Assert.Single(emailSender.Messages);
        var code = ExtractResetCode(sentEmail.TextBody);

        var invalidCodeException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ConfirmPasswordResetAsync("recovery.manager", "000000", "new-password"));
        await service.ConfirmPasswordResetAsync("recovery.manager", code, "new-password");

        var oldLogin = await service.AuthenticateAsync("recovery.manager", "old-password");
        var newLogin = await service.AuthenticateAsync("recovery.manager", "new-password");
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
            Password = "password",
        });
        await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "With Recovery",
            UserName = "with.recovery",
            Password = "password",
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
    public async Task DeleteAsync_RemovesAnotherManagerButPreventsSelfDeletion()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var owner = await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Owner Manager",
            UserName = "owner.manager",
            Password = "password",
        });
        var teammate = await service.CreateAsync(new CreateManagerAccountRequest
        {
            DisplayName = "Team Manager",
            UserName = "team.manager",
            Password = "password",
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
            Password = "password",
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

    private sealed record SentEmail(
        string ToEmail,
        string? ToName,
        string Subject,
        string TextBody);
}
