using System.Net;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApi.Options;
using WebApi.Services;
using WebPush;

namespace WebApi.Infrastructure;

public sealed class EmployeeWebPushHostedService(
    IServiceScopeFactory scopeFactory, TimeProvider timeProvider, IOptions<PushOptions> options,
    ILogger<EmployeeWebPushHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30), timeProvider);
        do
        {
            try { await ScanAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception) { logger.LogError("Employee push scan failed; a later scan will retry."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task ScanAsync(CancellationToken ct)
    {
        if (!options.Value.Enabled) return;
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var source = scope.ServiceProvider.GetRequiredService<IEmployeePushNotificationSource>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmployeeWebPushSender>();
        var now = timeProvider.GetUtcNow();
        var cutoff = now - EmployeeNotificationRetention.Duration;
        await db.EmployeePushDeliveries.Where(x => x.FinalizedAtUtc < cutoff).ExecuteDeleteAsync(ct);
        var after = 0;
        var attempted = 0;
        while (attempted < 100)
        {
            var page = await db.EmployeePushSubscriptions.AsNoTracking().Where(x => x.Id > after).OrderBy(x => x.Id).Take(100).ToListAsync(ct);
            if (page.Count == 0) break;
            after = page[^1].Id;
            foreach (var group in page.GroupBy(x => x.EmployeeId))
            {
                var candidates = await source.GetCurrentAsync(group.Key, now, ct);
                var readIds = (await db.EmployeeNotificationReads.AsNoTracking().Where(x => x.EmployeeId == group.Key).Select(x => x.NotificationId).ToListAsync(ct)).ToHashSet();
                foreach (var subscription in group)
                {
                    if (!options.Value.AllowsEndpoint(subscription.Endpoint))
                    {
                        logger.LogDebug("Employee push subscription {SubscriptionId} skipped: invalid endpoint.", subscription.Id);
                        continue;
                    }
                    logger.LogDebug("Employee push subscription {SubscriptionId}: {Eligible} candidates, {Old} predate opt-in, {Read} read.",
                        subscription.Id, candidates.Count, candidates.Count(x => x.OccurredAtUtc < subscription.CreatedAtUtc),
                        candidates.Count(x => readIds.Contains(x.NotificationId)));
                    foreach (var candidate in candidates.Where(x => x.OccurredAtUtc >= subscription.CreatedAtUtc && !readIds.Contains(x.NotificationId)))
                    {
                        // The endpoint may have transferred accounts since the page was loaded.
                        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
                            INSERT INTO employee_push_delivery (subscription_id, notification_id, created_at_utc, next_attempt_at_utc, attempt_count)
                            SELECT {subscription.Id}, {candidate.NotificationId}, {now.UtcTicks}, {now.UtcTicks}, 0
                            WHERE EXISTS (SELECT 1 FROM employee_push_subscription WHERE id = {subscription.Id} AND employee_id = {subscription.EmployeeId} AND created_at_utc = {subscription.CreatedAtUtc.UtcTicks})
                            ON CONFLICT(subscription_id, notification_id) DO NOTHING
                            """, ct);
                        if (inserted == 0) logger.LogDebug("Employee push subscription {SubscriptionId}: existing delivery or changed recipient; duplicate suppressed.", subscription.Id);
                    }
                    var jobs = await db.EmployeePushDeliveries.AsNoTracking().Where(x => x.SubscriptionId == subscription.Id && x.Outcome == null && x.NextAttemptAtUtc <= now && (x.LeaseUntilUtc == null || x.LeaseUntilUtc <= now))
                        .OrderBy(x => x.NextAttemptAtUtc).ThenBy(x => x.Id).Take(100 - attempted).ToListAsync(ct);
                    foreach (var job in jobs)
                    {
                        var token = Guid.NewGuid().ToString();
                        if (!await TryClaimAsync(db, job.Id, token, now, ct)) continue;
                        attempted++;
                        try
                        {
                            var current = await db.EmployeePushSubscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == subscription.Id && x.EmployeeId == subscription.EmployeeId && x.CreatedAtUtc == subscription.CreatedAtUtc, ct);
                            var eligible = current is null ? null : (await source.GetCurrentAsync(current.EmployeeId, timeProvider.GetUtcNow(), ct))
                                .FirstOrDefault(x => x.NotificationId == job.NotificationId && x.OccurredAtUtc >= current.CreatedAtUtc);
                            var isRead = current is not null && await db.EmployeeNotificationReads.AnyAsync(x => x.EmployeeId == current.EmployeeId && x.NotificationId == job.NotificationId, ct);
                            if (isRead || eligible is null || current is null || !options.Value.AllowsEndpoint(current.Endpoint))
                            {
                                logger.LogDebug("Employee push job {JobId} skipped: {Reason}.", job.Id,
                                    isRead ? "read" : current is null ? "recipient changed" : eligible is null ? "withdrawn, expired, or predates opt-in" : "invalid endpoint");
                                await FinishAsync(db, job.Id, token, isRead ? "read" : "expired", ct);
                                continue;
                            }
                            await sender.SendAsync(current, eligible, ct);
                            await FinishAsync(db, job.Id, token, "sent", ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                        catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                        {
                            await db.EmployeePushSubscriptions.Where(x => x.Id == subscription.Id && x.EmployeeId == subscription.EmployeeId &&
                                db.EmployeePushDeliveries.Any(d => d.Id == job.Id && d.LeaseToken == token)).ExecuteDeleteAsync(ct);
                            break;
                        }
                        catch (Exception exception)
                        {
                            var retry = exception is HttpRequestException or TaskCanceledException ||
                                exception is WebPushException push && (push.StatusCode == HttpStatusCode.TooManyRequests || (int)push.StatusCode >= 500);
                            var attempts = job.AttemptCount + 1;
                            if (!retry || attempts >= 8)
                            {
                                logger.LogWarning("Employee push job {JobId} ended in permanent failure (attempt {Attempt}).", job.Id, attempts);
                                await db.EmployeePushDeliveries.Where(x => x.Id == job.Id && x.LeaseToken == token && x.Outcome == null)
                                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.AttemptCount, attempts), ct);
                                await FinishAsync(db, job.Id, token, "permanent-failure", ct);
                            }
                            else
                            {
                                var next = timeProvider.GetUtcNow().AddSeconds(Math.Min(Math.Pow(2, attempts) * 30, 3600));
                                await db.EmployeePushDeliveries.Where(x => x.Id == job.Id && x.LeaseToken == token && x.Outcome == null)
                                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.AttemptCount, attempts).SetProperty(x => x.NextAttemptAtUtc, next)
                                        .SetProperty(x => x.LeaseToken, (string?)null).SetProperty(x => x.LeaseUntilUtc, (DateTimeOffset?)null), ct);
                            }
                        }
                    }
                    if (attempted >= 100) return;
                }
            }
        }
    }

    public static async Task<bool> TryClaimAsync(AppDbContext db, long id, string token, DateTimeOffset now, CancellationToken ct)
        => await db.EmployeePushDeliveries.Where(x => x.Id == id && x.Outcome == null && x.NextAttemptAtUtc <= now && (x.LeaseUntilUtc == null || x.LeaseUntilUtc <= now))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseToken, token).SetProperty(x => x.LeaseUntilUtc, now.AddMinutes(2)), ct) == 1;

    private Task<int> FinishAsync(AppDbContext db, long id, string token, string outcome, CancellationToken ct)
        => db.EmployeePushDeliveries.Where(x => x.Id == id && x.LeaseToken == token && x.Outcome == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Outcome, outcome).SetProperty(x => x.FinalizedAtUtc, timeProvider.GetUtcNow())
                .SetProperty(x => x.LeaseToken, (string?)null).SetProperty(x => x.LeaseUntilUtc, (DateTimeOffset?)null), ct);
}
