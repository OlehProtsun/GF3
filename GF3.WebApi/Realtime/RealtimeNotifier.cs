using DataAccessLayer.Models;
using Microsoft.AspNetCore.SignalR;
using WebApi.Contracts.WorkflowLogs;

namespace WebApi.Realtime;

public interface IRealtimeNotifier
{
    Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason);

    Task NotifyManagerDataChangedAsync(
        string resourceType,
        string? resourceId,
        string reason,
        int? containerId = null,
        int? graphId = null);

    Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null);

    Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry);

    Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state);

    Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state);

    Task NotifyEmployeeSessionRevokedAsync(int employeeId)
        => Task.CompletedTask;
}

public sealed class RealtimeNotifier(IHubContext<EmployeePresenceHub, IEmployeePresenceClient> hubContext)
    : IRealtimeNotifier
{
    public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason)
        => hubContext.Clients.All.ScheduleChanged(new ScheduleChangedMessage
        {
            ContainerId = containerId,
            GraphId = graphId,
            Reason = reason,
            ChangedAtUtc = DateTimeOffset.UtcNow,
        });

    public Task NotifyManagerDataChangedAsync(
        string resourceType,
        string? resourceId,
        string reason,
        int? containerId = null,
        int? graphId = null)
        => hubContext.Clients.All.ManagerDataChanged(new ManagerDataChangedMessage
        {
            ResourceType = resourceType,
            ResourceId = resourceId,
            ContainerId = containerId,
            GraphId = graphId,
            Reason = reason,
            ChangedAtUtc = DateTimeOffset.UtcNow,
        });

    public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null)
        => hubContext.Clients.All.ShiftSwapsChanged(new ShiftSwapsChangedMessage
        {
            ContainerId = containerId,
            GraphId = graphId,
            ScheduleId = scheduleId,
            ShiftSwapId = shiftSwapId,
            Reason = reason,
            ChangedAtUtc = DateTimeOffset.UtcNow,
        });

    public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry)
        => hubContext.Clients.All.WorkflowLogCreated(ToDto(entry));

    public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state)
        => hubContext.Clients.All.ScheduleEditLockChanged(new ScheduleEditLockChangedMessage
        {
            ContainerId = state.ContainerId,
            GraphId = state.GraphId,
            IsLocked = state.IsLocked,
            LockedBy = state.LockedBy,
            LockedByManagerId = state.LockedByManagerId,
            ChangedAtUtc = state.ChangedAtUtc,
        });

    public async Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state)
    {
        await hubContext.Clients.All.ManagerEditLockChanged(new ManagerEditLockChangedMessage
        {
            ResourceType = state.ResourceType,
            ResourceId = state.ResourceId,
            ContainerId = state.ContainerId,
            GraphId = state.GraphId,
            IsLocked = state.IsLocked,
            LockedBy = state.LockedBy,
            LockedByManagerId = state.LockedByManagerId,
            ChangedAtUtc = state.ChangedAtUtc,
        }).ConfigureAwait(false);

        if (state.ResourceType == ManagerEditResourceTypes.Schedule &&
            state.ContainerId is > 0 &&
            state.GraphId is > 0)
        {
            await NotifyScheduleEditLockChangedAsync(new ScheduleEditLockState(
                state.ContainerId.Value,
                state.GraphId.Value,
                state.IsLocked,
                state.LockedBy,
                state.ChangedAtUtc,
                state.LockedByManagerId)).ConfigureAwait(false);
        }
    }

    public Task NotifyEmployeeSessionRevokedAsync(int employeeId)
        => hubContext.Clients
            .Group(EmployeePresenceHub.GetEmployeeGroupName(employeeId))
            .SessionRevoked(new EmployeeSessionRevokedMessage
            {
                EmployeeId = employeeId,
                RevokedAtUtc = DateTimeOffset.UtcNow,
            });

    private static WorkflowLogDto ToDto(WorkflowLogEntryModel entry) => new()
    {
        Id = entry.Id,
        OccurredAtUtc = entry.OccurredAtUtc,
        ActorRole = entry.ActorRole,
        ActorEmployeeId = entry.ActorEmployeeId,
        ActorName = entry.ActorName,
        Action = entry.Action,
    };
}
