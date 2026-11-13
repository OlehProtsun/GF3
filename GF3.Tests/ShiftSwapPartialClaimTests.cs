using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApi.Contracts.ShiftSwaps;
using WebApi.ShiftSwaps;
using WebApi.Realtime;
using static GF3.Tests.Infrastructure.ShiftSwapScenarioFactory;

namespace GF3.Tests;

public sealed class ShiftSwapPartialClaimTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Migration_EmptyAndPopulatedUpgrade_DefaultStrictAndRollback(bool populated)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        if (populated) {
            var fixture = await SeedAsync(db, new());
            await EmployeeController(db, fixture.Owner).Create(Request(fixture, new()), default);
        }
        db.ChangeTracker.Clear();
        var before = System.Text.Json.JsonSerializer.Serialize(await db.ShiftSwapRequests.AsNoTracking().ToListAsync());
        var previous = db.Database.GetMigrations().ToArray()[^2];
        await db.GetService<IMigrator>().MigrateAsync(previous);
        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await db.ShiftSwapRequests.AsNoTracking().ToListAsync()));
        Assert.All(await db.ShiftSwapRequests.ToListAsync(), offer => Assert.False(offer.AllowPartialAcceptance));
    }

    private static ShiftSwapDto Created(ActionResult<ShiftSwapDto> result) => Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
    private static ShiftSwapDto Accepted(ActionResult<ShiftSwapDto> result) => Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>(result.Result).Value);

    [Theory]
    [InlineData("09:00", "21:00", "09:00", "15:00", 1)]
    [InlineData("10:00", "19:00", "12:00", "15:00", 2)]
    public async Task Employee_PartialClaim_OffersOnlyUnclaimedPublishedTime(string from, string to, string claimFrom, string claimTo, int residuals)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var slot = await db.ScheduleSlots.SingleAsync(s => s.Id == f.SourceSlot); slot.ToTime = "21:00"; await db.SaveChangesAsync();
        var request = Request(f, new()); request.FromTime = from; request.ToTime = to; request.AllowPartialAcceptance = true;
        var offer = Created(await EmployeeController(db, f.Owner).Create(request, default));
        var notifier = new CommittedNotifier(database);
        var accepted = Accepted(await EmployeeController(db, f.Target, notifier: notifier).Accept(offer.Id, default, new() { FromTime = claimFrom, ToTime = claimTo }));
        Assert.True(notifier.AllCommitted);
        Assert.Equal(1 + residuals, notifier.SwapIds.Count);
        Assert.Equal(claimFrom, accepted.FromTime); Assert.Equal(claimTo, accepted.ToTime);
        await using var fresh = database.CreateContext();
        var open = await fresh.ShiftSwapRequests.Include(r => r.ScheduleSlot).Where(r => r.Status == ShiftSwapStatus.Open).OrderBy(r => r.OfferedFromTime).ToListAsync();
        Assert.Equal(residuals, open.Count);
        Assert.All(open, r => { Assert.True(r.AllowPartialAcceptance); Assert.Equal(f.Owner, r.ScheduleSlot!.EmployeeId); Assert.Equal(offer.Visibility == "public" ? ShiftSwapVisibility.Public : ShiftSwapVisibility.Private, r.Visibility); });
        Assert.Equal(claimFrom, (await fresh.ShiftSwapHistories.SingleAsync()).FromTime);
        Assert.Equal(2, await fresh.ScheduleVersions.CountAsync());
        Assert.Equal(12 * 60, (await ReadAssignmentIntervalsAsync(fresh)).Where(s => s.ScheduleId == f.SourceSchedule).Sum(s => s.Minutes));
        if (residuals == 2) Assert.Equal(new[] { ("10:00", "12:00"), ("15:00", "19:00") }, open.Select(r => (r.OfferedFromTime!, r.OfferedToTime!)));
        await AssertNoOverlaps(fresh);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OccupiedSecondHalf_AdvisoryAndAcceptanceRespectFreeWindow(bool flexible)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await SeedAsync(db, new(DayRelation: DayRelation.SameDay));
        (await db.ScheduleSlots.SingleAsync(s => s.Id == f.SourceSlot)).ToTime = "21:00";
        var occupied = await db.ScheduleSlots.SingleAsync(s => s.Id == f.ReturnSlot); occupied.FromTime = "15:00"; occupied.ToTime = "21:00"; await db.SaveChangesAsync();
        var request = Request(f, new()); request.ToTime = "21:00"; request.AllowPartialAcceptance = flexible;
        var offer = Created(await EmployeeController(db, f.Owner).Create(request, default));
        var list = Assert.IsType<List<ShiftSwapDto>>(Assert.IsType<OkObjectResult>((await EmployeeController(db, f.Target).GetVisible(default)).Result).Value);
        var dto = Assert.Single(list); Assert.Equal(flexible, dto.CanAccept); Assert.False(dto.CanAcceptFull);
        if (flexible) { Assert.Equal(new[] { new ShiftSwapEligibleWindowDto("09:00","15:00") }, dto.EligibleClaimWindows); Assert.Equal(6, dto.CurrentEmployeeHoursAfter - dto.CurrentEmployeeHoursBefore); }
        var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Accept(offer.Id, default, new() { FromTime = "12:00", ToTime = "18:00" }));
        await AssertUnchangedAfterFailure(database, before);
        if (flexible) await EmployeeController(db, f.Target).Accept(offer.Id, default, new() { FromTime = "09:00", ToTime = "15:00" });
        else await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Accept(offer.Id, default));
    }

    [Theory]
    [InlineData(null, "15:00")][InlineData("09:00", null)][InlineData("", "15:00")]
    [InlineData("08:00", "15:00")][InlineData("15:00", "15:00")][InlineData("12:00", "09:00")]
    public async Task InvalidClaim_RollsBackEveryTable(string? from, string? to)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await SeedAsync(db, new()); var request = Request(f, new()); request.AllowPartialAcceptance = true;
        var offer = Created(await EmployeeController(db, f.Owner).Create(request, default)); var before = await SnapshotAsync(db);
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, f.Target).Accept(offer.Id, default, new() { FromTime = from, ToTime = to }));
        await AssertUnchangedAfterFailure(database, before);
    }

    [Fact]
    public async Task Strict_CustomRejected_AndLegacyNoBodyAcceptsFullOffer()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await SeedAsync(db, new()); var offer = Created(await EmployeeController(db, f.Owner).Create(Request(f,new()),default));
        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db,f.Target).Accept(offer.Id,default,new() { FromTime="09:00",ToTime="13:00" }));
        Assert.Equal("17:00", Accepted(await EmployeeController(db,f.Target).Accept(offer.Id,default)).ToTime);
    }

    [Fact]
    public async Task Manager_PartialAndFinalClaims_PreserveResidualDemandAndMetadata()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await SeedAsync(db, new());
        var schedule = await db.Schedules.SingleAsync(s => s.Id == f.SourceSchedule);
        schedule.Note = "old text\n\n[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"1\":\"09:00-21:00\",\"2\":\"text\"}]],\"g\":[7],\"o\":[-7],\"s\":[[0,1,123]],\"t\":[[1,2,\"note\"]],\"u\":{\"1\":[\"1:2\"]}}]]";
        await db.SaveChangesAsync();
        var offer = Created(await ManagerController(db).CreateManualOffer(f.Container, f.SourceSchedule,new() { ManualColumnId=7,DayOfMonth=1,FromTime="09:00",ToTime="21:00",AllowPartialAcceptance=true }, default));
        await EmployeeController(db,f.Target).Accept(offer.Id,default,new() { FromTime="09:00",ToTime="15:00" });
        await using var fresh = database.CreateContext();
        var residual = await fresh.ShiftSwapRequests.Include(r=>r.ScheduleSlot).SingleAsync(r=>r.Status==ShiftSwapStatus.Open);
        Assert.Null(residual.ScheduleSlot!.EmployeeId); Assert.Equal("15:00", residual.OfferedFromTime); Assert.Equal("21:00",residual.OfferedToTime);
        var note = (await fresh.Schedules.SingleAsync(s=>s.Id==f.SourceSchedule)).Note;
        Assert.StartsWith("old text\n\n",note); Assert.Equal("15:00 - 21:00",GraphManualColumnLabelResolver.ResolveCell(note,7,1));
        Assert.Equal("text", GraphManualColumnLabelResolver.ResolveCell(note,7,2));
        var meta = ShiftSwapRules.ParseGraphNoteMeta(note!.Split("[[GF3_GRAPH_META:")[1][..^2]);
        Assert.All(new[]{"g","o","s","t","u"}, key=>Assert.NotNull(meta![key]));
        Assert.Equal(ShiftSwapHighlightRules.ToArgb("#BAE6FD"), (await fresh.ScheduleCellStyles.SingleAsync()).BackgroundColorArgb);
        (await fresh.Schedules.SingleAsync(s => s.Id == f.SourceSchedule)).AcceptedManualShiftHighlightColor = "#FED7AA";
        await fresh.SaveChangesAsync();
        await EmployeeController(fresh,f.Third).Accept(residual.Id,default);
        await using var final = database.CreateContext();
        Assert.Null(GraphManualColumnLabelResolver.ResolveCell((await final.Schedules.SingleAsync(s=>s.Id==f.SourceSchedule)).Note,7,1));
        Assert.Equal(0,await final.ShiftSwapRequests.CountAsync(r=>r.Status==ShiftSwapStatus.Open));
        Assert.Equal(2,await final.ShiftSwapHistories.CountAsync()); Assert.Equal(3,await final.ScheduleVersions.CountAsync()); // The second before-state reuses the identical first after-state.
        Assert.Equal(2,await final.ScheduleSlots.CountAsync(s=>s.ScheduleId==f.SourceSchedule && (s.EmployeeId==f.Target||s.EmployeeId==f.Third)));
        Assert.Equal(ShiftSwapHighlightRules.ToArgb("#BAE6FD"), (await final.ScheduleCellStyles.SingleAsync(s => s.EmployeeId == f.Target)).BackgroundColorArgb);
        Assert.Equal(ShiftSwapHighlightRules.ToArgb("#FED7AA"), (await final.ScheduleCellStyles.SingleAsync(s => s.EmployeeId == f.Third)).BackgroundColorArgb);
        var finalNote = (await final.Schedules.SingleAsync(s => s.Id == f.SourceSchedule)).Note!;
        var finalMeta = ShiftSwapRules.ParseGraphNoteMeta(finalNote.Split("[[GF3_GRAPH_META:")[1][..^2])!;
        var manualStyle = Assert.Single(((System.Text.Json.Nodes.JsonArray)finalMeta["s"]!).OfType<System.Text.Json.Nodes.JsonArray>(), s => (int)s[0]! == -7);
        Assert.Equal(ShiftSwapHighlightRules.ToArgb("#FED7AA"), (int)manualStyle[2]!);
    }

    [Fact]
    public async Task Manager_IndependentAndDuplicateCapacity_CancelReleasesOnlyReservation()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext(); var f=await SeedAsync(db,new());
        var schedule=await db.Schedules.SingleAsync(s=>s.Id==f.SourceSchedule);
        schedule.Note="[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"1\":\"09:00-15:00,09:00-15:00,15:00-21:00\"}]]}]]"; await db.SaveChangesAsync();
        CreateManagerShiftSwapRequest request=new(){ManualColumnId=7,DayOfMonth=1,FromTime="09:00",ToTime="15:00"};
        var first=Created(await ManagerController(db).CreateManualOffer(f.Container,f.SourceSchedule,request,default));
        Created(await ManagerController(db).CreateManualOffer(f.Container,f.SourceSchedule,request,default));
        await Assert.ThrowsAsync<ValidationException>(()=>ManagerController(db).CreateManualOffer(f.Container,f.SourceSchedule,request,default));
        await ManagerController(db).CancelManualOffer(f.Container,f.SourceSchedule,first.Id,default);
        Created(await ManagerController(db).CreateManualOffer(f.Container,f.SourceSchedule,request,default));
        request.FromTime="15:00";request.ToTime="21:00"; Created(await ManagerController(db).CreateManualOffer(f.Container,f.SourceSchedule,request,default));
        Assert.Equal("09:00-15:00,09:00-15:00,15:00-21:00",GraphManualColumnLabelResolver.ResolveCell(schedule.Note,7,1));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CompetingPartialClaims_OneWinnerOneResidualOneHistory(bool manager)
    {
        await using var database=await SqliteTestDatabase.CreateAsync(); await using var setup=database.CreateContext(); var f=await SeedAsync(setup,new());
        ShiftSwapDto offer;
        if (manager) {
            (await setup.Schedules.SingleAsync(s=>s.Id==f.SourceSchedule)).Note="[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"1\":\"09:00-17:00\"}]]}]]";
            await setup.SaveChangesAsync(); offer=Created(await ManagerController(setup).CreateManualOffer(f.Container,f.SourceSchedule,new(){ManualColumnId=7,DayOfMonth=1,FromTime="09:00",ToTime="17:00",AllowPartialAcceptance=true},default));
        } else { var request=Request(f,new()); request.AllowPartialAcceptance=true; offer=Created(await EmployeeController(setup,f.Owner).Create(request,default)); }
        var gate=new ShiftSwapSafetyTests.TransactionGate();
        await using var first=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(gate).Options);
        await using var second=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(gate).Options);
        var results=await Task.WhenAll(Task.Run(()=>Record.ExceptionAsync(()=>EmployeeController(first,f.Target).Accept(offer.Id,default,new(){FromTime="09:00",ToTime="13:00"}))),
            Task.Run(()=>Record.ExceptionAsync(()=>EmployeeController(second,f.Third).Accept(offer.Id,default,new(){FromTime="09:00",ToTime="13:00"}))));
        Assert.Single(results,e=>e is null); Assert.IsType<ValidationException>(Assert.Single(results,e=>e is not null));
        await using var fresh=database.CreateContext(); Assert.Equal(1,await fresh.ShiftSwapHistories.CountAsync()); Assert.Equal(2,await fresh.ScheduleVersions.CountAsync());
        var residual=await fresh.ShiftSwapRequests.SingleAsync(r=>r.Status==ShiftSwapStatus.Open); Assert.Equal("13:00",residual.OfferedFromTime); Assert.Equal("17:00",residual.OfferedToTime);
    }

    [Theory]
    [InlineData(false,"history")][InlineData(false,"version")][InlineData(true,"history")][InlineData(true,"version")]
    public async Task PartialWriteFailure_RollsBackSlotsOffersNoteHistoryAndVersions(bool manager,string failure)
    {
        await using var database=await SqliteTestDatabase.CreateAsync(); await using var db=database.CreateContext(); var f=await SeedAsync(db,new());
        ShiftSwapDto offer;
        if (manager) {
            (await db.Schedules.SingleAsync(s=>s.Id==f.SourceSchedule)).Note="[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"1\":\"09:00-17:00\"}]]}]]";
            await db.SaveChangesAsync(); offer=Created(await ManagerController(db).CreateManualOffer(f.Container,f.SourceSchedule,new(){ManualColumnId=7,DayOfMonth=1,FromTime="09:00",ToTime="17:00",AllowPartialAcceptance=true},default));
        } else {var request=Request(f,new());request.AllowPartialAcceptance=true;offer=Created(await EmployeeController(db,f.Owner).Create(request,default));}
        await db.Database.ExecuteSqlRawAsync(failure=="history" ? "CREATE TRIGGER fail_partial BEFORE INSERT ON shift_swap_history BEGIN SELECT RAISE(ABORT, 'injected'); END;"
            : "CREATE TRIGGER fail_partial BEFORE INSERT ON schedule_version WHEN NEW.version_number>=2 BEGIN SELECT RAISE(ABORT, 'injected'); END;");
        var before=await SnapshotAsync(db);
        var notifier = new CommittedNotifier(database);
        Assert.NotNull(await Record.ExceptionAsync(()=>EmployeeController(db,f.Target,notifier: notifier).Accept(offer.Id,default,new(){FromTime="09:00",ToTime="13:00"})));
        Assert.Empty(notifier.SwapIds);
        await AssertUnchangedAfterFailure(database,before);
    }

    [Fact]
    public async Task Manager_DoublePublishRace_CannotOversubscribe()
    {
        await using var database=await SqliteTestDatabase.CreateAsync(); await using var setup=database.CreateContext();var f=await SeedAsync(setup,new());
        (await setup.Schedules.SingleAsync(s=>s.Id==f.SourceSchedule)).Note="[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"1\":\"09:00-17:00\"}]]}]]";await setup.SaveChangesAsync();
        var gate=new ShiftSwapSafetyTests.TransactionGate();
        await using var first=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(gate).Options);
        await using var second=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(gate).Options);
        CreateManagerShiftSwapRequest request=new(){ManualColumnId=7,DayOfMonth=1,FromTime="09:00",ToTime="17:00"};
        var results=await Task.WhenAll(Task.Run(()=>Record.ExceptionAsync(()=>ManagerController(first).CreateManualOffer(f.Container,f.SourceSchedule,request,default))),
            Task.Run(()=>Record.ExceptionAsync(()=>ManagerController(second).CreateManualOffer(f.Container,f.SourceSchedule,request,default))));
        Assert.Single(results,e=>e is null);Assert.IsType<ValidationException>(Assert.Single(results,e=>e is not null));
        await using var fresh=database.CreateContext();Assert.Equal(1,await fresh.ShiftSwapRequests.CountAsync());Assert.Equal(1,await fresh.ScheduleSlots.CountAsync(s=>s.EmployeeId==null));
    }

    private sealed class CommittedNotifier(SqliteTestDatabase database) : IRealtimeNotifier
    {
        public List<int?> SwapIds { get; } = [];
        public bool AllCommitted { get; private set; } = true;
        public async Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null)
        {
            await using var persisted = database.CreateContext();
            AllCommitted &= await persisted.ShiftSwapHistories.CountAsync() > 0 && await persisted.ScheduleVersions.CountAsync() >= 2;
            if (reason.EndsWith("created", StringComparison.Ordinal))
                AllCommitted &= await persisted.ShiftSwapRequests.AnyAsync(r => r.Id == shiftSwapId && r.Status == ShiftSwapStatus.Open);
            SwapIds.Add(shiftSwapId);
        }
        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason) => Task.CompletedTask;
        public Task NotifyManagerDataChangedAsync(string resourceType, string? resourceId, string reason, int? containerId = null, int? graphId = null) => Task.CompletedTask;
        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry) => Task.CompletedTask;
        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state) => Task.CompletedTask;
        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state) => Task.CompletedTask;
    }
}
