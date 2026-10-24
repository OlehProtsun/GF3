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

    Task<IReadOnlyList<WorkflowLogEntryModel>> GetRecentAsync(CancellationToken cancellationToken = default);

    Task<WorkflowLogSettingsModel> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<WorkflowLogSettingsModel> UpdateSettingsAsync(
        bool isEnabled,
        string audience,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<int> DeleteRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);
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
        var normalizedRole = NormalizeActorRole(actorRole);
        var entry = new WorkflowLogEntryModel
        {
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ActorRole = normalizedRole,
            ActorEmployeeId = actorEmployeeId,
            ActorName = string.IsNullOrWhiteSpace(actorName) ? GetFallbackActorName(actorRole) : actorName.Trim(),
            Action = action.Trim(),
        };

        var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (!ShouldPersist(settings, normalizedRole))
        {
            return entry;
        }

        db.WorkflowLogEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await realtimeNotifier.NotifyWorkflowLogCreatedAsync(entry).ConfigureAwait(false);
        return entry;
    }

    public async Task<IReadOnlyList<WorkflowLogEntryModel>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        var entries = await db.WorkflowLogEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entries
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ToList();
    }

    public async Task<WorkflowLogSettingsModel> GetSettingsAsync(CancellationToken cancellationToken = default)
        => await db.WorkflowLogSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(settings => settings.Id == WorkflowLogSettingsModel.SingletonId, cancellationToken)
            .ConfigureAwait(false)
            ?? new WorkflowLogSettingsModel();

    public async Task<WorkflowLogSettingsModel> UpdateSettingsAsync(
        bool isEnabled,
        string audience,
        CancellationToken cancellationToken = default)
    {
        var normalizedAudience = NormalizeAudience(audience);
        var settings = await db.WorkflowLogSettings
            .SingleOrDefaultAsync(item => item.Id == WorkflowLogSettingsModel.SingletonId, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
        {
            settings = new WorkflowLogSettingsModel();
            db.WorkflowLogSettings.Add(settings);
        }

        settings.IsEnabled = isEnabled;
        settings.Audience = normalizedAudience;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return settings;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return false;
        }

        return await db.WorkflowLogEntries
            .Where(entry => entry.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false) > 0;
    }

    public Task<int> DeleteRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        if (toUtc <= fromUtc)
        {
            throw new ArgumentException("The end of the workflow-log range must be after its start.", nameof(toUtc));
        }

        return db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM workflow_log_entry WHERE occurred_at_utc >= {fromUtc} AND occurred_at_utc < {toUtc}",
            cancellationToken);
    }

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
        => db.WorkflowLogEntries.ExecuteDeleteAsync(cancellationToken);

    private static bool ShouldPersist(WorkflowLogSettingsModel settings, string actorRole)
    {
        if (!settings.IsEnabled)
        {
            return false;
        }

        return settings.Audience switch
        {
            WorkflowLogSettingsModel.ManagersAudience => actorRole == AuthRoles.Manager,
            WorkflowLogSettingsModel.EmployeesAudience => actorRole == AuthRoles.Employee,
            _ => true,
        };
    }

    private static string NormalizeAudience(string audience)
    {
        var normalized = audience.Trim().ToLowerInvariant();
        return normalized switch
        {
            WorkflowLogSettingsModel.AllAudience => normalized,
            WorkflowLogSettingsModel.ManagersAudience => normalized,
            WorkflowLogSettingsModel.EmployeesAudience => normalized,
            _ => throw new ArgumentException("Audience must be all, managers, or employees.", nameof(audience)),
        };
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
