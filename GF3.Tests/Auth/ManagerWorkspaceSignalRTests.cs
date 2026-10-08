using System.Security.Claims;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Repositories.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.Auth;
using WebApi.Middleware;
using WebApi.Realtime;
using WorkflowLogEntryModel = DataAccessLayer.Models.WorkflowLogEntryModel;

namespace GF3.Tests;

public sealed class ManagerWorkspaceSignalRTests
{
    [Theory]
    [InlineData("phone")]
    [InlineData("choose")]
    public async Task RestrictedManagersCannotConnectOrAcquireEitherLock(string mode)
    {
        var context = Context(mode);
        var groups = new RecordingGroupManager(); var locks = new ManagerEditLockService();
        var hub = new EmployeePresenceHub(new EmployeePresenceService(), new ManagerPresenceService(), new RecordingEmployeeAccountService(), locks, new RecordingRealtimeNotifier(), NullLogger<EmployeePresenceHub>.Instance) { Context = context, Groups = groups };
        await hub.OnConnectedAsync(); Assert.True(context.Aborted); Assert.Empty(groups.AddedGroups);
        await Assert.ThrowsAsync<HubException>(() => hub.SetManagerEditLocks([ManagerEditLockTargets.Schedule(1, 1)]));
        await Assert.ThrowsAsync<HubException>(() => hub.SetScheduleEditLocks([new ScheduleEditLockTarget(1, 1)]));
        Assert.Empty(locks.ReleaseConnection(context.ConnectionId));
    }
    [Theory]
    [InlineData("pc")]
    [InlineData(null)]
    public async Task PcAndLegacyManagerLocksPreserveLifecycle(string? mode)
    {
        var context = Context(mode); var groups = new RecordingGroupManager(); var locks = new ManagerEditLockService();
        var hub = new EmployeePresenceHub(new EmployeePresenceService(), new ManagerPresenceService(), new RecordingEmployeeAccountService(), locks, new RecordingRealtimeNotifier(), NullLogger<EmployeePresenceHub>.Instance) { Context = context, Groups = groups };
        await hub.OnConnectedAsync(); Assert.False(context.Aborted); Assert.Single(groups.AddedGroups);
        var result = await hub.SetScheduleEditLocks([new ScheduleEditLockTarget(1, 1)]); Assert.Single(result); Assert.True(result[0].IsLocked);
        await hub.OnDisconnectedAsync(null); Assert.Empty(locks.ReleaseConnection(context.ConnectionId));
    }
    private static FakeHubCallerContext Context(string? mode)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, AuthRoles.Manager), new("manager_id", "1"), new(ClaimTypes.Name, "manager") };
        if (mode is not null) claims.Add(new(ManagerWorkspaceModes.ClaimType, mode));
        return new FakeHubCallerContext("mode-test", new ClaimsPrincipal(new ClaimsIdentity(claims, "tests")));
    }
    private sealed class RecordingEmployeeAccountService : IEmployeeAccountService
    {
        public BusinessLogicLayer.Contracts.Employees.EmployeeAccountModel? Account { get; init; }
        public bool FailNextTouch { get; set; }
        public List<int> TouchLastSeenEmployeeIds { get; } = [];

        public Task TouchLastSeenAsync(int employeeId, CancellationToken ct = default)
        {
            TouchLastSeenEmployeeIds.Add(employeeId);
            if (FailNextTouch)
            {
                FailNextTouch = false;
                throw new InvalidOperationException("touch failed");
            }

            return Task.CompletedTask;
        }

        public Task ClearPasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task CompletePasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<EmployeePasswordResetChallengeDto> CreatePasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<BusinessLogicLayer.Contracts.Employees.EmployeeAccountModel?> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
            => Task.FromResult(Account?.EmployeeId == employeeId ? Account : null);

        public Task<IReadOnlyDictionary<int, BusinessLogicLayer.Contracts.Employees.EmployeeAccountModel>> GetByEmployeeIdsAsync(IEnumerable<int> employeeIds, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<BusinessLogicLayer.Contracts.Employees.EmployeeAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task MarkLoginSucceededAsync(int employeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<BusinessLogicLayer.Contracts.Employees.EmployeeAccountModel?> UpsertForEmployeeAsync(int employeeId, string? username, string? password, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<(string ConnectionId, string GroupName)> AddedGroups { get; } = [];
        public List<(string ConnectionId, string GroupName)> RemovedGroups { get; } = [];

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            AddedGroups.Add((connectionId, groupName));
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            RemovedGroups.Add((connectionId, groupName));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHubCallerContext(string connectionId, ClaimsPrincipal user) : HubCallerContext
    {
        public override string ConnectionId { get; } = connectionId;

        public override string? UserIdentifier => null;

        public override ClaimsPrincipal? User { get; } = user;

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public bool Aborted { get; private set; }
        public override void Abort() { Aborted = true; }
    }

    private sealed class RecordingRealtimeNotifier : IRealtimeNotifier
    {
        public List<ManagerDataChangedMessage> ManagerDataChanges { get; } = [];
        public List<ManagerEditLockState> ManagerEditLockChanges { get; } = [];

        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason)
            => Task.CompletedTask;

        public Task NotifyManagerDataChangedAsync(
            string resourceType,
            string? resourceId,
            string reason,
            int? containerId = null,
            int? graphId = null)
        {
            ManagerDataChanges.Add(new ManagerDataChangedMessage
            {
                ResourceType = resourceType,
                ResourceId = resourceId,
                Reason = reason,
                ContainerId = containerId,
                GraphId = graphId,
            });
            return Task.CompletedTask;
        }

        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null)
            => Task.CompletedTask;

        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry)
            => Task.CompletedTask;

        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state)
            => Task.CompletedTask;

        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state)
        {
            ManagerEditLockChanges.Add(state);
            return Task.CompletedTask;
        }
    }
}
