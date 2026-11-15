using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;

namespace GF3.Tests.Infrastructure;

internal static class TestEmployeeFacadeFactory
{
    public static EmployeeFacade Create(
        IEmployeeService employeeService,
        IReadOnlyDictionary<int, EmployeeAccountModel>? accounts = null,
        IReadOnlySet<int>? onlineEmployeeIds = null)
        => new(
            employeeService,
            new TestEmployeeAccountService(accounts),
            new TestEmployeePresenceService(onlineEmployeeIds));

    private sealed class TestEmployeeAccountService : IEmployeeAccountService
    {
        private readonly Dictionary<int, EmployeeAccountModel> _accounts;

        public TestEmployeeAccountService(IReadOnlyDictionary<int, EmployeeAccountModel>? accounts)
        {
            _accounts = accounts?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        }

        public Task<EmployeeAccountModel?> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
            => Task.FromResult(_accounts.GetValueOrDefault(employeeId));

        public Task<IReadOnlyDictionary<int, EmployeeAccountModel>> GetByEmployeeIdsAsync(IEnumerable<int> employeeIds, CancellationToken ct = default)
        {
            var idSet = employeeIds.ToHashSet();
            IReadOnlyDictionary<int, EmployeeAccountModel> result = _accounts
                .Where(pair => idSet.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            return Task.FromResult(result);
        }

        public Task<EmployeeAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => Task.FromResult(_accounts.Values.FirstOrDefault(account =>
                string.Equals(account.Username, username, StringComparison.OrdinalIgnoreCase)));

        public Task<EmployeeAccountModel?> UpsertForEmployeeAsync(int employeeId, string? username, string? password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return GetByEmployeeIdAsync(employeeId, ct);
            }

            var account = _accounts.GetValueOrDefault(employeeId) ?? new EmployeeAccountModel
            {
                Id = employeeId,
                EmployeeId = employeeId,
                PasswordHash = string.Empty,
                PasswordUpdatedAtUtc = DateTimeOffset.UtcNow,
            };

            account.Username = username.Trim();
            _accounts[employeeId] = account;
            return Task.FromResult<EmployeeAccountModel?>(account);
        }

        public Task MarkLoginSucceededAsync(int employeeId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task TouchLastSeenAsync(int employeeId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<EmployeePasswordResetChallengeDto> CreatePasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
            => Task.FromResult(new EmployeePasswordResetChallengeDto
            {
                Code = "123456",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
            });

        public Task CompletePasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task ClearPasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class TestEmployeePresenceService : IEmployeePresenceService
    {
        private readonly IReadOnlySet<int> _onlineEmployeeIds;

        public TestEmployeePresenceService(IReadOnlySet<int>? onlineEmployeeIds)
        {
            _onlineEmployeeIds = onlineEmployeeIds ?? new HashSet<int>();
        }

        public bool IsEmployeeOnline(int employeeId)
            => _onlineEmployeeIds.Contains(employeeId);

        public IReadOnlyDictionary<int, bool> GetOnlineStates(IEnumerable<int> employeeIds)
            => employeeIds.Distinct().ToDictionary(employeeId => employeeId, IsEmployeeOnline);

        public EmployeePresenceChange ConnectEmployee(int employeeId, string connectionId, DateTimeOffset connectedAtUtc)
            => new(employeeId, true, true, connectedAtUtc);

        public EmployeePresenceChange? DisconnectConnection(string connectionId, DateTimeOffset disconnectedAtUtc)
            => null;
    }
}
