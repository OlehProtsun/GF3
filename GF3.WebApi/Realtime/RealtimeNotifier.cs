using DataAccessLayer.Models;
using Microsoft.AspNetCore.SignalR;
using WebApi.Contracts.WorkflowLogs;

namespace WebApi.Realtime;

public interface IRealtimeNotifier
{
    Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason);

    Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null);

    Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry);

    Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state);
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
            ChangedAtUtc = state.ChangedAtUtc,
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
