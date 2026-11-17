using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.SignalR;
using WebApi.Auth;
using WebApi.Contracts.WorkflowLogs;
using WebApi.Realtime;
using static GF3.Tests.EmployeeOnlineWorkflowLogTests;
using static GF3.Tests.ManagerWorkspaceSignalRTests;

namespace GF3.Tests;

public sealed class WorkflowLogRealtimeAuthorizationTests
{
    [Fact]
    public async Task WorkflowLogUsesOnlySystemManagerGroupAndPreservesDto()
    {
        var context = new RecordingHubContext();
        var notifier = new RealtimeNotifier(context);
        var entry = new WorkflowLogEntryModel
        {
            Id = 7,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ActorRole = AuthRoles.Employee,
            ActorEmployeeId = 42,
            ActorName = "Test Employee",
            Action = "Online",
        };

        await notifier.NotifyWorkflowLogCreatedAsync(entry);

        var delivery = Assert.Single(context.Recorder.Deliveries);
        Assert.Equal(EmployeePresenceHub.SystemManagersGroupName, delivery.Group);
        var dto = Assert.IsType<WorkflowLogDto>(delivery.Message);
        Assert.Equal(entry.Id, dto.Id);
        Assert.Equal(entry.OccurredAtUtc, dto.OccurredAtUtc);
        Assert.Equal(entry.ActorRole, dto.ActorRole);
        Assert.Equal(entry.ActorEmployeeId, dto.ActorEmployeeId);
        Assert.Equal(entry.ActorName, dto.ActorName);
        Assert.Equal(entry.Action, dto.Action);
    }

    [Fact]
    public async Task ManagerDataAndEmployeePresenceKeepTheirExistingDestinations()
    {
        var context = new RecordingHubContext();
        var notifier = new RealtimeNotifier(context);
        var hub = CreateHub(Employee(), new EmployeePresenceService(), notifier,
            new ThrowingWorkflowLogService(), context.Recorder);

        await notifier.NotifyManagerDataChangedAsync("manager-profile", "7", "updated");
        await hub.OnConnectedAsync();

        Assert.Collection(context.Recorder.Deliveries,
            delivery =>
            {
                Assert.Null(delivery.Group);
                var message = Assert.IsType<ManagerDataChangedMessage>(delivery.Message);
                Assert.Equal("manager-profile", message.ResourceType);
                Assert.Equal("7", message.ResourceId);
                Assert.Equal("updated", message.Reason);
            },
            delivery =>
            {
                Assert.Equal(EmployeePresenceHub.ManagersGroupName, delivery.Group);
                Assert.True(Assert.IsType<EmployeePresenceChangedMessage>(delivery.Message).IsOnline);
            });
    }

    private sealed class RecordingHubContext : IHubContext<EmployeePresenceHub, IEmployeePresenceClient>
    {
        public RecordingClients Recorder { get; } = new();
        public IHubClients<IEmployeePresenceClient> Clients => Recorder;
        public IGroupManager Groups { get; } = new RecordingGroupManager();
    }

    internal sealed class RecordingClients : IHubCallerClients<IEmployeePresenceClient>
    {
        public List<(string? Group, object Message)> Deliveries { get; } = [];

        public IEmployeePresenceClient All => new RecordingClient(null, Deliveries);
        public IEmployeePresenceClient Group(string groupName) => new RecordingClient(groupName, Deliveries);
        public IEmployeePresenceClient Caller => throw new NotSupportedException();
        public IEmployeePresenceClient Others => throw new NotSupportedException();
        public IEmployeePresenceClient OthersInGroup(string groupName) => throw new NotSupportedException();
        public IEmployeePresenceClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IEmployeePresenceClient Client(string connectionId) => throw new NotSupportedException();
        public IEmployeePresenceClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IEmployeePresenceClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IEmployeePresenceClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IEmployeePresenceClient User(string userId) => throw new NotSupportedException();
        public IEmployeePresenceClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class RecordingClient(string? group, List<(string? Group, object Message)> deliveries) : IEmployeePresenceClient
    {
        public Task WorkflowLogCreated(WorkflowLogDto message) => Record(message);
        public Task ManagerDataChanged(ManagerDataChangedMessage message) => Record(message);
        public Task PresenceChanged(EmployeePresenceChangedMessage message) => Record(message);
        public Task ScheduleChanged(ScheduleChangedMessage message) => throw new NotSupportedException();
        public Task ShiftSwapsChanged(ShiftSwapsChangedMessage message) => throw new NotSupportedException();
        public Task ScheduleEditLockChanged(ScheduleEditLockChangedMessage message) => throw new NotSupportedException();
        public Task ManagerEditLockChanged(ManagerEditLockChangedMessage message) => throw new NotSupportedException();

        private Task Record(object message)
        {
            deliveries.Add((group, message));
            return Task.CompletedTask;
        }
    }
}
