using System.Security.Claims;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.Auth;
using WebApi.Realtime;
using WebApi.Services;
using static GF3.Tests.ManagerWorkspaceSignalRTests;

namespace GF3.Tests;

public sealed class EmployeeOnlineWorkflowLogTests
{
    private const int EmployeeId = 42;
    private const string EmployeeName = "Test Employee";

    [Fact]
    public async Task FirstEmployeeConnectionPersistsAndStreamsOnlineWithActorDetails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var notifier = new RecordingRealtimeNotifier();
        var service = new WorkflowLogService(db, notifier);
        await service.UpdateSettingsAsync(true, WorkflowLogSettingsModel.AllAudience);
        var clients = new WorkflowLogRealtimeAuthorizationTests.RecordingClients();
        var hub = CreateHub(Employee(), new EmployeePresenceService(), notifier, service, clients);

        var before = DateTimeOffset.UtcNow;
        await hub.OnConnectedAsync();

        var entry = await db.WorkflowLogEntries.AsNoTracking().SingleAsync();
        Assert.Equal("Online", entry.Action);
        Assert.Equal(AuthRoles.Employee, entry.ActorRole);
        Assert.Equal(EmployeeId, entry.ActorEmployeeId);
        Assert.Equal(EmployeeName, entry.ActorName);
        Assert.InRange(entry.OccurredAtUtc, before, DateTimeOffset.UtcNow);
        Assert.Equal(TimeSpan.Zero, entry.OccurredAtUtc.Offset);
        Assert.Equal(entry.Id, Assert.Single(notifier.WorkflowLogEntries).Id);
        AssertOnlineBroadcast(clients);
    }

    [Fact]
    public async Task ExtraTabSharesOnlineIntervalAndReconnectAfterLastDisconnectLogsAgain()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var notifier = new RecordingRealtimeNotifier();
        var service = new WorkflowLogService(db, notifier);
        await service.UpdateSettingsAsync(true, WorkflowLogSettingsModel.AllAudience);
        var presence = new EmployeePresenceService();
        var clients = new WorkflowLogRealtimeAuthorizationTests.RecordingClients();
        var first = CreateHub(Employee(), presence, notifier, service, clients);
        var second = CreateHub(Employee(), presence, notifier, service, clients);
        var returning = CreateHub(Employee(), presence, notifier, service, clients);

        await first.OnConnectedAsync();
        await second.OnConnectedAsync();

        Assert.True(presence.IsEmployeeOnline(EmployeeId));
        Assert.Equal(1, await db.WorkflowLogEntries.CountAsync());
        Assert.Single(notifier.WorkflowLogEntries);
        AssertOnlineBroadcast(clients);

        await first.OnDisconnectedAsync(null);
        Assert.True(presence.IsEmployeeOnline(EmployeeId));
        AssertOnlineBroadcast(clients);

        await second.OnDisconnectedAsync(null);
        Assert.False(presence.IsEmployeeOnline(EmployeeId));
        Assert.Equal(1, await db.WorkflowLogEntries.CountAsync());

        await returning.OnConnectedAsync();

        Assert.True(presence.IsEmployeeOnline(EmployeeId));
        Assert.Equal(new[] { "Online", "Online" }, await db.WorkflowLogEntries.Select(entry => entry.Action).ToArrayAsync());
        Assert.Equal(2, notifier.WorkflowLogEntries.Count);
        Assert.Equal(new[] { true, false, true }, clients.Deliveries
            .Select(delivery => Assert.IsType<EmployeePresenceChangedMessage>(delivery.Message).IsOnline));
    }

    [Theory]
    [InlineData(false, WorkflowLogSettingsModel.AllAudience, 0)]
    [InlineData(true, WorkflowLogSettingsModel.ManagersAudience, 0)]
    [InlineData(true, WorkflowLogSettingsModel.EmployeesAudience, 1)]
    public async Task SettingsFilterOnlineLogWithoutAffectingPresence(bool enabled, string audience, int expectedCount)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var notifier = new RecordingRealtimeNotifier();
        var service = new WorkflowLogService(db, notifier);
        await service.UpdateSettingsAsync(enabled, audience);
        var presence = new EmployeePresenceService();
        var clients = new WorkflowLogRealtimeAuthorizationTests.RecordingClients();
        var hub = CreateHub(Employee(), presence, notifier, service, clients);

        await hub.OnConnectedAsync();

        Assert.True(presence.IsEmployeeOnline(EmployeeId));
        Assert.Equal(expectedCount, await db.WorkflowLogEntries.CountAsync());
        Assert.Equal(expectedCount, notifier.WorkflowLogEntries.Count);
        AssertOnlineBroadcast(clients);
    }

    [Fact]
    public async Task WorkflowLogFailureDoesNotPreventOnlineBroadcast()
    {
        var service = new ThrowingWorkflowLogService();
        var clients = new WorkflowLogRealtimeAuthorizationTests.RecordingClients();
        var presence = new EmployeePresenceService();
        var hub = CreateHub(Employee(), presence, new RecordingRealtimeNotifier(), service, clients);

        await hub.OnConnectedAsync();

        Assert.Equal(1, service.Attempts);
        Assert.True(presence.IsEmployeeOnline(EmployeeId));
        AssertOnlineBroadcast(clients);
    }

    [Theory]
    [InlineData(ManagerWorkspaceModes.Pc, false)]
    [InlineData(ManagerWorkspaceModes.Pc, true)]
    [InlineData(ManagerWorkspaceModes.Phone, true)]
    [InlineData(ManagerWorkspaceModes.Choose, true)]
    public async Task ManagerGroupsRequirePcModeAndSystemClaimForWorkflowLogs(string mode, bool systemManager)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, AuthRoles.Manager),
            new("manager_id", "7"),
            new(ManagerWorkspaceModes.ClaimType, mode),
        };
        if (systemManager)
        {
            claims.Add(new Claim(AuthPolicies.SystemManagerClaim, "true"));
        }

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var service = new ThrowingWorkflowLogService();
        var clients = new WorkflowLogRealtimeAuthorizationTests.RecordingClients();
        var hub = CreateHub(user, new EmployeePresenceService(), new RecordingRealtimeNotifier(), service, clients);
        var context = Assert.IsType<FakeHubCallerContext>(hub.Context);
        var groups = Assert.IsType<RecordingGroupManager>(hub.Groups);

        await hub.OnConnectedAsync();

        Assert.Equal(0, service.Attempts);
        Assert.Empty(clients.Deliveries);
        if (mode != ManagerWorkspaceModes.Pc)
        {
            Assert.Equal(mode == ManagerWorkspaceModes.Choose, context.Aborted);
            Assert.Empty(groups.AddedGroups);
            return;
        }

        Assert.False(context.Aborted);
        var expected = systemManager
            ? new[] { (context.ConnectionId, EmployeePresenceHub.ManagersGroupName), (context.ConnectionId, EmployeePresenceHub.SystemManagersGroupName) }
            : new[] { (context.ConnectionId, EmployeePresenceHub.ManagersGroupName) };
        Assert.Equal(expected, groups.AddedGroups);
    }

    [Fact]
    public async Task PcAndPhoneSharePresenceUntilTheLastManagerConnectionCloses()
    {
        var presence = new ManagerPresenceService();
        var notifier = new RecordingRealtimeNotifier();
        var log = new ThrowingWorkflowLogService();
        var clients = new WorkflowLogRealtimeAuthorizationTests.RecordingClients();
        EmployeePresenceHub Hub(string mode) => CreateHub(new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Role, AuthRoles.Manager), new Claim("manager_id", "7"),
            new Claim(ManagerWorkspaceModes.ClaimType, mode), new Claim(AuthPolicies.SystemManagerClaim, "true"),
        ], "test")), new EmployeePresenceService(), notifier, log, clients, presence);
        var pc = Hub(ManagerWorkspaceModes.Pc);
        var phone = Hub(ManagerWorkspaceModes.Phone);
        await pc.OnConnectedAsync();
        await phone.OnConnectedAsync();
        Assert.True(presence.IsManagerOnline(7));
        Assert.Empty(Assert.IsType<RecordingGroupManager>(phone.Groups).AddedGroups);
        await Assert.ThrowsAsync<HubException>(() => phone.SetManagerEditLocks([ManagerEditLockTargets.Schedule(1, 1)]));
        await Assert.ThrowsAsync<HubException>(() => phone.SetScheduleEditLocks([new ScheduleEditLockTarget(1, 1)]));
        await pc.OnDisconnectedAsync(null);
        Assert.True(presence.IsManagerOnline(7));
        Assert.Single(notifier.ManagerDataChanges);
        await phone.OnDisconnectedAsync(null);
        Assert.False(presence.IsManagerOnline(7));
        Assert.Equal(2, notifier.ManagerDataChanges.Count);
        Assert.Equal(0, log.Attempts);
    }

    [Fact]
    public async Task UnauthenticatedConnectionDoesNotLogOrBroadcastOnline()
    {
        var service = new ThrowingWorkflowLogService();
        var clients = new WorkflowLogRealtimeAuthorizationTests.RecordingClients();
        var presence = new EmployeePresenceService();
        var hub = CreateHub(Employee(authenticationType: null), presence, new RecordingRealtimeNotifier(), service, clients);

        await hub.OnConnectedAsync();

        Assert.Equal(0, service.Attempts);
        Assert.False(presence.IsEmployeeOnline(EmployeeId));
        Assert.Empty(clients.Deliveries);
    }

    internal static EmployeePresenceHub CreateHub(
        ClaimsPrincipal user,
        EmployeePresenceService presence,
        IRealtimeNotifier notifier,
        IWorkflowLogService service,
        IHubCallerClients<IEmployeePresenceClient> clients,
        ManagerPresenceService? managerPresence = null)
        => new(presence, managerPresence ?? new ManagerPresenceService(), new RecordingEmployeeAccountService(),
            new ManagerEditLockService(), notifier, service, NullLogger<EmployeePresenceHub>.Instance)
        {
            Context = new FakeHubCallerContext(Guid.NewGuid().ToString("N"), user),
            Groups = new RecordingGroupManager(),
            Clients = clients,
        };

    internal static ClaimsPrincipal Employee(string? authenticationType = "test")
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, AuthRoles.Employee),
            new Claim("employee_id", EmployeeId.ToString()),
            new Claim("display_name", EmployeeName),
        ], authenticationType));

    private static void AssertOnlineBroadcast(WorkflowLogRealtimeAuthorizationTests.RecordingClients clients)
    {
        var delivery = Assert.Single(clients.Deliveries);
        Assert.Equal(EmployeePresenceHub.ManagersGroupName, delivery.Group);
        var message = Assert.IsType<EmployeePresenceChangedMessage>(delivery.Message);
        Assert.True(message.IsOnline);
        Assert.Equal(EmployeeId, message.EmployeeId);
    }

    internal sealed class ThrowingWorkflowLogService : IWorkflowLogService
    {
        public int Attempts { get; private set; }

        public Task<WorkflowLogEntryModel> LogAsync(ClaimsPrincipal user, string action, CancellationToken cancellationToken)
        {
            Attempts++;
            return Task.FromException<WorkflowLogEntryModel>(new InvalidOperationException("Workflow log unavailable"));
        }

        public Task<WorkflowLogEntryModel> LogAsync(string actorRole, string actorName, int? actorEmployeeId, string action, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<WorkflowLogEntryModel>> GetRecentAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<WorkflowLogSettingsModel> GetSettingsAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<WorkflowLogSettingsModel> UpdateSettingsAsync(bool isEnabled, string audience, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<int> DeleteRangeAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<int> DeleteAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
