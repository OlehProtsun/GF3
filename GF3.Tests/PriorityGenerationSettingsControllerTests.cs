using System.Net;
using System.Net.Http.Json;
using System.Text;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApi.Contracts.Priority;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class PriorityGenerationSettingsControllerTests
{
    [Fact]
    public async Task DefaultsPersistenceRecoveryAndConstraints()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        Assert.False(db.Database.HasPendingModelChanges());
        var controller = new PriorityGenerationSettingsController(db);
        Assert.Equal(new(8, true, true), Value(await controller.Get(default)));
        foreach (var hours in new[] { 1, 12, 6, 6 })
            Assert.Equal(new(hours, false, true), Value(await controller.Update(new() { ShiftHours = hours, WeekendsOff = false }, default)));
        Assert.Equal(new(6, false, false), Value(await controller.Update(new() { ShiftHours = 6, WeekendsOff = false, RespectAvailability = false }, default)));
        Assert.Equal(new(10, true, false), Value(await controller.Update(new() { ShiftHours = 10, WeekendsOff = true }, default)));
        Assert.Equal(1, await db.PriorityGenerationSettings.CountAsync());
        await db.PriorityGenerationSettings.ExecuteDeleteAsync();
        Assert.Equal(new(8, true, true), Value(await controller.Get(default)));
        Assert.Empty(await db.PriorityGenerationSettings.ToListAsync());
        await controller.Update(new() { ShiftHours = 8, WeekendsOff = true }, default);
        Assert.Equal(1, await db.PriorityGenerationSettings.CountAsync());
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.ExecuteSqlRawAsync(
            "INSERT INTO priority_generation_settings (id, shift_hours, weekends_off) VALUES (2, 8, 1)"));
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE priority_generation_settings SET shift_hours = 13"));
    }

    [Fact]
    public async Task MigrationRetainsAssignmentsAndDefaultsFalse()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var (containerId, graphId, employeeId) = await ContainerServiceTests.SeedGenerationAsync(database);
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        await db.GetService<IMigrator>().MigrateAsync(migrations[^3]);
        await db.Database.MigrateAsync();
        Assert.Equal(employeeId, (await db.Employees.SingleAsync()).Id);
        var assignment = await db.ScheduleEmployees.SingleAsync();
        Assert.Equal(graphId, assignment.ScheduleId);
        Assert.False(assignment.IsPriority);
        Assert.Equal(containerId, (await db.Schedules.SingleAsync()).ContainerId);
        Assert.Equal(new(8, true, true), Value(await new PriorityGenerationSettingsController(db).Get(default)));
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task RespectAvailabilityMigrationPreservesSettingsEnrollmentAndReversesOnlyItsColumn()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var (_, graphId, _) = await ContainerServiceTests.SeedGenerationAsync(database);
        await using var db = database.CreateContext();
        await db.ScheduleEmployees.Where(e => e.ScheduleId == graphId).ExecuteUpdateAsync(s => s.SetProperty(e => e.IsPriority, true));
        await db.PriorityGenerationSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.ShiftHours, 10)
            .SetProperty(x => x.WeekendsOff, false).SetProperty(x => x.RespectAvailability, false));
        var migrator = db.GetService<IMigrator>();
        const string previous = "20261027160625_AddPriorityGeneration";
        await migrator.MigrateAsync(previous);
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('priority_generation_settings') WHERE name='respect_availability'";
        Assert.Equal(0L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT shift_hours || ':' || weekends_off FROM priority_generation_settings WHERE id=1";
        Assert.Equal("10:0", await command.ExecuteScalarAsync());
        Assert.True((await db.ScheduleEmployees.SingleAsync()).IsPriority);
        await migrator.MigrateAsync();
        Assert.Equal(new(10, false, true), Value(await new PriorityGenerationSettingsController(db).Get(default)));
        Assert.True((await db.ScheduleEmployees.SingleAsync()).IsPriority);
        command.CommandText = "PRAGMA quick_check";
        Assert.Equal("ok", await command.ExecuteScalarAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task ConcurrentOldAndNewClientWritesRetainOneSingleton()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        async Task Write(bool? respect)
        {
            await using var db = database.CreateContext();
            var result = await new PriorityGenerationSettingsController(db).Update(new() {
                ShiftHours = 8, WeekendsOff = true, RespectAvailability = respect }, default);
            Assert.False(Value(result).RespectAvailability);
        }
        await Write(false);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() => Write(i % 2 == 0 ? null : false))));
        await using var verify = database.CreateContext();
        Assert.False((await verify.PriorityGenerationSettings.SingleAsync()).RespectAvailability);
    }

    [Fact]
    public async Task HttpAuthorizationAndValidation()
    {
        const string endpoint = "/api/priority-generation-settings";
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PutAsJsonAsync(endpoint, new { shiftHours = 8, weekendsOff = true })).StatusCode);
        await host.Login("employee");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync(endpoint, new { shiftHours = 8, weekendsOff = true })).StatusCode);
        await host.Login("manager");
        await host.Exchange("phone");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync(endpoint, new { shiftHours = 8, weekendsOff = true })).StatusCode);
        await host.Exchange("pc");
        foreach (var json in new[] { "{}", "{\"shiftHours\":null,\"weekendsOff\":true}", "{\"shiftHours\":0,\"weekendsOff\":true}",
            "{\"shiftHours\":13,\"weekendsOff\":true}", "{\"shiftHours\":-1,\"weekendsOff\":true}", "{\"shiftHours\":8.5,\"weekendsOff\":true}",
            "{\"shiftHours\":\"x\",\"weekendsOff\":true}", "{\"shiftHours\":8}", "{\"shiftHours\":8,\"weekendsOff\":null}" })
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(new(10, false, true), await (await host.Client.PutAsJsonAsync(endpoint, new { shiftHours = 10, weekendsOff = false })).Content.ReadFromJsonAsync<PriorityGenerationSettingsDto>());
        Assert.Equal(new(10, false, false), await (await host.Client.PutAsJsonAsync(endpoint, new { shiftHours = 10, weekendsOff = false, respectAvailability = false })).Content.ReadFromJsonAsync<PriorityGenerationSettingsDto>());
        Assert.Equal(new(10, false, false), await (await host.Client.PutAsJsonAsync(endpoint, new { shiftHours = 10, weekendsOff = false })).Content.ReadFromJsonAsync<PriorityGenerationSettingsDto>());
        Assert.Equal(new(10, false, false), await host.Client.GetFromJsonAsync<PriorityGenerationSettingsDto>(endpoint));
        using var settingsJson = System.Text.Json.JsonDocument.Parse(await host.Client.GetStringAsync(endpoint));
        Assert.Equal(3, settingsJson.RootElement.EnumerateObject().Count());
        Assert.False(settingsJson.RootElement.GetProperty("respectAvailability").GetBoolean());
    }

    [Theory]
    [InlineData(null)] [InlineData(1)]
    public async Task GraphFlagsRoundTripAndOmittedUpdateRetainsPriority(int? graphId)
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        await host.Login("manager"); await host.Exchange("pc");
        const string endpoint = "/api/containers/1/graphs/1/employees";
        var added = await host.Client.PostAsJsonAsync(endpoint, new { employeeId = 1, minHoursMonth = 70, displayOrder = 0, isPriority = true });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var employee = (await added.Content.ReadFromJsonAsync<WebApi.Contracts.Containers.Graphs.Employees.GraphEmployeeDto>())!;
        Assert.True(employee.IsPriority);
        var update = endpoint + "/" + employee.Id;
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(update, new { employeeId = 1, minHoursMonth = 70, displayOrder = 0 })).StatusCode);
        Assert.True((await host.Client.GetFromJsonAsync<WebApi.Contracts.Containers.Graphs.Employees.GraphEmployeeDto[]>(endpoint))!.Single().IsPriority);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(update, new { employeeId = 1, minHoursMonth = 70, displayOrder = 0, isPriority = false })).StatusCode);
        Assert.False((await host.Client.GetFromJsonAsync<WebApi.Contracts.Containers.Graphs.Employees.GraphEmployeeDto[]>(endpoint))!.Single().IsPriority);
        var preview = await host.Client.PostAsJsonAsync("/api/containers/1/graphs/generate-preview", new {
            graphId,
            graph = new { name = "Synthetic", shopId = 1, year = 2026, month = 4, peoplePerShift = 1,
                shift1Time = "09:00 - 15:00", shift2Time = "15:00 - 21:00", maxHoursPerEmpMonth = 160,
                maxConsecutiveDays = 5, maxConsecutiveFull = 3, maxFullPerMonth = 10 },
            employees = new[] { new { employeeId = 1, minHoursMonth = 70, displayOrder = 0, isPriority = true } }
        });
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        var result = (await preview.Content.ReadFromJsonAsync<WebApi.Contracts.Containers.Graphs.GenerateGraphResponse>())!;
        Assert.NotEmpty(result.Slots!);
        Assert.All(result.Slots!.Where(slot => slot.EmployeeId == 1), slot => Assert.Equal(8, (TimeSpan.Parse(slot.ToTime) - TimeSpan.Parse(slot.FromTime)).TotalHours));
        await using var db = host.Database.CreateContext();
        Assert.False((await db.ScheduleEmployees.SingleAsync()).IsPriority);
        Assert.Empty(await db.ScheduleSlots.ToListAsync());
    }

    [Fact]
    public async Task FreshHttpPreviewDistinguishesInfeasibleMalformedAndMissingAvailability()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        await host.Login("manager"); await host.Exchange("pc");
        const string endpoint = "/api/containers/1/graphs/generate-preview";
        var request = new WebApi.Contracts.Containers.Graphs.GenerateGraphPreviewRequest {
            Graph = new() { Name = "Fresh synthetic", ShopId = 1, Year = 2026, Month = 11, PeoplePerShift = 4,
                Shift1Time = "09:00 - 15:00", Shift2Time = "15:00 - 22:00", MaxHoursPerEmpMonth = 80,
                MaxConsecutiveDays = 31, MaxConsecutiveFull = 31, MaxFullPerMonth = 31 },
            Employees = [new() { EmployeeId = 1, IsPriority = true, MinHoursMonth = 180 }] };
        var before = await ContainerServiceTests.GenerationSnapshotAsync(host.Database);
        var response = await host.Client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<WebApi.Contracts.Containers.Graphs.GenerateGraphResponse>())!;
        Assert.Contains(result.Slots!, s => s.EmployeeId == 1);
        Assert.Contains(result.Slots!, s => s.EmployeeId is null);
        request.Graph.Month = 13;
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync(endpoint, request)).StatusCode);
        request.Graph.Month = 11; request.Graph.AvailabilityGroupId = int.MaxValue;
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PostAsJsonAsync(endpoint, request)).StatusCode);
        Assert.Equal(before, await ContainerServiceTests.GenerationSnapshotAsync(host.Database));
    }

    private static PriorityGenerationSettingsDto Value(ActionResult<PriorityGenerationSettingsDto> result) =>
        Assert.IsType<PriorityGenerationSettingsDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
}
