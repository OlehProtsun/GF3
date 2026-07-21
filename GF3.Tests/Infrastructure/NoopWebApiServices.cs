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

    public Task<IReadOnlyList<WorkflowLogEntryModel>> GetRecentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WorkflowLogEntryModel>>(Array.Empty<WorkflowLogEntryModel>());

    public Task<WorkflowLogSettingsModel> GetSettingsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new WorkflowLogSettingsModel());

    public Task<WorkflowLogSettingsModel> UpdateSettingsAsync(
        bool isEnabled,
        string audience,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new WorkflowLogSettingsModel { IsEnabled = isEnabled, Audience = audience });

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<int> DeleteRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

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

    public Task NotifyManagerDataChangedAsync(
        string resourceType,
        string? resourceId,
        string reason,
        int? containerId = null,
        int? graphId = null)
        => Task.CompletedTask;

    public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null)
        => Task.CompletedTask;

    public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry)
        => Task.CompletedTask;

    public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state)
        => Task.CompletedTask;

    public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state)
        => Task.CompletedTask;
}
