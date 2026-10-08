using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Auth;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Controllers;
using WebApi.Options;
using WebApi.Realtime;
using WebApi.Services;

namespace GF3.Tests;

public sealed class ShiftSwapSafetyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_InvalidTargetId_DoesNotPublish(int targetId)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database, offer: false);
        await using var db = database.CreateContext();
        var notifier = new RecordingNotifier();
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, fixture.Owner, notifier)
            .Create(Request(fixture, targetId: targetId), default));
        Assert.Empty(notifier.Events);
        Assert.Equal(0, await db.ShiftSwapRequests.CountAsync());
    }

    [Theory]
    [InlineData("09:00", null)]
    [InlineData(null, "15:00")]
    [InlineData(" ", "15:00")]
    public async Task Create_PartiallyProvidedPeriod_IsRejected(string? from, string? to)
        => await AssertCreateRejectedAsync(from, to);

    [Theory]
    [InlineData("12:30:45", "15:00")]
    [InlineData("9:30", "15:00")]
    [InlineData("15:00", "09:00")]
    [InlineData("08:00", "15:00")]
    [InlineData("09:00", "23:00")]
    [InlineData("+9:00", "15:00")]
    [InlineData("09:00", "09:00")]
    public async Task Create_MalformedTime_IsRejected(string from, string to)
        => await AssertCreateRejectedAsync(from, to);

    [Theory]
    [InlineData("09:00", "15:00")]
    [InlineData(null, null)]
    [InlineData(" ", " ")]
    public async Task Create_ValidPeriod_IsAccepted(string? from, string? to)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database, offer: false);
        await using var db = database.CreateContext();
        var result = await Employee(db, fixture.Owner).Create(Request(fixture, from, to), default);
        Assert.IsType<CreatedAtActionResult>(result.Result);
        var swap = await db.ShiftSwapRequests.SingleAsync();
        Assert.Equal(from is null or " " ? "22:00" : to, swap.OfferedToTime);
    }

    [Fact]
    public async Task Create_DuplicateOpenSlot_ExistingIndexEnforced()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database, offer: false);
        var gate = new WriteGate("INSERT INTO \"shift_swap_request\"");
        await using var first = Context(database, gate);
        await using var second = Context(database, gate);
        var firstNotifier = new RecordingNotifier();
        var secondNotifier = new RecordingNotifier();
        var results = await RaceAsync(
            () => Employee(first, fixture.Owner, firstNotifier).Create(Request(fixture), default),
            () => Employee(second, fixture.Owner, secondNotifier).Create(Request(fixture), default));
        AssertWinner(results);
        Assert.Equal("This shift already has an open swap offer.", results.Single(error => error != null)!.Message);
        await using var persisted = database.CreateContext();
        Assert.Equal(1, await persisted.ShiftSwapRequests.CountAsync(swap => swap.Status == ShiftSwapStatus.Open));
        Assert.Equal(1, firstNotifier.Events.Count + secondNotifier.Events.Count);
    }

    [Fact]
    public async Task Accept_OpenOffer_PersistsExactlyOnce()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var db = database.CreateContext();
        var notifier = new RecordingNotifier();
        await Employee(db, fixture.Target, notifier).Accept(fixture.Swap, default);
        await AssertAcceptedAsync(database, fixture);
        Assert.Equal(["employee-swap-accepted", "employee-swap-accepted"], notifier.Events);
    }

    [Fact]
    public async Task Accept_SameOfferTwice_SecondIsConflict()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        var gate = new TransactionGate();
        await using var first = Context(database, gate);
        await using var second = Context(database, gate);
        var a = new RecordingNotifier();
        var b = new RecordingNotifier();
        AssertWinner(await RaceAsync(
            () => Employee(first, fixture.Target, a).Accept(fixture.Swap, default),
            () => Employee(second, fixture.Target, b).Accept(fixture.Swap, default)));
        await AssertAcceptedAsync(database, fixture);
        Assert.Equal(2, a.Events.Count + b.Events.Count);
        await using var third = database.CreateContext();
        await Assert.ThrowsAsync<ValidationException>(() => Employee(third, fixture.Target).Accept(fixture.Swap, default));
        await AssertAcceptedAsync(database, fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Accept_RacesOwnerCancel_OnlyOneTerminalState(bool manager)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        IInterceptor gate = manager ? new TransactionGate() : new WriteGate("unused");
        await using var first = Context(database, gate);
        await using var second = Context(database, gate);
        var a = new RecordingNotifier();
        var b = new RecordingNotifier();
        AssertWinner(await RaceAsync(
            () => Employee(first, fixture.Target, a).Accept(fixture.Swap, default),
            () => manager ? Manager(second, b).CancelContainerSwap(fixture.Container, fixture.Swap, default) :
                Employee(second, fixture.Owner, b).Cancel(fixture.Swap, default)));
        await using var persisted = database.CreateContext();
        var swap = await persisted.ShiftSwapRequests.SingleAsync();
        if (swap.Status == ShiftSwapStatus.Accepted)
        {
            await AssertAcceptedAsync(database, fixture);
            Assert.Empty(b.Events);
            Assert.Equal(2, a.Events.Count);
        }
        else
        {
            Assert.Equal(ShiftSwapStatus.Cancelled, swap.Status);
            Assert.NotNull(swap.CancelledAtUtc);
            Assert.Null(swap.AcceptedAtUtc);
            Assert.Equal(0, await persisted.ShiftSwapHistories.CountAsync());
            await AssertOriginalSlotAsync(persisted, fixture);
            Assert.Empty(a.Events);
            Assert.NotEmpty(b.Events);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Accept_NotTargetAndNoSharedPublicContainer_Denied(bool publicOffer)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var db = database.CreateContext();
        var swap = await db.ShiftSwapRequests.SingleAsync();
        if (publicOffer)
        {
            swap.Visibility = ShiftSwapVisibility.Public;
            swap.TargetEmployeeId = null;
        }
        await db.SaveChangesAsync();
        var controller = Employee(db, fixture.Outsider);
        Assert.Empty(Visible(await controller.GetVisible(default)));
        await AssertRejectedAsync(database, fixture, controller);
    }

    [Fact]
    public async Task Accept_OwnerAcceptAndWrongAccount_Denied()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var db = database.CreateContext();
        await AssertRejectedAsync(database, fixture, Employee(db, fixture.Owner));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("assigned-manual")]
    [InlineData("period")]
    [InlineData("malformed-period")]
    [InlineData("missing")]
    public async Task Accept_StaleSourceSlot_IsUnavailable(string change)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var db = database.CreateContext();
        var source = await db.ScheduleSlots.SingleAsync();
        var swap = await db.ShiftSwapRequests.SingleAsync();
        if (change == "owner") source.EmployeeId = fixture.Outsider;
        if (change == "assigned-manual") { swap.IsManagerCreated = true; swap.FromEmployeeId = null; }
        if (change == "period") source.ToTime = "14:00";
        if (change == "malformed-period") swap.OfferedToTime = "15:00:45";
        if (change == "missing") { swap.ScheduleSlotId = null; db.ScheduleSlots.Remove(source); }
        await db.SaveChangesAsync();
        var notifier = new RecordingNotifier();
        var controller = Employee(db, fixture.Target, notifier);
        var visible = Visible(await controller.GetVisible(default));
        if (change == "missing") Assert.Empty(visible);
        else Assert.False(Assert.Single(visible).CanAccept);
        await Assert.ThrowsAsync<ValidationException>(() => controller.Accept(fixture.Swap, default));
        await using var persisted = database.CreateContext();
        Assert.Equal(ShiftSwapStatus.Open, (await persisted.ShiftSwapRequests.SingleAsync()).Status);
        Assert.Equal(0, await persisted.ShiftSwapHistories.CountAsync());
        Assert.Empty(notifier.Events);
    }

    [Theory]
    [InlineData("overlap", false)]
    [InlineData("adjacent", true)]
    [InlineData("month", true)]
    [InlineData("day", true)]
    [InlineData("unpublished", true)]
    public async Task Accept_AlreadyWorks_Rejects(string otherShift, bool allowed)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var db = database.CreateContext();
        var original = await db.Schedules.SingleAsync();
        var schedule = TestDataFactory.CreateDalSchedule(fixture.Container, original.ShopId, "Other", 2026,
            otherShift == "month" ? 11 : 10);
        schedule.PublicationStatus = otherShift == "unpublished" ? SchedulePublicationStatus.Private : SchedulePublicationStatus.Public;
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync();
        db.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(schedule.Id, otherShift == "day" ? 24 : 23,
            1, fixture.Target, otherShift == "adjacent" ? "15:00" : "14:00", "16:00"));
        await db.SaveChangesAsync();
        var controller = Employee(db, fixture.Target);
        Assert.Equal(allowed, Assert.Single(Visible(await controller.GetVisible(default))).CanAccept);
        if (allowed) await controller.Accept(fixture.Swap, default);
        else await AssertRejectedAsync(database, fixture, controller);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("locked")]
    [InlineData("unpublished")]
    [InlineData("cancelled")]
    public async Task Accept_WhenDisallowedOrLockedOrUnpublished_Rejects(string guard)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var db = database.CreateContext();
        var schedule = await db.Schedules.SingleAsync();
        var locks = new ManagerEditLockService();
        if (guard == "disabled") schedule.AllowSwap = false;
        if (guard == "unpublished") schedule.PublicationStatus = SchedulePublicationStatus.Private;
        if (guard == "cancelled") (await db.ShiftSwapRequests.SingleAsync()).Status = ShiftSwapStatus.Cancelled;
        if (guard == "locked") ((IScheduleEditLockService)locks).SetLocks("test", "Test manager",
            [new ScheduleEditLockTarget(fixture.Container, fixture.Schedule)]);
        await db.SaveChangesAsync();
        var controller = Employee(db, fixture.Target, locks: locks);
        await Assert.ThrowsAsync<ValidationException>(() => controller.Accept(fixture.Swap, default));
        Assert.Equal(0, await db.ShiftSwapHistories.CountAsync());
        await AssertOriginalSlotAsync(db, fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Accept_FailedHistoryOrSlotWrite_RollsBackEntireClaim(bool history)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var db = database.CreateContext();
        if (history)
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_history BEFORE INSERT ON shift_swap_history BEGIN SELECT RAISE(ABORT, 'test history constraint'); END;");
        else
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_slot BEFORE UPDATE ON schedule_slot BEGIN SELECT RAISE(ABORT, 'test slot constraint'); END;");
        var notifier = new RecordingNotifier();
        await AssertRejectedAsync(database, fixture, Employee(db, fixture.Target, notifier));
        Assert.Empty(notifier.Events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancel_StaleTrackedOpenOffer_CannotOverwriteAccepted(bool manager)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var stale = database.CreateContext();
        await Employee(stale, fixture.Owner).GetVisible(default);
        await using (var winner = database.CreateContext())
            await Employee(winner, fixture.Target).Accept(fixture.Swap, default);
        var notifier = new RecordingNotifier();
        if (manager)
            await Assert.ThrowsAsync<ValidationException>(() => Manager(stale, notifier).CancelContainerSwap(fixture.Container, fixture.Swap, default));
        else
            await Assert.ThrowsAsync<ValidationException>(() => Employee(stale, fixture.Owner, notifier).Cancel(fixture.Swap, default));
        Assert.Empty(notifier.Events);
        await AssertAcceptedAsync(database, fixture);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("cancel")]
    [InlineData("manager")]
    public async Task WriterBusy_ReturnsControlledConflictWithoutMutation(string action)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var holder = database.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();
        await using var actor = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(database.ConnectionString + ";Default Timeout=1").Options);
        var notifier = new RecordingNotifier();
        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            if (action == "accept") await Employee(actor, fixture.Target, notifier).Accept(fixture.Swap, default);
            else if (action == "cancel") await Employee(actor, fixture.Owner, notifier).Cancel(fixture.Swap, default);
            else await Manager(actor, notifier).CancelContainerSwap(fixture.Container, fixture.Swap, default);
        });
        Assert.Equal("This swap changed during another request. Refresh and try again.", exception.Message);
        await transaction.RollbackAsync();
        await using var persisted = database.CreateContext();
        Assert.Equal(ShiftSwapStatus.Open, (await persisted.ShiftSwapRequests.SingleAsync()).Status);
        Assert.Equal(0, await persisted.ShiftSwapHistories.CountAsync());
        Assert.Empty(notifier.Events);
        await AssertOriginalSlotAsync(persisted, fixture);
    }

    [Fact]
    public async Task Cancel_OnlyOwnerCanCancelAndOnlyOnce()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        await using var db = database.CreateContext();
        var notifier = new RecordingNotifier();
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, fixture.Target, notifier).Cancel(fixture.Swap, default));
        await Employee(db, fixture.Owner, notifier).Cancel(fixture.Swap, default);
        await using var next = database.CreateContext();
        await Assert.ThrowsAsync<ValidationException>(() => Employee(next, fixture.Owner, notifier).Cancel(fixture.Swap, default));
        var swap = await next.ShiftSwapRequests.SingleAsync();
        Assert.Equal(ShiftSwapStatus.Cancelled, swap.Status);
        Assert.NotNull(swap.CancelledAtUtc);
        Assert.Single(notifier.Events);
        await AssertOriginalSlotAsync(next, fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManagerCancel_RacesAccept_PreservesWinningState(bool graphAction)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database, manual: true);
        var gate = new TransactionGate();
        await using var first = Context(database, gate);
        await using var second = Context(database, gate);
        var acceptNotifier = new RecordingNotifier();
        var cancelNotifier = new RecordingNotifier();
        var results = await RaceAsync(
            () => Employee(first, fixture.Target, acceptNotifier).Accept(fixture.Swap, default),
            async () => { if (graphAction) await Manager(second, cancelNotifier).CancelManualOffer(fixture.Container, fixture.Schedule, fixture.Swap, default);
                else await Manager(second, cancelNotifier).CancelContainerSwap(fixture.Container, fixture.Swap, default); });
        AssertWinner(results);
        await using var persisted = database.CreateContext();
        var swap = await persisted.ShiftSwapRequests.SingleOrDefaultAsync();
        if (swap is not null)
        {
            Assert.Equal(ShiftSwapStatus.Accepted, swap.Status);
            Assert.Equal(fixture.Target, (await persisted.ScheduleSlots.SingleAsync()).EmployeeId);
            Assert.Equal(1, await persisted.ShiftSwapHistories.CountAsync());
            Assert.Empty(cancelNotifier.Events);
        }
        else
        {
            Assert.Equal(0, await persisted.ScheduleSlots.CountAsync());
            Assert.Equal(0, await persisted.ShiftSwapHistories.CountAsync());
            Assert.Empty(acceptNotifier.Events);
        }
    }

    [Theory]
    [InlineData("Visible note\n\n[[GF3_GRAPH_META:b64:!!!]]")]
    [InlineData("Visible note\n\n[[GF3_GRAPH_META:{bad json}]]")]
    [InlineData("Visible note\n\n[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{}]]}]]")]
    [InlineData("Visible note\n\n[[GF3_GRAPH_META:{\"m\":[[7,123,{\"23\":42}]]}]]")]
    public async Task ManagerManualNote_MalformedMetadataFailsSafely(string note)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database, manual: true);
        await using var db = database.CreateContext();
        (await db.Schedules.SingleAsync()).Note = note;
        (await db.ShiftSwapRequests.SingleAsync()).ManualColumnId = 7;
        await db.SaveChangesAsync();
        var notifier = new RecordingNotifier();
        var controller = Employee(db, fixture.Target, notifier);
        Assert.False(Assert.Single(Visible(await controller.GetVisible(default))).CanAccept);
        await Assert.ThrowsAsync<ValidationException>(() => controller.Accept(fixture.Swap, default));
        await using var persisted = database.CreateContext();
        Assert.Equal(note, (await persisted.Schedules.SingleAsync()).Note);
        Assert.Null((await persisted.ScheduleSlots.SingleAsync()).EmployeeId);
        Assert.Equal(ShiftSwapStatus.Open, (await persisted.ShiftSwapRequests.SingleAsync()).Status);
        Assert.Equal(0, await persisted.ShiftSwapHistories.CountAsync());
        Assert.Empty(notifier.Events);
    }

    [Fact]
    public async Task ManagerManualNote_ValidCellIsConsumedWithHistoryAndHighlight()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database, manual: true);
        await using var db = database.CreateContext();
        (await db.Schedules.SingleAsync()).Note = "Visible note\n\n[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"23\":\"09:00 - 22:00\",\"24\":\"09:00 - 15:00\"}]]}]]";
        (await db.ShiftSwapRequests.SingleAsync()).ManualColumnId = 7;
        await db.SaveChangesAsync();
        await Employee(db, fixture.Target).Accept(fixture.Swap, default);
        await using var persisted = database.CreateContext();
        var schedule = await persisted.Schedules.SingleAsync();
        Assert.StartsWith("Visible note", schedule.Note);
        var history = await persisted.ShiftSwapHistories.SingleAsync();
        var before = JsonSerializer.Deserialize<ShiftSwapScheduleSnapshotDto>(history.BeforeSnapshotJson, JsonOptions)!;
        var after = JsonSerializer.Deserialize<ShiftSwapScheduleSnapshotDto>(history.AfterSnapshotJson, JsonOptions)!;
        Assert.Contains(before.Rows, row => row.Kind == "manual" && row.DayValues.ContainsKey(23));
        Assert.DoesNotContain(after.Rows, row => row.Kind == "manual" && row.DayValues.ContainsKey(23));
        Assert.Contains(after.Rows, row => row.Kind == "manual" && row.DayValues.ContainsKey(24));
        Assert.Equal(fixture.Target, (await persisted.ScheduleCellStyles.SingleAsync()).EmployeeId);
    }

    [Fact]
    public async Task GetEmployees_ExcludesContactData_AndEnforcesHttpRolePolicy()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database);
        var options = new JwtAuthOptions { Issuer = "tests", Audience = "tests", SigningKey = "shift-safety-tests-signing-key-with-enough-entropy" };
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDbContext<AppDbContext>(configuration => configuration.UseSqlite(database.ConnectionString));
        builder.Services.AddSingleton<IScheduleEditLockService, ManagerEditLockService>();
        builder.Services.AddSingleton<IWorkflowLogService, NoopWorkflowLogService>();
        builder.Services.AddSingleton<IRealtimeNotifier, NoopRealtimeNotifier>();
        builder.Services.AddSingleton(Options.Create(options));
        builder.Services.AddAuthentication(JwtAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>(JwtAuthenticationDefaults.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(EmployeeShiftSwapsController).Assembly);
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/employee-shift-swaps/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/employee-shift-swaps/{fixture.Swap}/accept", null)).StatusCode);
        var tokenService = new JwtTokenService(Options.Create(options));
        var wrongRole = tokenService.CreateAccessToken(new AuthenticatedSessionDto { Role = "Other", UserName = "wrong-role", EmployeeId = fixture.Target });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongRole.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/employee-shift-swaps/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/employee-shift-swaps/{fixture.Swap}/accept", null)).StatusCode);
        var employeeToken = tokenService.CreateAccessToken(new AuthenticatedSessionDto { Role = AuthRoles.Employee, UserName = "test", EmployeeId = fixture.Owner });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken.AccessToken);
        var response = await client.GetAsync("/api/employee-shift-swaps/employees");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, json.RootElement.GetArrayLength());
        foreach (var employee in json.RootElement.EnumerateArray())
        {
            Assert.Equal(["displayName", "firstName", "id", "lastName"], employee.EnumerateObject().Select(property => property.Name).Order());
            Assert.False(employee.TryGetProperty("email", out _));
            Assert.False(employee.TryGetProperty("phone", out _));
        }
        await app.StopAsync();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static async Task AssertCreateRejectedAsync(string? from, string? to)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var fixture = await SeedAsync(database, offer: false);
        await using var db = database.CreateContext();
        var notifier = new RecordingNotifier();
        await Assert.ThrowsAsync<ValidationException>(() => Employee(db, fixture.Owner, notifier).Create(Request(fixture, from, to), default));
        Assert.Equal(0, await db.ShiftSwapRequests.CountAsync());
        await AssertOriginalSlotAsync(db, fixture);
        Assert.Empty(notifier.Events);
    }

    private static async Task AssertRejectedAsync(SqliteTestDatabase database, Fixture fixture, EmployeeShiftSwapsController controller)
    {
        await Assert.ThrowsAsync<ValidationException>(() => controller.Accept(fixture.Swap, default));
        await using var persisted = database.CreateContext();
        var swap = await persisted.ShiftSwapRequests.SingleAsync();
        Assert.Equal(ShiftSwapStatus.Open, swap.Status);
        Assert.Null(swap.AcceptedAtUtc);
        Assert.Null(swap.AcceptedByEmployeeId);
        Assert.Equal(0, await persisted.ShiftSwapHistories.CountAsync());
        await AssertOriginalSlotAsync(persisted, fixture);
    }

    private static async Task AssertOriginalSlotAsync(AppDbContext db, Fixture fixture)
    {
        var slot = await db.ScheduleSlots.AsNoTracking().SingleAsync(slot => slot.Id == fixture.Slot);
        Assert.Equal((fixture.Owner, "09:00", "22:00"), (slot.EmployeeId!.Value, slot.FromTime, slot.ToTime));
    }

    private static async Task AssertAcceptedAsync(SqliteTestDatabase database, Fixture fixture)
    {
        await using var db = database.CreateContext();
        var swap = await db.ShiftSwapRequests.SingleAsync();
        Assert.Equal(ShiftSwapStatus.Accepted, swap.Status);
        Assert.Equal(fixture.Target, swap.AcceptedByEmployeeId);
        Assert.NotNull(swap.AcceptedAtUtc);
        var history = await db.ShiftSwapHistories.SingleAsync();
        Assert.Equal(swap.AcceptedAtUtc, history.AcceptedAtUtc);
        Assert.Equal(fixture.Swap, history.SourceShiftSwapRequestId);
        var slots = await db.ScheduleSlots.ToListAsync();
        Assert.Equal(13, slots.Sum(slot => (TimeSpan.Parse(slot.ToTime) - TimeSpan.Parse(slot.FromTime)).TotalHours));
        Assert.Equal((fixture.Target, "09:00", "15:00"),
            (slots.Single(slot => slot.Id == fixture.Slot).EmployeeId!.Value, slots.Single(slot => slot.Id == fixture.Slot).FromTime, slots.Single(slot => slot.Id == fixture.Slot).ToTime));
        Assert.Equal(2, slots.Count);
        Assert.NotEqual(history.BeforeSnapshotJson, history.AfterSnapshotJson);
    }

    private static IEnumerable<ShiftSwapDto> Visible(ActionResult<IEnumerable<ShiftSwapDto>> result)
        => Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);

    private static CreateEmployeeShiftSwapRequest Request(Fixture fixture, string? from = "09:00", string? to = "15:00", int? targetId = null)
        => new() { ScheduleId = fixture.Schedule, ScheduleSlotId = fixture.Slot, FromTime = from, ToTime = to, TargetEmployeeId = targetId };

    private static EmployeeShiftSwapsController Employee(AppDbContext db, int employeeId, RecordingNotifier? notifier = null, IScheduleEditLockService? locks = null)
    {
        var controller = new EmployeeShiftSwapsController(db, locks ?? new ManagerEditLockService(), new NoopWorkflowLogService(), notifier ?? new RecordingNotifier());
        SetUser(controller, AuthRoles.Employee, employeeId);
        return controller;
    }

    private static ShiftSwapLogsController Manager(AppDbContext db, RecordingNotifier notifier)
    {
        var controller = new ShiftSwapLogsController(db, new NoopWorkflowLogService(), notifier);
        SetUser(controller, AuthRoles.Manager, 0);
        return controller;
    }

    private static void SetUser(ControllerBase controller, string role, int id)
        => controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Name, "synthetic"), new Claim(ClaimTypes.Role, role), new Claim("employee_id", id.ToString()),
            ], JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role)) } };

    private static async Task<Fixture> SeedAsync(SqliteTestDatabase database, bool offer = true, bool manual = false)
    {
        await using var db = database.CreateContext();
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var owner = TestDataFactory.CreateDalEmployee("Owner", "Test", "owner@example.invalid");
        var target = TestDataFactory.CreateDalEmployee("Target", "Test", "target@example.invalid");
        var outsider = TestDataFactory.CreateDalEmployee("Other", "Test", "other@example.invalid");
        db.AddRange(container, shop, owner, target, outsider);
        await db.SaveChangesAsync();
        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Safety", 2026, 10);
        schedule.AllowSwap = true;
        schedule.PublicationStatus = SchedulePublicationStatus.Public;
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync();
        db.ScheduleEmployees.AddRange(new ScheduleEmployeeModel { ScheduleId = schedule.Id, EmployeeId = owner.Id, DisplayOrder = 0 },
            new ScheduleEmployeeModel { ScheduleId = schedule.Id, EmployeeId = target.Id, DisplayOrder = 1 });
        var slot = TestDataFactory.CreateDalSlot(schedule.Id, 23, 1, manual ? null : owner.Id, "09:00", "22:00");
        db.ScheduleSlots.Add(slot);
        await db.SaveChangesAsync();
        var swap = new ShiftSwapRequestModel { ScheduleId = schedule.Id, ScheduleSlotId = slot.Id,
            FromEmployeeId = manual ? null : owner.Id, TargetEmployeeId = target.Id, Visibility = ShiftSwapVisibility.Private,
            Status = ShiftSwapStatus.Open, OfferedFromTime = "09:00", OfferedToTime = manual ? "22:00" : "15:00",
            IsManagerCreated = manual, CreatedAtUtc = DateTimeOffset.UtcNow };
        if (offer) { db.ShiftSwapRequests.Add(swap); await db.SaveChangesAsync(); }
        return new(container.Id, schedule.Id, slot.Id, swap.Id, owner.Id, target.Id, outsider.Id);
    }

    private sealed record Fixture(int Container, int Schedule, int Slot, int Swap, int Owner, int Target, int Outsider);

    private static AppDbContext Context(SqliteTestDatabase database, IInterceptor gate)
        => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(gate).Options);

    private static async Task<Exception?[]> RaceAsync(Func<Task> first, Func<Task> second)
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        async Task<Exception?> RunAsync(Func<Task> action)
        {
            if (Interlocked.Increment(ref arrivals) == 2) ready.SetResult();
            await ready.Task;
            return await Record.ExceptionAsync(action);
        }
        return await Task.WhenAll(Task.Run(() => RunAsync(first)), Task.Run(() => RunAsync(second)))
            .WaitAsync(TimeSpan.FromSeconds(40));
    }

    private static void AssertWinner(Exception?[] results)
    {
        Assert.Single(results, error => error is null);
        var loser = Assert.Single(results, error => error is not null);
        Assert.True(loser is ValidationException or KeyNotFoundException, loser?.ToString());
    }

    // SQLite begins writer transactions immediately; rendezvous before BEGIN, never while holding the writer lock.
    private sealed class TransactionGate : DbTransactionInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(12), cancellationToken);
            return result;
        }
    }

    private sealed class WriteGate(string sqlPrefix) : DbCommandInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private async Task WaitAsync(DbCommand command, CancellationToken cancellationToken)
        {
            if (!command.CommandText.StartsWith(sqlPrefix)) return;
            if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(12), cancellationToken);
        }
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { await WaitAsync(command, cancellationToken); return result; }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { await WaitAsync(command, cancellationToken); return result; }
    }

    private sealed class RecordingNotifier : IRealtimeNotifier
    {
        public List<string> Events { get; } = [];
        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason) { Events.Add(reason); return Task.CompletedTask; }
        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null) { Events.Add(reason); return Task.CompletedTask; }
        public Task NotifyManagerDataChangedAsync(string resourceType, string? resourceId, string reason, int? containerId = null, int? graphId = null) => Task.CompletedTask;
        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry) => Task.CompletedTask;
        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state) => Task.CompletedTask;
        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state) => Task.CompletedTask;
    }
}
