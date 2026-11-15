using DataAccessLayer.Administration;

namespace WebApi.Infrastructure;

/// <summary>
/// Creates an automatic database snapshot once per hour while the application process is running.
/// </summary>
public sealed class DatabaseAutoBackupHostedService : BackgroundService
{
    private static readonly TimeSpan BackupInterval = TimeSpan.FromHours(1);

    private readonly ISqliteDatabaseWorkspace _databaseWorkspace;
    private readonly ILogger<DatabaseAutoBackupHostedService> _logger;

    public DatabaseAutoBackupHostedService(
        ISqliteDatabaseWorkspace databaseWorkspace,
        ILogger<DatabaseAutoBackupHostedService> logger)
    {
        _databaseWorkspace = databaseWorkspace;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(BackupInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    var createdBackup = await _databaseWorkspace.CreateAutomaticBackupAsync(stoppingToken).ConfigureAwait(false);
                    _logger.LogInformation("Created automatic database backup at {BackupPath}.", createdBackup.Path);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Automatic database backup failed for active database {DatabasePath}.",
                        _databaseWorkspace.DatabasePath);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
