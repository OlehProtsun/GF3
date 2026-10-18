using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Realtime;
using static GF3.Tests.Infrastructure.ShiftSwapScenarioFactory;

namespace GF3.Tests;

public sealed class ShiftSwapLifecycleMatrixTests
{
    [Fact]
    public async Task Competing_exchange_creations_claim_return_slot_exactly_once()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var setup = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: SwapKind.Exchange);
        var f = await SeedAsync(setup, scenario);
        var thirdSource = TestDataFactory.CreateDalSlot(f.ReturnSchedule, 4, 1, f.Third, "09:00", "17:00");
        setup.Add(thirdSource); await setup.SaveChangesAsync();
        var original = Request(f, scenario);
        var competing = Request(f, scenario); competing.ScheduleId = f.ReturnSchedule; competing.ScheduleSlotId = thirdSource.Id;
        var gate = new ShiftSwapSafetyTests.WriteGate("INSERT INTO \"shift_swap_request\"");
        await using var first = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(gate).Options);
        await using var second = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(gate).Options);
        var results = await Task.WhenAll(Task.Run(() => Record.ExceptionAsync(() => EmployeeController(first, f.Owner).Create(original, default))),
            Task.Run(() => Record.ExceptionAsync(() => EmployeeController(second, f.Third).Create(competing, default))));
        Assert.Single(results, e => e is null); Assert.IsType<ValidationException>(Assert.Single(results, e => e is not null));
        await using var fresh = database.CreateContext();
        Assert.Single(await fresh.ShiftSwapRequests.ToListAsync());
        Assert.Empty(await fresh.ShiftSwapHistories.ToListAsync()); Assert.Empty(await fresh.ScheduleVersions.ToListAsync());
        AssertIntervalsEqual([(1, 1, 540, 1020), (2, 2, 540, 1020), (3, 4, 540, 1020)], await ReadAssignmentIntervalsAsync(fresh));
    }

    [Fact]
    public async Task Manager_lock_rejects_manual_creation_without_writes()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var locks = new ManagerEditLockService();
        locks.SetLocks("other-manager", 2, "Other", [ManagerEditLockTargets.Schedule(f.Container, f.SourceSchedule)]);
        var before = await SnapshotAsync(db);
        var result = await ManagerController(db, locks: locks).CreateManualOffer(f.Container, f.SourceSchedule,
            new() { ManualColumnId = 7, DayOfMonth = 5, FromTime = "09:00", ToTime = "13:00" }, default);
        Assert.Equal(409, Assert.IsType<ConflictObjectResult>(result.Result).StatusCode);
        await AssertUnchangedAfterFailure(database, before);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Private_exchange_visibility_and_original_identity_survive_archiving(bool accepted)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: SwapKind.Exchange);
        var f = await SeedAsync(db, scenario);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(Request(f, scenario), default)).Result).Value);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await EmployeeController(db, f.Third).GetVisible(default)).Result).Value));
        if (accepted) await EmployeeController(db, f.Target).Accept(offer.Id, default);
        else await EmployeeController(db, f.Owner).Cancel(offer.Id, default);
        (await db.Employees.FindAsync(f.Target))!.IsDeleted = true;
        await db.SaveChangesAsync(); await db.ScheduleSlots.ExecuteDeleteAsync();
        await using var fresh = database.CreateContext();
        foreach (var actor in new[] { f.Owner, f.Target })
        {
            var view = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await EmployeeController(fresh, actor).GetVisible(default)).Result).Value));
            Assert.Equal(accepted ? "accepted" : "cancelled", view.Status);
            Assert.Equal("Worker1 Test", view.FromEmployeeName); Assert.Equal("Worker2 Test", view.TargetEmployeeName);
            Assert.Equal("Return", view.ReturnScheduleName); Assert.Equal(2, view.ReturnDayOfMonth);
        }
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await EmployeeController(fresh, f.Third).GetVisible(default)).Result).Value));
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await ManagerController(fresh).GetContainerSwaps(f.OtherContainer, default)).Result).Value));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Manual_offer_accept_or_cancel_preserves_unrelated_assignments(bool addressed, bool cancel)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new(Kind: SwapKind.ManagerManual));
        (await db.Schedules.FindAsync(f.SourceSchedule))!.Note = "Planner\n\n[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"5\":\"09:00 - 13:00\",\"6\":\"11:00 - 12:00\"}]]}]]";
        await db.SaveChangesAsync();
        var before = await ReadAssignmentIntervalsAsync(db);
        var manager = ManagerController(db);
        var request = new CreateManagerShiftSwapRequest { ManualColumnId = 7, DayOfMonth = 5, FromTime = "09:00", ToTime = "13:00", TargetEmployeeId = addressed ? f.Target : null };
        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await manager.CreateManualOffer(f.Container, f.SourceSchedule, request, default)).Result).Value);
        Assert.True(created.IsManagerCreated); Assert.Equal(addressed ? "private" : "public", created.Visibility);
        var snapshot = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => manager.CreateManualOffer(f.Container, f.SourceSchedule, request, default));
        await AssertUnchangedAfterFailure(database, snapshot);
        if (cancel) Assert.IsType<NoContentResult>(await manager.CancelManualOffer(f.Container, f.SourceSchedule, created.Id, default));
        else await EmployeeController(db, f.Target).Accept(created.Id, default);
        await using var fresh = database.CreateContext();
        var after = await ReadAssignmentIntervalsAsync(fresh);
        Assert.Equal(before, after.Where(x => x.Day != 5));
        if (cancel) {
            Assert.False(await fresh.ScheduleSlots.AnyAsync(s => s.Id == created.ScheduleSlotId));
            Assert.Empty(await fresh.ShiftSwapHistories.ToListAsync());
        } else {
            var manual = Assert.Single(after, x => x.Day == 5);
            Assert.Equal(f.Target, manual.EmployeeId); Assert.Equal(540, manual.FromMinute); Assert.Equal(780, manual.ToMinute);
            Assert.Equal(before.Sum(x => x.Minutes) + 240, after.Sum(x => x.Minutes));
            await AssertVersionAndHistoryConsistency(fresh, created.Id, f.SourceSchedule);
            var history = await fresh.ShiftSwapHistories.SingleAsync();
            var old = System.Text.Json.JsonSerializer.Deserialize<ShiftSwapScheduleSnapshotDto>(history.BeforeSnapshotJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var current = System.Text.Json.JsonSerializer.Deserialize<ShiftSwapScheduleSnapshotDto>(history.AfterSnapshotJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            Assert.Contains(old.Rows, r => r.Kind == "manual" && r.DayValues.ContainsKey(5));
            Assert.DoesNotContain(current.Rows, r => r.Kind == "manual" && r.DayValues.ContainsKey(5));
            Assert.Contains(current.Rows, r => r.Kind == "manual" && r.DayValues.ContainsKey(6));
        }
        await AssertInvariants(fresh);
    }

    [Theory]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData("unpublished")]
    [InlineData("disabled")]
    [InlineData("graph")]
    public async Task Manual_invalid_inputs_and_schedule_guards_leave_database_unchanged(string invalid)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, new(Published: invalid != "unpublished", AllowSwap: invalid != "disabled"));
        var request = new CreateManagerShiftSwapRequest { ManualColumnId = 7, DayOfMonth = invalid == "date" ? 32 : 5, FromTime = "09:00", ToTime = invalid == "time" ? "09:00" : "13:00" };
        var before = await SnapshotAsync(db);
        var action = () => ManagerController(db).CreateManualOffer(f.Container, invalid == "graph" ? 999 : f.SourceSchedule, request, default);
        if (invalid == "graph") await Assert.ThrowsAsync<KeyNotFoundException>(action);
        else await Assert.ThrowsAsync<ValidationException>(action);
        await AssertUnchangedAfterFailure(database, before);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Manager_delete_log_never_undoes_accepted_assignments(bool exchange)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: exchange ? SwapKind.Exchange : SwapKind.Give);
        var f = await SeedAsync(db, scenario);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(Request(f, scenario), default)).Result).Value);
        await EmployeeController(db, f.Target).Accept(offer.Id, default);
        var manager = ManagerController(db);
        foreach (var graph in exchange ? new[] { f.SourceSchedule, f.ReturnSchedule } : new[] { f.SourceSchedule })
            Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await manager.GetGraphLog(f.Container, graph, default)).Result).Value));
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => manager.CancelContainerSwap(f.Container, offer.Id, default));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => manager.DeleteContainerSwap(f.OtherContainer, offer.Id, default));
        await AssertUnchangedAfterFailure(database, before);
        var assignments = await ReadAssignmentIntervalsAsync(db);
        Assert.IsType<NoContentResult>(await manager.DeleteContainerSwap(f.Container, offer.Id, default));
        await using var fresh = database.CreateContext();
        Assert.Equal(assignments, await ReadAssignmentIntervalsAsync(fresh));
        Assert.Empty(await fresh.ShiftSwapHistories.ToListAsync());
        Assert.Equal(exchange ? 4 : 2, await fresh.ScheduleVersions.CountAsync());
    }

    [Fact]
    public async Task Opposite_reservations_block_give_and_exchange_until_cancelled()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: SwapKind.Exchange);
        var f = await SeedAsync(db, scenario);
        var request = Request(f, scenario);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(request, default)).Result).Value);
        var reverse = new CreateEmployeeShiftSwapRequest { ScheduleId = f.ReturnSchedule, ScheduleSlotId = f.ReturnSlot, TargetEmployeeId = f.Owner, FromTime = "09:00", ToTime = "17:00" };
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Create(reverse, default));
        reverse.RequestKind = "exchange"; reverse.ReturnScheduleId = f.SourceSchedule; reverse.ReturnScheduleSlotId = f.SourceSlot; reverse.ReturnFromTime = "09:00"; reverse.ReturnToTime = "17:00";
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Create(reverse, default));
        await AssertUnchangedAfterFailure(database, before);
        await EmployeeController(db, f.Owner).Cancel(offer.Id, default);
        before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Owner).Cancel(offer.Id, default));
        await AssertUnchangedAfterFailure(database, before);
        Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Target).Create(reverse, default)).Result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_recipients_or_owner_cancel_race_has_exactly_one_winner(bool cancel)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var setup = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: cancel ? SwapKind.Exchange : SwapKind.Give);
        var f = await SeedAsync(setup, scenario);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(setup, f.Owner).Create(Request(f, scenario), default)).Result).Value);
        var gate = new ShiftSwapSafetyTests.TransactionGate();
        await using var first = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(gate).Options);
        await using var second = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(gate).Options);
        var results = await Task.WhenAll(Task.Run(() => Record.ExceptionAsync(() => EmployeeController(first, f.Target).Accept(offer.Id, default))),
            Task.Run(() => Record.ExceptionAsync(() => cancel ? EmployeeController(second, f.Owner).Cancel(offer.Id, default) : EmployeeController(second, f.Third).Accept(offer.Id, default))));
        Assert.Single(results, e => e is null);
        Assert.IsType<ValidationException>(Assert.Single(results, e => e is not null));
        await using var fresh = database.CreateContext();
        var saved = await fresh.ShiftSwapRequests.SingleAsync();
        Assert.Contains(saved.Status, new[] { ShiftSwapStatus.Accepted, ShiftSwapStatus.Cancelled });
        Assert.Equal(saved.Status == ShiftSwapStatus.Accepted ? 1 : 0, await fresh.ShiftSwapHistories.CountAsync());
        Assert.Equal(saved.Status == ShiftSwapStatus.Accepted ? (cancel ? 4 : 2) : 0, await fresh.ScheduleVersions.CountAsync());
        Assert.Equal(960, (await ReadAssignmentIntervalsAsync(fresh)).Sum(s => s.Minutes));
        await AssertInvariants(fresh);
    }

    [Theory]
    [InlineData("history")]
    [InlineData("version")]
    public async Task Failed_write_rolls_back_every_table_and_sends_no_notification(string failure)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: SwapKind.Exchange);
        var f = await SeedAsync(db, scenario);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(Request(f, scenario), default)).Result).Value);
        await db.Database.ExecuteSqlRawAsync(failure == "history"
            ? "CREATE TRIGGER fail_matrix BEFORE INSERT ON shift_swap_history BEGIN SELECT RAISE(ABORT, 'injected'); END;"
            : $"CREATE TRIGGER fail_matrix BEFORE INSERT ON schedule_version WHEN NEW.schedule_id={f.ReturnSchedule} AND NEW.version_number=2 BEGIN SELECT RAISE(ABORT, 'injected'); END;");
        var before = await SnapshotAsync(db);
        var notifier = new CommittedNotifier(database);
        Assert.NotNull(await Record.ExceptionAsync(() => EmployeeController(db, f.Target, notifier: notifier).Accept(offer.Id, default)));
        Assert.Empty(notifier.Graphs);
        await AssertUnchangedAfterFailure(database, before);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cross_graph_events_are_after_commit_and_failure_is_best_effort(bool notificationFailure)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = new ShiftSwapScenario(Kind: SwapKind.Exchange);
        var f = await SeedAsync(db, scenario);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, f.Owner).Create(Request(f, scenario), default)).Result).Value);
        var notifier = new CommittedNotifier(database, notificationFailure);
        await EmployeeController(db, f.Target, notifier: notifier).Accept(offer.Id, default);
        Assert.Equal(new[] { f.SourceSchedule, f.ReturnSchedule }.Order(), notifier.Graphs.Distinct().Order());
        Assert.Equal(4, notifier.Graphs.Count); Assert.True(notifier.AllCommitted);
        await using var fresh = database.CreateContext();
        await AssertVersionAndHistoryConsistency(fresh, offer.Id, f.SourceSchedule, f.ReturnSchedule);
    }

    private sealed class CommittedNotifier(SqliteTestDatabase database, bool fail = false) : IRealtimeNotifier
    {
        public List<int> Graphs { get; } = [];
        public bool AllCommitted { get; private set; } = true;
        private async Task Record(int graph)
        {
            await using var db = database.CreateContext();
            AllCommitted &= await db.ShiftSwapHistories.CountAsync() == 1 && await db.ScheduleVersions.CountAsync() == 4;
            Graphs.Add(graph);
            if (fail) throw new InvalidOperationException("injected notification failure");
        }
        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason) => Record(graphId);
        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null) => Record(graphId!.Value);
        public Task NotifyManagerDataChangedAsync(string resourceType, string? resourceId, string reason, int? containerId = null, int? graphId = null) => Task.CompletedTask;
        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry) => Task.CompletedTask;
        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state) => Task.CompletedTask;
        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state) => Task.CompletedTask;
    }
}
