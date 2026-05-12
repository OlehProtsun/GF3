using BusinessLogicLayer.Common;
using BusinessLogicLayer.Security;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GF3.Tests;

public sealed class EmployeeAccountServiceWorkflowTests
{
    [Fact]
    public async Task UpsertForEmployeeAsync_NewAccount_NormalizesUsernameAndHashesPassword()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var hasher = new PasswordHasher();
        var service = CreateService(context, hasher);
        var employee = await AddEmployeeAsync(context);

        var account = await service.UpsertForEmployeeAsync(employee.Id, " worker.one ", " password-one ");
        var storedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.NotNull(account);
        Assert.Equal(employee.Id, account!.EmployeeId);
        Assert.Equal("worker.one", account.Username);
        Assert.Equal("worker.one", storedAccount.Username);
        Assert.True(hasher.VerifyPassword("password-one", storedAccount.PasswordHash));
        Assert.False(hasher.VerifyPassword("wrong-password", storedAccount.PasswordHash));
        Assert.True(storedAccount.PasswordUpdatedAtUtc > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task UpsertForEmployeeAsync_ExistingAccount_UpdatesUsernameAndChangesPasswordOnlyWhenProvided()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var hasher = new PasswordHasher();
        var service = CreateService(context, hasher);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "password-one");

        await service.UpsertForEmployeeAsync(employee.Id, " worker.two ", "");
        var renamedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.Equal("worker.two", renamedAccount.Username);
        Assert.True(hasher.VerifyPassword("password-one", renamedAccount.PasswordHash));

        await service.UpsertForEmployeeAsync(employee.Id, "worker.two", "password-two");
        var passwordChangedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.True(hasher.VerifyPassword("password-two", passwordChangedAccount.PasswordHash));
        Assert.False(hasher.VerifyPassword("password-one", passwordChangedAccount.PasswordHash));
    }

    [Fact]
    public async Task UpsertForEmployeeAsync_RejectsInvalidDuplicateAndManagerUsernames()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var hasher = new PasswordHasher();
        var service = CreateService(context, hasher);
        var firstEmployee = await AddEmployeeAsync(context, "First", "Employee");
        var secondEmployee = await AddEmployeeAsync(context, "Second", "Employee");
        await service.UpsertForEmployeeAsync(firstEmployee.Id, "shared.user", "password-one");
        context.ManagerAccounts.Add(new ManagerAccountModel
        {
            Username = "manager.user",
            DisplayName = "Manager User",
            PasswordHash = hasher.HashPassword("manager-password"),
            PasswordUpdatedAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var shortUsername = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(secondEmployee.Id, "ab", "password-two"));
        var badUsername = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(secondEmployee.Id, "bad user", "password-two"));
        var duplicateEmployee = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(secondEmployee.Id, "SHARED.USER", "password-two"));
        var duplicateManager = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(secondEmployee.Id, "manager.user", "password-two"));

        Assert.Equal("Username must be between 3 and 100 characters long.", shortUsername.Message);
        Assert.Equal("Username may contain only letters, numbers, dots, underscores, and dashes.", badUsername.Message);
        Assert.Equal("This username is already in use.", duplicateEmployee.Message);
        Assert.Equal("This username is already in use.", duplicateManager.Message);
    }

    [Fact]
    public async Task PasswordResetWorkflow_ValidatesCodeAndChangesPassword()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var hasher = new PasswordHasher();
        var service = CreateService(context, hasher);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "old-password");

        var challenge = await service.CreatePasswordResetChallengeAsync(employee.Id);
        var malformedCode = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, "123", "new-password"));
        var invalidCode = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, "000000", "new-password"));
        var weakPassword = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, challenge.Code, "short"));

        await service.CompletePasswordResetAsync(employee.Id, challenge.Code, "new-password");
        var storedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.Equal(["Enter the 6-digit code from your email."], malformedCode.Errors["code"]);
        Assert.Equal(["The password code is invalid."], invalidCode.Errors["code"]);
        Assert.Equal("Password must be between 6 and 200 characters long.", weakPassword.Message);
        Assert.True(hasher.VerifyPassword("new-password", storedAccount.PasswordHash));
        Assert.False(hasher.VerifyPassword("old-password", storedAccount.PasswordHash));
        Assert.Null(storedAccount.PasswordResetCodeHash);
        Assert.Null(storedAccount.PasswordResetExpiresAtUtc);
        Assert.Null(storedAccount.PasswordResetRequestedAtUtc);
    }

    [Fact]
    public async Task PasswordResetWorkflow_ExpiredCodeClearsChallengeAndThrottlePreventsImmediateRepeat()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "old-password");

        var challenge = await service.CreatePasswordResetChallengeAsync(employee.Id);
        var throttled = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreatePasswordResetChallengeAsync(employee.Id));

        var trackedAccount = await context.EmployeeAccounts.SingleAsync();
        trackedAccount.PasswordResetExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var expired = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, challenge.Code, "new-password"));
        var storedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.Equal("Please wait a moment before requesting another password code.", throttled.Message);
        Assert.Equal(["This password code expired. Request a new one."], expired.Errors["code"]);
        Assert.Null(storedAccount.PasswordResetCodeHash);
        Assert.Null(storedAccount.PasswordResetExpiresAtUtc);
        Assert.Null(storedAccount.PasswordResetRequestedAtUtc);
    }

    [Fact]
    public async Task LoginTrackingAndLastSeen_ArePersistedAndThrottled()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "old-password");

        await service.MarkLoginSucceededAsync(employee.Id);
        var afterLogin = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        await service.TouchLastSeenAsync(employee.Id);
        var afterImmediateTouch = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.NotNull(afterLogin.LastLoginAtUtc);
        Assert.NotNull(afterLogin.LastSeenAtUtc);
        Assert.Equal(afterLogin.LastSeenAtUtc, afterImmediateTouch.LastSeenAtUtc);
    }

    [Fact]
    public async Task GetByEmployeeIdsAsync_FiltersInvalidDuplicateAndMissingIds()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var firstEmployee = await AddEmployeeAsync(context, "First", "Employee");
        var secondEmployee = await AddEmployeeAsync(context, "Second", "Employee");
        await service.UpsertForEmployeeAsync(firstEmployee.Id, "first", "password-one");
        await service.UpsertForEmployeeAsync(secondEmployee.Id, "second", "password-two");

        var accounts = await service.GetByEmployeeIdsAsync([firstEmployee.Id, firstEmployee.Id, -1, 999]);

        var account = Assert.Single(accounts);
        Assert.Equal(firstEmployee.Id, account.Key);
        Assert.Equal("first", account.Value.Username);
    }

    private static EmployeeAccountService CreateService(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        PasswordHasher? passwordHasher = null)
        => new(
            new EmployeeAccountRepository(context),
            new ManagerAccountRepository(context),
            passwordHasher ?? new PasswordHasher());

    private static async Task<EmployeeModel> AddEmployeeAsync(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        string firstName = "Casey",
        string lastName = "Worker")
    {
        var employee = TestDataFactory.CreateDalEmployee(firstName, lastName);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee;
    }
}
