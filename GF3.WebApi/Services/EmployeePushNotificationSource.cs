using System.Text.Json;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace WebApi.Services;

public sealed record EmployeePushCandidate(string NotificationId, DateTimeOffset OccurredAtUtc, string Title, string Body, string Url, string Tone);

public interface IEmployeePushNotificationSource
{
    Task<IReadOnlyList<EmployeePushCandidate>> GetCurrentAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct);
}

public sealed class EmployeePushNotificationSource(AppDbContext db, IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions,
    ILogger<EmployeePushNotificationSource>? logger = null) : IEmployeePushNotificationSource
{
    private string Timestamp(DateTimeOffset value) => JsonSerializer.Serialize(value, jsonOptions.Value.JsonSerializerOptions).Trim('"');

    public async Task<IReadOnlyList<EmployeePushCandidate>> GetCurrentAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(x => x.Id == employeeId && !x.IsDeleted, ct))
        {
            logger?.LogDebug("Employee push source {EmployeeId}: invalid recipient.", employeeId);
            return [];
        }
        var language = await db.EmployeeAccounts.AsNoTracking().Where(x => x.EmployeeId == employeeId).Select(x => x.Language).FirstOrDefaultAsync(ct);
        var messages = new EmployeePushMessages(language);
        var result = new List<EmployeePushCandidate>();
        var legacyWithoutKnownEvent = new HashSet<string>(StringComparer.Ordinal);
        var cutoff = nowUtc - EmployeeNotificationRetention.Duration;
        var schedules = await db.Schedules.AsNoTracking()
            .Where(x => x.PublicationStatus == SchedulePublicationStatus.Public && x.Employees.Any(e => e.EmployeeId == employeeId))
            .Select(x => new { x.Id, x.PublishedAtUtc, x.Name, x.Year, x.Month }).ToListAsync(ct);
        foreach (var schedule in schedules)
            if (schedule.PublishedAtUtc is { } published)
            {
                var message = messages.Format(EmployeePushMessageKind.Schedule, schedule.Name, schedule.Year, schedule.Month);
                result.Add(new($"schedule-public:{schedule.Id}:{Timestamp(published)}", published, message.Title, message.Body, "/schedule", "schedule"));
            }

        var groups = await db.AvailabilityGroups.AsNoTracking()
            .Where(x => x.PublicationStatus == AvailabilityPublicationStatus.Public && x.Members.Any(m => m.EmployeeId == employeeId))
            .Include(x => x.Members.Where(m => m.EmployeeId == employeeId)).ToListAsync(ct);
        foreach (var group in groups)
        {
            var member = group.Members.Single();
            if (group.VisibleFromUtc > nowUtc)
            {
                logger?.LogDebug("Employee push availability {GroupId} for {EmployeeId} skipped: not yet visible.", group.Id, employeeId);
                continue;
            }
            if (AvailabilityNotificationTiming.OccurredAt(group, member) is not { } occurred) continue;
            if (!AvailabilityNotificationTiming.HasKnownEvent(group, member))
            {
                legacyWithoutKnownEvent.Add(AvailabilityNotificationTiming.Id(group));
                logger?.LogDebug("Employee push availability {GroupId} for {EmployeeId}: unknown legacy publication; no backfill.", group.Id, employeeId);
            }
            var message = messages.Format(EmployeePushMessageKind.Availability, group.Name, group.Year, group.Month,
                group.VisibleToUtc, group.VisibleToUtc < nowUtc);
            result.Add(new(AvailabilityNotificationTiming.Id(group), occurred, message.Title, message.Body, "/availability", "availability"));
        }

        var sharedContainers = db.Schedules.Where(x => x.PublicationStatus == SchedulePublicationStatus.Public && x.Employees.Any(e => e.EmployeeId == employeeId)).Select(x => x.ContainerId);
        var swaps = await db.ShiftSwapRequests.AsNoTracking().Where(x =>
            (x.Schedule.PublicationStatus == SchedulePublicationStatus.Public ||
                x.Status != ShiftSwapStatus.Open && x.ScheduleSlotId == null && x.ArchivedViewsJson != null) &&
            (x.Status == ShiftSwapStatus.Accepted && x.FromEmployeeId == employeeId ||
                x.Status == ShiftSwapStatus.Open && x.FromEmployeeId != employeeId &&
                x.Schedule.Slots.Any(slot => slot.Id == x.ScheduleSlotId) &&
                (x.TargetEmployeeId == employeeId || x.Visibility == ShiftSwapVisibility.Public && sharedContainers.Contains(x.Schedule.ContainerId))))
            .Select(x => new { x.Id, x.Status, x.CreatedAtUtc, x.AcceptedAtUtc, x.IsManagerCreated,
                x.Schedule.Name, x.Schedule.Year, x.Schedule.Month, Day = x.ScheduleSlot == null ? (int?)null : x.ScheduleSlot.DayOfMonth,
                From = x.OfferedFromTime ?? (x.ScheduleSlot == null ? null : x.ScheduleSlot.FromTime),
                To = x.OfferedToTime ?? (x.ScheduleSlot == null ? null : x.ScheduleSlot.ToTime) }).ToListAsync(ct);
        foreach (var swap in swaps)
        {
            if (swap.Status == ShiftSwapStatus.Accepted && swap.AcceptedAtUtc is { } accepted)
            {
                var message = messages.Format(EmployeePushMessageKind.AcceptedSwap, swap.Name, swap.Year, swap.Month, day: swap.Day, from: swap.From, to: swap.To);
                result.Add(new($"swap-accepted:{swap.Id}", accepted, message.Title, message.Body, $"/swap?swapId={swap.Id}", "swap"));
            }
            else if (swap.Status == ShiftSwapStatus.Open)
            {
                var manual = swap.IsManagerCreated;
                var message = messages.Format(manual ? EmployeePushMessageKind.OpenShift : EmployeePushMessageKind.Swap,
                    swap.Name, swap.Year, swap.Month, day: swap.Day, from: swap.From, to: swap.To);
                result.Add(new($"{(manual ? "open-shift" : "swap-public")}:{swap.Id}", swap.CreatedAtUtc,
                    message.Title, message.Body, $"/swap?swapId={swap.Id}", "swap"));
            }
        }
        logger?.LogDebug("Employee push source {EmployeeId}: {Expired} expired and {Future} future events excluded.",
            employeeId, result.Count(x => x.OccurredAtUtc <= cutoff), result.Count(x => x.OccurredAtUtc > nowUtc));
        return result.Where(x => x.OccurredAtUtc > cutoff && x.OccurredAtUtc <= nowUtc)
            .DistinctBy(x => x.NotificationId).OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.NotificationId, StringComparer.Ordinal).Take(80)
            .Where(x => !legacyWithoutKnownEvent.Contains(x.NotificationId)).ToArray();
    }
}
