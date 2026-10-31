using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Urlop;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.Urlop;

namespace GF3.Tests;

public sealed class UrlopWorkflowTests
{
    private static readonly DateOnly Today = new(2026, 11, 1);

    [Fact]
    public async Task MissingMonthIsClosed_OpenIsShopScoped_AndClosingPreservesRequests()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var service = UrlopTestData.Service(db);
        Assert.False((await service.GetManagerMonthAsync(f.Shop, 2026, 11, default)).IsOpen);
        Assert.Empty(await db.UrlopMonths.ToListAsync());
        var closed = await Assert.ThrowsAsync<UrlopConflictException>(() => service.SubmitAsync(f.Employee, f.Shop, 2026, 11, [3, 4], Today, default));
        Assert.Equal("urlop_month_closed", closed.Code);
        await service.SetMonthOpenAsync(f.Shop, 2026, 11, true, default);
        Assert.False((await service.GetManagerMonthAsync(f.OtherShop, 2026, 11, default)).IsOpen);
        var request = await service.SubmitAsync(f.Employee, f.Shop, 2026, 11, [4, 3], Today, default);
        Assert.Equal([3, 4], request.Days); Assert.Equal(UrlopStatus.Pending, request.Status);
        await service.SetMonthOpenAsync(f.Shop, 2026, 11, false, default);
        Assert.Single((await service.GetManagerMonthAsync(f.Shop, 2026, 11, default)).Requests);
        Assert.Equal(2, await db.UrlopDays.CountAsync());
    }

    [Fact]
    public async Task CalendarContainsTwelveMonths_PrivateMembership_AndOnlyOwnHistoryAndApprovedPeers()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var service = UrlopTestData.Service(db);
        await service.SetMonthOpenAsync(f.Shop, 2026, 12, true, default);
        var own = await service.SubmitAsync(f.Employee, f.Shop, 2026, 12, [3], Today, default);
        var peer = await service.SubmitAsync(f.Peer, f.Shop, 2026, 12, [4], Today, default);
        var rejected = await service.SubmitAsync(f.Peer, f.Shop, 2026, 12, [5], Today, default);
        await service.RejectAsync(f.Manager, rejected.RequestId, default);
        await service.CreateApprovedAsync(f.Manager, f.Shop, f.Peer, 2026, 12, [6], default);
        var calendar = await service.GetEmployeeCalendarAsync(f.Employee, new(2026, 12, 1), default);
        Assert.Single(calendar.Shops); Assert.Equal(12, calendar.Months.Count);
        Assert.Equal((2027, 11), (calendar.Months[^1].Year, calendar.Months[^1].Month));
        Assert.Equal(own.RequestId, Assert.Single(calendar.Months[0].Requests).RequestId);
        Assert.Equal(6, Assert.Single(calendar.Months[0].Approved).Date.Day);
        Assert.DoesNotContain(JsonSerializer.Serialize(calendar), "private@example.com");
        await service.ApproveAsync(f.Manager, peer.RequestId, default);
        Assert.Equal(2, (await service.GetEmployeeCalendarAsync(f.Employee, new(2026, 12, 1), default)).Months[0].Approved.Count);
        Assert.Empty((await service.GetEmployeeCalendarAsync(f.Outsider, Today, default)).Shops);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEmployeeApprovedMonthAsync(f.Employee, f.OtherShop, 2026, 11, default));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SubmitAsync(f.Employee, f.OtherShop, 2026, 11, [3], Today, default));
    }

    [Theory]
    [InlineData(2026, 2, 28, true)][InlineData(2028, 2, 29, true)][InlineData(2026, 2, 29, false)]
    [InlineData(2026, 4, 30, true)][InlineData(2026, 4, 31, false)][InlineData(2026, 1, 31, true)]
    [InlineData(0, 1, 1, false)][InlineData(2026, 13, 1, false)][InlineData(2026, 1, 0, false)]
    public async Task ManagerValidatesCalendarDates(int year, int month, int day, bool valid)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var service = UrlopTestData.Service(db);
        if (valid) Assert.Equal(day, Assert.Single((await service.CreateApprovedAsync(f.Manager, f.Shop, f.Outsider, year, month, [day], default)).Days));
        else await Assert.ThrowsAsync<ValidationException>(() => service.CreateApprovedAsync(f.Manager, f.Shop, f.Employee, year, month, [day], default));
    }

    [Theory]
    [InlineData(2026, 10, 31)][InlineData(2027, 11, 1)][InlineData(2026, 11, 0)]
    public async Task EmployeeRejectsPastAndOutsideHorizon(int year, int month, int day)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var service = UrlopTestData.Service(db);
        await service.SetMonthOpenAsync(f.Shop, year, month, true, default);
        await Assert.ThrowsAsync<ValidationException>(() => service.SubmitAsync(f.Employee, f.Shop, year, month, [day], Today, default));
        Assert.Empty(await db.UrlopDays.ToListAsync());
    }

    [Fact]
    public async Task RejectReapplyApproveReplaceAndDeleteAreAtomic_AndDoNotChangeAvailability()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var service = UrlopTestData.Service(db);
        await service.SetMonthOpenAsync(f.Shop, 2026, 11, true, default);
        foreach (var days in new int[][] { [], [3, 3] })
            await Assert.ThrowsAsync<ValidationException>(() => service.SubmitAsync(f.Employee, f.Shop, 2026, 11, days, Today, default));
        var first = await service.SubmitAsync(f.Employee, f.Shop, 2026, 11, [3, 4], Today, default);
        Assert.Empty(await service.GetApprovedDaysAsync(Today, Today.AddMonths(1), null, default));
        await Assert.ThrowsAsync<UrlopConflictException>(() => service.SubmitAsync(f.Employee, f.Shop, 2026, 11, [4, 5], Today, default));
        await service.ReplaceDaysAsync(f.Manager, first.RequestId, [4, 5], default);
        await service.RejectAsync(f.Manager, first.RequestId, default);
        await Assert.ThrowsAsync<UrlopConflictException>(() => service.ReplaceDaysAsync(f.Manager, first.RequestId, [6], default));
        var next = await service.SubmitAsync(f.Employee, f.Shop, 2026, 11, [4, 5], Today, default);
        await service.ApproveAsync(f.Manager, next.RequestId, default);
        await Assert.ThrowsAsync<UrlopConflictException>(() => service.ApproveAsync(f.Manager, next.RequestId, default));
        await Assert.ThrowsAsync<UrlopConflictException>(() => service.RejectAsync(f.Manager, next.RequestId, default));
        var edited = await service.ReplaceDaysAsync(f.Manager, next.RequestId, [6, 7], default);
        Assert.Equal(UrlopStatus.Approved, edited.Status); Assert.Equal(next.RequestId, edited.RequestId);
        Assert.Equal(2, (await service.GetApprovedDaysAsync(Today, Today.AddMonths(1), null, default)).Count);
        Assert.Equal(2, (await service.GetEmployeeApprovedMonthAsync(f.Employee, f.Shop, 2026, 11, default)).Count);
        Assert.Equal(DataAccessLayer.Models.Enums.AvailabilityKind.ANY, (await db.AvailabilityGroupDays.SingleAsync()).Kind);
        await service.DeleteAsync(f.Manager, edited.RequestId, default);
        Assert.DoesNotContain(await db.UrlopDays.ToListAsync(), x => x.RequestId == edited.RequestId);
    }

    [Fact]
    public async Task ExistingShiftsConflictOnlyInSameShop_AndRollbackAllDays()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var service = UrlopTestData.Service(db);
        await service.SetMonthOpenAsync(f.Shop, 2026, 11, true, default);
        db.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(f.Schedule, 4, 1, f.Employee, "09:00", "17:00")); await db.SaveChangesAsync();
        var pending = await service.SubmitAsync(f.Employee, f.Shop, 2026, 11, [3, 4], Today, default);
        var conflict = await Assert.ThrowsAsync<UrlopConflictException>(() => service.ApproveAsync(f.Manager, pending.RequestId, default));
        Assert.Equal([new DateOnly(2026, 11, 4)], conflict.ConflictingDates); Assert.Equal([f.Schedule], conflict.ScheduleIds);
        Assert.All(await db.UrlopDays.ToListAsync(), x => Assert.Equal(UrlopStatus.Pending, x.Status));
        await Assert.ThrowsAsync<UrlopConflictException>(() => service.CreateApprovedAsync(f.Manager, f.Shop, f.Employee, 2026, 11, [4, 8], default));
        var approved = await service.CreateApprovedAsync(f.Manager, f.Shop, f.Employee, 2026, 11, [6], default);
        await Assert.ThrowsAsync<UrlopConflictException>(() => service.ReplaceDaysAsync(f.Manager, approved.RequestId, [4, 7], default));
        Assert.Equal(6, (await db.UrlopDays.SingleAsync(x => x.RequestId == approved.RequestId)).Date.Day);
        await service.CreateApprovedAsync(f.Manager, f.OtherShop, f.Employee, 2026, 11, [4], default);
        await service.CreateApprovedAsync(f.Manager, f.Shop, f.Outsider, 2026, 11, [4], default);
        Assert.Empty((await service.GetEmployeeCalendarAsync(f.Outsider, Today, default)).Shops);
        Assert.Single(await db.ScheduleSlots.ToListAsync());
    }

    [Fact]
    public async Task HttpAuthorizationClaimsPhoneModeAndHistoricalReadAreEnforced()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(new UrlopTestClock());
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/employee-urlop")).StatusCode);
        await using (var db = host.Database.CreateContext())
        {
            db.ScheduleEmployees.Add(new() { ScheduleId = 1, EmployeeId = 1 });
            db.Shops.Add(TestDataFactory.CreateDalShop("Other"));
            await db.SaveChangesAsync();
            await UrlopTestData.Service(db).SetMonthOpenAsync(1, 2026, 11, true, default);
        }
        await host.Login("employee");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/manager-urlop?shopId=1&year=2026&month=11")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/employee-urlop/approved?shopId=2&year=2026&month=11")).StatusCode);
        var response = await host.Client.PostAsJsonAsync("/api/employee-urlop/requests", new { shopId = 1, year = 2026, month = 11, days = new[] { 3 }, employeeId = 999 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var request = (await response.Content.ReadFromJsonAsync<UrlopRequestDto>())!; Assert.Equal(1, request.EmployeeId);
        Assert.Equal(12, (await host.Client.GetFromJsonAsync<UrlopEmployeeCalendarDto>("/api/employee-urlop"))!.Months.Count);
        await host.Login("manager"); await host.Exchange("phone");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsync($"/api/manager-urlop/requests/{request.RequestId}/approve", null)).StatusCode);
        await host.Exchange("pc");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync($"/api/manager-urlop/requests/{request.RequestId}/approve", null)).StatusCode);
        var stale = await host.Client.PostAsync($"/api/manager-urlop/requests/{request.RequestId}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("urlop_request_not_pending", (await stale.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/employee-urlop")).StatusCode);
        await host.Login("employee");
        Assert.Single((await host.Client.GetFromJsonAsync<ApprovedUrlopDayDto[]>("/api/employee-urlop/approved?shopId=1&year=2026&month=11"))!);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/employee-urlop/approved?shopId=1&year=2020&month=1")).StatusCode);
    }

    [Fact]
    public async Task SoftDeletedEmployeeCannotCreate_AndPhysicalShopDeletionKeepsHistory()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var service = UrlopTestData.Service(db);
        await service.CreateApprovedAsync(f.Manager, f.Shop, f.Employee, 2026, 11, [3], default);
        (await db.Employees.FindAsync(f.Employee))!.IsDeleted = true; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateApprovedAsync(f.Manager, f.Shop, f.Employee, 2026, 11, [4], default));
        Assert.Single((await service.GetManagerMonthAsync(f.Shop, 2026, 11, default)).Requests);
        await db.Schedules.ExecuteDeleteAsync();
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Shops.Where(x => x.Id == f.Shop).ExecuteDeleteAsync());
        Assert.Single(await db.UrlopDays.ToListAsync());
    }

    [Fact]
    public void WarsawDateUsesCalendarDayAcrossUtcBoundary()
    {
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var utc = new DateTimeOffset(2026, 10, 31, 23, 30, 0, TimeSpan.Zero);
        Assert.Equal(Today, DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, warsaw).DateTime));
    }
}

