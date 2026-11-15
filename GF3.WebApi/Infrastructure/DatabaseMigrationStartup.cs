using DataAccessLayer.Administration;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Infrastructure;

/// <summary>
/// Applies startup migrations only after validating the active SQLite file and taking a
/// consistent snapshot. The existing-database guard prevents a mistyped Docker volume name
/// from silently creating a new, empty production database.
/// </summary>
public static class DatabaseMigrationStartup
{
    private const string RequireExistingDatabaseKey = "DatabaseStartup:RequireExistingDatabase";
    private const string BackupBeforeMigrateKey = "DatabaseStartup:BackupBeforeMigrate";

    public static async Task<DatabaseMigrationResult> ApplyAsync(
        WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var workspace = scope.ServiceProvider.GetRequiredService<ISqliteDatabaseWorkspace>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigrationStartup");

        var requireExistingDatabase = app.Configuration.GetValue<bool>(RequireExistingDatabaseKey);
        var backupBeforeMigrate = app.Configuration.GetValue(BackupBeforeMigrateKey, true);

        return await ApplyAsync(
                dbContext,
                workspace,
                requireExistingDatabase,
                backupBeforeMigrate,
                logger,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<DatabaseMigrationResult> ApplyAsync(
        AppDbContext dbContext,
        ISqliteDatabaseWorkspace workspace,
        bool requireExistingDatabase,
        bool backupBeforeMigrate,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(logger);

        var databaseFile = new FileInfo(workspace.DatabasePath);
        databaseFile.Refresh();
        var databaseExisted = databaseFile.Exists && databaseFile.Length > 0;

        if (requireExistingDatabase && !databaseExisted)
        {
            throw new InvalidOperationException(
                $"Production database guard refused to start because the existing SQLite database " +
                $"was not found at '{workspace.DatabasePath}'. Verify GF3_DATA_VOLUME and the /data mount. " +
                "For an intentional first installation only, set GF3_REQUIRE_EXISTING_DATABASE=false.");
        }

        if (databaseExisted)
        {
            await VerifyDatabaseIntegrityAsync(workspace.ConnectionString, "before migrations", cancellationToken)
                .ConfigureAwait(false);
        }

        var pendingMigrations = (await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken)
                .ConfigureAwait(false))
            .ToArray();

        string? backupPath = null;
        if (databaseExisted && backupBeforeMigrate && pendingMigrations.Length > 0)
        {
            var backup = await workspace.CreateAutomaticBackupAsync(cancellationToken).ConfigureAwait(false);
            backupPath = backup.Path;
            logger.LogInformation(
                "Created pre-migration database backup at {BackupPath} before applying {MigrationCount} migration(s).",
                backupPath,
                pendingMigrations.Length);
        }

        if (pendingMigrations.Length > 0)
        {
            logger.LogInformation(
                "Applying database migrations: {PendingMigrations}.",
                string.Join(", ", pendingMigrations));
        }

        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        await VerifyDatabaseIntegrityAsync(workspace.ConnectionString, "after migrations", cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Database startup verification completed for {DatabasePath}. Applied {MigrationCount} migration(s).",
            workspace.DatabasePath,
            pendingMigrations.Length);

        return new DatabaseMigrationResult(databaseExisted, pendingMigrations, backupPath);
    }

    private static async Task VerifyDatabaseIntegrityAsync(
        string connectionString,
        string phase,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA quick_check;";

        var results = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(reader.GetString(0));
        }

        if (results.Count != 1 || !string.Equals(results[0], "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SQLite integrity check failed {phase}: {string.Join("; ", results)}");
        }
    }
}

public sealed record DatabaseMigrationResult(
    bool DatabaseExisted,
    IReadOnlyList<string> AppliedMigrations,
    string? BackupPath);