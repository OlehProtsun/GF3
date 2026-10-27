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
        Assert.Equal(new(8, true), Value(await controller.Get(default)));
        foreach (var hours in new[] { 1, 12, 6, 6 })
            Assert.Equal(new(hours, false), Value(await controller.Update(new() { ShiftHours = hours, WeekendsOff = false }, default)));
        Assert.Equal(1, await db.PriorityGenerationSettings.CountAsync());
        await db.PriorityGenerationSettings.ExecuteDeleteAsync();
        Assert.Equal(new(8, true), Value(await controller.Get(default)));
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
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
        await db.Database.MigrateAsync();
        Assert.Equal(employeeId, (await db.Employees.SingleAsync()).Id);
        var assignment = await db.ScheduleEmployees.SingleAsync();
        Assert.Equal(graphId, assignment.ScheduleId);
        Assert.False(assignment.IsPriority);
        Assert.Equal(containerId, (await db.Schedules.SingleAsync()).ContainerId);
        Assert.Equal(new(8, true), Value(await new PriorityGenerationSettingsController(db).Get(default)));
        Assert.False(db.Database.HasPendingModelChanges());
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
        Assert.Equal(new(10, false), await (await host.Client.PutAsJsonAsync(endpoint, new { shiftHours = 10, weekendsOff = false })).Content.ReadFromJsonAsync<PriorityGenerationSettingsDto>());
        Assert.Equal(new(10, false), await host.Client.GetFromJsonAsync<PriorityGenerationSettingsDto>(endpoint));
    }

    [Fact]
    public async Task GraphFlagsRoundTripAndOmittedUpdateRetainsPriority()
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
            graphId = 1,
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

    private static PriorityGenerationSettingsDto Value(ActionResult<PriorityGenerationSettingsDto> result) =>
        Assert.IsType<PriorityGenerationSettingsDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
}
