using System.Text;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GF3.Tests;

public sealed class SqliteAdministrationTests
{
    [Fact]
    public void SqliteDatabaseSelectionStore_SaveTryReadAndResolve_Work()
    {
        var root = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "selected.db");
        File.WriteAllText(databasePath, "db");

        SqliteDatabaseSelectionStore.Save(databasePath, root);

        var resolved = SqliteDatabaseSelectionStore.ResolveDatabasePath(Path.Combine(root, "fallback.db"), root);
        var success = SqliteDatabaseSelectionStore.TryRead(out var selected, root);

        Assert.True(success);
        Assert.Equal(Path.GetFullPath(databasePath), selected);
        Assert.Equal(Path.GetFullPath(databasePath), resolved);
    }

    [Fact]
    public async Task SqliteDatabaseWorkspace_CreateCopiesStateAndRetention_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var workspace = new SqliteDatabaseWorkspace(database.DatabasePath);

        Directory.CreateDirectory(workspace.AutomaticBackupDirectoryPath);
        for (var i = 0; i < 12; i++)
        {
            var path = Path.Combine(workspace.AutomaticBackupDirectoryPath, $"old-{i}.db");
            File.WriteAllText(path, "backup");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-100 - i));
        }

        var manualCopy = await workspace.CreateManualCopyAsync(CancellationToken.None);
        var automaticCopy = await workspace.CreateAutomaticBackupAsync(CancellationToken.None);
        var state = workspace.GetWorkspaceState();

        Assert.True(File.Exists(manualCopy.Path));
        Assert.True(File.Exists(automaticCopy.Path));
        Assert.Equal("manualCopy", manualCopy.Category);
        Assert.Equal("backup", automaticCopy.Category);
        Assert.Contains(state.AvailableDatabases, entry => entry.IsActive && entry.Path == database.DatabasePath);
        Assert.True(state.ManualCopies.Count >= 1);
        Assert.True(state.AutomaticBackups.Count <= workspace.AutomaticBackupRetentionLimit);
    }

    [Fact]
    public async Task SqliteAdminService_ExecuteSqlImportInfoAndHash_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var workspace = new SqliteDatabaseWorkspace(database.DatabasePath);
        var service = new SqliteAdminService(workspace);

        var insert = await service.ExecuteSqlAsync(
            "INSERT INTO employee(first_name,last_name,phone,email) VALUES ('John','Smith','1','john@example.com');",
            CancellationToken.None);
        var query = await service.ExecuteSqlAsync(
            "SELECT first_name, last_name FROM employee ORDER BY id;",
            CancellationToken.None);

        await service.ImportSqlScriptAsync(
            "INSERT INTO shop(name,address,description) VALUES ('Shop','Address','Desc');",
            CancellationToken.None);

        var info = await service.GetDatabaseInfoAsync(CancellationToken.None);
        var hash = await service.ComputeFileHashAsync(database.DatabasePath, CancellationToken.None);

        Assert.False(insert.IsSelect);
        Assert.Equal(1, insert.AffectedRows);
        Assert.True(query.IsSelect);
        Assert.NotNull(query.ResultTable);
        Assert.Single(query.ResultTable!.Rows.Cast<System.Data.DataRow>());
        Assert.Contains(info.Tables, table => string.Equals(table, "employee", StringComparison.OrdinalIgnoreCase));
        Assert.Matches("^[a-f0-9]{64}$", hash);
    }

    [Fact]
    public async Task AdminDbService_QueryAndExecute_ValidateAllowedSqlFamilies()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var provider = BuildAdminProvider(database);
        await using var scope = provider.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<IAdminDbService>();
        var affectedRows = await service.ExecuteNonQueryAsync(
            "INSERT INTO AvailabilityBinds(Key,Value,IsActive) VALUES ('A','+',1);",
            maxSqlLength: 1_000);
        var result = await service.ExecuteQueryAsync(
            "SELECT Key, Value FROM AvailabilityBinds;",
            maxSqlLength: 1_000);

        Assert.Equal(1, affectedRows);
        Assert.Equal(["Key", "Value"], result.Columns);
        Assert.Single(result.Rows);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ExecuteQueryAsync("DELETE FROM AvailabilityBinds;", 1_000));
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ExecuteNonQueryAsync("SELECT * FROM AvailabilityBinds;", 1_000));
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ExecuteNonQueryAsync("DROP TABLE employee;", 1_000));
    }

    [Fact]
    public async Task AdminDbService_ImportMetadataAndHash_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var provider = BuildAdminProvider(database);
        await using var scope = provider.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<IAdminDbService>();
        var importBytes = Encoding.UTF8.GetBytes(
            """
            -- comment
            BEGIN TRANSACTION;
            INSERT OR IGNORE INTO AvailabilityBinds(Key,Value,IsActive) VALUES ('A','+',1);
            INSERT OR IGNORE INTO AvailabilityBinds(Key,Value,IsActive) VALUES ('A','+',1);
            COMMIT;
            """);

        var importResult = await service.ImportSqlAsync(importBytes, maxImportBytes: 10_000);
        var metadata = await service.GetMetadataAsync();
        var hash = await service.GetDbHashAsync();

        Assert.Equal(2, importResult.StatementsExecuted);
        Assert.Equal(1, importResult.StatementsApplied);
        Assert.Equal(1, importResult.StatementsAlreadyExisted);
        Assert.Equal(3, importResult.ServiceStatementsSkipped);
        Assert.Contains(metadata.Tables, table => string.Equals(table, "employee", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(metadata.Objects);
        Assert.NotNull(metadata.StorageWorkspace);
        Assert.Matches("^[a-f0-9]{64}$", hash);
    }

    [Fact]
    public async Task AdminDbService_SelectDatabase_SwitchesWorkspaceAndPersistsSelection()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var secondDatabasePath = Path.Combine(database.RootPath, "other.db");
        File.WriteAllBytes(secondDatabasePath, []);

        await using var provider = BuildAdminProvider(database);
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDbService>();

        var selected = await service.SelectDatabaseAsync(secondDatabasePath);
        var readSuccess = SqliteDatabaseSelectionStore.TryRead(out var persistedPath, database.RootPath);

        Assert.True(readSuccess);
        Assert.True(selected.IsActive);
        Assert.Equal(Path.GetFullPath(secondDatabasePath), persistedPath);
    }
    private static ServiceProvider BuildAdminProvider(SqliteTestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISqliteDatabaseWorkspace>(_ => new SqliteDatabaseWorkspace(database.DatabasePath));
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var workspace = serviceProvider.GetRequiredService<ISqliteDatabaseWorkspace>();
            options.UseSqlite(workspace.ConnectionString);
        });
        services.AddScoped<ISqliteAdminService, SqliteAdminService>();
        services.AddScoped<ISqliteAdminFacade, SqliteAdminFacade>();
        services.AddScoped<IAdminDbService>(serviceProvider => new AdminDbService(
            serviceProvider.GetRequiredService<AppDbContext>(),
            serviceProvider.GetRequiredService<ISqliteAdminFacade>(),
            serviceProvider.GetRequiredService<ISqliteDatabaseWorkspace>(),
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            database.RootPath));

        return services.BuildServiceProvider();
    }
}
