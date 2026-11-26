using System.Net;
using System.Text.Json;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;
using WebApi.Services;

namespace GF3.Tests;

public sealed class ShiftCorrectionScheduleCandidatesTests
{
    [Fact]
    public async Task CatalogRejectsMissingOrDeletedEmployee()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var deleted = TestDataFactory.CreateDalEmployee("Deleted", "Fixture");
        deleted.IsDeleted = true;
        db.Add(deleted);
        await db.SaveChangesAsync();
        var controller = new EmployeeShiftCorrectionsController(db, null!, null!, null!);
        foreach (var employeeId in new[] { deleted.Id, int.MaxValue })
        {
            controller.ControllerContext = EmployeePublicSchedulesTests.Context(employeeId);
            Assert.IsType<ForbidResult>((await controller.GetSchedules(new ScheduleLastUpdateService(db), default)).Result);
        }
    }

    [Fact]
    public async Task CatalogReturnsAllPublicSchedulesAndOnlyRequestersSlotsWithoutEnrolling()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        int employeeId;
        int ownSlotId;
        await using (var db = host.Database.CreateContext())
        {
            employeeId = await db.Employees.Select(employee => employee.Id).SingleAsync();
            var fixture = await EmployeePublicSchedulesTests.Seed(db, "Catalog Shop");
            var ownSlot = TestDataFactory.CreateDalSlot(fixture.Preview, 8, 1, employeeId, "08:00", "12:00");
            db.Add(ownSlot);
            await db.SaveChangesAsync();
            ownSlotId = ownSlot.Id;
        }

        const string route = "/api/employee-shift-corrections/schedules";
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync(route)).StatusCode);
        await host.Login("manager");
        await host.Exchange("pc");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync(route)).StatusCode);
        await host.Login("employee");
        await using (var db = host.Database.CreateContext())
        {
            var account = await db.EmployeeAccounts.SingleAsync();
            account.LastSeenAtUtc = null;
            await db.SaveChangesAsync();
        }
        using var response = await host.Client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = body.RootElement.EnumerateArray().ToList();
        Assert.Equal(new[] { "Alpha", "Other container", "Own", "Zulu", "July" },
            rows.Select(row => row.GetProperty("name").GetString()));
        Assert.All(rows, row =>
        {
            Assert.Equal("public", row.GetProperty("publicationStatus").GetString());
            Assert.Equal(new[] { "id", "lastUpdatedAtUtc", "month", "name", "publicationStatus", "slots", "year" },
                row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        });
        var slot = Assert.Single(rows.SelectMany(row => row.GetProperty("slots").EnumerateArray()));
        Assert.Equal(ownSlotId, slot.GetProperty("id").GetInt32());
        Assert.Equal(employeeId, slot.GetProperty("employeeId").GetInt32());

        await using (var db = host.Database.CreateContext())
        {
            Assert.False(await db.ScheduleEmployees.AnyAsync(member => member.EmployeeId == employeeId));
            Assert.Empty(await db.ShiftCorrectionRequests.ToListAsync());
            Assert.Null(await db.EmployeeAccounts.Select(account => account.LastSeenAtUtc).SingleAsync());
            await db.Schedules.ExecuteUpdateAsync(setters => setters.SetProperty(schedule => schedule.PublicationStatus, SchedulePublicationStatus.Private));
        }
        using var empty = await host.Client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal("[]", await empty.Content.ReadAsStringAsync());
    }
}
