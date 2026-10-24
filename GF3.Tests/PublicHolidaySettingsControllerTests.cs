using System.Net;
using System.Net.Http.Json;
using System.Text;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApi.Auth;
using WebApi.Contracts.PublicHolidays;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class PublicHolidaySettingsControllerTests
{
    [Fact]
    public async Task SeedPersistenceIdempotenceAndMissingRowRecovery()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var db = database.CreateContext())
        {
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.True((await db.PublicHolidaySettings.AsNoTracking().SingleAsync()).ShowPublicHolidays);
            var controller = new PublicHolidaySettingsController(db);
            Assert.True(Value(await controller.Get(default)).ShowPublicHolidays);
            foreach (var enabled in new[] { false, false, true, false })
                Assert.Equal(enabled, Value(await controller.Update(new() { ShowPublicHolidays = enabled }, default)).ShowPublicHolidays);
        }
        await using (var db = database.CreateContext())
        {
            Assert.False((await db.PublicHolidaySettings.SingleAsync()).ShowPublicHolidays);
            Assert.Equal(1, await db.PublicHolidaySettings.CountAsync());
            await db.PublicHolidaySettings.ExecuteDeleteAsync();
            var controller = new PublicHolidaySettingsController(db);
            Assert.True(Value(await controller.Get(default)).ShowPublicHolidays);
            Assert.Empty(await db.PublicHolidaySettings.ToListAsync());
            await controller.Update(new() { ShowPublicHolidays = false }, default);
            await controller.Update(new() { ShowPublicHolidays = true }, default);
            Assert.True((await db.PublicHolidaySettings.AsNoTracking().SingleAsync()).ShowPublicHolidays);
            Assert.Equal(1, await db.PublicHolidaySettings.CountAsync());
        }
    }

    [Fact]
    public async Task MigrationPreservesExistingDataAndEnsureCreatedSeedsDefault()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        var holidayMigrationIndex = Array.FindIndex(migrations, migration => migration.EndsWith("_AddPublicHolidayDisplaySettings", StringComparison.Ordinal));
        Assert.True(holidayMigrationIndex > 0);
        await db.GetService<IMigrator>().MigrateAsync(migrations[holidayMigrationIndex - 1]);
        db.Employees.Add(new EmployeeModel { FirstName = "Retained", LastName = "Employee" });
        await db.SaveChangesAsync();
        await db.Database.MigrateAsync();
        Assert.Equal("Retained", (await db.Employees.SingleAsync()).FirstName);
        Assert.True((await db.PublicHolidaySettings.SingleAsync()).ShowPublicHolidays);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        Assert.True((await db.PublicHolidaySettings.SingleAsync()).ShowPublicHolidays);
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.ExecuteSqlRawAsync(
            "INSERT INTO public_holiday_settings (id, show_public_holidays) VALUES (2, 1)"));
    }

    [Fact]
    public void AuthorizationMetadataHasBothReadRolesAndManagerOnlyWrite()
    {
        var type = typeof(PublicHolidaySettingsController);
        var readRoles = Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AuthRoles.Manager + "," + AuthRoles.Employee, readRoles.Roles);
        var writeRoles = Assert.Single(type.GetMethod("Update")!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AuthRoles.Manager, writeRoles.Roles);
    }

    [Fact]
    public async Task HttpPipelineEnforcesAuthorizationValidationAndGlobalState()
    {
        const string endpoint = "/api/public-holiday-settings";
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync(endpoint)).StatusCode);
        await host.Login("employee");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync(endpoint, new { showPublicHolidays = false })).StatusCode);
        await host.Login("manager");
        await host.Exchange("phone");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync(endpoint, new { showPublicHolidays = false })).StatusCode);
        await host.Exchange("pc");
        foreach (var json in new[] { "{}", "{\"showPublicHolidays\":null}", "{\"showPublicHolidays\":\"false\"}", "{" })
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"))).StatusCode);
        var response = await host.Client.PutAsJsonAsync(endpoint, new { showPublicHolidays = false });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<PublicHolidayDisplaySettingsDto>())!.ShowPublicHolidays);
        await host.Login("employee");
        Assert.False((await host.Client.GetFromJsonAsync<PublicHolidayDisplaySettingsDto>(endpoint))!.ShowPublicHolidays);
    }

    private static PublicHolidayDisplaySettingsDto Value(ActionResult<PublicHolidayDisplaySettingsDto> result) =>
        Assert.IsType<PublicHolidayDisplaySettingsDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
}
