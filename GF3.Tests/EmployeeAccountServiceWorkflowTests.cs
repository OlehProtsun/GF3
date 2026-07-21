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
    [Theory]
    [InlineData("12345")]
    [InlineData("123abc")]
    [InlineData("1234567")]
    public async Task UpsertForEmployeeAsync_RejectsShortAndNonNumericPasswords(string password)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(employee.Id, "worker.one", password));

        Assert.Equal("Password must contain exactly 6 digits.", exception.Message);
        Assert.Empty(await context.EmployeeAccounts.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task UpsertForEmployeeAsync_NewAccount_NormalizesUsernameAndHashesPassword()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var hasher = new PasswordHasher();
        var service = CreateService(context, hasher);
        var employee = await AddEmployeeAsync(context);

        var account = await service.UpsertForEmployeeAsync(employee.Id, " worker.one ", " 111111 ");
        var storedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.NotNull(account);
        Assert.Equal(employee.Id, account!.EmployeeId);
        Assert.Equal("worker.one", account.Username);
        Assert.Equal("worker.one", storedAccount.Username);
        Assert.True(hasher.VerifyPassword("111111", storedAccount.PasswordHash));
        Assert.False(hasher.VerifyPassword("999999", storedAccount.PasswordHash));
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
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "111111");

        await service.UpsertForEmployeeAsync(employee.Id, " worker.two ", "");
        var renamedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.Equal("worker.two", renamedAccount.Username);
        Assert.True(hasher.VerifyPassword("111111", renamedAccount.PasswordHash));

        await service.UpsertForEmployeeAsync(employee.Id, "worker.two", "222222");
        var passwordChangedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.True(hasher.VerifyPassword("222222", passwordChangedAccount.PasswordHash));
        Assert.False(hasher.VerifyPassword("111111", passwordChangedAccount.PasswordHash));
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
        await service.UpsertForEmployeeAsync(firstEmployee.Id, "shared.user", "111111");
        context.ManagerAccounts.Add(new ManagerAccountModel
        {
            Username = "manager.user",
            DisplayName = "Manager User",
            PasswordHash = hasher.HashPassword("333333"),
            PasswordUpdatedAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var shortUsername = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(secondEmployee.Id, "ab", "222222"));
        var badUsername = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(secondEmployee.Id, "bad user", "222222"));
        var duplicateEmployee = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(secondEmployee.Id, "SHARED.USER", "222222"));
        var duplicateManager = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(secondEmployee.Id, "manager.user", "222222"));

        Assert.Equal("Username must be between 3 and 100 characters long.", shortUsername.Message);
        Assert.Equal("Username may contain only letters, numbers, dots, underscores, and dashes.", badUsername.Message);
        Assert.Equal("This username is already in use.", duplicateEmployee.Message);
        Assert.Equal("This username is already in use.", duplicateManager.Message);
    }

    [Fact]
    public async Task UpsertForEmployeeAsync_ReturnsNullForBlankNewAccountAndRejectsMissingRequiredFields()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);

        var noAccount = await service.UpsertForEmployeeAsync(employee.Id, " ", null);
        var invalidEmployeeId = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(0, "worker.one", "111111"));
        var missingUsername = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(employee.Id, null, "111111"));
        var missingPassword = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(employee.Id, "worker.one", null));

        Assert.Null(noAccount);
        Assert.Empty(await context.EmployeeAccounts.AsNoTracking().ToListAsync());
        Assert.Equal("A valid employee id is required before creating an account.", invalidEmployeeId.Message);
        Assert.Equal("Username is required when creating a login account.", missingUsername.Message);
        Assert.Equal("Password is required when creating a login account.", missingPassword.Message);
    }

    [Fact]
    public async Task UpsertForEmployeeAsync_ExistingAccountRejectsBlankUsername()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "111111");

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpsertForEmployeeAsync(employee.Id, " ", "222222"));

        Assert.Equal("Username is required for employees that already have a login account.", exception.Message);
    }

    [Fact]
    public async Task GetAccountMethods_MapStoredAccountsByEmployeeAndUsername()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "111111");

        var byEmployee = await service.GetByEmployeeIdAsync(employee.Id);
        var byUsername = await service.GetByUsernameAsync("worker.one");
        var missingEmployee = await service.GetByEmployeeIdAsync(999);
        var missingUsername = await service.GetByUsernameAsync("missing");

        Assert.NotNull(byEmployee);
        Assert.Equal(employee.Id, byEmployee!.EmployeeId);
        Assert.NotNull(byUsername);
        Assert.Equal("worker.one", byUsername!.Username);
        Assert.Null(missingEmployee);
        Assert.Null(missingUsername);
    }

    [Fact]
    public async Task PasswordResetWorkflow_ValidatesCodeAndChangesPassword()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var hasher = new PasswordHasher();
        var service = CreateService(context, hasher);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "555555");

        var challenge = await service.CreatePasswordResetChallengeAsync(employee.Id);
        var malformedCode = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, "123", "666666"));
        var invalidCode = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, "000000", "666666"));
        var weakPassword = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, challenge.Code, "123abc"));

        await service.CompletePasswordResetAsync(employee.Id, challenge.Code, "666666");
        var storedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.Equal(["Enter the 6-digit code from your email."], malformedCode.Errors["code"]);
        Assert.Equal(["The password code is invalid."], invalidCode.Errors["code"]);
        Assert.Equal("Password must contain exactly 6 digits.", weakPassword.Message);
        Assert.True(hasher.VerifyPassword("666666", storedAccount.PasswordHash));
        Assert.False(hasher.VerifyPassword("555555", storedAccount.PasswordHash));
        Assert.Null(storedAccount.PasswordResetCodeHash);
        Assert.Null(storedAccount.PasswordResetExpiresAtUtc);
        Assert.Null(storedAccount.PasswordResetRequestedAtUtc);
    }

    [Fact]
    public async Task PasswordResetWorkflow_RejectsMissingAccountMissingChallengeAndBlankNewPassword()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "555555");

        var missingChallengeAccount = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreatePasswordResetChallengeAsync(999));
        var missingCompleteAccount = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(999, "123456", "666666"));
        var missingChallenge = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, "123456", "666666"));

        var challenge = await service.CreatePasswordResetChallengeAsync(employee.Id);
        var blankPassword = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, challenge.Code, "   "));

        Assert.Equal("The login account for this employee could not be found.", missingChallengeAccount.Message);
        Assert.Equal("The login account for this employee could not be found.", missingCompleteAccount.Message);
        Assert.Equal(["Request a new password code before changing the password."], missingChallenge.Errors["code"]);
        Assert.Equal(["New password is required."], blankPassword.Errors["newPassword"]);
    }

    [Fact]
    public async Task PasswordResetWorkflow_ExpiredCodeClearsChallengeAndThrottlePreventsImmediateRepeat()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "555555");

        var challenge = await service.CreatePasswordResetChallengeAsync(employee.Id);
        var throttled = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreatePasswordResetChallengeAsync(employee.Id));

        var trackedAccount = await context.EmployeeAccounts.SingleAsync();
        trackedAccount.PasswordResetExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var expired = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CompletePasswordResetAsync(employee.Id, challenge.Code, "666666"));
        var storedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.Equal("Please wait a moment before requesting another password code.", throttled.Message);
        Assert.Equal(["This password code expired. Request a new one."], expired.Errors["code"]);
        Assert.Null(storedAccount.PasswordResetCodeHash);
        Assert.Null(storedAccount.PasswordResetExpiresAtUtc);
        Assert.Null(storedAccount.PasswordResetRequestedAtUtc);
    }

    [Fact]
    public async Task ClearPasswordResetChallengeAsync_IgnoresMissingOrEmptyChallengeAndClearsExistingChallenge()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "555555");

        await service.ClearPasswordResetChallengeAsync(999);
        await service.ClearPasswordResetChallengeAsync(employee.Id);
        var accountBeforeChallenge = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        await service.CreatePasswordResetChallengeAsync(employee.Id);
        var challengedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        await service.ClearPasswordResetChallengeAsync(employee.Id);
        var clearedAccount = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.Null(accountBeforeChallenge.PasswordResetCodeHash);
        Assert.NotNull(challengedAccount.PasswordResetCodeHash);
        Assert.Null(clearedAccount.PasswordResetCodeHash);
        Assert.Null(clearedAccount.PasswordResetExpiresAtUtc);
        Assert.Null(clearedAccount.PasswordResetRequestedAtUtc);
    }

    [Fact]
    public async Task LoginTrackingAndLastSeen_ArePersistedAndThrottled()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "555555");

        await service.MarkLoginSucceededAsync(employee.Id);
        var afterLogin = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        await service.TouchLastSeenAsync(employee.Id);
        var afterImmediateTouch = await context.EmployeeAccounts.AsNoTracking().SingleAsync();

        Assert.NotNull(afterLogin.LastLoginAtUtc);
        Assert.NotNull(afterLogin.LastSeenAtUtc);
        Assert.Equal(afterLogin.LastSeenAtUtc, afterImmediateTouch.LastSeenAtUtc);
    }

    [Fact]
    public async Task RevokeSessionsAsync_IncrementsThePersistedSessionVersion()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var employee = await AddEmployeeAsync(context);
        await service.UpsertForEmployeeAsync(employee.Id, "worker.one", "555555");

        Assert.True(await service.RevokeSessionsAsync(employee.Id));
        Assert.True(await service.RevokeSessionsAsync(employee.Id));

        context.ChangeTracker.Clear();
        var account = await context.EmployeeAccounts.AsNoTracking().SingleAsync();
        Assert.Equal(2, account.SessionVersion);
        Assert.False(await service.RevokeSessionsAsync(999_999));
    }

    [Fact]
    public async Task GetByEmployeeIdsAsync_FiltersInvalidDuplicateAndMissingIds()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);
        var firstEmployee = await AddEmployeeAsync(context, "First", "Employee");
        var secondEmployee = await AddEmployeeAsync(context, "Second", "Employee");
        await service.UpsertForEmployeeAsync(firstEmployee.Id, "first", "111111");
        await service.UpsertForEmployeeAsync(secondEmployee.Id, "second", "222222");

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
