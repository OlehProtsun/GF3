using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Managers;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Security;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;

namespace GF3.Tests;

public sealed class AuthServiceRoutingTests
{
    [Fact]
    public async Task AuthenticateAsync_ManagerAccountWinsAndRecordsManagerLogin()
    {
        var managerService = new FakeManagerAccountService
        {
            Account = new ManagerAccountModel
            {
                Id = 9,
                UserName = "manager",
                DisplayName = "Main Manager",
            },
            AuthenticatedAccount = new ManagerAccountModel
            {
                Id = 9,
                UserName = "manager",
                DisplayName = "Main Manager",
            },
        };
        var employeeAccountService = new FakeEmployeeAccountService();
        var authService = CreateService(managerService, employeeAccountService);

        var session = await authService.AuthenticateAsync(" manager ", "123456");

        Assert.NotNull(session);
        Assert.Equal(AuthService.ManagerRole, session!.Role);
        Assert.Equal("manager", session.UserName);
        Assert.Equal("Main Manager", session.DisplayName);
        Assert.Equal(9, session.ManagerId);
        Assert.Null(session.EmployeeId);
        Assert.Equal(9, managerService.MarkedLoginManagerId);
        Assert.Null(employeeAccountService.MarkedLoginEmployeeId);
    }

    [Fact]
    public async Task AuthenticateAsync_EmployeeAccountRequiresValidPasswordAndExistingEmployee()
    {
        var hasher = new PasswordHasher();
        var employeeAccountService = new FakeEmployeeAccountService();
        employeeAccountService.AccountByUsername["worker"] = new EmployeeAccountModel
        {
            Id = 1,
            EmployeeId = 5,
            Username = "worker",
            PasswordHash = hasher.HashPassword("824681"),
            PasswordUpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        var employeeService = new FakeEmployeeService();
        employeeService.EmployeeById[5] = new EmployeeModel
        {
            Id = 5,
            FirstName = "Casey",
            LastName = "Worker",
            Email = "casey@example.com",
        };
        var authService = CreateService(new FakeManagerAccountService(), employeeAccountService, employeeService, hasher);

        var wrongPassword = await authService.AuthenticateAsync("worker", "999999");
        var session = await authService.AuthenticateAsync(" worker ", "824681");

        Assert.Null(wrongPassword);
        Assert.NotNull(session);
        Assert.Equal(AuthService.EmployeeRole, session!.Role);
        Assert.Equal("worker", session.UserName);
        Assert.Equal("Casey Worker", session.DisplayName);
        Assert.Equal(5, session.EmployeeId);
        Assert.Null(session.ManagerId);
        Assert.Equal(5, employeeAccountService.MarkedLoginEmployeeId);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNullWhenEmployeeRecordIsMissing()
    {
        var hasher = new PasswordHasher();
        var employeeAccountService = new FakeEmployeeAccountService();
        employeeAccountService.AccountByUsername["orphan"] = new EmployeeAccountModel
        {
            Id = 1,
            EmployeeId = 99,
            Username = "orphan",
            PasswordHash = hasher.HashPassword("824681"),
            PasswordUpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        var authService = CreateService(new FakeManagerAccountService(), employeeAccountService, new FakeEmployeeService(), hasher);

        var session = await authService.AuthenticateAsync("orphan", "824681");

        Assert.Null(session);
        Assert.Null(employeeAccountService.MarkedLoginEmployeeId);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("123abc")]
    [InlineData("1234567")]
    public async Task AuthenticateAsync_RejectsPasswordsOutsideNumericPolicy(string password)
    {
        var managerService = new FakeManagerAccountService
        {
            AuthenticatedAccount = new ManagerAccountModel
            {
                Id = 9,
                UserName = "manager",
                DisplayName = "Main Manager",
            },
        };
        var authService = CreateService(managerService, new FakeEmployeeAccountService());

        var session = await authService.AuthenticateAsync("manager", password);

        Assert.Null(session);
    }

    [Fact]
    public async Task PasswordRecovery_RoutesManagerBeforeEmployee()
    {
        var managerService = new FakeManagerAccountService
        {
            Account = new ManagerAccountModel
            {
                Id = 9,
                UserName = "shared",
                DisplayName = "Shared Manager",
            },
        };
        var employeeAccountService = new FakeEmployeeAccountService();
        employeeAccountService.AccountByUsername["shared"] = new EmployeeAccountModel
        {
            Id = 1,
            EmployeeId = 5,
            Username = "shared",
            PasswordHash = "hash",
        };
        var employeeProfileService = new FakeEmployeeProfileService();
        var authService = CreateService(managerService, employeeAccountService, employeeProfileService: employeeProfileService);

        var dispatch = await authService.SendPasswordResetCodeAsync(" shared ");
        await authService.ConfirmPasswordResetAsync("shared", "123456", "654321");

        Assert.Equal("manager", dispatch.DeliveryHint);
        Assert.Equal(["shared"], managerService.PasswordResetDispatchUsernames);
        Assert.Equal([("shared", "123456", "654321")], managerService.ConfirmedPasswordResets);
        Assert.Empty(employeeProfileService.DispatchedEmployeeIds);
        Assert.Empty(employeeProfileService.ConfirmedResets);
    }

    [Fact]
    public async Task PasswordRecovery_EmployeePathValidatesUsernameAndRecoveryEmail()
    {
        var employeeAccountService = new FakeEmployeeAccountService();
        var employeeService = new FakeEmployeeService();
        var authService = CreateService(new FakeManagerAccountService(), employeeAccountService, employeeService);

        var blankUsername = await Assert.ThrowsAsync<ValidationException>(() =>
            authService.SendPasswordResetCodeAsync(" "));
        var missingAccount = await Assert.ThrowsAsync<ValidationException>(() =>
            authService.SendPasswordResetCodeAsync("missing"));

        employeeAccountService.AccountByUsername["worker"] = new EmployeeAccountModel
        {
            Id = 1,
            EmployeeId = 5,
            Username = "worker",
            PasswordHash = "hash",
        };
        employeeService.EmployeeById[5] = new EmployeeModel
        {
            Id = 5,
            FirstName = "Casey",
            LastName = "Worker",
            Email = "",
        };

        var missingEmail = await Assert.ThrowsAsync<ValidationException>(() =>
            authService.SendPasswordResetCodeAsync("worker"));

        Assert.Equal(["Enter the username for this account."], blankUsername.Errors["username"]);
        Assert.Equal(["No account was found for this username."], missingAccount.Errors["username"]);
        Assert.Equal([
            "This account does not have a recovery email. Ask a manager to add one before using password recovery.",
        ], missingEmail.Errors["recoveryEmail"]);
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_RoutesEmployeeResetThroughProfileService()
    {
        var employeeAccountService = new FakeEmployeeAccountService();
        employeeAccountService.AccountByUsername["worker"] = new EmployeeAccountModel
        {
            Id = 1,
            EmployeeId = 5,
            Username = "worker",
            PasswordHash = "hash",
        };
        var employeeService = new FakeEmployeeService();
        employeeService.EmployeeById[5] = new EmployeeModel
        {
            Id = 5,
            FirstName = "Casey",
            LastName = "Worker",
            Email = "casey@example.com",
        };
        var employeeProfileService = new FakeEmployeeProfileService();
        var authService = CreateService(
            new FakeManagerAccountService(),
            employeeAccountService,
            employeeService,
            employeeProfileService: employeeProfileService);

        await authService.ConfirmPasswordResetAsync("worker", "123456", "654321");

        Assert.Equal([(5, "123456", "654321")], employeeProfileService.ConfirmedResets);
    }

    private static AuthService CreateService(
        FakeManagerAccountService managerAccountService,
        FakeEmployeeAccountService employeeAccountService,
        FakeEmployeeService? employeeService = null,
        PasswordHasher? passwordHasher = null,
        FakeEmployeeProfileService? employeeProfileService = null)
        => new(
            managerAccountService,
            employeeAccountService,
            employeeProfileService ?? new FakeEmployeeProfileService(),
            employeeService ?? new FakeEmployeeService(),
            passwordHasher ?? new PasswordHasher());

    private sealed class FakeManagerAccountService : IManagerAccountService
    {
        public ManagerAccountModel? Account { get; set; }

        public ManagerAccountModel? AuthenticatedAccount { get; set; }

        public int? MarkedLoginManagerId { get; private set; }

        public List<string> PasswordResetDispatchUsernames { get; } = [];

        public List<(string Username, string Code, string NewPassword)> ConfirmedPasswordResets { get; } = [];

        public Task<ManagerAccountModel?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
            => Task.FromResult(AuthenticatedAccount);

        public Task<ManagerAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => Task.FromResult(
                string.Equals(Account?.UserName, username.Trim(), StringComparison.OrdinalIgnoreCase)
                    ? Account
                    : null);

        public Task MarkLoginSucceededAsync(int managerId, CancellationToken ct = default)
        {
            MarkedLoginManagerId = managerId;
            return Task.CompletedTask;
        }

        public Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default)
        {
            PasswordResetDispatchUsernames.Add(username);
            return Task.FromResult(new PasswordResetDispatchResult
            {
                DeliveryHint = "manager",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
            });
        }

        public Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default)
        {
            ConfirmedPasswordResets.Add((username, code, newPassword));
            return Task.CompletedTask;
        }

        public Task<ManagerProfileDto> CreateAsync(CreateManagerAccountRequest request, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ManagerProfileDto> DeleteAsync(int managerId, int? currentManagerId, string? currentUserName, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ManagerProfileDto> GetProfileAsync(int? managerId, string? userName, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ManagerProfileDto>> ListProfilesAsync(CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ManagerProfileDto> UpdateProfileAsync(int? managerId, string? userName, UpdateManagerProfileRequest request, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeEmployeeAccountService : IEmployeeAccountService
    {
        public Dictionary<string, EmployeeAccountModel> AccountByUsername { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int? MarkedLoginEmployeeId { get; private set; }

        public Task<EmployeeAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => Task.FromResult(AccountByUsername.GetValueOrDefault(username.Trim()));

        public Task MarkLoginSucceededAsync(int employeeId, CancellationToken ct = default)
        {
            MarkedLoginEmployeeId = employeeId;
            return Task.CompletedTask;
        }

        public Task ClearPasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task CompletePasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<EmployeePasswordResetChallengeDto> CreatePasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<EmployeeAccountModel?> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<int, EmployeeAccountModel>> GetByEmployeeIdsAsync(IEnumerable<int> employeeIds, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task TouchLastSeenAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<EmployeeAccountModel?> UpsertForEmployeeAsync(int employeeId, string? username, string? password, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeEmployeeProfileService : IEmployeeProfileService
    {
        public List<int> DispatchedEmployeeIds { get; } = [];

        public List<(int EmployeeId, string Code, string NewPassword)> ConfirmedResets { get; } = [];

        public Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(int employeeId, CancellationToken ct = default)
        {
            DispatchedEmployeeIds.Add(employeeId);
            return Task.FromResult(new PasswordResetDispatchResult
            {
                DeliveryHint = "employee",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
            });
        }

        public Task ConfirmPasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)
        {
            ConfirmedResets.Add((employeeId, code, newPassword));
            return Task.CompletedTask;
        }

        public Task<EmployeeProfileDto> GetAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<EmployeeProfileDto> UpdateContactAsync(int employeeId, string? recoveryEmail, string? phone, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeEmployeeService : IEmployeeService
    {
        public Dictionary<int, EmployeeModel> EmployeeById { get; } = [];

        public Task<EmployeeModel?> GetAsync(int id, CancellationToken ct = default)
            => Task.FromResult(EmployeeById.GetValueOrDefault(id));

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

        public Task<EmployeeModel> UpdateContactAsync(int employeeId, string? email, string? phone, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
