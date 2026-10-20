using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Tests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GF3.Tests;

public sealed class SchedulePresetGlobalScopeMigrationTests
{
    private const string Baseline = "20261019193512_TrackAvailabilityNotificationEvents";

    [Fact]
    public async Task Upgrade_PreservesRowsAndConstraints_AndDowngradeRefusesOrphans()
    {
        var root = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={Path.Combine(root, "migration.db")};Pooling=False").Options;
        try
        {
            await using var context = new AppDbContext(options);
            await context.GetService<IMigrator>().MigrateAsync(Baseline);
            var a = TestDataFactory.CreateDalContainer("A");
            var b = TestDataFactory.CreateDalContainer("B");
            var shop = TestDataFactory.CreateDalShop();
            var employee = TestDataFactory.CreateDalEmployee();
            var group = new AvailabilityGroupModel { Name = "Availability", Year = 2026, Month = 4 };
            context.AddRange(a, b, shop, employee, group);
            await context.SaveChangesAsync();
            SchedulePresetModel Preset(int origin) => new()
            {
                ContainerId = origin, Name = "Shared name", ScheduleName = "Schedule", ShopId = shop.Id,
                Year = 2026, Month = 4, PeoplePerShift = 2, Shift1Time = "08:00 - 16:00", Shift2Time = "16:00 - 20:00",
                MaxHoursPerEmpMonth = 160, MaxConsecutiveDays = 5, MaxConsecutiveFull = 2, MaxFullPerMonth = 4,
                AvailabilityGroupId = group.Id,
                Employees = [new() { EmployeeId = employee.Id, MinHoursMonth = 80 }],
            };
            var first = Preset(a.Id);
            var second = Preset(b.Id);
            context.AddRange(first, second);
            await context.SaveChangesAsync();
            var before = await Snapshot(context);
            var childBefore = await ChildSnapshot(context);
            var schemaBefore = await TableSql(context);
            var indexesBefore = await IndexSnapshot(context);
            await context.Database.MigrateAsync();
            context.ChangeTracker.Clear();
            Assert.Equal(before, await Snapshot(context));
            Assert.Equal(childBefore, await ChildSnapshot(context));
            Assert.Equal(indexesBefore, await IndexSnapshot(context));
            var schemaAfter = await TableSql(context);
            foreach (var constraint in new[] { "ck_schedule_preset_month", "ck_schedule_preset_people_per_shift",
                "ck_schedule_preset_max_hours_per_emp_month", "ck_schedule_preset_max_consecutive_days",
                "ck_schedule_preset_max_consecutive_full", "ck_schedule_preset_max_full_per_month",
                "ck_schedule_preset_shift1_format", "ck_schedule_preset_shift2_format" })
            {
                Assert.Contains(constraint, schemaBefore);
                Assert.Contains(constraint, schemaAfter);
            }
            Assert.Contains("ON DELETE RESTRICT", schemaAfter);
            Assert.Contains("ON DELETE SET NULL", schemaAfter);
            // A safe downgrade with all origins present must retain every value and child.
            await context.GetService<IMigrator>().MigrateAsync(Baseline);
            Assert.Equal(before, await Snapshot(context));
            Assert.Equal(childBefore, await ChildSnapshot(context));
            await context.Database.MigrateAsync();
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM container WHERE id = {a.Id}");
            var retained = await context.SchedulePresets.AsNoTracking().Include(p => p.Employees).SingleAsync(p => p.Id == first.Id);
            Assert.Null(retained.ContainerId);
            Assert.Equal(first.Employees.Single().Id, Assert.Single(retained.Employees).Id);
            Assert.Equal(80, retained.Employees.Single().MinHoursMonth);
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM container WHERE id = {b.Id}");
            Assert.Equal(2, await context.SchedulePresets.CountAsync(p => p.ContainerId == null));
            var orphanSnapshot = await Snapshot(context);
            await context.Database.MigrateAsync();
            Assert.Equal(orphanSnapshot, await Snapshot(context));
            Assert.Equal(childBefore, await ChildSnapshot(context));
            await Assert.ThrowsAsync<SqliteException>(() => context.GetService<IMigrator>().MigrateAsync(Baseline));
            Assert.Equal(orphanSnapshot, await Snapshot(context));
            Assert.Equal(childBefore, await ChildSnapshot(context));
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            await context.Database.OpenConnectionAsync();
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA foreign_key_check";
            await using (var reader = await command.ExecuteReaderAsync()) Assert.False(await reader.ReadAsync());
            command.CommandText = "PRAGMA integrity_check";
            Assert.Equal("ok", await command.ExecuteScalarAsync());
            command.CommandText = "PRAGMA foreign_key_list(schedule_preset)";
            var foreignKeys = new Dictionary<string, string>();
            await using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) foreignKeys.Add(reader.GetString(3), reader.GetString(6));
            Assert.Equal("SET NULL", foreignKeys["container_id"]);
            Assert.Equal("SET NULL", foreignKeys["availability_group_id"]);
            Assert.Equal("RESTRICT", foreignKeys["shop_id"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<string[]> Snapshot(AppDbContext context) =>
        (await context.SchedulePresets.AsNoTracking().OrderBy(p => p.Id).ToListAsync())
        .Select(p => System.Text.Json.JsonSerializer.Serialize(p)).ToArray();

    private static async Task<string[]> ChildSnapshot(AppDbContext context) =>
        await context.Set<SchedulePresetEmployeeModel>().AsNoTracking().OrderBy(p => p.Id)
            .Select(p => $"{p.Id}:{p.SchedulePresetId}:{p.EmployeeId}:{p.MinHoursMonth}").ToArrayAsync();

    private static async Task<string> TableSql(AppDbContext context) =>
        await context.Database.SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='schedule_preset'").SingleAsync();

    private static async Task<string[]> IndexSnapshot(AppDbContext context) =>
        await context.Database.SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='index' AND tbl_name='schedule_preset'").OrderBy(s => s).ToArrayAsync();
}
