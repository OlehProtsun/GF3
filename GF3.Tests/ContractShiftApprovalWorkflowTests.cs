using System.Data.Common;
using System.Security.Claims;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Realtime;
using WebApi.Services;
using WebApi.ShiftSwaps;

namespace GF3.Tests;

public sealed class ContractShiftApprovalWorkflowTests
{
    [Theory]
    [InlineData("claim", false, false, false)]
    [InlineData("claim", false, true, false)]
    [InlineData("claim", true, false, false)]
    [InlineData("claim", false, false, true)]
    [InlineData("below_minimum_give", false, false, false)]
    [InlineData("below_minimum_give", false, true, false)]
    public async Task PresentationMapsParticipantsWithoutUsingCurrentSlotOwner(string kind, bool managerCreated, bool privateOffer, bool missingOffer)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db);
        var slot = (await db.ScheduleSlots.FindAsync(f.SourceSlot))!;
        var schedule = (await db.Schedules.FindAsync(f.SourceSchedule))!;
        var owner = (await db.Employees.FindAsync(f.Owner))!;
        owner.IsDeleted = true;
        var offer = new ShiftSwapRequestModel { ScheduleId = f.SourceSchedule, ScheduleSlotId = f.SourceSlot,
            FromEmployeeId = managerCreated ? null : f.Owner, IsManagerCreated = managerCreated,
            TargetEmployeeId = privateOffer ? f.Target : null, CreatedAtUtc = DateTimeOffset.UtcNow };
        if (!missingOffer) { db.Add(offer); await db.SaveChangesAsync(); }
        var request = new ContractShiftApprovalRequestModel { Kind = kind, Status = "rejected", EmployeeId = kind == "claim" ? f.Target : f.Owner,
            ScheduleId = f.SourceSchedule, ScheduleSlotId = f.SourceSlot, OriginalDayOfMonth = slot.DayOfMonth,
            OriginalFromTime = slot.FromTime, OriginalToTime = slot.ToTime, FromTime = slot.FromTime, ToTime = slot.ToTime,
            ShiftSwapRequestId = missingOffer ? null : offer.Id, TargetEmployeeId = privateOffer ? f.Target : null,
            ShopId = schedule.ShopId, Year = schedule.Year, Month = schedule.Month, CreatedAtUtc = DateTimeOffset.UtcNow,
            ReviewedAtUtc = DateTimeOffset.UtcNow, ReviewedByManagerId = 99,
            RequestKind = "exchange", ReturnOriginalDayOfMonth = 7, ReturnYear = 2027, ReturnMonth = 1,
            ReturnFromTime = "10:00", ReturnToTime = "16:00" };
        db.Add(request); slot.EmployeeId = f.Third; await db.SaveChangesAsync();
        var dto = Assert.Single(await ContractTestFactory.Approvals(db).GetGraphRequestsAsync(f.Container, f.SourceSchedule));
        Assert.Equal(kind == "claim" ? managerCreated || missingOffer ? (int?)null : f.Owner : f.Owner, dto.FromEmployeeId);
        Assert.Equal(kind == "claim" ? f.Target : privateOffer ? f.Target : (int?)null, dto.ToEmployeeId);
        if (dto.FromEmployeeId.HasValue) Assert.Equal("Worker1 Test", dto.FromEmployeeName); else Assert.Null(dto.FromEmployeeName);
        if (dto.ToEmployeeId.HasValue) Assert.Equal("Worker2 Test", dto.ToEmployeeName); else Assert.Null(dto.ToEmployeeName);
        Assert.Equal(kind == "claim" ? missingOffer ? (bool?)null : managerCreated : false, dto.IsManagerCreated);
        Assert.Equal(7, dto.ReturnDayOfMonth); Assert.Equal(2027, dto.ReturnYear); Assert.Equal(1, dto.ReturnMonth);
        Assert.Equal(EntityState.Unchanged, db.Entry(request).State);
    }

    [Fact]
    public async Task PresentationKeepsCompletedTransferNamesFromHistoryAfterEmployeeRename()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db);
        var service = ContractTestFactory.Approvals(db, engine);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
        var request = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        Assert.Equal(f.Owner, request.FromEmployeeId); Assert.Equal(f.Target, request.ToEmployeeId);
        await service.ApproveAsync(99, f.Container, f.SourceSchedule, request.Id, ContractTestFactory.Manager());
        (await db.Employees.FindAsync(f.Owner))!.FirstName = "Renamed owner";
        (await db.Employees.FindAsync(f.Target))!.FirstName = "Renamed recipient"; await db.SaveChangesAsync();
        var dto = Assert.Single(await service.GetGraphRequestsAsync(f.Container, f.SourceSchedule));
        Assert.Equal("Worker1 Test", dto.FromEmployeeName); Assert.Equal("Worker2 Test", dto.ToEmployeeName);
        Assert.Equal(f.Owner, dto.FromEmployeeId); Assert.Equal(f.Target, dto.ToEmployeeId);
        Assert.Equal(f.Target, (await db.ScheduleSlots.FindAsync(f.SourceSlot))!.EmployeeId);
        Assert.Single(await db.ShiftSwapHistories.ToListAsync());
    }

    [Fact]
    public async Task EmploymentOwnerToZlecenieRecipientCannotTransferWithoutManagerApproval()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db, ownerContract: true);
        (await db.Employees.FindAsync(f.Target))!.ContractType = "zlecenie";
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequireBelowMinimumGiveApproval, false));
        await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db); var approvals = ContractTestFactory.Approvals(db, engine);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
        var before = await ShiftSwapScenarioFactory.SnapshotAsync(db);
        var error = await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Target,
            ContractTestFactory.Employee(f.Target), offer.Id, default));
        Assert.Equal("CONTRACT_CLAIM_APPROVAL_REQUIRED", error.Code);
        Assert.Equal(before, await ShiftSwapScenarioFactory.SnapshotAsync(db));
        var visible = Assert.Single(await engine.GetVisible(f.Target, ContractTestFactory.Employee(f.Target), default));
        Assert.True(visible.RequiresManagerApproval); Assert.True(visible.CanRequestManagerApproval);
        var pending = await approvals.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
        Assert.Equal(f.Owner, (await db.ScheduleSlots.AsNoTracking().SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
        await approvals.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager());
        Assert.Equal(f.Target, (await db.ScheduleSlots.AsNoTracking().SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
        await ShiftSwapScenarioFactory.AssertVersionAndHistoryConsistency(db, offer.Id, f.SourceSchedule);
    }

    public static IEnumerable<object[]> PickupContractPaths()
    {
        foreach (var ownerContract in new[] { false, true })
        foreach (var recipientContract in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var path in new[] { "public", "private", "partial", "exchange", "manual" })
            yield return [ownerContract, recipientContract, enabled, path];
    }

    [Theory, MemberData(nameof(PickupContractPaths))]
    public async Task PickupApprovalContractMatrixCoversEveryMutationPath(bool ownerContract, bool recipientContract, bool enabled, string path)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db, ownerContract);
        (await db.Employees.FindAsync(f.Target))!.ContractType = recipientContract ? "o_prace" : "zlecenie";
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequirePickupApproval, enabled)
            .SetProperty(v => v.RequireBelowMinimumGiveApproval, false).SetProperty(v => v.RestrictSwapsToEightHours, false));
        await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db); var approvals = ContractTestFactory.Approvals(db, engine);
        ShiftSwapDto offer;
        if (path == "manual")
        {
            (await db.Schedules.FindAsync(f.SourceSchedule))!.Note = "[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"3\":\"09:00-17:00\"}]],\"g\":[7],\"o\":[-7]}]]";
            await db.SaveChangesAsync();
            var result = await ShiftSwapScenarioFactory.ManagerController(db).CreateManualOffer(f.Container, f.SourceSchedule,
                new() { ManualColumnId = 7, DayOfMonth = 3, FromTime = "09:00", ToTime = "17:00" }, default);
            offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        }
        else
        {
            var command = ShiftSwapScenarioFactory.Request(f, new(Kind: path == "exchange" ? SwapKind.Exchange : SwapKind.Give,
                Scope: path is "private" or "exchange" ? SwapScope.Private : SwapScope.Public));
            command.AllowPartialAcceptance = path == "partial";
            offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), command, default);
        }
        var required = enabled && (recipientContract || ownerContract && path != "manual");
        var visible = Assert.Single(await engine.GetVisible(f.Target, ContractTestFactory.Employee(f.Target), default));
        Assert.Equal(required, visible.RequiresManagerApproval); Assert.Equal(required, visible.CanRequestManagerApproval);
        Assert.Equal(offer.Visibility == "public" ? enabled : required, visible.PickupApprovalPolicyApplies);
        var selected = path == "partial" ? new AcceptEmployeeShiftSwapRequest { FromTime = "09:00", ToTime = "13:00" } : null;
        if (required)
        {
            var before = await ShiftSwapScenarioFactory.SnapshotAsync(db);
            var error = await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Target,
                ContractTestFactory.Employee(f.Target), offer.Id, default, selected));
            Assert.Equal("CONTRACT_CLAIM_APPROVAL_REQUIRED", error.Code);
            Assert.Equal(before, await ShiftSwapScenarioFactory.SnapshotAsync(db));
            var pending = await approvals.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id,
                FromTime = selected?.FromTime, ToTime = selected?.ToTime }, ContractTestFactory.Employee(f.Target));
            Assert.Equal("pending", pending.Status);
            Assert.Equal(ShiftSwapStatus.Open, (await db.ShiftSwapRequests.AsNoTracking().SingleAsync()).Status);
            Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
            await approvals.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager());
            Assert.Equal("approved", (await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync()).Status);
        }
        else
        {
            await engine.Accept(f.Target, ContractTestFactory.Employee(f.Target), offer.Id, default, selected);
            Assert.Empty(await db.ContractShiftApprovalRequests.ToListAsync());
        }
        Assert.Single(await db.ShiftSwapHistories.ToListAsync());
        await ShiftSwapScenarioFactory.AssertVersionAndHistoryConsistency(db, offer.Id,
            path == "exchange" ? [f.SourceSchedule, f.ReturnSchedule] : [f.SourceSchedule]);
        if (path == "partial")
        {
            var residual = await db.ShiftSwapRequests.AsNoTracking().SingleAsync(x => x.Status == ShiftSwapStatus.Open);
            Assert.Equal("13:00", residual.OfferedFromTime); Assert.Equal("17:00", residual.OfferedToTime);
            Assert.Equal(f.Owner, residual.FromEmployeeId);
        }
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task MutationReadsCurrentPersistedPickupSettingAfterAStaleList(bool initiallyEnabled)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db, ownerContract: true);
        (await db.Employees.FindAsync(f.Target))!.ContractType = "zlecenie";
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequirePickupApproval, initiallyEnabled)
            .SetProperty(v => v.RequireBelowMinimumGiveApproval, false));
        await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
        Assert.Equal(initiallyEnabled, Assert.Single(await engine.GetVisible(f.Target, ContractTestFactory.Employee(f.Target), default)).RequiresManagerApproval);
        var trackedSettings = await db.EmploymentContractSettings.SingleAsync();
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequirePickupApproval, !initiallyEnabled));
        Assert.Equal(initiallyEnabled, trackedSettings.RequirePickupApproval);
        if (!initiallyEnabled)
        {
            var error = await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Target, ContractTestFactory.Employee(f.Target), offer.Id, default));
            Assert.Equal("CONTRACT_CLAIM_APPROVAL_REQUIRED", error.Code);
            Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
        }
        else
        {
            await engine.Accept(f.Target, ContractTestFactory.Employee(f.Target), offer.Id, default);
            Assert.Single(await db.ShiftSwapHistories.ToListAsync());
        }
    }

    [Theory]
    [InlineData("owner-contract")][InlineData("setting")]
    public async Task OwnerBasedApprovalExpiresAndReleasesHoldWhenItsRuleNoLongerApplies(string change)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db, ownerContract: true);
        (await db.Employees.FindAsync(f.Target))!.ContractType = "zlecenie";
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequireBelowMinimumGiveApproval, false)); await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db); var approvals = ContractTestFactory.Approvals(db, engine);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
        var pending = await approvals.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        if (change == "owner-contract") await db.Employees.Where(x => x.Id == f.Owner).ExecuteUpdateAsync(x => x.SetProperty(v => v.ContractType, "zlecenie"));
        else await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequirePickupApproval, false));
        var error = await Assert.ThrowsAsync<ContractShiftConflictException>(() => approvals.ApproveAsync(99, f.Container,
            f.SourceSchedule, pending.Id, ContractTestFactory.Manager()));
        Assert.Equal("CONTRACT_APPROVAL_EXPIRED", error.Code);
        Assert.Equal("expired", (await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync()).Status);
        var visible = Assert.Single(await engine.GetVisible(f.Target, ContractTestFactory.Employee(f.Target), default));
        Assert.False(visible.IsAwaitingManagerApproval); Assert.False(visible.RequiresManagerApproval); Assert.True(visible.CanAccept);
        Assert.Equal(f.Owner, (await db.ScheduleSlots.AsNoTracking().SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
        Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
    }

    [Fact]
    public async Task RequestCancellationRacingApprovalHasExactlyOneOutcome()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); SwapFixture f; int approvalId;
        await using (var db = database.CreateContext())
        {
            f = await ContractTestFactory.SeedAsync(db, ownerContract: true);
            (await db.Employees.FindAsync(f.Target))!.ContractType = "zlecenie";
            await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequireBelowMinimumGiveApproval, false)); await db.SaveChangesAsync();
            var engine = ContractTestFactory.Engine(db);
            var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
            approvalId = (await ContractTestFactory.Approvals(db, engine).CreateClaimRequestAsync(f.Target,
                new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target))).Id;
        }
        using var gate = new Barrier(2);
        async Task<bool> Execute(bool approve)
        {
            await using var db = database.CreateContext(); var service = ContractTestFactory.Approvals(db);
            Assert.True(gate.SignalAndWait(TimeSpan.FromSeconds(10)));
            try
            {
                if (approve) await service.ApproveAsync(99, f.Container, f.SourceSchedule, approvalId, ContractTestFactory.Manager());
                else await service.CancelAsync(f.Target, approvalId, ContractTestFactory.Employee(f.Target));
                return true;
            }
            catch (ContractShiftConflictException error) { Assert.Equal("CONTRACT_ALREADY_REVIEWED", error.Code); return false; }
        }
        Assert.Single(await Task.WhenAll(Task.Run(() => Execute(true)), Task.Run(() => Execute(false))), success => success);
        await using var check = database.CreateContext(); var approval = await check.ContractShiftApprovalRequests.SingleAsync();
        Assert.Contains(approval.Status, new[] { "approved", "cancelled" });
        var approved = approval.Status == "approved";
        Assert.Equal(approved ? 1 : 0, await check.ShiftSwapHistories.CountAsync());
        Assert.Equal(approved ? 2 : 0, await check.ScheduleVersions.CountAsync());
        Assert.Equal(approved ? f.Target : f.Owner, (await check.ScheduleSlots.SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
    }

    [Fact]
    public async Task PendingClaimDoesNotMutateAndApprovalTransfersExactlyOnce()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
        var service = ContractTestFactory.Approvals(db, engine); var before = await ShiftSwapScenarioFactory.SnapshotAsync(db);
        await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Target, ContractTestFactory.Employee(f.Target), offer.Id, default));
        var pending = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        Assert.Equal("pending", pending.Status); var beforeJson = System.Text.Json.Nodes.JsonNode.Parse(before)!.AsObject();
        var afterJson = System.Text.Json.Nodes.JsonNode.Parse(await ShiftSwapScenarioFactory.SnapshotAsync(db))!.AsObject();
        beforeJson.Remove("contract_shift_approval_request"); afterJson.Remove("contract_shift_approval_request");
        Assert.Equal(beforeJson.ToJsonString(), afterJson.ToJsonString());
        await Assert.ThrowsAsync<ContractShiftConflictException>(() => service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target)));
        db.ChangeTracker.Clear();
        var approved = await service.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager());
        Assert.Equal("approved", approved.Status); Assert.Equal(offer.Id, approved.ResultingShiftSwapRequestId);
        await Assert.ThrowsAsync<ContractShiftConflictException>(() => service.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager()));
        await ShiftSwapScenarioFactory.AssertVersionAndHistoryConsistency(db, offer.Id, f.SourceSchedule);
        Assert.Equal(f.Target, (await db.ScheduleSlots.AsNoTracking().SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
    }

    [Fact]
    public async Task GiveExceptionPublishesWithoutRemovingHoursAndRecipientStillNeedsApproval()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db, ownerContract: true); var engine = ContractTestFactory.Engine(db);
        var service = ContractTestFactory.Approvals(db, engine); var command = ShiftSwapScenarioFactory.Request(f, new());
        await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), command, default));
        db.ChangeTracker.Clear();
        var pending = await service.CreateGiveExceptionRequestAsync(f.Owner, command, ContractTestFactory.Employee(f.Owner));
        Assert.Empty(await db.ShiftSwapRequests.ToListAsync()); Assert.Empty(await db.ScheduleVersions.ToListAsync());
        var approved = await service.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager());
        Assert.Equal(f.Owner, (await db.ScheduleSlots.AsNoTracking().SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
        Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
        var offer = await db.ShiftSwapRequests.AsNoTracking().SingleAsync(); Assert.Equal(pending.Id, offer.ApprovedEmploymentExceptionApprovalId);
        await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Target, ContractTestFactory.Employee(f.Target), offer.Id, default));
        var claim = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        await service.ApproveAsync(99, f.Container, f.SourceSchedule, claim.Id, ContractTestFactory.Manager());
        Assert.Single(await db.ShiftSwapHistories.ToListAsync());
    }

    [Theory]
    [InlineData("reject")][InlineData("cancel")][InlineData("settings")][InlineData("contract")][InlineData("private")][InlineData("day")][InlineData("time")][InlineData("deleted")]
    public async Task RejectionCancellationAndStalenessNeverTransfer(string change)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db); var service = ContractTestFactory.Approvals(db, engine);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
        var pending = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        if (change == "reject") Assert.Equal("rejected", (await service.RejectAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager())).Status);
        else if (change == "cancel") Assert.Equal("cancelled", (await service.CancelAsync(f.Target, pending.Id, ContractTestFactory.Employee(f.Target))).Status);
        else
        {
            if (change == "settings") await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequirePickupApproval, false));
            if (change == "contract") await db.Employees.Where(x => x.Id == f.Target).ExecuteUpdateAsync(x => x.SetProperty(v => v.ContractType, "zlecenie"));
            if (change == "private") await db.Schedules.Where(x => x.Id == f.SourceSchedule).ExecuteUpdateAsync(x => x.SetProperty(v => v.PublicationStatus, SchedulePublicationStatus.Private));
            if (change == "day") await db.ScheduleSlots.Where(x => x.Id == f.SourceSlot).ExecuteUpdateAsync(x => x.SetProperty(v => v.DayOfMonth, 4));
            if (change == "time") await db.ScheduleSlots.Where(x => x.Id == f.SourceSlot).ExecuteUpdateAsync(x => x.SetProperty(v => v.FromTime, "08:00"));
            if (change == "deleted") await db.ScheduleSlots.Where(x => x.Id == f.SourceSlot).ExecuteDeleteAsync();
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<ContractShiftConflictException>(() => service.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager()));
            Assert.Equal("expired", (await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync()).Status);
        }
        Assert.Empty(await db.ShiftSwapHistories.ToListAsync()); Assert.Empty(await db.ScheduleVersions.ToListAsync());
    }

    [Fact]
    public async Task ScopeLocksAndRequesterAccessAreEnforced()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db); var locks = new ManagerEditLockService();
        var engine = ContractTestFactory.Engine(db, locks); var service = ContractTestFactory.Approvals(db, engine);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
        var pending = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        Assert.Empty(await service.GetMyRequestsAsync(f.Third));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CancelAsync(f.Third, pending.Id, ContractTestFactory.Employee(f.Third)));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ApproveAsync(99, f.Container, f.ReturnSchedule, pending.Id, ContractTestFactory.Manager()));
        locks.SetLocks("other", 100, "Other", [ManagerEditLockTargets.Schedule(f.Container, f.SourceSchedule)]);
        await Assert.ThrowsAsync<ContractShiftConflictException>(() => service.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager()));
        Assert.Equal("pending", (await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(await db.ScheduleVersions.ToListAsync());
        locks.SetLocks("other", 100, "Other", []);
        await service.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager());
        Assert.Equal("approved", (await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync()).Status);
        Assert.Single(await db.ShiftSwapHistories.ToListAsync());
    }

    [Theory]
    [InlineData(false, false)][InlineData(true, false)][InlineData(true, true)]
    public async Task CompetingClaimCreationHasOnePendingWinnerAcrossSeparateConnections(bool ownerBased, bool sameRecipient)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        SwapFixture f; int offerId;
        await using (var db = database.CreateContext())
        {
            f = await ContractTestFactory.SeedAsync(db, ownerBased);
            (await db.Employees.FindAsync(f.Target))!.ContractType = ownerBased ? "zlecenie" : "o_prace";
            (await db.Employees.FindAsync(f.Third))!.ContractType = ownerBased ? "zlecenie" : "o_prace";
            await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequireBelowMinimumGiveApproval, false));
            await db.SaveChangesAsync();
            offerId = (await ContractTestFactory.Engine(db).Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default)).Id;
        }
        using var gate = new Barrier(2);
        async Task<ContractShiftApprovalDto?> Claim(int employeeId)
        {
            await using var db = database.CreateContext();
            Assert.True(gate.SignalAndWait(TimeSpan.FromSeconds(10)));
            try { return await ContractTestFactory.Approvals(db).CreateClaimRequestAsync(employeeId, new() { ShiftSwapRequestId = offerId }, ContractTestFactory.Employee(employeeId)); }
            catch (ContractShiftConflictException exception) { Assert.Equal(sameRecipient ? "CONTRACT_DUPLICATE_PENDING" : "CONTRACT_OFFER_PENDING_APPROVAL", exception.Code); return null; }
        }
        var results = await Task.WhenAll(Task.Run(() => Claim(f.Target)), Task.Run(() => Claim(sameRecipient ? f.Target : f.Third)));
        var pending = Assert.Single(results, x => x is not null)!;
        await using var check = database.CreateContext();
        Assert.Equal("pending", (await check.ContractShiftApprovalRequests.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(await check.ScheduleVersions.ToListAsync()); Assert.Empty(await check.ShiftSwapHistories.ToListAsync());
        await ContractTestFactory.Approvals(check).ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager());
        Assert.Single(await check.ShiftSwapHistories.ToListAsync()); Assert.Equal(2, await check.ScheduleVersions.CountAsync());
        Assert.Equal(pending.EmployeeId, (await check.ScheduleSlots.AsNoTracking().SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
        await ShiftSwapScenarioFactory.AssertVersionAndHistoryConsistency(check, offerId, f.SourceSchedule);
    }

    [Theory]
    [InlineData("reject")][InlineData("cancel")][InlineData("expire")]
    public async Task ReleasedReservationAllowsAnotherEmployeeToRequest(string action)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db);
        (await db.Employees.FindAsync(f.Third))!.ContractType = "o_prace"; await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db); var service = ContractTestFactory.Approvals(db, engine);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
        var pending = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        if (action == "reject") await service.RejectAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager());
        else if (action == "cancel") await service.CancelAsync(f.Target, pending.Id, ContractTestFactory.Employee(f.Target));
        else
        {
            await db.Employees.Where(x => x.Id == f.Target).ExecuteUpdateAsync(x => x.SetProperty(e => e.ContractType, "zlecenie"));
            db.ChangeTracker.Clear();
            var failure = await Assert.ThrowsAsync<ContractShiftConflictException>(() => service.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager()));
            Assert.Equal("CONTRACT_APPROVAL_EXPIRED", failure.Code);
        }
        var visible = Assert.Single(await engine.GetVisible(f.Third, ContractTestFactory.Employee(f.Third), default));
        Assert.False(visible.IsAwaitingManagerApproval); Assert.True(visible.CanRequestManagerApproval);
        var next = await service.CreateClaimRequestAsync(f.Third, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Third));
        Assert.Equal("pending", next.Status);
        Assert.Equal(2, await db.ContractShiftApprovalRequests.CountAsync());
        Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
    }

    [Fact]
    public async Task PendingHoldBlocksEveryViewerAndDirectZlecenieAcceptance()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db);
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
        var ordinary = Assert.Single(await engine.GetVisible(f.Third, ContractTestFactory.Employee(f.Third), default));
        Assert.True(ordinary.PickupApprovalPolicyApplies); Assert.False(ordinary.RequiresManagerApproval); Assert.True(ordinary.CanAccept);
        await ContractTestFactory.Approvals(db, engine).CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        foreach (var viewer in new[] { f.Owner, f.Target, f.Third })
        {
            var dto = Assert.Single(await engine.GetVisible(viewer, ContractTestFactory.Employee(viewer), default));
            Assert.True(dto.IsAwaitingManagerApproval); Assert.True(dto.PickupApprovalPolicyApplies);
            Assert.Equal(viewer == f.Target, dto.ApprovalPendingForCurrentEmployee);
            Assert.False(dto.CanAccept); Assert.False(dto.CanAcceptFull); Assert.False(dto.CanRequestManagerApproval); Assert.Empty(dto.EligibleClaimWindows);
            var error = await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(viewer, ContractTestFactory.Employee(viewer), offer.Id, default));
            Assert.Equal("CONTRACT_OFFER_PENDING_APPROVAL", error.Code);
        }
        Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
    }

    [Fact]
    public async Task DifferentPartialWindowsStillReserveTheEntireOffer()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db);
        (await db.Employees.FindAsync(f.Third))!.ContractType = "o_prace";
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RestrictSwapsToEightHours, false)); await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db); var service = ContractTestFactory.Approvals(db, engine);
        var command = ShiftSwapScenarioFactory.Request(f, new()); command.AllowPartialAcceptance = true;
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), command, default);
        var first = new CreateContractClaimApprovalRequest { ShiftSwapRequestId = offer.Id, FromTime = "09:00", ToTime = "13:00" };
        await service.CreateClaimRequestAsync(f.Target, first, ContractTestFactory.Employee(f.Target));
        var duplicate = await Assert.ThrowsAsync<ContractShiftConflictException>(() => service.CreateClaimRequestAsync(f.Target, first, ContractTestFactory.Employee(f.Target)));
        Assert.Equal("CONTRACT_DUPLICATE_PENDING", duplicate.Code);
        foreach (var id in new[] { f.Target, f.Third })
        {
            var error = await Assert.ThrowsAsync<ContractShiftConflictException>(() => service.CreateClaimRequestAsync(id,
                new() { ShiftSwapRequestId = offer.Id, FromTime = "13:00", ToTime = "17:00" }, ContractTestFactory.Employee(id)));
            Assert.Equal("CONTRACT_OFFER_PENDING_APPROVAL", error.Code);
        }
        Assert.Single(await db.ContractShiftApprovalRequests.ToListAsync()); Assert.Empty(await db.ScheduleVersions.ToListAsync());
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task OwnerCancellationExpiresGiveAndExchangeHoldsAtomically(bool exchange)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db);
        var command = ShiftSwapScenarioFactory.Request(f, new(Kind: exchange ? SwapKind.Exchange : SwapKind.Give, Scope: exchange ? SwapScope.Private : SwapScope.Public));
        var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), command, default);
        var service = ContractTestFactory.Approvals(db, engine);
        var pending = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        var conflict = await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Target, ContractTestFactory.Employee(f.Target), offer.Id, default));
        Assert.Equal("CONTRACT_OFFER_PENDING_APPROVAL", conflict.Code);
        await engine.Cancel(f.Owner, ContractTestFactory.Employee(f.Owner), offer.Id, default);
        var row = await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync();
        Assert.Equal("expired", row.Status); Assert.Equal("The offer was cancelled by its owner.", row.FailureReason);
        Assert.Equal(ShiftSwapStatus.Cancelled, (await db.ShiftSwapRequests.AsNoTracking().SingleAsync()).Status);
        await Assert.ThrowsAsync<ContractShiftConflictException>(() => service.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager()));
        Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
    }

    [Fact]
    public async Task FailedOwnerCancellationCommitKeepsOfferAndPendingHoldUnchanged()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); SwapFixture f; int offerId;
        await using (var db = database.CreateContext())
        {
            await db.Database.OpenConnectionAsync();
            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE");
            f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db);
            offerId = (await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default)).Id;
            await ContractTestFactory.Approvals(db, engine).CreateClaimRequestAsync(f.Target,
                new() { ShiftSwapRequestId = offerId }, ContractTestFactory.Employee(f.Target));
        }
        await using var holder = database.CreateContext();
        await holder.Database.OpenConnectionAsync();
        await using var readTransaction = ((SqliteConnection)holder.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await holder.Database.UseTransactionAsync(readTransaction);
        Assert.True(await holder.ShiftSwapRequests.AnyAsync());
        await using (var actor = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(database.ConnectionString + ";Default Timeout=1").Options))
        {
            var error = await Assert.ThrowsAsync<BusinessLogicLayer.Common.ValidationException>(() =>
                ContractTestFactory.Engine(actor).Cancel(f.Owner, ContractTestFactory.Employee(f.Owner), offerId, default));
            Assert.Equal("This swap changed during another request. Refresh and try again.", error.Message);
        }
        await readTransaction.RollbackAsync();
        await using var check = database.CreateContext();
        Assert.Equal(ShiftSwapStatus.Open, (await check.ShiftSwapRequests.SingleAsync()).Status);
        Assert.Equal("pending", (await check.ContractShiftApprovalRequests.SingleAsync()).Status);
        Assert.Equal(f.Owner, (await check.ScheduleSlots.SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
        Assert.Empty(await check.ScheduleVersions.ToListAsync()); Assert.Empty(await check.ShiftSwapHistories.ToListAsync());
    }

    [Theory]
    [InlineData("approve")][InlineData("reject")]
    public async Task ConcurrentReviewAndDirectAcceptPreserveOneTransferAtMost(string action)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); SwapFixture f; int offerId, approvalId;
        await using (var db = database.CreateContext())
        {
            f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db);
            offerId = (await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default)).Id;
            approvalId = (await ContractTestFactory.Approvals(db, engine).CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offerId }, ContractTestFactory.Employee(f.Target))).Id;
        }
        using var gate = new Barrier(2);
        async Task Review()
        {
            await using var db = database.CreateContext(); Assert.True(gate.SignalAndWait(TimeSpan.FromSeconds(10)));
            var service = ContractTestFactory.Approvals(db);
            if (action == "approve") await service.ApproveAsync(99, f.Container, f.SourceSchedule, approvalId, ContractTestFactory.Manager());
            else await service.RejectAsync(99, f.Container, f.SourceSchedule, approvalId, ContractTestFactory.Manager());
        }
        async Task<bool> Accept()
        {
            await using var db = database.CreateContext(); Assert.True(gate.SignalAndWait(TimeSpan.FromSeconds(10)));
            try { await ContractTestFactory.Engine(db).Accept(f.Third, ContractTestFactory.Employee(f.Third), offerId, default); return true; }
            catch (ContractShiftConflictException error) { Assert.Equal("CONTRACT_OFFER_PENDING_APPROVAL", error.Code); return false; }
            catch (BusinessLogicLayer.Common.ValidationException) { return false; }
        }
        var review = Task.Run(Review); var accept = Task.Run(Accept); await Task.WhenAll(review, accept);
        await using var check = database.CreateContext();
        var transferred = action == "approve" || await accept;
        Assert.Equal(transferred ? 1 : 0, await check.ShiftSwapHistories.CountAsync());
        Assert.Equal(transferred ? 2 : 0, await check.ScheduleVersions.CountAsync());
        Assert.Equal(action == "approve" ? "approved" : "rejected", (await check.ContractShiftApprovalRequests.SingleAsync()).Status);
        var owner = (await check.ScheduleSlots.SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId;
        Assert.Equal(action == "approve" ? f.Target : await accept ? f.Third : f.Owner, owner);
    }

    [Fact]
    public async Task ManagerCreatedManualOfferHasTheSameGlobalPendingHold()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db);
        var schedule = (await db.Schedules.FindAsync(f.SourceSchedule))!;
        schedule.Note = "[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"3\":\"09:00-17:00\"}]],\"g\":[7],\"o\":[-7]}]]";
        await db.SaveChangesAsync();
        var result = await ShiftSwapScenarioFactory.ManagerController(db).CreateManualOffer(f.Container, f.SourceSchedule,
            new() { ManualColumnId = 7, DayOfMonth = 3, FromTime = "09:00", ToTime = "17:00" }, default);
        var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        var engine = ContractTestFactory.Engine(db); var service = ContractTestFactory.Approvals(db, engine);
        var pending = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        var conflict = await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Third, ContractTestFactory.Employee(f.Third), offer.Id, default));
        Assert.Equal("CONTRACT_OFFER_PENDING_APPROVAL", conflict.Code);
        Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
        await service.ApproveAsync(99, f.Container, f.SourceSchedule, pending.Id, ContractTestFactory.Manager());
        Assert.Single(await db.ShiftSwapHistories.ToListAsync()); Assert.Equal(2, await db.ScheduleVersions.CountAsync());
        Assert.Equal(f.Target, (await db.ScheduleSlots.AsNoTracking().SingleAsync(x => x.Id == offer.ScheduleSlotId)).EmployeeId);
    }

    [Fact]
    public async Task PendingBelowMinimumGiveRequestDoesNotReserveAnotherPublishedPickup()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db, ownerContract: true);
        var otherSlot = TestDataFactory.CreateDalSlot(f.SourceSchedule, 3, 1, f.Third, "09:00", "17:00");
        db.Add(otherSlot); db.ScheduleEmployees.Add(new() { ScheduleId = f.SourceSchedule, EmployeeId = f.Third }); await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db); var service = ContractTestFactory.Approvals(db, engine);
        var outgoing = await service.CreateGiveExceptionRequestAsync(f.Owner, ShiftSwapScenarioFactory.Request(f, new()), ContractTestFactory.Employee(f.Owner));
        var offer = await engine.Create(f.Third, ContractTestFactory.Employee(f.Third), new() { ScheduleId = f.SourceSchedule, ScheduleSlotId = otherSlot.Id }, default);
        var dto = Assert.Single(await engine.GetVisible(f.Target, ContractTestFactory.Employee(f.Target), default));
        Assert.False(dto.IsAwaitingManagerApproval); Assert.True(dto.CanRequestManagerApproval);
        var claim = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
        Assert.Equal("pending", claim.Status);
        Assert.Equal("pending", (await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync(x => x.Id == outgoing.Id)).Status);
        Assert.Empty(await db.ScheduleVersions.ToListAsync()); Assert.Empty(await db.ShiftSwapHistories.ToListAsync());
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task ReservationMigrationPreservesAuditRowsAndEnforcesOnePendingClaim(bool populated)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261028182000_AddContractShiftApprovals");
        int? survivor = null;
        if (populated)
        {
            var f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db);
            var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
            var first = await ContractTestFactory.Approvals(db, engine).CreateClaimRequestAsync(f.Target,
                new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target));
            survivor = first.Id;
            var original = await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync();
            var duplicate = new ContractShiftApprovalRequestModel();
            db.Entry(duplicate).CurrentValues.SetValues(original); duplicate.Id = 0; duplicate.EmployeeId = f.Third; db.Add(duplicate);
            var giveaway = new ContractShiftApprovalRequestModel();
            db.Entry(giveaway).CurrentValues.SetValues(original); giveaway.Id = 0; giveaway.Kind = "below_minimum_give"; giveaway.EmployeeId = f.Owner; db.Add(giveaway);
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        }
        await migrator.MigrateAsync("20261029193000_ReservePendingClaimOffer");
        await db.Database.OpenConnectionAsync();
        await using var index = db.Database.GetDbConnection().CreateCommand();
        index.CommandText = "SELECT sql FROM sqlite_master WHERE type='index' AND name='ux_contract_approval_one_pending_claim_per_offer'";
        var sql = Assert.IsType<string>(await index.ExecuteScalarAsync());
        Assert.Contains("UNIQUE INDEX", sql); Assert.Contains("kind='claim' AND status='pending'", sql);
        if (populated)
        {
            var rows = await db.ContractShiftApprovalRequests.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
            Assert.Equal(3, rows.Count); Assert.Equal(survivor, rows[0].Id); Assert.Equal("pending", rows[0].Status);
            Assert.Equal("expired", rows[1].Status); Assert.Equal("Superseded by exclusive pending pickup request.", rows[1].FailureReason);
            Assert.Equal("below_minimum_give", rows[2].Kind); Assert.Equal("pending", rows[2].Status);
            var another = new ContractShiftApprovalRequestModel(); db.Entry(another).CurrentValues.SetValues(rows[1]);
            another.Id = 0; another.Status = "pending"; db.Add(another);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(2067, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
            db.ChangeTracker.Clear();
        }
        await migrator.MigrateAsync("20261028182000_AddContractShiftApprovals");
        Assert.Null(await index.ExecuteScalarAsync());
        if (populated)
        {
            Assert.Equal(3, await db.ContractShiftApprovalRequests.CountAsync());
            Assert.Equal("expired", (await db.ContractShiftApprovalRequests.AsNoTracking().OrderBy(x => x.Id).Skip(1).FirstAsync()).Status);
        }
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task ConcurrentReviewsOfSameRequestHaveExactlyOneWinner(bool differentManagers)
    {
        await using var database=await SqliteTestDatabase.CreateAsync(); SwapFixture f; int id;
        await using(var db=database.CreateContext())
        {
            f=await ContractTestFactory.SeedAsync(db);
            if (differentManagers)
            {
                db.ManagerAccounts.Add(new() { Id = 100, Username = "second-reviewer", DisplayName = "Second reviewer", PasswordHash = "synthetic-only",
                    CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow, PasswordUpdatedAtUtc = DateTimeOffset.UtcNow });
                await db.SaveChangesAsync();
            }
            var engine=ContractTestFactory.Engine(db);
            var offer=await engine.Create(f.Owner,ContractTestFactory.Employee(f.Owner),ShiftSwapScenarioFactory.Request(f,new()),default);
            id=(await ContractTestFactory.Approvals(db,engine).CreateClaimRequestAsync(f.Target,new() {ShiftSwapRequestId=offer.Id},ContractTestFactory.Employee(f.Target))).Id;
        }
        using var gate=new Barrier(2);
        async Task<bool> Review(int managerId)
        {
            await using var db=database.CreateContext(); Assert.True(gate.SignalAndWait(TimeSpan.FromSeconds(10)));
            try { await ContractTestFactory.Approvals(db).ApproveAsync(managerId,f.Container,f.SourceSchedule,id,ContractTestFactory.Manager(managerId)); return true; }
            catch(ContractShiftConflictException) {return false;}
        }
        Assert.Single(await Task.WhenAll(Task.Run(() => Review(99)),Task.Run(() => Review(differentManagers ? 100 : 99))),value=>value);
        await using var check=database.CreateContext(); Assert.Equal("approved",(await check.ContractShiftApprovalRequests.SingleAsync()).Status);
        Assert.Single(await check.ShiftSwapHistories.ToListAsync()); Assert.Equal(2,await check.ScheduleVersions.CountAsync());
    }

    [Fact]
    public async Task ConcurrentApproveAndRejectHaveExactlyOneTerminalDecision()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); SwapFixture f; int approvalId;
        await using (var db = database.CreateContext())
        {
            f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db);
            var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
            approvalId = (await ContractTestFactory.Approvals(db, engine).CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target))).Id;
        }
        using var gate = new Barrier(2);
        async Task<bool> Review(bool approve)
        {
            await using var db = database.CreateContext(); Assert.True(gate.SignalAndWait(TimeSpan.FromSeconds(10)));
            var service = ContractTestFactory.Approvals(db);
            try
            {
                if (approve) await service.ApproveAsync(99, f.Container, f.SourceSchedule, approvalId, ContractTestFactory.Manager());
                else await service.RejectAsync(99, f.Container, f.SourceSchedule, approvalId, ContractTestFactory.Manager());
                return true;
            }
            catch (ContractShiftConflictException error) { Assert.Equal("CONTRACT_ALREADY_REVIEWED", error.Code); return false; }
        }
        Assert.Single(await Task.WhenAll(Task.Run(() => Review(true)), Task.Run(() => Review(false))), won => won);
        await using var check = database.CreateContext(); var row = await check.ContractShiftApprovalRequests.SingleAsync();
        Assert.Contains(row.Status, new[] { "approved", "rejected" });
        Assert.Equal(row.Status == "approved" ? 1 : 0, await check.ShiftSwapHistories.CountAsync());
        Assert.Equal(row.Status == "approved" ? 2 : 0, await check.ScheduleVersions.CountAsync());
        Assert.Equal(row.Status == "approved" ? f.Target : f.Owner, (await check.ScheduleSlots.SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
    }

    [Fact]
    public async Task HistoryFailureRollsBackTransferVersionsAndApproval()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); SwapFixture f; int pending;
        await using (var db = database.CreateContext())
        {
            f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db); var service = ContractTestFactory.Approvals(db, engine);
            var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
            pending = (await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target))).Id;
        }
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(new FailHistory()).Options;
        await using (var failing = new AppDbContext(options))
            await Assert.ThrowsAsync<DbUpdateException>(() => ContractTestFactory.Approvals(failing).ApproveAsync(99, f.Container, f.SourceSchedule, pending, ContractTestFactory.Manager()));
        await using var check = database.CreateContext();
        Assert.Equal("pending", (await check.ContractShiftApprovalRequests.SingleAsync()).Status);
        Assert.Equal(f.Owner, (await check.ScheduleSlots.SingleAsync(x => x.Id == f.SourceSlot)).EmployeeId);
        Assert.Empty(await check.ScheduleVersions.ToListAsync()); Assert.Empty(await check.ShiftSwapHistories.ToListAsync());
    }


    [Fact]
    public async Task ExchangeReturnFloorRequiresReviewWhenPickupApprovalIsDisabled()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db); var engine = ContractTestFactory.Engine(db); var service = ContractTestFactory.Approvals(db,engine);
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequirePickupApproval,false));
        var offer = await engine.Create(f.Owner,ContractTestFactory.Employee(f.Owner),ShiftSwapScenarioFactory.Request(f,new(Kind:SwapKind.Exchange,Scope:SwapScope.Private)),default);
        await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Target,ContractTestFactory.Employee(f.Target),offer.Id,default));
        var pending = await service.CreateClaimRequestAsync(f.Target,new() { ShiftSwapRequestId = offer.Id },ContractTestFactory.Employee(f.Target));
        Assert.Empty(await db.ScheduleVersions.ToListAsync());
        await service.ApproveAsync(99,f.Container,f.SourceSchedule,pending.Id,ContractTestFactory.Manager());
        await ShiftSwapScenarioFactory.AssertVersionAndHistoryConsistency(db,offer.Id,f.SourceSchedule,f.ReturnSchedule);
    }

    [Fact]
    public async Task SourceFloorIsCheckedAgainAtTransferAfterHoursChange()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db,ownerContract:true);
        (await db.Employees.FindAsync(f.Target))!.ContractType = "zlecenie";
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequirePickupApproval, false));
        (await db.ScheduleEmployees.SingleAsync(x => x.ScheduleId == f.SourceSchedule && x.EmployeeId == f.Owner)).MinHoursMonth = 8;
        var extra = TestDataFactory.CreateDalSlot(f.SourceSchedule,4,1,f.Owner,"09:00","17:00"); db.Add(extra); await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db); var offer = await engine.Create(f.Owner,ContractTestFactory.Employee(f.Owner),ShiftSwapScenarioFactory.Request(f,new()),default);
        db.Remove(extra); await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Target,ContractTestFactory.Employee(f.Target),offer.Id,default));
        Assert.Equal("CONTRACT_GIVE_APPROVAL_REQUIRED", error.Code);
        Assert.Empty(await db.ShiftSwapHistories.ToListAsync()); Assert.Empty(await db.ScheduleVersions.ToListAsync());
    }

    [Fact]
    public async Task ApprovedExceptionFollowsOnlyResidualSubperiods()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ContractTestFactory.SeedAsync(db,ownerContract:true); (await db.Employees.FindAsync(f.Target))!.ContractType = "zlecenie";
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RestrictSwapsToEightHours,false));
        (await db.ScheduleSlots.FindAsync(f.SourceSlot))!.ToTime = "21:00"; await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db); var service = ContractTestFactory.Approvals(db,engine);
        var command = ShiftSwapScenarioFactory.Request(f,new()); command.ToTime = "21:00"; command.AllowPartialAcceptance = true;
        var pending = await service.CreateGiveExceptionRequestAsync(f.Owner,command,ContractTestFactory.Employee(f.Owner));
        var approved = await service.ApproveAsync(99,f.Container,f.SourceSchedule,pending.Id,ContractTestFactory.Manager());
        var firstClaim = await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = approved.ResultingShiftSwapRequestId!.Value,
            FromTime = "09:00", ToTime = "13:00" }, ContractTestFactory.Employee(f.Target));
        await service.ApproveAsync(99, f.Container, f.SourceSchedule, firstClaim.Id, ContractTestFactory.Manager());
        var residual = await db.ShiftSwapRequests.AsNoTracking().SingleAsync(x => x.Status == ShiftSwapStatus.Open);
        Assert.Equal(pending.Id,residual.ApprovedEmploymentExceptionApprovalId); Assert.Equal("13:00",residual.OfferedFromTime);
        var secondClaim = await service.CreateClaimRequestAsync(f.Third, new() { ShiftSwapRequestId = residual.Id }, ContractTestFactory.Employee(f.Third));
        await service.ApproveAsync(99, f.Container, f.SourceSchedule, secondClaim.Id, ContractTestFactory.Manager());
        Assert.Equal(2,await db.ShiftSwapHistories.CountAsync());
    }


    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task HttpApprovalRoutesEnforceRolesPhoneModeRequesterAndGraphScope(bool ownerBased)
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(); int offerId;
        await using (var db = host.Database.CreateContext())
        {
            (await db.Employees.SingleAsync()).ContractType = ownerBased ? "zlecenie" : "o_prace";
            await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequireBelowMinimumGiveApproval, false));
            var owner = TestDataFactory.CreateDalEmployee("Source","Worker"); owner.ContractType = ownerBased ? "o_prace" : "zlecenie"; db.Add(owner); await db.SaveChangesAsync();
            var graph = await db.Schedules.SingleAsync(); graph.PublicationStatus = SchedulePublicationStatus.Public;
            db.ScheduleEmployees.AddRange(new ScheduleEmployeeModel { ScheduleId=graph.Id,EmployeeId=1 },new ScheduleEmployeeModel { ScheduleId=graph.Id,EmployeeId=owner.Id });
            var slot = TestDataFactory.CreateDalSlot(graph.Id,1,1,owner.Id,"09:00","17:00"); db.Add(slot); await db.SaveChangesAsync();
            var offer = new ShiftSwapRequestModel { ScheduleId=graph.Id,ScheduleSlotId=slot.Id,FromEmployeeId=owner.Id,CreatedAtUtc=DateTimeOffset.UtcNow };
            db.Add(offer); await db.SaveChangesAsync(); offerId=offer.Id;
        }
        const string mine="/api/employee-shift-swap-approvals";
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized,(await host.Client.GetAsync(mine)).StatusCode);
        await host.Login("employee");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await host.Client.GetAsync("/api/containers/1/graphs/1/shift-swap-approvals")).StatusCode);
        var bypass = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(host.Client,
            $"/api/employee-shift-swaps/{offerId}/accept", new { requiresManagerApproval = false, managerApproved = true });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, bypass.StatusCode);
        using (var json = System.Text.Json.JsonDocument.Parse(await bypass.Content.ReadAsStringAsync()))
            Assert.Equal("CONTRACT_CLAIM_APPROVAL_REQUIRED", json.RootElement.GetProperty("code").GetString());
        await using (var bypassState = host.Database.CreateContext())
        {
            Assert.Empty(await bypassState.ScheduleVersions.ToListAsync()); Assert.Empty(await bypassState.ShiftSwapHistories.ToListAsync());
            Assert.Equal(ShiftSwapStatus.Open, (await bypassState.ShiftSwapRequests.SingleAsync()).Status);
        }
        var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(host.Client,mine+"/claims",new {shiftSwapRequestId=offerId,employeeId=999,status="approved",managerApproved=true});
        Assert.Equal(System.Net.HttpStatusCode.Created,response.StatusCode);
        var pending = (await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<ContractShiftApprovalDto>(response.Content))!;
        Assert.Equal(1,pending.EmployeeId); Assert.Equal("pending",pending.Status);
        var duplicate = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(host.Client,mine+"/claims",new {shiftSwapRequestId=offerId});
        Assert.Equal(System.Net.HttpStatusCode.Conflict,duplicate.StatusCode);
        using (var json=System.Text.Json.JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync()))
            Assert.Equal("CONTRACT_DUPLICATE_PENDING",json.RootElement.GetProperty("code").GetString());
        var heldAccept = await host.Client.PostAsync($"/api/employee-shift-swaps/{offerId}/accept", null);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, heldAccept.StatusCode);
        using (var json = System.Text.Json.JsonDocument.Parse(await heldAccept.Content.ReadAsStringAsync()))
            Assert.Equal("CONTRACT_OFFER_PENDING_APPROVAL", json.RootElement.GetProperty("code").GetString());
        await host.Login("manager"); await host.Exchange("phone");
        var review=$"/api/containers/1/graphs/1/shift-swap-approvals/{pending.Id}/approve";
        Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await host.Client.PostAsync(review,null)).StatusCode);
        await host.Exchange("pc");
        Assert.Equal(System.Net.HttpStatusCode.NotFound,(await host.Client.PostAsync($"/api/containers/2/graphs/1/shift-swap-approvals/{pending.Id}/approve",null)).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK,(await host.Client.PostAsync(review,null)).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Conflict,(await host.Client.PostAsync(review,null)).StatusCode);
        await host.Login("employee");
        Assert.Equal(System.Net.HttpStatusCode.Conflict,(await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(host.Client,mine+"/claims",new {shiftSwapRequestId=offerId})).StatusCode);
        await using var check=host.Database.CreateContext();
        Assert.Equal(1,(await check.ScheduleSlots.SingleAsync()).EmployeeId); Assert.Single(await check.ShiftSwapHistories.ToListAsync()); Assert.Equal(2,await check.ScheduleVersions.CountAsync());
    }


    [Fact]
    public async Task PostCommitNotifierFailureDoesNotReplayOrUndoApprovedTransfer()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f=await ContractTestFactory.SeedAsync(db); var notifier=new FailingNotifier();
        var engine=new ShiftSwapExecutionService(db,new ManagerEditLockService(),new NoopWorkflowLogService(),notifier,new ScheduleVersionCommitService(db));
        var service=new ContractShiftApprovalService(db,engine,new EmploymentContractPolicyService(db),new NoopWorkflowLogService(),notifier);
        var offer=await engine.Create(f.Owner,ContractTestFactory.Employee(f.Owner),ShiftSwapScenarioFactory.Request(f,new()),default);
        var request=await service.CreateClaimRequestAsync(f.Target,new() { ShiftSwapRequestId=offer.Id },ContractTestFactory.Employee(f.Target));
        var result=await service.ApproveAsync(99,f.Container,f.SourceSchedule,request.Id,ContractTestFactory.Manager());
        Assert.Equal("approved",result.Status); Assert.Equal(1,notifier.ScheduleChanges);
        await Assert.ThrowsAsync<ContractShiftConflictException>(()=>service.ApproveAsync(99,f.Container,f.SourceSchedule,request.Id,ContractTestFactory.Manager()));
        Assert.Equal(1,notifier.ScheduleChanges); Assert.Single(await db.ShiftSwapHistories.ToListAsync()); Assert.Equal(2,await db.ScheduleVersions.CountAsync());
    }

    private sealed class FailingNotifier : IRealtimeNotifier
    {
        public int ScheduleChanges { get; private set; }
        public Task NotifyScheduleChangedAsync(int containerId,int graphId,string reason) { ScheduleChanges++; throw new InvalidOperationException("Synthetic notifier failure"); }
        public Task NotifyManagerDataChangedAsync(string resourceType,string? resourceId,string reason,int? containerId=null,int? graphId=null)=>Task.FromException(new InvalidOperationException("Synthetic notifier failure"));
        public Task NotifyShiftSwapsChangedAsync(int? containerId,int? graphId,int? scheduleId,string reason,int? shiftSwapId=null)=>Task.CompletedTask;
        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry)=>Task.CompletedTask;
        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state)=>Task.CompletedTask;
        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state)=>Task.CompletedTask;
    }

    private sealed class FailHistory : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO \"shift_swap_history\"", StringComparison.Ordinal)) throw new InvalidOperationException("Injected history persistence failure");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}

