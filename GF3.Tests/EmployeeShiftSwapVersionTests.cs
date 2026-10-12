using System.Security.Claims;
using System.Text.Json;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Controllers;
using WebApi.Realtime;
using WebApi.Services;

namespace GF3.Tests;

public sealed class EmployeeShiftSwapVersionTests
{
    [Theory]
    [InlineData("current", false)]
    [InlineData("current", true)]
    [InlineData("none", false)]
    [InlineData("stale", false)]
    public async Task Accept_PreservesBeforeAndCompleteAfterSnapshots(string history, bool full)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, full: full);
        var writer = new ScheduleVersionCommitService(db);
        if (history != "none") await writer.EnsureInitialVersionAsync(f.Schedule, default);
        if (history == "stale")
        {
            var schedule = await db.Schedules.SingleAsync(s => s.Id == f.Schedule);
            schedule.Name = "Unversioned recovery state";
            schedule.Note = "Unversioned note";
            schedule.AcceptedSwapHighlightColor = "#FF0000";
            await db.SaveChangesAsync();
        }
        await Employee(db, f.Target).Accept(f.Swap, default);
        db.ChangeTracker.Clear();
        var versions = await db.ScheduleVersions.OrderBy(v => v.VersionNumber).ToListAsync();
        Assert.Equal(history == "stale" ? 3 : 2, versions.Count);
        var before = versions[^2];
        var after = versions[^1];
        Assert.Equal(before.Id, after.ParentVersionId);
        Assert.Equal(after.Id, (await db.ScheduleVersionStates.SingleAsync()).CurrentVersionId);
        Assert.All(versions, v => Assert.Equal("main", v.BranchName));
        Assert.Null(after.CreatedByManagerId);
        Assert.Equal("Swap accepted: Target Worker", after.CreatedByManagerName);
        using var oldJson = JsonDocument.Parse(before.SnapshotJson);
        using var newJson = JsonDocument.Parse(after.SnapshotJson);
        var old = oldJson.RootElement;
        var current = newJson.RootElement;
        Assert.Equal(f.Owner, Assert.Single(old.GetProperty("slots").EnumerateArray()).GetProperty("employeeId").GetInt32());
        Assert.Single(old.GetProperty("employees").EnumerateArray());
        Assert.Empty(old.GetProperty("cellStyles").EnumerateArray());
        var slots = current.GetProperty("slots").EnumerateArray().ToList();
        Assert.Equal(full ? 1 : 2, slots.Count);
        Assert.Contains(slots, s => s.GetProperty("employeeId").GetInt32() == f.Target && s.GetProperty("fromTime").GetString() == "09:00" && s.GetProperty("toTime").GetString() == (full ? "22:00" : "15:00"));
        if (!full) Assert.Contains(slots, s => s.GetProperty("employeeId").GetInt32() == f.Owner && s.GetProperty("fromTime").GetString() == "15:00" && s.GetProperty("toTime").GetString() == "22:00");
        Assert.Equal(2, current.GetProperty("employees").GetArrayLength());
        var styles = current.GetProperty("cellStyles").EnumerateArray().ToList();
        Assert.Contains(styles, s => s.GetProperty("employeeId").GetInt32() == f.Target);
        Assert.Contains(styles, s => s.GetProperty("employeeId").GetInt32() == f.Owner);
        Assert.All(styles, s => Assert.Equal(23, s.GetProperty("dayOfMonth").GetInt32()));
        Assert.All(styles, s => Assert.Equal(history == "stale" ? unchecked((int)0xFFFF0000) : unchecked((int)0xFFBBF7D0), s.GetProperty("backgroundColorArgb").GetInt32()));
        if (history == "stale")
        {
            Assert.Equal("Unversioned recovery state", old.GetProperty("name").GetString());
            Assert.Equal("Unversioned note", old.GetProperty("note").GetString());
            Assert.Equal("#FF0000", old.GetProperty("acceptedSwapHighlightColor").GetString());
        }
        Assert.Equal(ShiftSwapStatus.Accepted, (await db.ShiftSwapRequests.SingleAsync()).Status);
        Assert.Single(await db.ShiftSwapHistories.ToListAsync());
        Assert.Equal(full ? 1 : 2, await db.ScheduleSlots.CountAsync(s => s.ScheduleId == f.Schedule));

        var checkout = await Manager(db).Checkout(f.Container, f.Schedule, before.Id, default);
        Assert.IsType<OkObjectResult>(checkout.Result);
        db.ChangeTracker.Clear();
        var restored = Assert.Single(await db.ScheduleSlots.Where(s => s.ScheduleId == f.Schedule).ToListAsync());
        Assert.Equal((f.Owner, "09:00", "22:00"), (restored.EmployeeId!.Value, restored.FromTime, restored.ToTime));
        Assert.Single(await db.ScheduleEmployees.Where(e => e.ScheduleId == f.Schedule).ToListAsync());
        Assert.Empty(await db.ScheduleCellStyles.ToListAsync());
        var restoredSchedule = await db.Schedules.SingleAsync(s => s.Id == f.Schedule);
        Assert.Equal(old.GetProperty("name").GetString(), restoredSchedule.Name);
        Assert.Equal(old.GetProperty("note").GetString(), restoredSchedule.Note);
        Assert.Equal(old.GetProperty("acceptedSwapHighlightColor").GetString(), restoredSchedule.AcceptedSwapHighlightColor);
        var archived = await db.ShiftSwapRequests.SingleAsync();
        Assert.Equal(ShiftSwapStatus.Accepted, archived.Status);
        Assert.Null(archived.ScheduleSlotId);
        Assert.NotNull(archived.ArchivedViewsJson);
        Assert.Single(await db.ShiftSwapHistories.ToListAsync());
        Assert.Equal(before.Id, (await db.ScheduleVersionStates.SingleAsync()).CurrentVersionId);
    }

    [Fact]
    public async Task Accept_AfterCheckout_BranchesFromCheckedOutParent()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db);
        var writer = new ScheduleVersionCommitService(db);
        var initial = (await writer.CommitAsync(f.Schedule, null, "Baseline", default))!;
        var schedule = await db.Schedules.SingleAsync(s => s.Id == f.Schedule);
        schedule.Name = "Second commit";
        await db.SaveChangesAsync();
        var previousTip = (await writer.CommitAsync(f.Schedule, null, "Manager", default))!;
        var swap = await db.ShiftSwapRequests.SingleAsync();
        swap.Status = ShiftSwapStatus.Cancelled;
        await db.SaveChangesAsync();
        await Manager(db).Checkout(f.Container, f.Schedule, initial.Id, default);
        db.ChangeTracker.Clear();
        var slot = await db.ScheduleSlots.SingleAsync(s => s.ScheduleId == f.Schedule);
        var newOffer = Offer(f.Schedule, slot.Id, f.Owner, f.Target);
        db.ShiftSwapRequests.Add(newOffer);
        await db.SaveChangesAsync();
        await Employee(db, f.Target).Accept(newOffer.Id, default);
        db.ChangeTracker.Clear();
        var tip = await db.ScheduleVersions.SingleAsync(v => v.VersionNumber == 3);
        Assert.Equal("branch-2", tip.BranchName);
        Assert.Equal(initial.Id, tip.ParentVersionId);
        Assert.NotEqual(previousTip.Id, tip.ParentVersionId);
        Assert.Equal(tip.Id, (await db.ScheduleVersionStates.SingleAsync()).CurrentVersionId);
        Assert.Equal(ShiftSwapStatus.Accepted, (await db.ShiftSwapRequests.SingleAsync(s => s.Id == newOffer.Id)).Status);
        Assert.Single(await db.ShiftSwapHistories.ToListAsync());
        Assert.Equal(2, await db.ScheduleSlots.CountAsync(s => s.ScheduleId == f.Schedule));
    }

    [Fact]
    public async Task Accept_ManagerOffer_VersionsAssignmentAndRemovedNoteCell()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db, manual: true);
        await Employee(db, f.Target).Accept(f.Swap, default);
        db.ChangeTracker.Clear();
        var versions = await db.ScheduleVersions.OrderBy(v => v.VersionNumber).ToListAsync();
        Assert.Equal(2, versions.Count);
        using var before = JsonDocument.Parse(versions[0].SnapshotJson);
        using var after = JsonDocument.Parse(versions[1].SnapshotJson);
        Assert.Contains("Reserve", before.RootElement.GetProperty("note").GetString());
        Assert.DoesNotContain("Reserve", after.RootElement.GetProperty("note").GetString() ?? "");
        var slot = Assert.Single(after.RootElement.GetProperty("slots").EnumerateArray());
        Assert.Equal(f.Target, slot.GetProperty("employeeId").GetInt32());
        Assert.Equal("ASSIGNED", slot.GetProperty("status").GetString());
        Assert.Equal(f.Target, (await db.ScheduleSlots.SingleAsync(s => s.ScheduleId == f.Schedule)).EmployeeId);
        Assert.Equal(ShiftSwapStatus.Accepted, (await db.ShiftSwapRequests.SingleAsync()).Status);
        Assert.Single(await db.ShiftSwapHistories.ToListAsync());
        Assert.Equal(versions[1].Id, (await db.ScheduleVersionStates.SingleAsync()).CurrentVersionId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("closed")]
    [InlineData("unauthorized")]
    [InlineData("locked")]
    [InlineData("duplicate")]
    public async Task Accept_RejectedOrRepeated_DoesNotAppendVersions(string reason)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db);
        await new ScheduleVersionCommitService(db).EnsureInitialVersionAsync(f.Schedule, default);
        var locks = new ManagerEditLockService();
        if (reason == "closed")
        {
            (await db.ShiftSwapRequests.SingleAsync()).Status = ShiftSwapStatus.Cancelled;
            await db.SaveChangesAsync();
        }
        if (reason == "locked") ((IScheduleEditLockService)locks).SetLocks("manager", "Manager", [new ScheduleEditLockTarget(f.Container, f.Schedule)]);
        if (reason == "duplicate") await Employee(db, f.Target).Accept(f.Swap, default);
        var beforeVersions = await db.ScheduleVersions.CountAsync();
        var beforePointer = (await db.ScheduleVersionStates.SingleAsync()).CurrentVersionId;
        var beforeSlots = await db.ScheduleSlots.AsNoTracking().Where(s => s.ScheduleId == f.Schedule).OrderBy(s => s.SlotNo).Select(s => new { s.EmployeeId, s.FromTime, s.ToTime }).ToListAsync();
        var error = await Record.ExceptionAsync(() => Employee(db, reason == "unauthorized" ? f.Owner : f.Target, locks: locks).Accept(reason == "missing" ? -1 : f.Swap, default));
        Assert.True(error is ValidationException or KeyNotFoundException, error?.ToString());
        db.ChangeTracker.Clear();
        Assert.Equal(beforeVersions, await db.ScheduleVersions.CountAsync());
        Assert.Equal(beforePointer, (await db.ScheduleVersionStates.SingleAsync()).CurrentVersionId);
        Assert.Equal(beforeSlots, await db.ScheduleSlots.AsNoTracking().Where(s => s.ScheduleId == f.Schedule).OrderBy(s => s.SlotNo).Select(s => new { s.EmployeeId, s.FromTime, s.ToTime }).ToListAsync());
        Assert.Equal(reason == "duplicate" ? 1 : 0, await db.ShiftSwapHistories.CountAsync());
        Assert.Equal(reason == "duplicate" ? ShiftSwapStatus.Accepted : reason == "closed" ? ShiftSwapStatus.Cancelled : ShiftSwapStatus.Open, (await db.ShiftSwapRequests.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Accept_VersionInsertFails_RollsBackEntireTransaction(bool failAfterRecovery)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db);
        await new ScheduleVersionCommitService(db).EnsureInitialVersionAsync(f.Schedule, default);
        var baseline = await db.ScheduleVersions.SingleAsync();
        if (failAfterRecovery)
        {
            (await db.Schedules.SingleAsync(s => s.Id == f.Schedule)).Name = "Unversioned drift";
            await db.SaveChangesAsync();
        }
        // Let the recovery insert succeed, then abort the accepted snapshot insert.
        await db.Database.ExecuteSqlRawAsync(failAfterRecovery
            ? "CREATE TRIGGER fail_version BEFORE INSERT ON schedule_version WHEN NEW.version_number = 3 BEGIN SELECT RAISE(ABORT, 'version failure'); END;"
            : "CREATE TRIGGER fail_version BEFORE INSERT ON schedule_version WHEN NEW.version_number = 2 BEGIN SELECT RAISE(ABORT, 'version failure'); END;");
        try
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => Employee(db, f.Target).Accept(f.Swap, default));
            await using var persisted = database.CreateContext();
            Assert.Single(await persisted.ScheduleVersions.ToListAsync());
            Assert.Equal(baseline.Id, (await persisted.ScheduleVersionStates.SingleAsync()).CurrentVersionId);
            var swap = await persisted.ShiftSwapRequests.SingleAsync();
            Assert.Equal(ShiftSwapStatus.Open, swap.Status);
            Assert.Null(swap.AcceptedByEmployeeId);
            Assert.Null(swap.AcceptedAtUtc);
            Assert.Empty(await persisted.ShiftSwapHistories.ToListAsync());
            var slot = await persisted.ScheduleSlots.SingleAsync(s => s.ScheduleId == f.Schedule);
            Assert.Equal((f.Owner, "09:00", "22:00"), (slot.EmployeeId!.Value, slot.FromTime, slot.ToTime));
            Assert.Empty(await persisted.ScheduleCellStyles.ToListAsync());
            Assert.Single(await persisted.ScheduleEmployees.Where(e => e.ScheduleId == f.Schedule).ToListAsync());
        }
        finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_version;"); }
    }

    [Fact]
    public async Task Accept_PostCommitActionsFail_SwapAndVersionsRemainCommitted()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var f = await SeedAsync(db);
        var result = await Employee(db, f.Target, failPostCommit: true).Accept(f.Swap, default);
        Assert.IsType<OkObjectResult>(result.Result);
        await using var persisted = database.CreateContext();
        Assert.Equal(ShiftSwapStatus.Accepted, (await persisted.ShiftSwapRequests.SingleAsync()).Status);
        Assert.Single(await persisted.ShiftSwapHistories.ToListAsync());
        Assert.Equal(2, await persisted.ScheduleVersions.CountAsync());
        var tip = await persisted.ScheduleVersions.SingleAsync(v => v.VersionNumber == 2);
        Assert.Equal(tip.Id, (await persisted.ScheduleVersionStates.SingleAsync()).CurrentVersionId);
        Assert.Contains(await persisted.ScheduleSlots.ToListAsync(), s => s.EmployeeId == f.Target);
    }

    private static EmployeeShiftSwapsController Employee(AppDbContext db, int employee, bool failPostCommit = false, IScheduleEditLockService? locks = null)
    {
        var controller = new EmployeeShiftSwapsController(db, locks ?? new ManagerEditLockService(),
            failPostCommit ? System.Reflection.DispatchProxy.Create<IWorkflowLogService, ShiftCorrectionControllerTests.FailingPostCommitProxy>() : new NoopWorkflowLogService(),
            failPostCommit ? System.Reflection.DispatchProxy.Create<IRealtimeNotifier, ShiftCorrectionControllerTests.FailingPostCommitProxy>() : new NoopRealtimeNotifier(),
            new ScheduleVersionCommitService(db));
        SetUser(controller, AuthRoles.Employee, employee);
        return controller;
    }

    private static GraphVersionsController Manager(AppDbContext db)
    {
        var controller = new GraphVersionsController(db, new NoopWorkflowLogService(), new NoopRealtimeNotifier(), new ScheduleVersionCommitService(db));
        SetUser(controller, AuthRoles.Manager, 0);
        return controller;
    }

    private static void SetUser(ControllerBase controller, string role, int employee)
        => controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test"), new Claim(ClaimTypes.Role, role), new Claim("employee_id", employee.ToString())], JwtAuthenticationDefaults.SchemeName)) } };

    private static ShiftSwapRequestModel Offer(int schedule, int slot, int? owner, int target, bool full = false, bool manual = false)
        => new() { ScheduleId = schedule, ScheduleSlotId = slot, FromEmployeeId = owner, TargetEmployeeId = target,
            Visibility = ShiftSwapVisibility.Private, Status = ShiftSwapStatus.Open, OfferedFromTime = "09:00", OfferedToTime = full || manual ? "22:00" : "15:00",
            IsManagerCreated = manual, ManualColumnId = manual ? 1 : null, CreatedAtUtc = DateTimeOffset.UtcNow };

    private static async Task<Fixture> SeedAsync(AppDbContext db, bool full = false, bool manual = false)
    {
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var owner = TestDataFactory.CreateDalEmployee("Owner", "Worker", "owner@example.invalid");
        var target = TestDataFactory.CreateDalEmployee("Target", "Worker", "target@example.invalid");
        db.AddRange(container, shop, owner, target);
        await db.SaveChangesAsync();
        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Swap versions", 2026, 10);
        schedule.PublicationStatus = SchedulePublicationStatus.Public;
        schedule.AllowSwap = true;
        if (manual) schedule.Note = "[[GF3_GRAPH_META:{\"manualColumns\":[{\"id\":1,\"label\":\"Coverage\",\"cells\":{\"23\":\"Reserve\"}}]}]]";
        var eligibility = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Eligibility", 2026, 10);
        eligibility.PublicationStatus = SchedulePublicationStatus.Public;
        db.AddRange(schedule, eligibility);
        await db.SaveChangesAsync();
        db.ScheduleEmployees.AddRange(new ScheduleEmployeeModel { ScheduleId = schedule.Id, EmployeeId = owner.Id },
            new ScheduleEmployeeModel { ScheduleId = eligibility.Id, EmployeeId = target.Id });
        var slot = TestDataFactory.CreateDalSlot(schedule.Id, 23, 1, manual ? null : owner.Id, "09:00", "22:00");
        db.ScheduleSlots.Add(slot);
        await db.SaveChangesAsync();
        var swap = Offer(schedule.Id, slot.Id, manual ? null : owner.Id, target.Id, full, manual);
        db.ShiftSwapRequests.Add(swap);
        await db.SaveChangesAsync();
        return new(container.Id, schedule.Id, swap.Id, owner.Id, target.Id);
    }

    private sealed record Fixture(int Container, int Schedule, int Swap, int Owner, int Target);
}
