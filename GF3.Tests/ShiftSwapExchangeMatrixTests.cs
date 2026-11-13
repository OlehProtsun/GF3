using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Services;
using WebApi.Realtime;
using static GF3.Tests.Infrastructure.ShiftSwapScenarioFactory;

namespace GF3.Tests;

public sealed class ShiftSwapExchangeMatrixTests
{
    public static TheoryData<string, string, string, string, string, string, string> Periods => new() {
        { "full-full", "09:00", "17:00", "09:00", "17:00", "2:540:1020", "1:540:1020" },
        { "full-prefix", "09:00", "17:00", "09:00", "13:00", "2:540:1020", "1:540:780;2:780:1020" },
        { "full-suffix", "09:00", "17:00", "13:00", "17:00", "2:540:1020", "2:540:780;1:780:1020" },
        { "full-middle", "09:00", "17:00", "11:00", "15:00", "2:540:1020", "2:540:660;1:660:900;2:900:1020" },
        { "prefix-full", "09:00", "13:00", "09:00", "17:00", "2:540:780;1:780:1020", "1:540:1020" },
        { "suffix-full", "13:00", "17:00", "09:00", "17:00", "1:540:780;2:780:1020", "1:540:1020" },
        { "middle-full", "11:00", "15:00", "09:00", "17:00", "1:540:660;2:660:900;1:900:1020", "1:540:1020" },
        { "middle-middle", "11:00", "15:00", "10:00", "16:00", "1:540:660;2:660:900;1:900:1020", "2:540:600;1:600:960;2:960:1020" },
        { "minute-boundaries", "09:00", "09:01", "16:59", "17:00", "2:540:541;1:541:1020", "2:540:1019;1:1019:1020" },
        { "unequal-four-six", "09:00", "13:00", "10:00", "16:00", "2:540:780;1:780:1020", "2:540:600;1:600:960;2:960:1020" },
    };

