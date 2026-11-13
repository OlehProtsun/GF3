using System.Globalization;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;

namespace WebApi.Services;

/// <summary>
/// Resolves schedule update timestamps from data that already exists in production.
/// Manager edits come from the workflow log, while accepted employee swaps use their stable
/// schedule id and acceptance timestamp, so this feature needs no schedule schema change.
/// </summary>
public interface IScheduleLastUpdateService
{
    Task<IReadOnlyDictionary<int, DateTimeOffset>> GetLastUpdatesAsync(
        IEnumerable<int> scheduleIds,
        CancellationToken cancellationToken = default);
}

public sealed class ScheduleLastUpdateService(AppDbContext db) : IScheduleLastUpdateService
{
    public async Task<IReadOnlyDictionary<int, DateTimeOffset>> GetLastUpdatesAsync(
        IEnumerable<int> scheduleIds,
        CancellationToken cancellationToken = default)
    {
        var ids = scheduleIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, DateTimeOffset>();
        }

        var schedules = await db.Schedules
            .AsNoTracking()
            .Where(schedule => ids.Contains(schedule.Id))
            .Select(schedule => new ScheduleIdentity(
                schedule.Id,
                schedule.Name,
                schedule.Year,
                schedule.Month,
                schedule.Shop.Name,
                schedule.Container.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var acceptedSwaps = await db.ShiftSwapRequests
            .AsNoTracking()
            .Where(request => ids.Contains(request.ScheduleId) && request.AcceptedAtUtc != null)
            .Select(request => new { request.ScheduleId, request.AcceptedAtUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var managerScheduleLogs = await db.WorkflowLogEntries
            .AsNoTracking()
            .Where(entry =>
                entry.ActorRole == AuthRoles.Manager &&
                (entry.Action.Contains("schedule \"") || entry.Action.Contains("schedule #")))
            .Select(entry => new { entry.Action, entry.OccurredAtUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = acceptedSwaps
            .Where(request => request.AcceptedAtUtc.HasValue)
            .GroupBy(request => request.ScheduleId)
            .ToDictionary(
                group => group.Key,
                group => group.Max(request => request.AcceptedAtUtc!.Value));

        foreach (var schedule in schedules)
        {
            var matchingLogs = managerScheduleLogs
                .Where(entry => MatchesSchedule(entry.Action, schedule))
                .ToArray();
            if (matchingLogs.Length == 0)
            {
                continue;
            }

            // Prefer full shop/container context. The fallback preserves older entries after
            // a shop or container rename, while still requiring the schedule name and period.
            var contextualLogs = matchingLogs
                .Where(entry => MatchesCurrentContext(entry.Action, schedule))
                .ToArray();
            var managerUpdatedAt = (contextualLogs.Length > 0 ? contextualLogs : matchingLogs)
                .Max(entry => entry.OccurredAtUtc);

            if (!result.TryGetValue(schedule.Id, out var currentValue) || managerUpdatedAt > currentValue)
            {
                result[schedule.Id] = managerUpdatedAt;
            }
        }

        return result;
    }

    private static bool MatchesSchedule(string action, ScheduleIdentity schedule)
    {
        if (action.Contains($"schedule #{schedule.Id}", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var scheduleToken = $"schedule \"{schedule.Name}\"";
        var period = new DateTime(schedule.Year, schedule.Month, 1)
            .ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        return action.Contains(scheduleToken, StringComparison.OrdinalIgnoreCase) &&
               action.Contains($"for {period}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesCurrentContext(string action, ScheduleIdentity schedule)
        => action.Contains($"shop \"{schedule.ShopName}\"", StringComparison.OrdinalIgnoreCase) &&
           action.Contains($"container \"{schedule.ContainerName}\"", StringComparison.OrdinalIgnoreCase);

    private sealed record ScheduleIdentity(
        int Id,
        string Name,
        int Year,
        int Month,
        string ShopName,
        string ContainerName);
}
