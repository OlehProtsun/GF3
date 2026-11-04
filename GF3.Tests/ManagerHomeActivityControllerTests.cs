using System.Net;
using System.Net.Http.Json;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ManagerHome;
using WebApi.Services;

namespace GF3.Tests;

public sealed class ManagerHomeActivityControllerTests
{
    private static readonly DateTimeOffset Stamp = DateTimeOffset.Parse("2026-11-04T10:00:00Z");
    [Fact]
    public async Task EndpointEnforcesRolesAndKeepsPhoneReadOnly()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        const string path = "/api/manager-home/activity?year=2026&month=11";
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync(path)).StatusCode);
        await host.Login("employee");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync(path)).StatusCode);
        await host.Login("manager"); await host.Exchange("pc");
        var response = await host.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<ManagerHomeActivityPageDto>();
        Assert.NotNull(page); Assert.Empty(page.Items); Assert.Equal(0, page.NextOffset);
        await host.Exchange("phone");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync("/api/guard-write", new { })).StatusCode);
    }
    [Fact]
    public async Task EndpointStrictlyValidatesFiltersIncludingExtremes()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(); await host.Login("manager"); await host.Exchange("pc");
        foreach (var query in new[] { "year=0&month=11", "year=10000&month=11", "year=2026&month=0", "year=2026&month=13", "year=x&month=11", "year=2026&month=11&tab=invalid", "year=2026&month=11&offset=-1", "year=2026&month=11&offset=10001", "year=2026&month=11&limit=0", "year=2026&month=11&limit=51", "year=2026" })
            {
            var response = await host.Client.GetAsync("/api/manager-home/activity?" + query);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, query + ": " + response.StatusCode + " " + await response.Content.ReadAsStringAsync());
        }
        foreach (var query in new[] { "year=1&month=1", "year=9999&month=12", "year=2026&month=11&limit=50&offset=10000&tab=approvals" })
            Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/manager-home/activity?" + query)).StatusCode);
    }
    [Fact]
    public async Task SourcesUseCurrentStatusRealTransitionsAndCanonicalGroupedCounts()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext(); await Seed(db);
        var correction = Correction(1, 1, ShiftCorrectionStatus.Approved); correction.ScheduleSlotId = 1; correction.OriginalFromTime = "08:00"; correction.OriginalToTime = "16:00"; correction.ReviewedAtUtc = Stamp.AddHours(1);
        db.Add(correction); db.Add(Correction(1, 2, ShiftCorrectionStatus.Pending, Stamp.AddMonths(-2)));
        db.Add(Correction(2, 3, ShiftCorrectionStatus.Rejected));
        db.Add(new ContractShiftApprovalRequestModel { EmployeeId = 1, ScheduleId = 2, ScheduleSlotId = 2, ShopId = 1, Year = 2026, Month = 11, OriginalDayOfMonth = 4, FromTime = "08:00", ToTime = "16:00", OriginalFromTime = "08:00", OriginalToTime = "16:00", CreatedAtUtc = Stamp });
        db.Add(new AvailabilityGroupMemberModel { AvailabilityGroupId = 1, EmployeeId = 1, EmployeeLastModifiedAtUtc = Stamp });
        var requestId = Guid.NewGuid();
        for (var day = 10; day <= 12; day++) db.Add(new UrlopDayModel { RequestId = requestId, EmployeeId = 1, ShopId = 1, Date = new(2026, 12, day), Status = "pending", CreatedAtUtc = Stamp });
        db.Add(new UrlopDayModel { RequestId = Guid.NewGuid(), EmployeeId = 1, ShopId = 1, Date = new(2026, 12, 14), Status = "approved", CreatedAtUtc = Stamp, CreatedByManagerId = 1, ReviewedAtUtc = Stamp.AddHours(1) });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var all = await Read(db); var pending = await Read(db, "approvals");
        Assert.Equal(8, all.AllActivityCount); Assert.Equal(3, all.RequiresApprovalCount); Assert.Equal(3, pending.Items.Count);
        Assert.All(pending.Items, item => { Assert.True(item.RequiresApproval); Assert.Equal("pending", item.Status); });
        Assert.Contains(pending.Items, item => item.OccurredAtUtc.Month == 9);
        Assert.Single(all.Items, x => x.Kind == "availability_updated");
        var vacation = Assert.Single(all.Items, x => x.Kind == "urlop_requested");
        Assert.Equal(3, vacation.VacationDays!.Count); Assert.Equal(12, vacation.Month); Assert.Equal(2026, vacation.Year); Assert.Equal(requestId.ToString(), vacation.Id[14..]);
        Assert.Single(all.Items, x => x.Kind == "urlop_manager_created"); Assert.Single(all.Items, x => x.Kind == "urlop_reviewed");
        var reviewed = Assert.Single(all.Items, x => x.Kind == "correction_reviewed"); Assert.Equal("approved", reviewed.Status); Assert.False(reviewed.RequiresApproval);
        Assert.All(all.Items.Where(x => x.TargetType == "graph"), item => Assert.Equal(item.GraphId == 1 ? 1 : 2, item.ContainerId));
        Assert.Empty(db.ChangeTracker.Entries()); Assert.Equal(4, await db.UrlopDays.CountAsync());
    }
    [Fact]
    public async Task SwapArchiveDeduplicatesCreationAndPreservesAcceptedAndCancelledTransitions()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext(); await Seed(db);
        db.Add(new ShiftSwapRequestModel { Id = 10, ScheduleId = 1, ScheduleSlotId = 1, FromEmployeeId = 1, Status = ShiftSwapStatus.Accepted, CreatedAtUtc = Stamp });
        db.Add(new ShiftSwapRequestModel { Id = 11, ScheduleId = 2, ScheduleSlotId = 2, FromEmployeeId = 1, Status = ShiftSwapStatus.Cancelled, CreatedAtUtc = Stamp, CancelledAtUtc = Stamp.AddHours(1) });
        foreach (var sourceId in new[] { 10, 12 }) db.Add(new ShiftSwapHistoryModel { SourceShiftSwapRequestId = sourceId, ScheduleId = 1, ScheduleSlotId = 1, ScheduleName = "First", ContainerName = "A", ShopName = "Shop", FromTime = "08:00", ToTime = "16:00", FromEmployeeName = "Archived Person", AcceptedByEmployeeName = "Receiver Name", AcceptedByEmployeeId = 1, Year = 2026, Month = 11, DayOfMonth = 4, CreatedAtUtc = Stamp, AcceptedAtUtc = Stamp.AddHours(1) });
        await db.SaveChangesAsync(); var all = await Read(db);
        Assert.Equal(6, all.AllActivityCount); Assert.Equal(0, all.RequiresApprovalCount);
        Assert.Single(all.Items, x => x.Id == "swap-created:10"); Assert.Single(all.Items, x => x.Id == "swap-created:12");
        Assert.Equal(2, all.Items.Count(x => x.Kind == "swap_accepted")); Assert.Single(all.Items, x => x.Kind == "swap_cancelled");
        Assert.All(all.Items.Where(x => x.Kind == "swap_accepted"), item => { Assert.Equal("Receiver Name", item.CounterpartyName); Assert.Equal("accepted", item.Status); });
    }
    [Theory]
    [InlineData(3, "2026-02-28T23:00:00Z", "2026-03-31T22:00:00Z")]
    [InlineData(10, "2026-09-30T22:00:00Z", "2026-10-31T23:00:00Z")]
    public async Task WarsawMonthBoundsAndSubMillisecondTieOrderingAreExact(int month, string startText, string endText)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext(); await Seed(db);
        var start = DateTimeOffset.Parse(startText); var end = DateTimeOffset.Parse(endText);
        var stamps = new[] { start.AddTicks(-1), start, start.AddTicks(1), end.AddTicks(-1), end };
        for (int i = 0; i < stamps.Length; i++) db.Add(Correction(1, i + 1, ShiftCorrectionStatus.Pending, stamps[i]));
        await db.SaveChangesAsync(); var page = await ManagerHomeActivityReader.ReadAsync(db, 2026, month, "all", 0, 50, default);
        Assert.Equal(3, page.AllActivityCount); Assert.Equal(5, page.RequiresApprovalCount);
        Assert.Equal(new[] { end.AddTicks(-1), start.AddTicks(1), start }, page.Items.Select(x => x.OccurredAtUtc));
    }
    [Fact]
    public async Task PagingIsStableCountsAreIndependentAndDeletedTargetsAreOmitted()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext(); await Seed(db);
        for (int day = 1; day <= 18; day++) db.Add(Correction(day <= 9 ? 1 : 2, day, ShiftCorrectionStatus.Pending));
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var first = await ManagerHomeActivityReader.ReadAsync(db, 2026, 11, "all", 0, 7, default);
        var second = await ManagerHomeActivityReader.ReadAsync(db, 2026, 11, "all", first.NextOffset, 7, default);
        var third = await ManagerHomeActivityReader.ReadAsync(db, 2026, 11, "all", second.NextOffset, 7, default);
        Assert.Equal(18, first.AllActivityCount); Assert.Equal(18, second.RequiresApprovalCount); Assert.True(first.HasMore); Assert.False(third.HasMore);
        var ids = first.Items.Concat(second.Items).Concat(third.Items).Select(x => x.Id).ToArray();
        Assert.Equal(18, ids.Distinct().Count()); Assert.Equal(ids.Order(StringComparer.Ordinal), ids); Assert.Equal(18, third.NextOffset);
        Assert.Empty(db.ChangeTracker.Entries());
        db.Schedules.Remove((await db.Schedules.FindAsync(2))!); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var remaining = await Read(db); Assert.Equal(9, remaining.AllActivityCount); Assert.Equal(9, remaining.RequiresApprovalCount); Assert.All(remaining.Items, item => Assert.Equal(1, item.ContainerId));
    }
    [Fact]
    public async Task CancellationPropagates()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ManagerHomeActivityReader.ReadAsync(db, 2026, 11, "all", 0, 7, cancel.Token));
    }
    private static Task<ManagerHomeActivityPageDto> Read(AppDbContext db, string tab = "all") => ManagerHomeActivityReader.ReadAsync(db, 2026, 11, tab, 0, 50, default);
    private static ShiftCorrectionRequestModel Correction(int scheduleId, int day, ShiftCorrectionStatus status, DateTimeOffset? stamp = null) => new() { ScheduleId = scheduleId, EmployeeId = 1, DayOfMonth = day, RequestedFromTime = "09:00", RequestedToTime = "17:00", Status = status, CreatedAtUtc = stamp ?? Stamp };
    private static async Task Seed(AppDbContext db)
    {
        db.AddRange(TestDataFactory.CreateDalContainer("A"), TestDataFactory.CreateDalContainer("B"), TestDataFactory.CreateDalShop("Shop"), TestDataFactory.CreateDalEmployee("Actual", "Person"), new AvailabilityGroupModel { Name = "Availability", Year = 2026, Month = 11 }, new ManagerAccountModel { Username = "test", DisplayName = "Manager", PasswordHash = "test", CreatedAtUtc = Stamp, UpdatedAtUtc = Stamp, PasswordUpdatedAtUtc = Stamp });
        await db.SaveChangesAsync();
        db.AddRange(TestDataFactory.CreateDalSchedule(1, 1, "First", 2026, 11), TestDataFactory.CreateDalSchedule(2, 1, "Second", 2026, 11)); await db.SaveChangesAsync();
        db.AddRange(TestDataFactory.CreateDalSlot(1, 4, 1, 1, "08:00", "16:00"), TestDataFactory.CreateDalSlot(2, 4, 1, 1, "08:00", "16:00")); await db.SaveChangesAsync();
    }
}