internal sealed class UrlopTestClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
}

internal sealed record UrlopFixture(int Shop, int OtherShop, int Employee, int Peer, int Outsider, int Manager, int Schedule);
internal static class UrlopTestData
{
    public static UrlopService Service(AppDbContext db) => new(db, new UrlopTestClock());
    public static async Task<UrlopFixture> SeedAsync(AppDbContext db)
    {
        var shop = TestDataFactory.CreateDalShop("A"); var other = TestDataFactory.CreateDalShop("B");
        var employee = TestDataFactory.CreateDalEmployee("One", "Worker", email: "private@example.com");
        var peer = TestDataFactory.CreateDalEmployee("Two", "Worker"); var outsider = TestDataFactory.CreateDalEmployee("New", "Worker");
        var container = TestDataFactory.CreateDalContainer();
        var manager = new ManagerAccountModel { Username = "manager", DisplayName = "Manager", PasswordHash = "test-only", CreatedAtUtc = new UrlopTestClock().GetUtcNow(), UpdatedAtUtc = new UrlopTestClock().GetUtcNow(), PasswordUpdatedAtUtc = new UrlopTestClock().GetUtcNow() };
        db.AddRange(shop, other, employee, peer, outsider, container, manager); await db.SaveChangesAsync();
        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, year: 2026, month: 11);
        schedule.Employees.Add(new() { EmployeeId = employee.Id }); schedule.Employees.Add(new() { EmployeeId = peer.Id });
        var group = new AvailabilityGroupModel { Name = "Raw availability", Year = 2026, Month = 11,
            Members = [new() { EmployeeId = employee.Id, Days = [new() { DayOfMonth = 3, Kind = DataAccessLayer.Models.Enums.AvailabilityKind.ANY }] }] };
        db.AddRange(schedule, group); await db.SaveChangesAsync();
        return new(shop.Id, other.Id, employee.Id, peer.Id, outsider.Id, manager.Id, schedule.Id);
    }
}
