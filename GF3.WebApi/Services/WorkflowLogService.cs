using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Realtime;

namespace WebApi.Services;

public interface IWorkflowLogService
{
    Task<WorkflowLogEntryModel> LogAsync(
        ClaimsPrincipal user,
        string action,
        CancellationToken cancellationToken = default);

    Task<WorkflowLogEntryModel> LogAsync(
        string actorRole,
        string actorName,
        int? actorEmployeeId,
        string action,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowLogEntryModel>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
}

public sealed class WorkflowLogService(AppDbContext db, IRealtimeNotifier realtimeNotifier) : IWorkflowLogService
{
    public Task<WorkflowLogEntryModel> LogAsync(
        ClaimsPrincipal user,
        string action,
        CancellationToken cancellationToken = default)
    {
        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var actorName = user.FindFirstValue("display_name") ?? user.Identity?.Name ?? GetFallbackActorName(role);
        var actorEmployeeId = int.TryParse(user.FindFirstValue("employee_id"), out var employeeId) ? employeeId : (int?)null;

        return LogAsync(role, actorName, actorEmployeeId, action, cancellationToken);
    }

    public async Task<WorkflowLogEntryModel> LogAsync(
        string actorRole,
        string actorName,
        int? actorEmployeeId,
        string action,
        CancellationToken cancellationToken = default)
    {
        var entry = new WorkflowLogEntryModel
        {
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ActorRole = NormalizeActorRole(actorRole),
            ActorEmployeeId = actorEmployeeId,
            ActorName = string.IsNullOrWhiteSpace(actorName) ? GetFallbackActorName(actorRole) : actorName.Trim(),
            Action = action.Trim(),
        };

        db.WorkflowLogEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await realtimeNotifier.NotifyWorkflowLogCreatedAsync(entry).ConfigureAwait(false);
        return entry;
    }

    public async Task<IReadOnlyList<WorkflowLogEntryModel>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        var entries = await db.WorkflowLogEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entries
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Take(safeLimit)
            .ToList();
    }

    private static string NormalizeActorRole(string actorRole)
        => string.Equals(actorRole, AuthRoles.Employee, StringComparison.OrdinalIgnoreCase)
            ? AuthRoles.Employee
            : AuthRoles.Manager;

    private static string GetFallbackActorName(string? actorRole)
        => string.Equals(actorRole, AuthRoles.Employee, StringComparison.OrdinalIgnoreCase)
            ? "Employee"
            : "Manager";
}
