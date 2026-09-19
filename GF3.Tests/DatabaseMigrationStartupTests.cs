using System.Text.Json;
using DataAccessLayer.Administration;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.Infrastructure;

namespace GF3.Tests;

public sealed class DatabaseMigrationStartupTests
{
    private const string PreviousProductionMigration = "20260509101500_AddManagerRecoveryFields";

    [Fact]
    public async Task ApplyAsync_RequireExistingDatabase_RefusesMissingFile()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "SQLite.db");
        var workspace = new SqliteDatabaseWorkspace(databasePath);

        try
        {
            await using var context = CreateContext(workspace.ConnectionString);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                DatabaseMigrationStartup.ApplyAsync(
                    context,
                    workspace,
                    requireExistingDatabase: true,
                    backupBeforeMigrate: true,
                    NullLogger.Instance));

            Assert.Contains("GF3_DATA_VOLUME", exception.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(databasePath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyAsync_ExistingProductionSchema_BacksUpMigratesAndPreservesData()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "SQLite.db");
        var workspace = new SqliteDatabaseWorkspace(databasePath);

        try
        {
            await using (var oldContext = CreateContext(workspace.ConnectionString))
            {
                var migrator = oldContext.GetService<IMigrator>();
                await migrator.MigrateAsync(PreviousProductionMigration);
                await SeedExistingProductionDataAsync(oldContext);
            }

            await using (var migrationContext = CreateContext(workspace.ConnectionString))
            {
                var result = await DatabaseMigrationStartup.ApplyAsync(
                    migrationContext,
                    workspace,
                    requireExistingDatabase: true,
                    backupBeforeMigrate: true,
                    NullLogger.Instance);

                Assert.True(result.DatabaseExisted);
                Assert.Contains("20260720120000_AddEmployeeSessionVersion", result.AppliedMigrations);
                Assert.NotNull(result.BackupPath);
                Assert.True(File.Exists(result.BackupPath));
            }

            await using var verificationContext = CreateContext(workspace.ConnectionString);
            Assert.Equal(2, await verificationContext.Employees.CountAsync());
            var migratedSchedule = Assert.Single(await verificationContext.Schedules.ToListAsync());
            Assert.True(migratedSchedule.AllowSwap);
            Assert.Single(await verificationContext.ScheduleSlots.ToListAsync());
            Assert.Single(await verificationContext.ShiftSwapRequests.ToListAsync());
            var importedVersion = Assert.Single(await verificationContext.ScheduleVersions.ToListAsync());
            Assert.Equal(1, importedVersion.VersionNumber);
            Assert.Equal("main", importedVersion.BranchName);
            Assert.Equal("Production import", importedVersion.CreatedByManagerName);
            Assert.Contains("Release schedule", importedVersion.SnapshotJson, StringComparison.Ordinal);
            using var importedSnapshot = JsonDocument.Parse(importedVersion.SnapshotJson);
            Assert.Equal(JsonValueKind.String, importedSnapshot.RootElement.GetProperty("publicationStatus").ValueKind);
            Assert.Equal(JsonValueKind.True, importedSnapshot.RootElement.GetProperty("allowSwap").ValueKind);
            Assert.Single(importedSnapshot.RootElement.GetProperty("slots").EnumerateArray());
            Assert.Equal(
                JsonValueKind.String,
                importedSnapshot.RootElement.GetProperty("slots")[0].GetProperty("status").ValueKind);
            Assert.Equal(importedVersion.Id, Assert.Single(await verificationContext.ScheduleVersionStates.ToListAsync()).CurrentVersionId);

            var history = Assert.Single(await verificationContext.ShiftSwapHistories.ToListAsync());
            Assert.Equal("Release schedule", history.ScheduleName);
            Assert.Equal("Alice Existing", history.FromEmployeeName);
            Assert.Equal("Bob Existing", history.AcceptedByEmployeeName);
            Assert.Empty(await verificationContext.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    private static AppDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedExistingProductionDataAsync(AppDbContext context)
    {
        var container = new ContainerModel { Name = "Release container" };
        var shop = new ShopModel { Name = "Release shop", Address = "Production address" };
        var alice = new EmployeeModel { FirstName = "Alice", LastName = "Existing" };
        var bob = new EmployeeModel { FirstName = "Bob", LastName = "Existing" };

        context.AddRange(container, shop, alice, bob);
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO schedule
                (container_id, shop_id, name, year, month, publication_status, people_per_shift,
                 shift1_time, shift2_time, max_hours_per_emp_month, max_consecutive_days,
                 max_consecutive_full, max_full_per_month)
            VALUES
                ({container.Id}, {shop.Id}, {"Release schedule"}, {2026}, {7}, {SchedulePublicationStatus.Public.ToString()}, {1},
                 {"09:00 - 17:00"}, {"17:00 - 22:00"}, {200}, {6}, {3}, {10});
            """);
        var scheduleId = checked((int)await context.Database
            .SqlQueryRaw<long>("SELECT last_insert_rowid() AS Value")
            .SingleAsync());

        var slot = new ScheduleSlotModel
        {
            ScheduleId = scheduleId,
            DayOfMonth = 5,
            SlotNo = 1,
            EmployeeId = alice.Id,
            Status = SlotStatus.ASSIGNED,
            FromTime = "09:00",
            ToTime = "17:00",
        };
        context.ScheduleSlots.Add(slot);
        await context.SaveChangesAsync();

        // Seed the historical schema without columns introduced by later migrations.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO shift_swap_request
                (schedule_id, schedule_slot_id, from_employee_id, accepted_by_employee_id,
                 visibility, status, created_at_utc, accepted_at_utc)
            VALUES ({scheduleId}, {slot.Id}, {alice.Id}, {bob.Id}, {"Public"}, {"Accepted"},
                {new DateTimeOffset(2026, 6, 25, 8, 0, 0, TimeSpan.Zero)},
                {new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.Zero)});
            """);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
