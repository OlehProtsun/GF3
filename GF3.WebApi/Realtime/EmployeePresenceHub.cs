using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebApi.Auth;
using WebApi.Contracts.WorkflowLogs;

namespace WebApi.Realtime;

public interface IEmployeePresenceClient
{
    Task PresenceChanged(EmployeePresenceChangedMessage message);

    Task ScheduleChanged(ScheduleChangedMessage message);

    Task ShiftSwapsChanged(ShiftSwapsChangedMessage message);

    Task WorkflowLogCreated(WorkflowLogDto message);

    Task ScheduleEditLockChanged(ScheduleEditLockChangedMessage message);
}

public sealed class EmployeePresenceChangedMessage
{
    public int EmployeeId { get; init; }

    public bool IsOnline { get; init; }

    public DateTimeOffset ChangedAtUtc { get; init; }

    public DateTimeOffset? LastLoginAtUtc { get; init; }
}

public sealed class ScheduleChangedMessage
{
    public int ContainerId { get; init; }

    public int GraphId { get; init; }

    public string Reason { get; init; } = string.Empty;

    public DateTimeOffset ChangedAtUtc { get; init; }
}

public sealed class ShiftSwapsChangedMessage
{
    public int? ContainerId { get; init; }

    public int? GraphId { get; init; }

    public int? ScheduleId { get; init; }

    public string Reason { get; init; } = string.Empty;

    public DateTimeOffset ChangedAtUtc { get; init; }
}

public sealed class ScheduleEditLockChangedMessage
{
    public int ContainerId { get; init; }

    public int GraphId { get; init; }

    public bool IsLocked { get; init; }

    public string? LockedBy { get; init; }

    public DateTimeOffset ChangedAtUtc { get; init; }
}

/// <summary>
/// Shared realtime hub used for precise employee presence and future notification fan-out.
/// Employees connect once per browser session; managers subscribe to presence updates for live UI.
/// </summary>
[Authorize]
public sealed class EmployeePresenceHub : Hub<IEmployeePresenceClient>
{
    public const string RoutePattern = "/api/realtime/presence";

    private const string ManagersGroupName = "presence:managers";
    private readonly IEmployeePresenceService _employeePresenceService;
    private readonly IEmployeeAccountService _employeeAccountService;
    private readonly IScheduleEditLockService _scheduleEditLockService;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<EmployeePresenceHub> _logger;

    public EmployeePresenceHub(
        IEmployeePresenceService employeePresenceService,
        IEmployeeAccountService employeeAccountService,
        IScheduleEditLockService scheduleEditLockService,
        IRealtimeNotifier realtimeNotifier,
        ILogger<EmployeePresenceHub> logger)
    {
        _employeePresenceService = employeePresenceService;
        _employeeAccountService = employeeAccountService;
        _scheduleEditLockService = scheduleEditLockService;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task SetScheduleEditLocks(IReadOnlyList<ScheduleEditLockTarget> locks)
    {
        if (!string.Equals(Context.User?.FindFirstValue(ClaimTypes.Role), AuthRoles.Manager, StringComparison.Ordinal))
        {
            throw new HubException("Only managers can lock schedules for editing.");
        }

        var lockedBy = Context.User?.FindFirstValue("display_name") ?? Context.User?.Identity?.Name ?? "Manager";
        var changes = _scheduleEditLockService.SetLocks(Context.ConnectionId, lockedBy, locks);
        foreach (var change in changes)
        {
            await _realtimeNotifier.NotifyScheduleEditLockChangedAsync(change).ConfigureAwait(false);
        }
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
        {
            await base.OnConnectedAsync().ConfigureAwait(false);
            return;
        }

        if (string.Equals(Context.User.FindFirstValue(ClaimTypes.Role), AuthRoles.Manager, StringComparison.Ordinal))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ManagersGroupName).ConfigureAwait(false);
        }

        if (TryGetEmployeeId(Context.User, out var employeeId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetEmployeeGroupName(employeeId)).ConfigureAwait(false);

            var nowUtc = DateTimeOffset.UtcNow;
            var change = _employeePresenceService.ConnectEmployee(employeeId, Context.ConnectionId, nowUtc);

            DateTimeOffset? lastLoginAtUtc = null;
            try
            {
                await _employeeAccountService.TouchLastSeenAsync(employeeId, Context.ConnectionAborted).ConfigureAwait(false);
                lastLoginAtUtc = (await _employeeAccountService
                    .GetByEmployeeIdAsync(employeeId, Context.ConnectionAborted)
                    .ConfigureAwait(false))?.LastLoginAtUtc;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Employee presence DB touch skipped for employee {EmployeeId}.", employeeId);
            }

            if (change.StateChanged)
            {
                await BroadcastPresenceChangedAsync(change, lastLoginAtUtc).ConfigureAwait(false);
            }
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var lockChanges = _scheduleEditLockService.ReleaseConnection(Context.ConnectionId);
        foreach (var lockChange in lockChanges)
        {
            await _realtimeNotifier.NotifyScheduleEditLockChangedAsync(lockChange).ConfigureAwait(false);
        }

        var change = _employeePresenceService.DisconnectConnection(Context.ConnectionId, DateTimeOffset.UtcNow);
        if (change is not null)
        {
            try
            {
                await _employeeAccountService.TouchLastSeenAsync(change.EmployeeId, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Employee presence disconnect touch skipped for employee {EmployeeId}.", change.EmployeeId);
            }

            if (change.StateChanged)
            {
                await BroadcastPresenceChangedAsync(change).ConfigureAwait(false);
            }
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private Task BroadcastPresenceChangedAsync(EmployeePresenceChange change, DateTimeOffset? lastLoginAtUtc = null)
        => Clients.Group(ManagersGroupName).PresenceChanged(new EmployeePresenceChangedMessage
        {
            EmployeeId = change.EmployeeId,
            IsOnline = change.IsOnline,
            ChangedAtUtc = change.OccurredAtUtc,
            LastLoginAtUtc = lastLoginAtUtc,
        });

    private static bool TryGetEmployeeId(ClaimsPrincipal user, out int employeeId)
    {
        employeeId = 0;

        if (!string.Equals(user.FindFirstValue(ClaimTypes.Role), AuthRoles.Employee, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(user.FindFirstValue("employee_id"), out employeeId) && employeeId > 0;
    }

    private static string GetEmployeeGroupName(int employeeId)
        => $"presence:employee:{employeeId}";
}