    [Theory]
    [MemberData(nameof(Periods))]
    public async Task Pairwise_exact_transfers_preview_and_persistence(string name, string from, string to, string backFrom, string backTo, string sourceExpected, string returnExpected)
    {
        _ = name;
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: SwapKind.Exchange, Scope: SwapScope.Private);
        var f = await SeedAsync(db, scenario);
        var request = Request(f, scenario); request.FromTime = from; request.ToTime = to; request.ReturnFromTime = backFrom; request.ReturnToTime = backTo;
        var before = await ReadAssignmentIntervalsAsync(db);
        var expected = Parse(sourceExpected, f.SourceSchedule, 1).Concat(Parse(returnExpected, f.ReturnSchedule, 2)).ToList();
        var preview = Assert.IsType<ShiftSwapOfferPreviewDto>(Assert.IsType<OkObjectResult>((await EmployeeController(db, f.Owner).Preview(request, default)).Result).Value);
        Assert.True(preview.IsValid);
        AssertStats(preview.ProjectedMonths, before, expected, f.Owner);
        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(request, default)).Result).Value);
        var targetView = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await EmployeeController(db, f.Target).GetVisible(default)).Result).Value));
        AssertStats(targetView.CurrentEmployeeProjectedMonths!, before, expected, f.Target);
        var accepted = Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>((await EmployeeController(db, f.Target).Accept(created.Id, default)).Result).Value);
        Assert.Equal("exchange", accepted.RequestKind); Assert.Equal("accepted", accepted.Status);
        Assert.Equal(backFrom, accepted.ReturnFromTime); Assert.Equal(backTo, accepted.ReturnToTime);
        await using var fresh = database.CreateContext();
        var after = await ReadAssignmentIntervalsAsync(fresh);
        Assert.Equal(expected.OrderBy(x => x.EmployeeId).ThenBy(x => x.Day).ThenBy(x => x.FromMinute).Select(x => x with { SlotId = 0 }),
            after.Select(x => x with { SlotId = 0 }));
        AssertSourceReturnMinuteConservation(before, after, f.Owner, f.Target, Minute(to) - Minute(from), Minute(backTo) - Minute(backFrom));
        AssertStats(accepted.CurrentEmployeeProjectedMonths!, before, after, f.Target);
        await AssertVersionAndHistoryConsistency(fresh, created.Id, f.SourceSchedule, f.ReturnSchedule);
        var history = await fresh.ShiftSwapHistories.SingleAsync();
        Assert.NotEqual(history.ReturnBeforeSnapshotJson, history.ReturnAfterSnapshotJson);
        Assert.Equal(4, await fresh.ScheduleCellStyles.CountAsync());
        Assert.Single(await fresh.ScheduleEmployees.Where(e => e.ScheduleId == f.ReturnSchedule && e.EmployeeId == f.Owner).ToListAsync());
        await AssertInvariants(fresh);
    }

    private static IEnumerable<AssignmentInterval> Parse(string rows, int schedule, int day)
        => rows.Split(';').Select(s => s.Split(':').Select(int.Parse).ToArray()).Select(x => new AssignmentInterval(schedule, 2026, 12, day, x[0], x[1], x[2], 0));

    private static void AssertStats(IEnumerable<ShiftSwapPreviewMonthStatsDto> stats, List<AssignmentInterval> before, List<AssignmentInterval> after, int employee)
    {
        foreach (var month in stats)
        {
            var old = before.Where(x => x.EmployeeId == employee && x.Year == month.Year && x.Month == month.Month).ToList();
            var current = after.Where(x => x.EmployeeId == employee && x.Year == month.Year && x.Month == month.Month).ToList();
            Assert.Equal(old.Sum(x => x.Minutes) / 60d, month.HoursBefore, 6);
            Assert.Equal(current.Sum(x => x.Minutes) / 60d, month.HoursAfter, 6);
            Assert.Equal(old.Select(x => x.Day).Distinct().Count(), month.WorkDaysBefore);
            Assert.Equal(current.Select(x => x.Day).Distinct().Count(), month.WorkDaysAfter);
        }
    }

    [Theory]
    [InlineData("SameSchedule", "DifferentDay")]
    [InlineData("SameSchedule", "SameDay")]
    [InlineData("SameContainerDifferentSchedule", "SameDay")]
    [InlineData("SameContainerDifferentSchedule", "DifferentMonth")]
    [InlineData("SameContainerDifferentSchedule", "DifferentYear")]
    public async Task Calendar_and_graph_boundaries_preserve_dates_and_distinct_versions(string relation, string days)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: SwapKind.Exchange, ScheduleRelation: Enum.Parse<ScheduleRelation>(relation), DayRelation: Enum.Parse<DayRelation>(days));
        var f = await SeedAsync(db, scenario);
        var request = Request(f, scenario);
        if (days == "SameDay") { request.ReturnFromTime = "17:00"; request.ReturnToTime = "23:00"; }
        var before = await ReadAssignmentIntervalsAsync(db);
        var preview = Assert.IsType<ShiftSwapOfferPreviewDto>(Assert.IsType<OkObjectResult>((await EmployeeController(db, f.Owner).Preview(request, default)).Result).Value);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(request, default)).Result).Value);
        await EmployeeController(db, f.Target).Accept(offer.Id, default);
        await using var fresh = database.CreateContext();
        var after = await ReadAssignmentIntervalsAsync(fresh);
        var expected = before.Select(x => x with { EmployeeId = x.EmployeeId == f.Owner ? f.Target : f.Owner }).OrderBy(x => x.EmployeeId).ToList();
        Assert.Equal(expected, after);
        AssertStats(preview.ProjectedMonths, before, after, f.Owner);
        await AssertVersionAndHistoryConsistency(fresh, offer.Id, f.SourceSchedule, f.ReturnSchedule);
        await AssertInvariants(fresh);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("same-slot")]
    [InlineData("unowned")]
    [InlineData("other-container")]
    [InlineData("unpublished")]
    [InlineData("disabled")]
    [InlineData("locked")]
    [InlineData("overlap")]
    public async Task Invalid_exchange_has_no_persisted_mutation(string invalid)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: SwapKind.Exchange);
        var f = await SeedAsync(db, scenario);
        var request = Request(f, scenario);
        var back = await db.ScheduleSlots.Include(s => s.Schedule).SingleAsync(s => s.Id == f.ReturnSlot);
        var locks = new ManagerEditLockService();
        switch (invalid)
        {
            case "public": request.TargetEmployeeId = null; break;
            case "same-slot": request.ReturnScheduleId = f.SourceSchedule; request.ReturnScheduleSlotId = f.SourceSlot; break;
            case "unowned": back.EmployeeId = f.Third; break;
            case "other-container": back.Schedule.ContainerId = f.OtherContainer; break;
            case "unpublished": back.Schedule.PublicationStatus = SchedulePublicationStatus.Private; break;
            case "disabled": back.Schedule.AllowSwap = false; break;
            case "locked": ((IScheduleEditLockService)locks).SetLocks("manager", "Manager", [new(f.Container, f.ReturnSchedule)]); break;
            case "overlap": db.Add(TestDataFactory.CreateDalSlot(f.SourceSchedule, 2, 2, f.Owner, "12:00", "13:00")); break;
        }
        await db.SaveChangesAsync();
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Owner, locks).Create(request, default));
        await AssertUnchangedAfterFailure(database, before);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Only_target_can_accept_and_stale_return_rejects_atomically(int actor)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: SwapKind.Exchange);
        var f = await SeedAsync(db, scenario);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(Request(f, scenario), default)).Result).Value);
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, actor).Accept(offer.Id, default));
        await AssertUnchangedAfterFailure(database, before);
        await db.ScheduleSlots.Where(s => s.Id == f.ReturnSlot).ExecuteDeleteAsync();
        db.ChangeTracker.Clear(); before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Accept(offer.Id, default));
        await AssertUnchangedAfterFailure(database, before);
    }

    [Fact]
    public async Task Candidates_paginate_over_31_rows_across_schedules_without_container_disclosure()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new(Kind: SwapKind.Exchange));
        for (var day = 3; day <= 31; day++) db.Add(TestDataFactory.CreateDalSlot(f.ReturnSchedule, day, 1, f.Target, "09:00", "17:00"));
        db.Add(TestDataFactory.CreateDalSlot(f.SourceSchedule, 1, 1, f.Target, "17:00", "18:00"));
        db.Add(TestDataFactory.CreateDalSlot(f.SourceSchedule, 2, 1, f.Target, "17:00", "18:00"));
        var other = TestDataFactory.CreateDalSchedule(f.OtherContainer, (await db.Shops.FirstAsync()).Id, "Secret", 2026, 12);
        other.PublicationStatus = SchedulePublicationStatus.Public; other.AllowSwap = true; db.Add(other); await db.SaveChangesAsync();
        db.Add(TestDataFactory.CreateDalSlot(other.Id, 5, 1, f.Target, "09:00", "17:00")); await db.SaveChangesAsync();
        var controller = EmployeeController(db, f.Owner);
        var first = Assert.IsType<ShiftSwapReturnCandidatePageDto>(Assert.IsType<OkObjectResult>((await controller.GetReturnCandidates(f.SourceSchedule, f.SourceSlot, "09:00", "17:00", f.Target, default, 1, 30)).Result).Value);
        var second = Assert.IsType<ShiftSwapReturnCandidatePageDto>(Assert.IsType<OkObjectResult>((await controller.GetReturnCandidates(f.SourceSchedule, f.SourceSlot, "09:00", "17:00", f.Target, default, 2, 30)).Result).Value);
        Assert.Equal(30, first.Items.Count); Assert.True(first.HasMore); Assert.Equal(2, second.Items.Count); Assert.False(second.HasMore);
        var candidates = first.Items.Concat(second.Items).ToList();
        Assert.Equal(32, candidates.Select(c => c.SlotId).Distinct().Count());
        Assert.DoesNotContain(candidates, c => c.ScheduleId == other.Id);
        Assert.Equal(2, candidates.Select(c => c.ScheduleId).Distinct().Count());
        Assert.All(candidates, c => { Assert.True(c.CanSelect); Assert.NotEmpty(c.EligibleWindows); Assert.Equal(c.EligibleWindows[0].FromTime, c.SuggestedFromTime); });
        foreach (var (page, size) in new[] { (0, 30), (1, 0), (1, 101), (int.MaxValue, 30) })
            await Assert.ThrowsAsync<ValidationException>(() => controller.GetReturnCandidates(f.SourceSchedule, f.SourceSlot, "09:00", "17:00", f.Target, default, page, size));
    }
}
