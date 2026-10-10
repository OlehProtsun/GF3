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

public sealed class RealtimeMiddlewareAndGenericCoverageTests
{
    [Fact]
    public async Task WorkflowLog_IsSentOnlyToManagers()
    {
        var all = new RecordingPresenceClient();
        var managers = new RecordingPresenceClient();
        var notifier = new RealtimeNotifier(new FakeHubContext(all, managers));
        await notifier.NotifyWorkflowLogCreatedAsync(new WorkflowLogEntryModel
        {
            Id = 42, ActorRole = "Manager", ActorName = "Manager", Action = "Private action",
            OccurredAtUtc = DateTimeOffset.UtcNow,
        });
        Assert.Empty(all.WorkflowLogMessages);
        Assert.Equal(42, Assert.Single(managers.WorkflowLogMessages).Id);
    }
    [Fact]
    public async Task EmployeePresenceHub_ConnectsAndDisconnectsEmployeePresence()
    {
        var client = new RecordingPresenceClient();
        var groups = new RecordingGroupManager();
        var accountService = new RecordingEmployeeAccountService
        {
            Account = new BusinessLogicLayer.Contracts.Employees.EmployeeAccountModel
            {
                EmployeeId = 12,
                Username = "worker",
                PasswordHash = "hash",
                LastLoginAtUtc = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
            },
        };
        var hub = CreateHub(
            CreateUser(AuthRoles.Employee, employeeId: 12),
            client,
            groups,
            accountService: accountService);

        await hub.OnConnectedAsync();
        await hub.OnDisconnectedAsync(null);

        Assert.Equal([("connection-1", "presence:employee:12")], groups.AddedGroups);
        Assert.Equal([12, 12], accountService.TouchLastSeenEmployeeIds);
        Assert.Equal([true, false], client.PresenceMessages.Select(message => message.IsOnline));
        Assert.Equal(12, client.PresenceMessages[0].EmployeeId);
        Assert.Equal(new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero), client.PresenceMessages[0].LastLoginAtUtc);
    }

    [Fact]
    public async Task EmployeePresenceHub_ConnectsManagerAndPublishesEditLockChanges()
    {
        var client = new RecordingPresenceClient();
        var groups = new RecordingGroupManager();
        var notifier = new RecordingRealtimeNotifier();
        var hub = CreateHub(
            CreateUser(AuthRoles.Manager, managerId: 3),
            client,
            groups,
            notifier: notifier);
        var target = ManagerEditLockTargets.Schedule(1, 2);

        await hub.OnConnectedAsync();
        var states = await hub.SetManagerEditLocks([target]);
        var scheduleStates = await hub.SetScheduleEditLocks([new ScheduleEditLockTarget(1, 2)]);
        await hub.OnDisconnectedAsync(null);

        Assert.Equal([("connection-1", "presence:managers")], groups.AddedGroups);
        Assert.Single(states);
        Assert.True(states[0].IsLocked);
        Assert.Equal(3, states[0].LockedByManagerId);
        Assert.Single(scheduleStates);
        Assert.True(scheduleStates[0].IsLocked);
        Assert.Contains(notifier.ManagerDataChanges, message => message.ResourceType == ManagerEditResourceTypes.ManagerProfile && message.ResourceId == "3");
        Assert.Equal([true, false], notifier.ManagerEditLockChanges.Select(change => change.IsLocked));
    }

    [Fact]
    public async Task EmployeePresenceHub_RejectsEditLocksForEmployees()
    {
        var hub = CreateHub(
            CreateUser(AuthRoles.Employee, employeeId: 12),
            new RecordingPresenceClient(),
            new RecordingGroupManager());

        var exception = await Assert.ThrowsAsync<HubException>(() =>
            hub.SetManagerEditLocks([ManagerEditLockTargets.Schedule(1, 2)]));

        Assert.Equal("Only managers can lock records for editing.", exception.Message);
    }

    [Fact]
    public async Task RealtimeNotifier_MapsEveryNotificationToHubClientMessages()
    {
        var client = new RecordingPresenceClient();
        var notifier = new RealtimeNotifier(new FakeHubContext(client));
        var changedAtUtc = new DateTimeOffset(2026, 5, 13, 12, 0, 0, TimeSpan.Zero);

        await notifier.NotifyScheduleChangedAsync(1, 2, "schedule-updated");
        await notifier.NotifyManagerDataChangedAsync("employee", "7", "employee-updated", containerId: 1, graphId: 2);
        await notifier.NotifyShiftSwapsChangedAsync(1, 2, 2, "swap-updated", shiftSwapId: 9);
        await notifier.NotifyWorkflowLogCreatedAsync(new WorkflowLogEntryModel
        {
            Id = 11,
            OccurredAtUtc = changedAtUtc,
            ActorRole = "Manager",
            ActorName = "Chief",
            Action = "Created graph.",
        });
        await notifier.NotifyScheduleEditLockChangedAsync(new ScheduleEditLockState(1, 2, true, "Chief", changedAtUtc, 3));
        await notifier.NotifyManagerEditLockChangedAsync(new ManagerEditLockState(
            ManagerEditResourceTypes.Schedule,
            "1:2",
            1,
            2,
            IsLocked: false,
            LockedBy: null,
            LockedByManagerId: null,
            changedAtUtc));

        Assert.Equal((1, 2, "schedule-updated"), client.ScheduleMessages.Single().AsTuple());
        Assert.Equal(("employee", "7", 1, 2, "employee-updated"), client.ManagerDataMessages.Single().AsTuple());
        Assert.Equal((1, 2, 2, 9, "swap-updated"), client.ShiftSwapMessages.Single().AsTuple());
        Assert.Equal(11, client.WorkflowLogMessages.Single().Id);
        Assert.Equal([true, false], client.ScheduleEditLockMessages.Select(message => message.IsLocked));
        Assert.Equal(ManagerEditResourceTypes.Schedule, client.ManagerEditLockMessages.Single().ResourceType);
    }

    [Fact]
    public async Task EmployeePresenceMiddleware_TouchesOnlyAuthenticatedEmployeesAndThrottlesWrites()
    {
        var nextCallCount = 0;
        var middleware = new EmployeePresenceMiddleware(
            context =>
            {
                nextCallCount += 1;
                return Task.CompletedTask;
            },
            NullLogger<EmployeePresenceMiddleware>.Instance);
        var accountService = new RecordingEmployeeAccountService();
        var employeeContext = CreateHttpContext(CreateUser(AuthRoles.Employee, employeeId: 12));
        var managerContext = CreateHttpContext(CreateUser(AuthRoles.Manager, managerId: 3));
        var anonymousContext = CreateHttpContext(new ClaimsPrincipal(new ClaimsIdentity()));

        await middleware.InvokeAsync(employeeContext, accountService);
        await middleware.InvokeAsync(employeeContext, accountService);
        await middleware.InvokeAsync(managerContext, accountService);
        await middleware.InvokeAsync(anonymousContext, accountService);

        Assert.Equal(4, nextCallCount);
        Assert.Equal([12], accountService.TouchLastSeenEmployeeIds);
    }

    [Fact]
    public async Task EmployeePresenceMiddleware_RollsBackThrottleReservation_WhenTouchFails()
    {
        var middleware = new EmployeePresenceMiddleware(
            _ => Task.CompletedTask,
            NullLogger<EmployeePresenceMiddleware>.Instance);
        var accountService = new RecordingEmployeeAccountService
        {
            FailNextTouch = true,
        };
        var context = CreateHttpContext(CreateUser(AuthRoles.Employee, employeeId: 12));

        await middleware.InvokeAsync(context, accountService);
        await middleware.InvokeAsync(context, accountService);

        Assert.Equal([12, 12], accountService.TouchLastSeenEmployeeIds);
    }

    [Fact]
    public void ManagerEditLockHttp_ReturnsConflictOnlyForOtherManagers()
    {
        var lockService = new ManagerEditLockService();
        var target = ManagerEditLockTargets.Schedule(1, 2);
        lockService.SetLocks("connection-a", managerId: 3, "Chief", [target]);
        var otherManagerController = new EmptyController();
        SetControllerContext(otherManagerController, CreateUser(AuthRoles.Manager, managerId: 4), "/api/locked");
        var ownerController = new EmptyController();
        SetControllerContext(ownerController, CreateUser(AuthRoles.Manager, managerId: 3), "/api/locked");

        var conflict = ManagerEditLockHttp.CreateConflictResult(otherManagerController, lockService, target, "This schedule");
        var ownerResult = ManagerEditLockHttp.CreateConflictResult(ownerController, lockService, target, "This schedule");
        var disabledResult = ManagerEditLockHttp.CreateConflictResult(ownerController, null, target, "This schedule");

        var conflictResult = Assert.IsType<ConflictObjectResult>(conflict);
        var problem = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("edit_lock_conflict", problem.Type);
        Assert.Equal("This schedule is currently being edited by Chief.", problem.Detail);
        Assert.Equal("/api/locked", problem.Instance);
        Assert.Null(ownerResult);
        Assert.Null(disabledResult);
    }

    [Fact]
    public async Task GenericService_DelegatesCrudOperationsToRepository()
    {
        var entity = new ContainerModel { Id = 7, Name = "Main" };
        var repository = new RecordingRepository<ContainerModel>
        {
            Entity = entity,
            Entities = [entity],
        };
        var service = new GenericService<ContainerModel>(repository);

        var byId = await service.GetAsync(7);
        var all = await service.GetAllAsync();
        var created = await service.CreateAsync(entity);
        await service.UpdateAsync(entity);
        await service.DeleteAsync(7);

        Assert.Same(entity, byId);
        Assert.Equal([entity], all);
        Assert.Same(entity, created);
        Assert.Equal(7, repository.LastGetId);
        Assert.Same(entity, repository.LastAdded);
        Assert.Same(entity, repository.LastUpdated);
        Assert.Equal(7, repository.LastDeletedId);
    }

    private static DefaultHttpContext CreateHttpContext(ClaimsPrincipal user)
        => new()
        {
            User = user,
        };

    private static ClaimsPrincipal CreateUser(string role, int? employeeId = null, int? managerId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, role == AuthRoles.Employee ? "employee" : "manager"),
            new(ClaimTypes.Role, role),
        };

        if (employeeId.HasValue)
        {
            claims.Add(new Claim("employee_id", employeeId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (managerId.HasValue)
        {
            claims.Add(new Claim("manager_id", managerId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role));
    }

    private static void SetControllerContext(ControllerBase controller, ClaimsPrincipal user, string path)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user,
            },
        };
        controller.HttpContext.Request.Path = path;
    }

    private sealed class EmptyController : ControllerBase;

    private static EmployeePresenceHub CreateHub(
        ClaimsPrincipal user,
        RecordingPresenceClient client,
        RecordingGroupManager groups,
        RecordingEmployeeAccountService? accountService = null,
        RecordingRealtimeNotifier? notifier = null)
        => new(
            new EmployeePresenceService(),
            new ManagerPresenceService(),
            accountService ?? new RecordingEmployeeAccountService(),
            new ManagerEditLockService(),
            notifier ?? new RecordingRealtimeNotifier(),
            NullLogger<EmployeePresenceHub>.Instance)
        {
            Context = new FakeHubCallerContext("connection-1", user),
            Clients = new FakeHubCallerClients(client),
            Groups = groups,
        };

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

    private sealed class RecordingRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : class
    {
        public TEntity? Entity { get; init; }
        public List<TEntity> Entities { get; init; } = [];
        public int? LastGetId { get; private set; }
        public TEntity? LastAdded { get; private set; }
        public TEntity? LastUpdated { get; private set; }
        public int? LastDeletedId { get; private set; }

        public Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            LastGetId = id;
            return Task.FromResult(Entity);
        }

        public Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Entities);

        public Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
        {
            LastAdded = entity;
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(TEntity entity, CancellationToken ct = default)
        {
            LastUpdated = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
        {
            LastDeletedId = id;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHubContext(RecordingPresenceClient client, RecordingPresenceClient? managers = null) : IHubContext<EmployeePresenceHub, IEmployeePresenceClient>
    {
        public IHubClients<IEmployeePresenceClient> Clients { get; } = new FakeHubClients(client, managers);

        public IGroupManager Groups { get; } = new FakeGroupManager();
    }

    private sealed class FakeHubClients(RecordingPresenceClient client, RecordingPresenceClient? managers = null) : IHubClients<IEmployeePresenceClient>
    {
        public IEmployeePresenceClient All => client;

        public IEmployeePresenceClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => client;

        public IEmployeePresenceClient Client(string connectionId) => client;

        public IEmployeePresenceClient Clients(IReadOnlyList<string> connectionIds) => client;

        public IEmployeePresenceClient Group(string groupName) => groupName == EmployeePresenceHub.ManagersGroupName ? managers ?? client : client;

        public IEmployeePresenceClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => client;

        public IEmployeePresenceClient Groups(IReadOnlyList<string> groupNames) => client;

        public IEmployeePresenceClient User(string userId) => client;

        public IEmployeePresenceClient Users(IReadOnlyList<string> userIds) => client;
    }

    private sealed class FakeHubCallerClients(RecordingPresenceClient client) : IHubCallerClients<IEmployeePresenceClient>
    {
        public IEmployeePresenceClient All => client;

        public IEmployeePresenceClient Caller => client;

        public IEmployeePresenceClient Others => client;

        public IEmployeePresenceClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => client;

        public IEmployeePresenceClient Client(string connectionId) => client;

        public IEmployeePresenceClient Clients(IReadOnlyList<string> connectionIds) => client;

        public IEmployeePresenceClient Group(string groupName) => client;

        public IEmployeePresenceClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => client;

        public IEmployeePresenceClient Groups(IReadOnlyList<string> groupNames) => client;

        public IEmployeePresenceClient OthersInGroup(string groupName) => client;

        public IEmployeePresenceClient User(string userId) => client;

        public IEmployeePresenceClient Users(IReadOnlyList<string> userIds) => client;
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
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

        public override void Abort()
        {
        }
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

    private sealed class RecordingPresenceClient : IEmployeePresenceClient
    {
        public List<EmployeePresenceChangedMessage> PresenceMessages { get; } = [];
        public List<ScheduleChangedMessage> ScheduleMessages { get; } = [];
        public List<ManagerDataChangedMessage> ManagerDataMessages { get; } = [];
        public List<ShiftSwapsChangedMessage> ShiftSwapMessages { get; } = [];
        public List<WebApi.Contracts.WorkflowLogs.WorkflowLogDto> WorkflowLogMessages { get; } = [];
        public List<ScheduleEditLockChangedMessage> ScheduleEditLockMessages { get; } = [];
        public List<ManagerEditLockChangedMessage> ManagerEditLockMessages { get; } = [];

        public Task PresenceChanged(EmployeePresenceChangedMessage message)
        {
            PresenceMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task ScheduleChanged(ScheduleChangedMessage message)
        {
            ScheduleMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task ManagerDataChanged(ManagerDataChangedMessage message)
        {
            ManagerDataMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task ShiftSwapsChanged(ShiftSwapsChangedMessage message)
        {
            ShiftSwapMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task WorkflowLogCreated(WebApi.Contracts.WorkflowLogs.WorkflowLogDto message)
        {
            WorkflowLogMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task ScheduleEditLockChanged(ScheduleEditLockChangedMessage message)
        {
            ScheduleEditLockMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task ManagerEditLockChanged(ManagerEditLockChangedMessage message)
        {
            ManagerEditLockMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}

internal static class RealtimeMessageTestExtensions
{
    public static (int ContainerId, int GraphId, string Reason) AsTuple(this ScheduleChangedMessage message)
        => (message.ContainerId, message.GraphId, message.Reason);

    public static (string ResourceType, string? ResourceId, int? ContainerId, int? GraphId, string Reason) AsTuple(this ManagerDataChangedMessage message)
        => (message.ResourceType, message.ResourceId, message.ContainerId, message.GraphId, message.Reason);

    public static (int? ContainerId, int? GraphId, int? ScheduleId, int? ShiftSwapId, string Reason) AsTuple(this ShiftSwapsChangedMessage message)
        => (message.ContainerId, message.GraphId, message.ScheduleId, message.ShiftSwapId, message.Reason);
}
