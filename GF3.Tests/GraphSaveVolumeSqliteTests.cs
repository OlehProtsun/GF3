using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using WebApi.Services;
using Xunit.Abstractions;

namespace GF3.Tests;

public sealed class GraphSaveVolumeSqliteTests(ITestOutputHelper output)
{
    [Fact]
    public async Task TenGraphs_FiveHundredCells_ReloadsSlotsStylesAndDeduplicatedVersions()
    {
        await using var fixture = await SqliteTestDatabase.CreateAsync();
        var snapshot = Environment.GetEnvironmentVariable("GF3_SAVE_SQLITE_SNAPSHOT");
        if (!string.IsNullOrEmpty(snapshot))
        {
            await using var source = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={snapshot};Mode=ReadOnly;Pooling=False");
            await using var target = new Microsoft.Data.Sqlite.SqliteConnection(fixture.ConnectionString);
            await source.OpenAsync();
            await target.OpenAsync();
            source.BackupDatabase(target);
        }
        await using var db = fixture.CreateContext();
        await db.Database.MigrateAsync();
        var container = TestDataFactory.CreateDalContainer($"Save volume {Guid.NewGuid():N}");
        var shop = TestDataFactory.CreateDalShop($"Save volume {Guid.NewGuid():N}");
        var employees = Enumerable.Range(1, 20).Select(i => TestDataFactory.CreateDalEmployee("Volume", i.ToString())).ToArray();
        db.AddRange(container, shop);
        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();
        var graphs = Enumerable.Range(1, 10).Select(i => TestDataFactory.CreateDalSchedule(container.Id, shop.Id, $"Volume {i}", 2026, 11)).ToArray();
        db.Schedules.AddRange(graphs);
        await db.SaveChangesAsync();
        foreach (var graph in graphs)
        {
            db.ScheduleEmployees.AddRange(employees.Select((employee, index) => new ScheduleEmployeeModel
            { ScheduleId = graph.Id, EmployeeId = employee.Id, DisplayOrder = index }));
            db.ScheduleSlots.AddRange(Slots(graph.Id, employees, "08:00", "12:00"));
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var versions = new ScheduleVersionCommitService(db);
        var styles = new ScheduleCellStyleRepository(db);
        var slots = new ScheduleSlotRepository(db);
        foreach (var graph in graphs) await versions.EnsureInitialVersionAsync(graph.Id, default);
        foreach (var graph in graphs)
        {
            await slots.ReplaceForScheduleAsync(graph.Id, Slots(graph.Id, employees, "09:00", "13:00"), true);
            var patches = employees.SelectMany((employee, index) => Enumerable.Range(1, 25).Select(day =>
                new ScheduleCellStyleFieldPatchModel(employee.Id, day, true, null, -15584170, true, null,
                    (index * 25 + day) % 2 == 1 ? -5517841 : null))).ToArray();
            Assert.True(await styles.ApplyFieldPatchesIfUnchangedAsync(graph.Id, patches));
            Assert.NotNull(await versions.CommitAsync(graph.Id, null, "Save volume fixture", default));
            Assert.False(await styles.ApplyFieldPatchesIfUnchangedAsync(graph.Id, patches));
            Assert.Null(await versions.CommitAsync(graph.Id, null, "Save volume fixture", default));
        }
        await using var reloaded = fixture.CreateContext();
        foreach (var graph in graphs)
        {
            var actual = await reloaded.Schedules.AsNoTracking().AsSplitQuery()
                .Include(row => row.Employees).Include(row => row.Slots).Include(row => row.CellStyles)
                .SingleAsync(row => row.Id == graph.Id);
            Assert.Equal(employees.Select(row => row.Id), actual.Employees.OrderBy(row => row.DisplayOrder).Select(row => row.EmployeeId));
            Assert.Equal(500, actual.Slots.Count);
            Assert.All(actual.Slots, row => { Assert.Equal("09:00", row.FromTime); Assert.Equal("13:00", row.ToTime); });
            Assert.Equal(Enumerable.Range(1, 20), actual.Slots.Where(row => row.DayOfMonth == 1).OrderBy(row => row.SlotNo).Select(row => row.SlotNo));
            Assert.Equal(500, actual.CellStyles.Count);
            Assert.All(actual.CellStyles, row => Assert.Equal(-15584170, row.BackgroundColorArgb));
            Assert.Equal(250, actual.CellStyles.Count(row => row.TextColorArgb == -5517841));
            Assert.Equal(2, await reloaded.ScheduleVersions.CountAsync(row => row.ScheduleId == graph.Id));
        }
        output.WriteLine($"SQLite {(snapshot is null ? "seeded fixture" : "isolated local snapshot")}: 10 graphs, 5000 styles, 5000 slots, 10 new versions; idempotent replay adds zero versions.");
    }

    private static IEnumerable<ScheduleSlotModel> Slots(int graphId, EmployeeModel[] employees, string from, string to) =>
        employees.SelectMany((employee, index) => Enumerable.Range(1, 25).Select(day =>
            TestDataFactory.CreateDalSlot(graphId, day, index + 1, employee.Id, from, to)));
}
