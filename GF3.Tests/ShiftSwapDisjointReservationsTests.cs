using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApi.Contracts.ShiftSwaps;
using WebApi.ShiftSwaps;
using static GF3.Tests.Infrastructure.ShiftSwapScenarioFactory;

namespace GF3.Tests;

public sealed class ShiftSwapDisjointReservationsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdjacentGiveOffersRemainAcceptableInEitherOrder(bool acceptRightFirst)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedSixHourShift(database);
        await using var db = database.CreateContext();
        var left = await Create(db, fixture.Owner, Give(fixture, "09:00", "13:00", fixture.Target));
        var right = await Create(db, fixture.Owner, Give(fixture, "13:00", "15:00", fixture.Third));
        Assert.All(await db.ShiftSwapRequests.ToListAsync(), request => Assert.Equal(fixture.SourceSlot, request.ScheduleSlotId));
        Assert.Single(await db.ScheduleSlots.Where(slot => slot.ScheduleId == fixture.SourceSchedule).ToListAsync());
        var first = acceptRightFirst ? right : left;
        var second = acceptRightFirst ? left : right;
        await EmployeeController(db, first.TargetEmployeeId!.Value).Accept(first.Id, default);

        db.ChangeTracker.Clear();
        var pending = await db.ShiftSwapRequests.Include(r => r.ScheduleSlot).SingleAsync(r => r.Id == second.Id);
        Assert.Equal(ShiftSwapStatus.Open, pending.Status);
        Assert.NotEqual(fixture.SourceSlot, pending.ScheduleSlotId);
        Assert.Equal((second.FromTime, second.ToTime, fixture.Owner),
            (pending.ScheduleSlot!.FromTime, pending.ScheduleSlot.ToTime, pending.ScheduleSlot.EmployeeId!.Value));
        await EmployeeController(db, second.TargetEmployeeId!.Value).Accept(second.Id, default);

        var assignments = await db.ScheduleSlots.Where(slot => slot.ScheduleId == fixture.SourceSchedule).OrderBy(slot => slot.FromTime).ToListAsync();
        Assert.Equal([(fixture.Target, "09:00", "13:00"), (fixture.Third, "13:00", "15:00")],
            assignments.Select(slot => (slot.EmployeeId!.Value, slot.FromTime, slot.ToTime)));
        Assert.Equal(2, await db.ShiftSwapHistories.CountAsync());
        Assert.True(await db.ScheduleVersions.CountAsync() >= 2);
    }

    [Fact]
    public async Task MiddleAcceptanceRebindsBothSides_AndCancelFreesOnlyItsInterval()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var f = await SeedSixHourShift(database);
        await using var db = database.CreateContext();
        var left = await Create(db, f.Owner, Give(f, "09:00", "11:00", f.Target));
        var middle = await Create(db, f.Owner, Give(f, "11:00", "13:00", f.Third));
        var right = await Create(db, f.Owner, Give(f, "13:00", "15:00", f.Target));
        await EmployeeController(db, f.Owner).Cancel(left.Id, default);
        var replacement = await Create(db, f.Owner, Give(f, "09:00", "11:00", f.Target));
        await EmployeeController(db, f.Third).Accept(middle.Id, default);
        db.ChangeTracker.Clear();
        var pendingSlots = await db.ShiftSwapRequests.Where(r => r.Status == ShiftSwapStatus.Open).Select(r => r.ScheduleSlotId).ToListAsync();
        Assert.Equal(2, pendingSlots.Distinct().Count());
        Assert.DoesNotContain(f.SourceSlot, pendingSlots);
        await EmployeeController(db, f.Target).Accept(right.Id, default);
        await EmployeeController(db, f.Target).Accept(replacement.Id, default);
        Assert.Equal(3, await db.ShiftSwapHistories.CountAsync());
        var slots = await db.ScheduleSlots.Where(s => s.ScheduleId == f.SourceSchedule).ToListAsync();
        Assert.Equal(3, slots.Count);
        Assert.Equal(6, slots.Sum(ShiftSwapRules.GetSlotDurationHours));
    }

    [Fact]
    public async Task GiveReservationBlocksReturnOverlapAndLeavesFreeCandidateWindow()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var f = await SeedSixHourShift(database);
        await using var db = database.CreateContext();
        await Create(db, f.Target, new() { ScheduleId = f.ReturnSchedule, ScheduleSlotId = f.ReturnSlot, FromTime = "09:00", ToTime = "13:00" });
        var owner = EmployeeController(db, f.Owner);
        var result = await owner.GetReturnCandidates(f.SourceSchedule, f.SourceSlot, "13:00", "15:00", f.Target, default);
        var page = Assert.IsType<ShiftSwapReturnCandidatePageDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        var candidate = Assert.Single(page.Items);
        Assert.True(candidate.CanSelect);
        var window = Assert.Single(candidate.EligibleWindows);
        Assert.Equal(("13:00", "17:00"), (window.FromTime, window.ToTime));

        var conflict = Exchange(f, "13:00", "15:00", "12:59", "15:00");
        await Assert.ThrowsAsync<ValidationException>(() => owner.Preview(conflict, default));
        await Assert.ThrowsAsync<ValidationException>(() => owner.Create(conflict, default));
        await Create(db, f.Owner, Exchange(f, "13:00", "15:00", "13:00", "17:00"));

        result = await owner.GetReturnCandidates(f.SourceSchedule, f.SourceSlot, "09:00", "13:00", f.Target, default);
        page = Assert.IsType<ShiftSwapReturnCandidatePageDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.False(Assert.Single(page.Items).CanSelect);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Create(new()
            { ScheduleId = f.ReturnSchedule, ScheduleSlotId = f.ReturnSlot, FromTime = "13:00", ToTime = "15:00" }, default));
        Assert.Equal(2, await db.ShiftSwapRequests.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExchangeRebindsBothSidesAndRefreshesSnapshot(bool sameSchedule)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var f = await SeedSixHourShift(database, sameSchedule);
        await using var db = database.CreateContext();
        var first = await Create(db, f.Owner, Exchange(f, "09:00", "13:00", "09:00", "13:00"));
        var second = await Create(db, f.Owner, Exchange(f, "13:00", "15:00", "13:00", "15:00"));
        await EmployeeController(db, f.Target).Accept(first.Id, default);
        db.ChangeTracker.Clear();
        var pending = await db.ShiftSwapRequests.SingleAsync(r => r.Id == second.Id);
        var snapshot = Assert.IsType<ShiftSwapArchive.ExchangeReservationSnapshot>(ShiftSwapArchive.ReadReservation(pending));
        Assert.NotEqual(f.SourceSlot, pending.ScheduleSlotId);
        Assert.NotEqual(f.ReturnSlot, pending.ReturnScheduleSlotId);
        Assert.Equal(pending.ScheduleSlotId, snapshot.Source.SlotId);
        Assert.Equal(pending.ReturnScheduleSlotId, snapshot.Return.SlotId);
        Assert.Equal("13:00", snapshot.Source.FromTime);
        Assert.Equal("13:00", snapshot.Return.FromTime);
        await EmployeeController(db, f.Target).Accept(second.Id, default);
        Assert.Equal(2, await db.ShiftSwapHistories.CountAsync());
        Assert.Equal(0, await db.ShiftSwapRequests.CountAsync(r => r.Status == ShiftSwapStatus.Open));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task ConcurrentCreatesAreSerialized(bool adjacent, int expectedCount)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var f = await SeedSixHourShift(database);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Write(string from, string to)
        {
            await ready.Task;
            await using var context = database.CreateContext();
            try { await Create(context, f.Owner, Give(f, from, to)); return true; }
            catch (ValidationException) { return false; }
        }

        var left = Write("09:00", "13:00");
        var right = Write(adjacent ? "13:00" : "09:00", "15:00");
        ready.SetResult();
        Assert.Equal(expectedCount, (await Task.WhenAll(left, right)).Count(result => result));
        await using var db = database.CreateContext();
        Assert.Equal(expectedCount, await db.ShiftSwapRequests.CountAsync(r => r.Status == ShiftSwapStatus.Open));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HistoryOrFinalVersionFailureRollsBackTheSplitAndRebind(bool versionFailure)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var f = await SeedSixHourShift(database);
        await using (var db = database.CreateContext())
        {
            var left = await Create(db, f.Owner, Give(f, "09:00", "13:00"));
            await Create(db, f.Owner, Give(f, "13:00", "15:00"));
            if (versionFailure)
            {
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER reject_version BEFORE INSERT ON schedule_version
                    WHEN NEW.created_by_manager_name LIKE 'Swap accepted:%'
                    BEGIN SELECT RAISE(ABORT, 'test rollback'); END;
                    """);
                await Assert.ThrowsAsync<DbUpdateException>(() => EmployeeController(db, f.Target).Accept(left.Id, default));
            }
            else
            {
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER reject_history BEFORE INSERT ON shift_swap_history
                    BEGIN SELECT RAISE(ABORT, 'test rollback'); END;
                    """);
                await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Accept(left.Id, default));
            }
        }

        await using var reloaded = database.CreateContext();
        Assert.All(await reloaded.ShiftSwapRequests.ToListAsync(), r =>
        {
            Assert.Equal(f.SourceSlot, r.ScheduleSlotId);
            Assert.Equal(ShiftSwapStatus.Open, r.Status);
        });
        var original = Assert.Single(await reloaded.ScheduleSlots.Where(s => s.ScheduleId == f.SourceSchedule).ToListAsync());
        Assert.Equal(("09:00", "15:00", f.Owner), (original.FromTime, original.ToTime, original.EmployeeId!.Value));
        Assert.Empty(await reloaded.ShiftSwapHistories.ToListAsync());
        Assert.Empty(await reloaded.ScheduleVersions.ToListAsync());
    }

    [Fact]
    public async Task PendingClaimFollowsItsRemainderAndExistingReviewRejectsStaleBounds()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db);
        var original = (await db.ScheduleSlots.FindAsync(f.SourceSlot))!;
        original.ToTime = "15:00";
        await db.EmploymentContractSettings.ExecuteUpdateAsync(update => update.SetProperty(s => s.RequirePickupApproval, true)
            .SetProperty(s => s.RequireBelowMinimumGiveApproval, false).SetProperty(s => s.RestrictSwapsToEightHours, false));
        await db.SaveChangesAsync();

        var left = await Create(db, f.Owner, Give(f, "09:00", "13:00", f.Third));
        var right = await Create(db, f.Owner, Give(f, "13:00", "15:00", f.Target));
        var approvals = ContractTestFactory.Approvals(db);
        var pending = await approvals.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = right.Id }, ContractTestFactory.Employee(f.Target));
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Owner).Create(Give(f, "12:59", "15:00"), default));
        await EmployeeController(db, f.Third).Accept(left.Id, default);
        var rebound = await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync(r => r.Id == pending.Id);
        var rightOffer = await db.ShiftSwapRequests.AsNoTracking().SingleAsync(r => r.Id == right.Id);
        Assert.Equal(rightOffer.ScheduleSlotId, rebound.ScheduleSlotId);
        Assert.NotEqual(f.SourceSlot, rebound.ScheduleSlotId);
        Assert.Equal(("13:00", "15:00", "09:00"), (rebound.FromTime, rebound.ToTime, rebound.OriginalFromTime));
        var stale = await Assert.ThrowsAsync<ContractShiftConflictException>(() => approvals.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager()));
        Assert.Equal("CONTRACT_APPROVAL_EXPIRED", stale.Code);
        Assert.Equal(ShiftSwapStatus.Open, (await db.ShiftSwapRequests.AsNoTracking().SingleAsync(r => r.Id == right.Id)).Status);
    }

    [Fact]
    public async Task AcceptCancelCreateRaceRetainsOneTerminalStateAndSafeRetry()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var f = await SeedSixHourShift(database);
        int id;
        await using (var setup = database.CreateContext())
            id = (await Create(setup, f.Owner, Give(f, "09:00", "13:00", f.Target))).Id;

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Attempt(Func<AppDbContext, Task> action)
        {
            await start.Task;
            await using var context = database.CreateContext();
            try { await action(context); return true; }
            catch (ValidationException) { return false; }
        }

        var accept = Attempt(db => EmployeeController(db, f.Target).Accept(id, default));
        var cancel = Attempt(db => EmployeeController(db, f.Owner).Cancel(id, default));
        var create = Attempt(db => Create(db, f.Owner, Give(f, "13:00", "15:00", f.Third)));
        start.SetResult();
        await Task.WhenAll(accept, cancel, create);
        Assert.NotEqual(await accept, await cancel);

        await using var snapshot = database.CreateContext();
        var won = await accept;
        var request = await snapshot.ShiftSwapRequests.AsNoTracking().SingleAsync(r => r.Id == id);
        Assert.Equal(won ? ShiftSwapStatus.Accepted : ShiftSwapStatus.Cancelled, request.Status);
        Assert.Equal(won ? 1 : 0, await snapshot.ShiftSwapHistories.CountAsync());
        var ownerSlot = await snapshot.ScheduleSlots.AsNoTracking().SingleAsync(s => s.ScheduleId == f.SourceSchedule && s.EmployeeId == f.Owner);
        var retry = Give(f with { SourceSlot = ownerSlot.Id }, "13:00", "15:00", f.Third);
        if (await create)
            await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(snapshot, f.Owner).Create(retry, default));
        else
            await Create(snapshot, f.Owner, retry);
        var open = await snapshot.ShiftSwapRequests.AsNoTracking().SingleAsync(r => r.Status == ShiftSwapStatus.Open);
        Assert.Equal(ownerSlot.Id, open.ScheduleSlotId);
        Assert.Equal(won ? 1 : 0, await snapshot.ShiftSwapHistories.CountAsync());
    }

    [Fact]
    public async Task UpgradeAndDowngradePreserveLegacyOpenOfferExchangeSnapshotAndHistory()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var f = await SeedSixHourShift(database);
        await using var db = database.CreateContext();
        var given = await Create(db, f.Owner, Give(f, "09:00", "10:00", f.Third));
        await EmployeeController(db, f.Third).Accept(given.Id, default);
        var remainder = await db.ScheduleSlots.SingleAsync(s => s.ScheduleId == f.SourceSchedule && s.EmployeeId == f.Owner);
        var exchange = await Create(db, f.Owner, Exchange(f with { SourceSlot = remainder.Id }, "10:00", "13:00", "09:00", "12:00"));
        var legacy = new ShiftSwapRequestModel { ScheduleId = f.SourceSchedule, ScheduleSlotId = f.SourceSlot,
            FromEmployeeId = f.Third, Status = ShiftSwapStatus.Open, CreatedAtUtc = DateTimeOffset.UtcNow };
        db.Add(legacy);
        await db.SaveChangesAsync();
        var before = await SwapRows();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261115135735_RefreshManualShiftOffer");
        await migrator.MigrateAsync();
        Assert.Equal(before, await SwapRows());
        Assert.Contains(before, row => row.Id == legacy.Id && row.OfferedFromTime == null && row.Status == ShiftSwapStatus.Open);
        Assert.Contains(before, row => row.Id == exchange.Id && row.RequestKind == ShiftSwapRequestKind.Exchange && row.ArchivedViewsJson != null);
        Assert.Single(await db.ShiftSwapHistories.AsNoTracking().Where(h => h.SourceShiftSwapRequestId == given.Id).ToListAsync());

        async Task<List<(int Id, string? OfferedFromTime, ShiftSwapStatus Status, ShiftSwapRequestKind RequestKind, string? ArchivedViewsJson)>> SwapRows()
            => (await db.ShiftSwapRequests.AsNoTracking().OrderBy(r => r.Id).ToListAsync())
                .Select(r => (r.Id, r.OfferedFromTime, r.Status, r.RequestKind, r.ArchivedViewsJson)).ToList();
    }

    [Fact]
    public async Task DowngradeRefusesMultiOpenWithoutDeletingRows()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var f = await SeedSixHourShift(database);
        await using var db = database.CreateContext();
        await Create(db, f.Owner, Give(f, "09:00", "13:00"));
        await Create(db, f.Owner, Give(f, "13:00", "15:00"));
        var migrator = db.GetService<IMigrator>();
        await Assert.ThrowsAsync<SqliteException>(() => migrator.MigrateAsync("20261115135735_RefreshManualShiftOffer"));
        Assert.Equal(2, await db.ShiftSwapRequests.CountAsync());
        await db.Database.OpenConnectionAsync();
        await using var indexes = db.Database.GetDbConnection().CreateCommand();
        indexes.CommandText = "SELECT COUNT(*) FROM pragma_index_list('shift_swap_request') WHERE name = 'ix_shift_swap_open_slot'";
        Assert.Equal(1L, await indexes.ExecuteScalarAsync());
    }

    private static async Task<SwapFixture> SeedSixHourShift(SqliteTestDatabase database, bool sameSchedule = false)
    {
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new(ScheduleRelation: sameSchedule ? ScheduleRelation.SameSchedule : ScheduleRelation.SameContainerDifferentSchedule));
        var source = await db.ScheduleSlots.FindAsync(f.SourceSlot);
        source!.ToTime = "15:00";
        await db.SaveChangesAsync();
        return f;
    }

    private static CreateEmployeeShiftSwapRequest Give(SwapFixture f, string from, string to, int? target = null)
        => new() { ScheduleId = f.SourceSchedule, ScheduleSlotId = f.SourceSlot, FromTime = from, ToTime = to, TargetEmployeeId = target };

    private static CreateEmployeeShiftSwapRequest Exchange(SwapFixture f, string from, string to, string returnFrom, string returnTo)
        => new() { RequestKind = "exchange", ScheduleId = f.SourceSchedule, ScheduleSlotId = f.SourceSlot, FromTime = from, ToTime = to,
            TargetEmployeeId = f.Target, ReturnScheduleId = f.ReturnSchedule, ReturnScheduleSlotId = f.ReturnSlot,
            ReturnFromTime = returnFrom, ReturnToTime = returnTo };

    private static async Task<ShiftSwapDto> Create(AppDbContext db, int owner, CreateEmployeeShiftSwapRequest request)
        => Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, owner).Create(request, default)).Result).Value);
}
