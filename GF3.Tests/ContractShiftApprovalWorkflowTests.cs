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
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Realtime;
using WebApi.Services;
using WebApi.ShiftSwaps;

namespace GF3.Tests;

public sealed class ContractShiftApprovalWorkflowTests
{
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
    }

    [Fact]
    public async Task CompetingPendingClaimsHaveOneWinnerAndVisibleExpiredLoser()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        SwapFixture f; int first, second;
        await using (var db = database.CreateContext())
        {
            f = await ContractTestFactory.SeedAsync(db); (await db.Employees.FindAsync(f.Third))!.ContractType = "o_prace"; await db.SaveChangesAsync();
            var engine = ContractTestFactory.Engine(db); var service = ContractTestFactory.Approvals(db, engine);
            var offer = await engine.Create(f.Owner, ContractTestFactory.Employee(f.Owner), ShiftSwapScenarioFactory.Request(f, new()), default);
            first = (await service.CreateClaimRequestAsync(f.Target, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Target))).Id;
            second = (await service.CreateClaimRequestAsync(f.Third, new() { ShiftSwapRequestId = offer.Id }, ContractTestFactory.Employee(f.Third))).Id;
        }
        using var gate = new Barrier(2);
        async Task<bool> Approve(int id)
        {
            await using var db = database.CreateContext();
            Assert.True(gate.SignalAndWait(TimeSpan.FromSeconds(10)));
            try { await ContractTestFactory.Approvals(db).ApproveAsync(99, f.Container, f.SourceSchedule, id, ContractTestFactory.Manager()); return true; }
            catch (ContractShiftConflictException) { return false; }
        }
        Assert.Single(await Task.WhenAll(Task.Run(() => Approve(first)), Task.Run(() => Approve(second))), x => x);
        await using var check = database.CreateContext(); Assert.Single(await check.ShiftSwapHistories.ToListAsync());
        Assert.Equal(2, await check.ScheduleVersions.CountAsync());
        Assert.Equal(new[] { "approved", "expired" }, (await check.ContractShiftApprovalRequests.AsNoTracking().ToListAsync()).Select(x => x.Status).Order().ToArray());
    }


    [Fact]
    public async Task ConcurrentReviewsOfSameRequestHaveExactlyOneWinner()
    {
        await using var database=await SqliteTestDatabase.CreateAsync(); SwapFixture f; int id;
        await using(var db=database.CreateContext())
        {
            f=await ContractTestFactory.SeedAsync(db); var engine=ContractTestFactory.Engine(db);
            var offer=await engine.Create(f.Owner,ContractTestFactory.Employee(f.Owner),ShiftSwapScenarioFactory.Request(f,new()),default);
            id=(await ContractTestFactory.Approvals(db,engine).CreateClaimRequestAsync(f.Target,new() {ShiftSwapRequestId=offer.Id},ContractTestFactory.Employee(f.Target))).Id;
        }
        using var gate=new Barrier(2);
        async Task<bool> Review()
        {
            await using var db=database.CreateContext(); Assert.True(gate.SignalAndWait(TimeSpan.FromSeconds(10)));
            try { await ContractTestFactory.Approvals(db).ApproveAsync(99,f.Container,f.SourceSchedule,id,ContractTestFactory.Manager()); return true; }
            catch(ContractShiftConflictException) {return false;}
        }
        Assert.Single(await Task.WhenAll(Task.Run(Review),Task.Run(Review)),value=>value);
        await using var check=database.CreateContext(); Assert.Equal("approved",(await check.ContractShiftApprovalRequests.SingleAsync()).Status);
        Assert.Single(await check.ShiftSwapHistories.ToListAsync()); Assert.Equal(2,await check.ScheduleVersions.CountAsync());
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
        (await db.ScheduleEmployees.SingleAsync(x => x.ScheduleId == f.SourceSchedule && x.EmployeeId == f.Owner)).MinHoursMonth = 8;
        var extra = TestDataFactory.CreateDalSlot(f.SourceSchedule,4,1,f.Owner,"09:00","17:00"); db.Add(extra); await db.SaveChangesAsync();
        var engine = ContractTestFactory.Engine(db); var offer = await engine.Create(f.Owner,ContractTestFactory.Employee(f.Owner),ShiftSwapScenarioFactory.Request(f,new()),default);
        db.Remove(extra); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ContractShiftConflictException>(() => engine.Accept(f.Target,ContractTestFactory.Employee(f.Target),offer.Id,default));
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
        await engine.Accept(f.Target,ContractTestFactory.Employee(f.Target),approved.ResultingShiftSwapRequestId!.Value,default,new() { FromTime="09:00",ToTime="13:00" });
        var residual = await db.ShiftSwapRequests.AsNoTracking().SingleAsync(x => x.Status == ShiftSwapStatus.Open);
        Assert.Equal(pending.Id,residual.ApprovedEmploymentExceptionApprovalId); Assert.Equal("13:00",residual.OfferedFromTime);
        await engine.Accept(f.Third,ContractTestFactory.Employee(f.Third),residual.Id,default);
        Assert.Equal(2,await db.ShiftSwapHistories.CountAsync());
    }


    [Fact]
    public async Task HttpApprovalRoutesEnforceRolesPhoneModeRequesterAndGraphScope()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(); int offerId;
        await using (var db = host.Database.CreateContext())
        {
            (await db.Employees.SingleAsync()).ContractType = "o_prace";
            var owner = TestDataFactory.CreateDalEmployee("Source","Worker"); db.Add(owner); await db.SaveChangesAsync();
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
        var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(host.Client,mine+"/claims",new {shiftSwapRequestId=offerId,employeeId=999,status="approved",managerApproved=true});
        Assert.Equal(System.Net.HttpStatusCode.Created,response.StatusCode);
        var pending = (await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<ContractShiftApprovalDto>(response.Content))!;
        Assert.Equal(1,pending.EmployeeId); Assert.Equal("pending",pending.Status);
        var duplicate = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(host.Client,mine+"/claims",new {shiftSwapRequestId=offerId});
        Assert.Equal(System.Net.HttpStatusCode.Conflict,duplicate.StatusCode);
        using (var json=System.Text.Json.JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync()))
            Assert.Equal("CONTRACT_DUPLICATE_PENDING",json.RootElement.GetProperty("code").GetString());
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
    internal static HttpContext Manager() => Actor(AuthRoles.Manager, "manager_id", 99);
    private static HttpContext Actor(string role, string claim, int id) => new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
        new Claim(ClaimTypes.Role, role), new Claim(claim, id.ToString()), new Claim(ClaimTypes.Name, "Synthetic")], "test")) };
}
