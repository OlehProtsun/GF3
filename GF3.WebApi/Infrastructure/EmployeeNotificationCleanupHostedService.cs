using DataAccessLayer.Models.DataBaseContext;
using WebApi.Services;

namespace WebApi.Infrastructure;

public sealed class EmployeeNotificationCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<EmployeeNotificationCleanupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1), timeProvider);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await EmployeeNotificationRetention.CleanupAsync(db, timeProvider.GetUtcNow(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Employee notification read-state cleanup failed; the next scheduled run will retry.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
