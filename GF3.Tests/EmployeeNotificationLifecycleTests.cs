using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class EmployeeNotificationLifecycleTests
{
    [Fact]
    public async Task PublicationTimestamp_ChangesOnlyWhenPublished_IncludingBulkPublication()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var container = TestDataFactory.CreateDalContainer("Notifications");
        var shop = TestDataFactory.CreateDalShop("Notifications");
        db.AddRange(container, shop);
        await db.SaveChangesAsync();
        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        schedule.PublicationStatus = SchedulePublicationStatus.Private;
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync();
        Assert.Null(schedule.PublishedAtUtc);
        schedule.PublicationStatus = SchedulePublicationStatus.Public;
        await db.SaveChangesAsync();
        var firstPublished = schedule.PublishedAtUtc;
        Assert.NotNull(firstPublished);
        schedule.Name = "Edited";
        schedule.PublishedAtUtc = null; // DTO updates cannot erase the server timestamp.
        await db.SaveChangesAsync();
        Assert.Equal(firstPublished, schedule.PublishedAtUtc);
        var repository = new ScheduleRepository(db);
        await repository.UpdatePublicationByContainerAsync(container.Id, SchedulePublicationStatus.Public, true);
        await db.Entry(schedule).ReloadAsync();
        Assert.Equal(firstPublished, schedule.PublishedAtUtc);
        await repository.UpdatePublicationByContainerAsync(container.Id, SchedulePublicationStatus.Private, null);
        await repository.UpdatePublicationByContainerAsync(container.Id, SchedulePublicationStatus.Public, null);
        await db.Entry(schedule).ReloadAsync();
        Assert.True(schedule.PublishedAtUtc > firstPublished);
        schedule.PublicationStatus = SchedulePublicationStatus.Private;
        await db.SaveChangesAsync();
        schedule.PublicationStatus = SchedulePublicationStatus.Public;
        var before = DateTimeOffset.UtcNow;
        db.SaveChanges();
        Assert.True(schedule.PublishedAtUtc >= before);
    }

    [Fact]
    public async Task ReadState_CleansExpiredTransientRows_AndIsolatesEmployees()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee("First", "Worker");
        var other = TestDataFactory.CreateDalEmployee("Other", "Worker");
        db.AddRange(employee, other);
        await db.SaveChangesAsync();
        var now = DateTimeOffset.UtcNow;
        db.EmployeeNotificationReads.AddRange(
            Read(employee.Id, "swap-public:expired", now.AddDays(-5).AddSeconds(-1)),
            Read(employee.Id, "swap-public:fresh", now.AddDays(-4)),
            Read(employee.Id, "schedule-public:1", now.AddDays(-30)),
            Read(other.Id, "swap-public:other", now.AddDays(-6)));
        await db.SaveChangesAsync();
        var controller = Controller(db, employee.Id);
        var result = await controller.GetCurrent(CancellationToken.None);
        var state = Assert.IsType<EmployeeUiStateDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Contains("swap-public:fresh", state.ReadNotificationIds);
        Assert.DoesNotContain("schedule-public:1", state.ReadNotificationIds);
        Assert.DoesNotContain("swap-public:expired", state.ReadNotificationIds);
        Assert.DoesNotContain("swap-public:other", state.ReadNotificationIds);
        Assert.False(await db.EmployeeNotificationReads.AnyAsync(r => r.EmployeeId == employee.Id && r.NotificationId == "swap-public:expired"));
        Assert.True(await db.EmployeeNotificationReads.AnyAsync(r => r.EmployeeId == other.Id));
    }

    [Fact]
    public async Task ReadState_HandlesNullsDuplicatesAndBatchLimits_WithoutDroppingOlderReads()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee("Read", "Worker");
        db.Add(employee);
        await db.SaveChangesAsync();
        var controller = Controller(db, employee.Id);
        Assert.IsType<NoContentResult>(await controller.MarkNotificationsRead(new() { NotificationIds = [null!, " ", " swap-public:1 ", "swap-public:1"] }, CancellationToken.None));
        Assert.Single(await db.EmployeeNotificationReads.ToListAsync());
        var ids = Enumerable.Range(2, 300).Select(i => $"swap-public:{i}").ToArray();
        Assert.IsType<NoContentResult>(await controller.MarkNotificationsRead(new() { NotificationIds = ids }, CancellationToken.None));
        var result = await controller.GetCurrent(CancellationToken.None);
        Assert.Equal(301, Assert.IsType<EmployeeUiStateDto>(Assert.IsType<OkObjectResult>(result.Result).Value).ReadNotificationIds.Count);
        Assert.IsType<ObjectResult>(await controller.MarkNotificationsRead(new() { NotificationIds = ids.Append("extra").ToArray() }, CancellationToken.None));
        Assert.Equal(301, await db.EmployeeNotificationReads.CountAsync());
    }

    [Fact]
    public async Task BackgroundCleanup_DeletesAtFiveDayBoundary_WithTimezoneOffsets()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var employee = TestDataFactory.CreateDalEmployee("Cleanup", "Worker");
        db.Add(employee);
        await db.SaveChangesAsync();
        var now = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
        db.EmployeeNotificationReads.AddRange(
            Read(employee.Id, "schedule-public:old", now.AddDays(-6)),
            Read(employee.Id, "availability-public:boundary", now.AddDays(-5).ToOffset(TimeSpan.FromHours(2))),
            Read(employee.Id, "swap-public:fresh", now.AddDays(-5).AddSeconds(1)));
        await db.SaveChangesAsync();
        Assert.Equal(2, await WebApi.Services.EmployeeNotificationRetention.CleanupAsync(db, now, CancellationToken.None));
        Assert.Equal("swap-public:fresh", (await db.EmployeeNotificationReads.SingleAsync()).NotificationId);
    }

    private static EmployeeNotificationReadModel Read(int employeeId, string id, DateTimeOffset at) => new()
    { EmployeeId = employeeId, NotificationId = id, ReadAtUtc = at };

    private static EmployeeUiStateController Controller(DataAccessLayer.Models.DataBaseContext.AppDbContext db, int employeeId) => new(db)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("employee_id", employeeId.ToString()), new Claim(ClaimTypes.Role, AuthRoles.Employee)], "test"))
        }}
    };
}
