using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ShiftSwaps;
using static GF3.Tests.Infrastructure.ShiftSwapScenarioFactory;

namespace GF3.Tests;

public sealed class ShiftSwapGiveMatrixTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Different_recipient_receives_public_or_private_give(bool addressed)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(RecipientId: 3, Scope: addressed ? SwapScope.Private : SwapScope.Public);
        var f = await SeedAsync(db, scenario);
        var before = await ReadAssignmentIntervalsAsync(db);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(Request(f, scenario), default)).Result).Value);
        await EmployeeController(db, f.Target).Accept(offer.Id, default);
        await using var fresh = database.CreateContext();
        var after = await ReadAssignmentIntervalsAsync(fresh);
        AssertIntervalsEqual([(3, 1, 540, 1020), (3, 2, 540, 1020)], after);
        AssertSourceReturnMinuteConservation(before, after, 1, 3, 480); await AssertInvariants(fresh);
    }

    [Fact]
    public async Task Give_accept_rejects_source_edited_to_invalid_calendar_date_without_mutation()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(Request(f, new()), default)).Result).Value);
        var source = await db.ScheduleSlots.Include(s => s.Schedule).SingleAsync(s => s.Id == f.SourceSlot);
        source.Schedule.Year = 2027; source.Schedule.Month = 2; source.DayOfMonth = 29;
        await db.SaveChangesAsync();
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Accept(offer.Id, default));
        await AssertUnchangedAfterFailure(database, before);
    }

    [Theory]
    [InlineData(2027, 2, 29, false)]
    [InlineData(2026, 4, 31, false)]
    [InlineData(2028, 2, 29, true)]
    public async Task Calendar_dates_are_validated_and_omitted_times_mean_full_slot(int year, int month, int day, bool valid)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var source = await db.ScheduleSlots.Include(s => s.Schedule).SingleAsync(s => s.Id == f.SourceSlot);
        source.Schedule.Year = year; source.Schedule.Month = month; source.DayOfMonth = day;
        await db.SaveChangesAsync();
        var request = Request(f, new()); request.FromTime = null; request.ToTime = null;
        var before = await SnapshotAsync(db);
        if (!valid) {
            await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Owner).Create(request, default));
            await AssertUnchangedAfterFailure(database, before); return;
        }
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(request, default)).Result).Value);
        Assert.Equal("09:00", offer.FromTime); Assert.Equal("17:00", offer.ToTime);
        await EmployeeController(db, f.Target).Accept(offer.Id, default);
        await using var fresh = database.CreateContext();
        var transferred = Assert.Single(await ReadAssignmentIntervalsAsync(fresh), s => s.ScheduleId == f.SourceSchedule);
        Assert.Equal((year, month, day, f.Target, 540, 1020), (transferred.Year, transferred.Month, transferred.Day, transferred.EmployeeId, transferred.FromMinute, transferred.ToMinute));
        await AssertInvariants(fresh);
    }

    public static TheoryData<string, string, string, string> Periods => new() {
        { "full", "09:00", "17:00", "2:540:1020" },
        { "prefix-half", "09:00", "13:00", "2:540:780;1:780:1020" },
        { "suffix-half", "13:00", "17:00", "1:540:780;2:780:1020" },
        { "middle", "11:00", "15:00", "1:540:660;2:660:900;1:900:1020" },
        { "one-minute", "12:01", "12:02", "1:540:721;2:721:722;1:722:1020" },
        { "irregular", "09:17", "15:43", "1:540:557;2:557:943;1:943:1020" },
    };

    [Theory]
    [MemberData(nameof(Periods))]
    public async Task Give_exact_intervals_conservation_history_and_stats(string name, string from, string to, string expected)
    {
        _ = name;
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario();
        var f = await SeedAsync(db, scenario);
        var before = await ReadAssignmentIntervalsAsync(db);
        var request = Request(f, scenario); request.FromTime = from; request.ToTime = to;
        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(request, default)).Result).Value);
        var accepted = Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>((await EmployeeController(db, f.Target).Accept(created.Id, default)).Result).Value);
        Assert.Equal("accepted", accepted.Status); Assert.Equal(from, accepted.FromTime); Assert.Equal(to, accepted.ToTime);
        Assert.Equal(f.Owner, accepted.FromEmployeeId); Assert.Equal(f.Target, accepted.AcceptedByEmployeeId);
        await using var fresh = database.CreateContext();
        var after = await ReadAssignmentIntervalsAsync(fresh);
        var intervals = expected.Split(';').Select(row => row.Split(':').Select(int.Parse).ToArray()).Select(x => (x[0], 1, x[1], x[2])).Append((2, 2, 540, 1020));
        AssertIntervalsEqual(intervals, after);
        Assert.All(after.Where(s => s.Day == 1), s => { Assert.Equal(f.SourceSchedule, s.ScheduleId); Assert.Equal(2026, s.Year); Assert.Equal(12, s.Month); });
        var minutes = Minute(to) - Minute(from);
        AssertSourceReturnMinuteConservation(before, after, f.Owner, f.Target, minutes);
        Assert.Equal(minutes == 480 ? 0 : 1, after.Where(s => s.EmployeeId == f.Owner).Select(s => (s.Year, s.Month, s.Day)).Distinct().Count());
        Assert.Equal(2, after.Where(s => s.EmployeeId == f.Target).Select(s => (s.Year, s.Month, s.Day)).Distinct().Count());
        Assert.Equal(Math.Round((480 + minutes) / 60d, 2), accepted.CurrentEmployeeHoursAfter);
        Assert.Equal(2, accepted.CurrentEmployeeWorkDaysAfter);
        Assert.Equal(Math.Round((480 - minutes) / 60d, 2), accepted.FromEmployeeHoursAfter);
        Assert.Single(await fresh.ScheduleEmployees.Where(e => e.ScheduleId == f.SourceSchedule && e.EmployeeId == f.Target).ToListAsync());
        Assert.NotEmpty(await fresh.ScheduleCellStyles.ToListAsync());
        await AssertVersionAndHistoryConsistency(fresh, created.Id, f.SourceSchedule);
        await AssertInvariants(fresh);
        var snapshot = await SnapshotAsync(fresh);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Accept(created.Id, default));
        await AssertUnchangedAfterFailure(database, snapshot);
    }

    [Theory]
    [InlineData(null, "13:00")]
    [InlineData("09:00", null)]
    [InlineData("00:00", "13:00")]
    [InlineData("09:00", "23:59")]
    [InlineData("09:00", "24:00")]
    [InlineData("13:00", "09:00")]
    [InlineData("09:00", "09:00")]
    [InlineData("9:00", "13:00")]
    [InlineData("09:00:00", "13:00")]
    [InlineData("08:59", "13:00")]
    public async Task Invalid_time_does_not_mutate_any_rows(string? from, string? to)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var request = Request(f, new()); request.FromTime = from; request.ToTime = to;
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Owner).Create(request, default));
        await AssertUnchangedAfterFailure(database, before);
    }

    [Theory]
    [InlineData(false, 2, true)]
    [InlineData(true, 2, true)]
    [InlineData(true, 3, false)]
    [InlineData(false, 1, false)]
    public async Task Targeting_and_owner_rules(bool addressed, int acceptingEmployee, bool allowed)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Scope: addressed ? SwapScope.Private : SwapScope.Public);
        var f = await SeedAsync(db, scenario);
        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(Request(f, scenario), default)).Result).Value);
        var before = await SnapshotAsync(db);
        if (!allowed) {
            await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, acceptingEmployee).Accept(created.Id, default));
            await AssertUnchangedAfterFailure(database, before); return;
        }
        await EmployeeController(db, acceptingEmployee).Accept(created.Id, default);
        await using var fresh = database.CreateContext();
        Assert.Equal(acceptingEmployee, (await fresh.ScheduleSlots.SingleAsync(s => s.Id == f.SourceSlot)).EmployeeId);
        await AssertInvariants(fresh);
    }

    [Fact]
    public async Task Public_offer_does_not_cross_container_and_cancel_releases_reservation()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new(ScheduleRelation: ScheduleRelation.DifferentContainer));
        var request = Request(f, new());
        var controller = EmployeeController(db, f.Owner);
        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await controller.Create(request, default)).Result).Value);
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Accept(created.Id, default));
        request.FromTime = "11:00"; request.ToTime = "12:00"; request.TargetEmployeeId = 3;
        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(request, default));
        await AssertUnchangedAfterFailure(database, before);
        await controller.Cancel(created.Id, default);
        Assert.IsType<CreatedAtActionResult>((await controller.Create(request, default)).Result);
        await using var fresh = database.CreateContext();
        Assert.Equal(f.Owner, (await fresh.ScheduleSlots.SingleAsync(s => s.Id == f.SourceSlot)).EmployeeId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Adjacent_is_allowed_overlap_is_rejected(bool overlap)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        db.Add(TestDataFactory.CreateDalSlot(f.ReturnSchedule, 1, 1, f.Target, overlap ? "12:59" : "13:00", "18:00"));
        await db.SaveChangesAsync();
        var request = Request(f, new(SourcePart: SwapPart.Prefix));
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(request, default)).Result).Value);
        var before = await SnapshotAsync(db);
        if (overlap) {
            await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Accept(offer.Id, default));
            await AssertUnchangedAfterFailure(database, before);
        } else {
            await EmployeeController(db, f.Target).Accept(offer.Id, default);
            await using var fresh = database.CreateContext(); await AssertInvariants(fresh);
        }
    }

    [Fact]
    public async Task Repeated_chain_and_split_remainder_preserve_all_minutes()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var initial = await ReadAssignmentIntervalsAsync(db);
        foreach (var (owner, target, from, to) in new[] { (1, 2, "11:00", "15:00"), (2, 3, "11:00", "15:00"), (1, 3, "09:00", "11:00") })
        {
            var slot = (await db.ScheduleSlots.Where(s => s.ScheduleId == f.SourceSchedule && s.EmployeeId == owner).ToListAsync())
                .Single(s => Minute(s.FromTime) <= Minute(from) && Minute(s.ToTime) >= Minute(to));
            var request = new CreateEmployeeShiftSwapRequest { ScheduleId = f.SourceSchedule, ScheduleSlotId = slot.Id, TargetEmployeeId = target, FromTime = from, ToTime = to };
            var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, owner).Create(request, default)).Result).Value);
            await EmployeeController(db, target).Accept(offer.Id, default);
        }
        await using var fresh = database.CreateContext();
        AssertIntervalsEqual([(1, 1, 900, 1020), (3, 1, 540, 660), (3, 1, 660, 900), (2, 2, 540, 1020)], await ReadAssignmentIntervalsAsync(fresh));
        Assert.Equal(initial.Sum(s => s.Minutes), (await ReadAssignmentIntervalsAsync(fresh)).Sum(s => s.Minutes));
        Assert.Equal(3, await fresh.ShiftSwapHistories.CountAsync()); await AssertInvariants(fresh);
    }
}
