using System.Text.Json;
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

public sealed class EmployeePushNotificationSource(AppDbContext db, IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions) : IEmployeePushNotificationSource
{
    private string Timestamp(DateTimeOffset value) => JsonSerializer.Serialize(value, jsonOptions.Value.JsonSerializerOptions).Trim('"');

    public async Task<IReadOnlyList<EmployeePushCandidate>> GetCurrentAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(x => x.Id == employeeId && !x.IsDeleted, ct)) return [];
        var result = new List<EmployeePushCandidate>();
        var cutoff = nowUtc - EmployeeNotificationRetention.Duration;
        var schedules = await db.Schedules.AsNoTracking()
            .Where(x => x.PublicationStatus == SchedulePublicationStatus.Public && x.Employees.Any(e => e.EmployeeId == employeeId))
            .Select(x => new { x.Id, x.PublishedAtUtc }).ToListAsync(ct);
        foreach (var schedule in schedules)
            if (schedule.PublishedAtUtc is { } published)
                result.Add(new($"schedule-public:{schedule.Id}:{Timestamp(published)}", published, "New schedule published", "A published schedule is available in GF.", "/schedule", "schedule"));

        var groups = await db.AvailabilityGroups.AsNoTracking()
            .Where(x => x.PublicationStatus == AvailabilityPublicationStatus.Public && x.Members.Any(m => m.EmployeeId == employeeId))
            .Select(x => new { x.Id, x.VisibleFromUtc, x.VisibleToUtc }).ToListAsync(ct);
        foreach (var group in groups)
            if (group.VisibleFromUtc is { } start && start <= nowUtc && (group.VisibleToUtc is null || nowUtc <= group.VisibleToUtc))
                result.Add(new($"availability-public:{group.Id}:{Timestamp(start)}", start, "New availability published", "A published availability form is open in GF.", "/availability", "availability"));

        var sharedContainers = db.Schedules.Where(x => x.PublicationStatus == SchedulePublicationStatus.Public && x.Employees.Any(e => e.EmployeeId == employeeId)).Select(x => x.ContainerId);
        var swaps = await db.ShiftSwapRequests.AsNoTracking().Where(x =>
            (x.Schedule.PublicationStatus == SchedulePublicationStatus.Public ||
                x.Status != ShiftSwapStatus.Open && x.ScheduleSlotId == null && x.ArchivedViewsJson != null) &&
            (x.Status == ShiftSwapStatus.Accepted && x.FromEmployeeId == employeeId ||
                x.Status == ShiftSwapStatus.Open && x.FromEmployeeId != employeeId &&
                x.Schedule.Slots.Any(slot => slot.Id == x.ScheduleSlotId) &&
                (x.TargetEmployeeId == employeeId || x.Visibility == ShiftSwapVisibility.Public && sharedContainers.Contains(x.Schedule.ContainerId))))
            .Select(x => new { x.Id, x.Status, x.CreatedAtUtc, x.AcceptedAtUtc, x.IsManagerCreated, x.ManualColumnId }).ToListAsync(ct);
        foreach (var swap in swaps)
        {
            if (swap.Status == ShiftSwapStatus.Accepted && swap.AcceptedAtUtc is { } accepted)
                result.Add(new($"swap-accepted:{swap.Id}", accepted, "Your shift was accepted", "View your accepted shift in GF.", $"/swap?swapId={swap.Id}", "swap"));
            else if (swap.Status == ShiftSwapStatus.Open)
            {
                var manual = swap.IsManagerCreated && swap.ManualColumnId != null;
                result.Add(new($"{(manual ? "open-shift" : "swap-public")}:{swap.Id}", swap.CreatedAtUtc,
                    manual ? "Open shift posted" : "Shift swap available", "A shift offer is available in GF.", $"/swap?swapId={swap.Id}", "swap"));
            }
        }
        return result.Where(x => x.OccurredAtUtc > cutoff && x.OccurredAtUtc <= nowUtc)
            .DistinctBy(x => x.NotificationId).OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.NotificationId).Take(80).ToArray();
    }
}
