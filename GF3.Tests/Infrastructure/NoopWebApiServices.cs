using System.Security.Claims;
using DataAccessLayer.Models;
using WebApi.Realtime;
using WebApi.Services;

namespace GF3.Tests.Infrastructure;

internal sealed class NoopWorkflowLogService : IWorkflowLogService
{
    public Task<WorkflowLogEntryModel> LogAsync(
        ClaimsPrincipal user,
        string action,
        CancellationToken cancellationToken = default)
        => Task.FromResult(CreateEntry(action));

    public Task<WorkflowLogEntryModel> LogAsync(
        string actorRole,
        string actorName,
        int? actorEmployeeId,
        string action,
        CancellationToken cancellationToken = default)
        => Task.FromResult(CreateEntry(action));

    public Task<IReadOnlyList<WorkflowLogEntryModel>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WorkflowLogEntryModel>>(Array.Empty<WorkflowLogEntryModel>());

    private static WorkflowLogEntryModel CreateEntry(string action) => new()
    {
        OccurredAtUtc = DateTimeOffset.UtcNow,
        ActorRole = "Manager",
        ActorName = "Test",
        Action = action,
    };
}

internal sealed class NoopRealtimeNotifier : IRealtimeNotifier
{
    public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason)
        => Task.CompletedTask;

    public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null)
        => Task.CompletedTask;

    public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry)
        => Task.CompletedTask;

    public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state)
        => Task.CompletedTask;
}