internal static class ContractTestFactory
{
    internal static async Task<SwapFixture> SeedAsync(AppDbContext db, bool ownerContract = false)
    {
        var f = await ShiftSwapScenarioFactory.SeedAsync(db, new());
        (await db.Employees.FindAsync(f.Target))!.ContractType = "o_prace";
        if (ownerContract) (await db.Employees.FindAsync(f.Owner))!.ContractType = "o_prace";
        db.ManagerAccounts.Add(new() { Id = 99, Username = "reviewer", DisplayName = "Reviewer", PasswordHash = "synthetic-only",
            CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow, PasswordUpdatedAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return f;
    }
    internal static ShiftSwapExecutionService Engine(AppDbContext db, IScheduleEditLockService? locks = null)
        => new(db, locks ?? new ManagerEditLockService(), new NoopWorkflowLogService(), new NoopRealtimeNotifier(), new ScheduleVersionCommitService(db), new EmploymentContractPolicyService(db));
    internal static ContractShiftApprovalService Approvals(AppDbContext db, ShiftSwapExecutionService? engine = null)
        => new(db, engine ?? Engine(db), new EmploymentContractPolicyService(db), new NoopWorkflowLogService(), new NoopRealtimeNotifier());
    internal static HttpContext Employee(int id) => Actor(AuthRoles.Employee, "employee_id", id);
    internal static HttpContext Manager(int id = 99) => Actor(AuthRoles.Manager, "manager_id", id);
    private static HttpContext Actor(string role, string claim, int id) => new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
        new Claim(ClaimTypes.Role, role), new Claim(claim, id.ToString()), new Claim(ClaimTypes.Name, "Synthetic")], "test")) };
}
