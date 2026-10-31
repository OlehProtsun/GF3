using BusinessLogicLayer.Contracts.Urlop;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebApi.Controllers;
using WebApi.Services;

namespace GF3.Tests;

public sealed class UrlopScheduleGuardTests
{
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task ExchangeChecksBothFinalRecipients(bool returnRecipient)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db);
        (await db.Employees.FindAsync(f.Target))!.ContractType = "zlecenie"; await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner),
            ShiftSwapScenarioFactory.Request(f, new(Kind: SwapKind.Exchange, Scope: SwapScope.Private)), default);
        var slot = await db.ScheduleSlots.Include(x => x.Schedule).SingleAsync(x => x.Id == (returnRecipient ? f.ReturnSlot : f.SourceSlot));
        await UrlopTestData.Service(db).CreateApprovedAsync(99, slot.Schedule.ShopId, returnRecipient ? f.Owner : f.Target,
            slot.Schedule.Year, slot.Schedule.Month, [slot.DayOfMonth], default);
        var before = await ShiftSwapScenarioFactory.SnapshotAsync(db);
        await Assert.ThrowsAsync<UrlopConflictException>(() => engine.Accept(f.Target, ContractTestFactory.Employee(f.Target), offer.Id, default));
        db.ChangeTracker.Clear();
        Assert.Equal(before, await ShiftSwapScenarioFactory.SnapshotAsync(db));
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task PartialPickupAndManagerManualClaimCheckRecipient(bool manual)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db);
        (await db.Employees.FindAsync(f.Target))!.ContractType = "zlecenie"; await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db);
        WebApi.Contracts.ShiftSwaps.ShiftSwapDto offer;
        if (manual)
        {
            (await db.Schedules.FindAsync(f.SourceSchedule))!.Note = "[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"5\":\"09:00 - 17:00\"}]]}]]";
            await db.SaveChangesAsync();
            var result = await ShiftSwapScenarioFactory.ManagerController(db).CreateManualOffer(f.Container, f.SourceSchedule,
                new() { ManualColumnId = 7, DayOfMonth = 5, FromTime = "09:00", ToTime = "17:00", AllowPartialAcceptance = true }, default);
            offer = Assert.IsType<WebApi.Contracts.ShiftSwaps.ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        }
        else
        {
            var request = ShiftSwapScenarioFactory.Request(f, new()); request.AllowPartialAcceptance = true;
            offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), request, default);
        }
        var schedule = await db.Schedules.SingleAsync(x => x.Id == f.SourceSchedule);
        await UrlopTestData.Service(db).CreateApprovedAsync(99, schedule.ShopId, f.Target, schedule.Year, schedule.Month, [manual ? 5 : 1], default);
        var before = await ShiftSwapScenarioFactory.SnapshotAsync(db);
        await Assert.ThrowsAsync<UrlopConflictException>(() => engine.Accept(f.Target, ContractTestFactory.Employee(f.Target), offer.Id, default,
            new() { FromTime = "09:00", ToTime = "13:00" }));
        db.ChangeTracker.Clear();
        Assert.Equal(before, await ShiftSwapScenarioFactory.SnapshotAsync(db));
    }

    internal static ContainerService Container(AppDbContext db) => new(new ContainerRepository(db), new ScheduleRepository(db),
        new SchedulePresetRepository(db), new ScheduleSlotRepository(db), new ScheduleEmployeeRepository(db),
        new ScheduleCellStyleRepository(db), new AvailabilityGroupRepository(db), new ScheduleGenerator(),
        urlop: UrlopTestData.Service(db), db: db);

    [Theory]
    [InlineData("create")][InlineData("update")][InlineData("replace")][InlineData("month")][InlineData("shop")]
    public async Task AllManualWritesRejectVacationAtomically(string operation)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var urlop = UrlopTestData.Service(db); var service = Container(db);
        var graph = (await service.GetGraphByIdAsync(1, f.Schedule))!;
        var slot = new ScheduleSlotModel { ScheduleId = f.Schedule, DayOfMonth = 3, EmployeeId = f.Employee, FromTime = "09:00", ToTime = "17:00", SlotNo = 1, Status = BusinessLogicLayer.Contracts.Enums.SlotStatus.ASSIGNED };
        if (operation is "update" or "month" or "shop") slot = await service.CreateGraphSlotAsync(1, f.Schedule, slot);
        if (operation == "update") slot.DayOfMonth = 4;
        var targetShop = operation == "shop" ? f.OtherShop : f.Shop;
        var targetMonth = operation == "month" ? 12 : 11;
        await urlop.CreateApprovedAsync(f.Manager, targetShop, f.Employee, 2026, targetMonth, [operation == "update" ? 4 : 3], default);
        await Assert.ThrowsAsync<UrlopConflictException>(async () =>
        {
            if (operation == "create") await service.CreateGraphSlotAsync(1, f.Schedule, slot);
            else if (operation == "update") await service.UpdateGraphSlotAsync(1, f.Schedule, slot.Id, slot);
            else if (operation == "replace") await service.ReplaceGraphSlotsAsync(1, f.Schedule, [slot]);
            else { graph.Month = targetMonth; graph.ShopId = targetShop; await service.UpdateGraphAsync(1, f.Schedule, graph); }
        });
        db.ChangeTracker.Clear();
        Assert.Equal(11, (await db.Schedules.SingleAsync()).Month);
        Assert.Equal(f.Shop, (await db.Schedules.SingleAsync()).ShopId);
        var saved = await db.ScheduleSlots.ToListAsync();
        if (operation is "update" or "month" or "shop") Assert.Equal(3, Assert.Single(saved).DayOfMonth);
        else Assert.Empty(saved);
    }

    [Fact]
    public async Task OtherShopAssignmentAndDeletionRemainAllowed()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var service = Container(db);
        await UrlopTestData.Service(db).CreateApprovedAsync(f.Manager, f.OtherShop, f.Employee, 2026, 11, [3], default);
        var slot = await service.CreateGraphSlotAsync(1, f.Schedule, new() { DayOfMonth = 3, EmployeeId = f.Employee, FromTime = "09:00", ToTime = "17:00", SlotNo = 1, Status = BusinessLogicLayer.Contracts.Enums.SlotStatus.ASSIGNED });
        await service.DeleteGraphSlotAsync(1, f.Schedule, slot.Id);
        Assert.Empty(await db.ScheduleSlots.ToListAsync());
    }

    [Fact]
    public async Task VersionRestoreCannotReintroduceVacationAssignment()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await UrlopTestData.SeedAsync(db); var service = Container(db);
        await service.CreateGraphSlotAsync(1, f.Schedule, new() { DayOfMonth = 3, EmployeeId = f.Employee, FromTime = "09:00", ToTime = "17:00", SlotNo = 1, Status = BusinessLogicLayer.Contracts.Enums.SlotStatus.ASSIGNED });
        var versions = new ScheduleVersionCommitService(db);
        await versions.EnsureInitialVersionAsync(f.Schedule, default);
        var version = await db.ScheduleVersions.SingleAsync();
        await service.ReplaceGraphSlotsAsync(1, f.Schedule, []);
        await UrlopTestData.Service(db).CreateApprovedAsync(f.Manager, f.Shop, f.Employee, 2026, 11, [3], default);
        var controller = new GraphVersionsController(db, new NoopWorkflowLogService(), new NoopRealtimeNotifier(), versions)
        { ControllerContext = new ControllerContext { HttpContext = ContractTestFactory.Manager(f.Manager) } };
        await Assert.ThrowsAsync<UrlopConflictException>(() => controller.Checkout(1, f.Schedule, version.Id, default));
        Assert.Empty(await db.ScheduleSlots.ToListAsync());
    }

    [Theory]
    [InlineData("submit")][InlineData("close")][InlineData("approve")][InlineData("replace")]
    public async Task ConcurrentMutationsLeaveOneValidHistory(string operation)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        UrlopFixture f; Guid request;
        await using (var seed = database.CreateContext())
        {
            f = await UrlopTestData.SeedAsync(seed); var service = UrlopTestData.Service(seed);
            await service.SetMonthOpenAsync(f.Shop, 2026, 11, true, default);
            request = (await service.SubmitAsync(f.Employee, f.Shop, 2026, 11, [3, 4], new(2026, 11, 1), default)).RequestId;
            if (operation is "submit" or "close") await service.DeleteAsync(f.Manager, request, default);
        }
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Write(bool first)
        {
            await start.Task;
            await using var db = database.CreateContext(); var urlop = UrlopTestData.Service(db);
            try
            {
                if (operation == "submit" || operation == "close" && first)
                    await urlop.SubmitAsync(f.Employee, f.Shop, 2026, 11, [3, 4], new(2026, 11, 1), default);
                else if (operation == "close") await urlop.SetMonthOpenAsync(f.Shop, 2026, 11, false, default);
                else if (first) await urlop.ApproveAsync(f.Manager, request, default);
                else if (operation == "replace") await urlop.ReplaceDaysAsync(f.Manager, request, [4, 5], default);
                else await Container(db).CreateGraphSlotAsync(1, f.Schedule, new() { EmployeeId = f.Employee, DayOfMonth = 3, SlotNo = 1, Status = BusinessLogicLayer.Contracts.Enums.SlotStatus.ASSIGNED, FromTime = "09:00", ToTime = "17:00" });
                return true;
            }
            catch (UrlopConflictException) { return false; }
            catch (SqliteException ex) when (ex.SqliteErrorCode is 5 or 6) { return false; }
        }
        var tasks = new[] { Task.Run(() => Write(true)), Task.Run(() => Write(false)) }; start.SetResult();
        var results = await Task.WhenAll(tasks);
        Assert.Contains(true, results);
        await using var check = database.CreateContext();
        var rows = await check.UrlopDays.ToListAsync();
        Assert.All(rows.GroupBy(x => x.RequestId), group => { Assert.Single(group.Select(x => x.Status).Distinct()); Assert.Equal(2, group.Count()); });
        Assert.All(rows.Where(x => x.Status != "rejected").GroupBy(x => (x.ShopId, x.EmployeeId, x.Date)), group => Assert.Single(group));
        if (operation == "submit") Assert.Single(results, x => x);
        if (operation == "close") Assert.False((await check.UrlopMonths.SingleAsync()).IsOpen);
        if (operation == "approve") Assert.False(await check.UrlopDays.AnyAsync(x => x.Status == "approved" && check.ScheduleSlots.Any(s => s.EmployeeId == x.EmployeeId && s.DayOfMonth == x.Date.Day && s.Schedule.ShopId == x.ShopId && s.Schedule.Year == x.Date.Year && s.Schedule.Month == x.Date.Month)));
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task SwapAndActualManagerApprovalRejectVacationRecipient(bool reviewed)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db);
        var request = ShiftSwapScenarioFactory.Request(f, new());
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), request, default);
        var source = await db.ScheduleSlots.Include(x => x.Schedule).SingleAsync(x => x.Id == f.SourceSlot);
        var date = new DateOnly(source.Schedule.Year, source.Schedule.Month, source.DayOfMonth);
        await UrlopTestData.Service(db).CreateApprovedAsync(99, source.Schedule.ShopId, f.Target, date.Year, date.Month, [date.Day], default);
        if (reviewed)
        {
            var approvals = ContractTestFactory.Approvals(db, engine);
            var approval = await approvals.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
            await Assert.ThrowsAsync<UrlopConflictException>(() => approvals.ApproveAsync(99, f.Container, f.SourceSchedule, approval.Id, ContractTestFactory.Manager()));
        }
        else
        {
            (await db.Employees.FindAsync(f.Target))!.ContractType = "zlecenie"; await db.SaveChangesAsync();
            await Assert.ThrowsAsync<UrlopConflictException>(() => engine.Accept(f.Target, ContractTestFactory.Employee(f.Target), offer.Id, default));
        }
        db.ChangeTracker.Clear();
        Assert.Equal(f.Owner, (await db.ScheduleSlots.FindAsync(f.SourceSlot))!.EmployeeId);
    }
}
