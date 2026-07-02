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
                Assert.Equal(4, result.AppliedMigrations.Count);
                Assert.NotNull(result.BackupPath);
                Assert.True(File.Exists(result.BackupPath));
            }

            await using var verificationContext = CreateContext(workspace.ConnectionString);
            Assert.Equal(2, await verificationContext.Employees.CountAsync());
            Assert.Single(await verificationContext.Schedules.ToListAsync());
            Assert.Single(await verificationContext.ScheduleSlots.ToListAsync());
            Assert.Single(await verificationContext.ShiftSwapRequests.ToListAsync());

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

        var schedule = new ScheduleModel
        {
            ContainerId = container.Id,
            ShopId = shop.Id,
            Name = "Release schedule",
            Year = 2026,
            Month = 7,
            PublicationStatus = SchedulePublicationStatus.Public,
            PeoplePerShift = 1,
            Shift1Time = "09:00 - 17:00",
            Shift2Time = "17:00 - 22:00",
            MaxHoursPerEmpMonth = 200,
            MaxConsecutiveDays = 6,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
        };
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        var slot = new ScheduleSlotModel
        {
            ScheduleId = schedule.Id,
            DayOfMonth = 5,
            SlotNo = 1,
            EmployeeId = alice.Id,
            Status = SlotStatus.ASSIGNED,
            FromTime = "09:00",
            ToTime = "17:00",
        };
        context.ScheduleSlots.Add(slot);
        await context.SaveChangesAsync();

        context.ShiftSwapRequests.Add(new ShiftSwapRequestModel
        {
            ScheduleId = schedule.Id,
            ScheduleSlotId = slot.Id,
            FromEmployeeId = alice.Id,
            AcceptedByEmployeeId = bob.Id,
            Visibility = ShiftSwapVisibility.Public,
            Status = ShiftSwapStatus.Accepted,
            CreatedAtUtc = new DateTimeOffset(2026, 6, 25, 8, 0, 0, TimeSpan.Zero),
            AcceptedAtUtc = new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.Zero),
        });
        await context.SaveChangesAsync();
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}