using System.Security.Claims;
using System.Text.Json;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Controllers;
using WebApi.Realtime;

namespace GF3.Tests;

public sealed class ShiftSwapSlotNoConflictTests
{
    [Fact]
    public async Task Accept_PartialPrivateF35Swap_ReallocatesConflictingSlotNumbers()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context);
        var before = await ReadSlotsAsync(context);

        var result = await CreateController(context, fixture.TargetId).Accept(fixture.SwapId, CancellationToken.None);

        var dto = Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("accepted", dto.Status);
        await using var persisted = database.CreateContext();
        var after = await ReadSlotsAsync(persisted);
        Assert.Equal(before.Length + 1, after.Length);
        Assert.Equal(new SlotState(fixture.SourceId, fixture.ScheduleId, 23, 3, fixture.TargetId,
            SlotStatus.ASSIGNED, "15:00", "22:00"), after.Single(slot => slot.Id == fixture.SourceId));
        var remainder = Assert.Single(after, slot => slot.EmployeeId == fixture.OwnerId);
        Assert.Equal(("09:00", "15:00", 3), (remainder.FromTime, remainder.ToTime, remainder.SlotNo));
        Assert.True(remainder.Id != fixture.SourceId);
        Assert.Equal(before.Where(slot => slot.Id != fixture.SourceId),
            after.Where(slot => before.Any(old => old.Id == slot.Id) && slot.Id != fixture.SourceId));
        AssertUniqueKeys(after);
        Assert.Equal(13, after.Where(slot => slot.Day == 23 &&
            (slot.EmployeeId == fixture.OwnerId || slot.EmployeeId == fixture.TargetId)).Sum(DurationHours));
    }

    [Fact]
    public async Task Accept_PartialSwap_PersistsAcceptedRequestAndHistory()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context);
        var notifier = new RecordingNotifier(database, context, fixture.SwapId);
        var startedAt = DateTimeOffset.UtcNow;

        await CreateController(context, fixture.TargetId, notifier).Accept(fixture.SwapId, CancellationToken.None);

        await using var persisted = database.CreateContext();
        var swap = await persisted.ShiftSwapRequests.SingleAsync();
        Assert.Equal(ShiftSwapStatus.Accepted, swap.Status);
        Assert.Equal(fixture.TargetId, swap.AcceptedByEmployeeId);
        Assert.Equal(fixture.SourceId, swap.ScheduleSlotId);
        Assert.NotNull(swap.AcceptedAtUtc);
        Assert.Equal(TimeSpan.Zero, swap.AcceptedAtUtc.Value.Offset);
        Assert.InRange(swap.AcceptedAtUtc.Value, startedAt, DateTimeOffset.UtcNow);
        var history = await persisted.ShiftSwapHistories.SingleAsync();
        Assert.Equal(fixture.SwapId, history.SourceShiftSwapRequestId);
        Assert.Equal(fixture.SourceId, history.ScheduleSlotId);
        Assert.Equal(fixture.OwnerId, history.FromEmployeeId);
        Assert.Equal(fixture.TargetId, history.AcceptedByEmployeeId);
        Assert.Equal(swap.AcceptedAtUtc, history.AcceptedAtUtc);
        Assert.Equal(("15:00", "22:00"), (history.FromTime, history.ToTime));
        var before = JsonSerializer.Deserialize<ShiftSwapScheduleSnapshotDto>(history.BeforeSnapshotJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var after = JsonSerializer.Deserialize<ShiftSwapScheduleSnapshotDto>(history.AfterSnapshotJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal("09:00 - 22:00", before.Rows.Single(row => row.EmployeeId == fixture.OwnerId).DayValues[23]);
        Assert.False(before.Rows.Single(row => row.EmployeeId == fixture.TargetId).DayValues.ContainsKey(23));
        Assert.Equal("09:00 - 15:00", after.Rows.Single(row => row.EmployeeId == fixture.OwnerId).DayValues[23]);
        Assert.Equal("15:00 - 22:00", after.Rows.Single(row => row.EmployeeId == fixture.TargetId).DayValues[23]);
        Assert.Equal(["schedule", "swaps"], notifier.Events);
        Assert.True(notifier.AllAfterCommit);
        Assert.True(notifier.AllSawPersistedAcceptance);
    }

    [Fact]
    public async Task Accept_FullSlotSwap_PreservesSlotNumber()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context, sourceFrom: "15:00", sourceSlotNo: 5);
        var before = await ReadSlotsAsync(context);

        var result = await CreateController(context, fixture.TargetId).Accept(fixture.SwapId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        await using var persisted = database.CreateContext();
        var after = await ReadSlotsAsync(persisted);
        Assert.Equal(before.Length, after.Length);
        Assert.Equal(before.Single(slot => slot.Id == fixture.SourceId) with { EmployeeId = fixture.TargetId },
            after.Single(slot => slot.Id == fixture.SourceId));
        Assert.DoesNotContain(after, slot => slot.EmployeeId == fixture.OwnerId);
        AssertUniqueKeys(after);
    }

    [Fact]
    public async Task Accept_PrefixPartialSwap_AllocatesRemainderPosition()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context, offeredFrom: "09:00", offeredTo: "15:00");

        var result = await CreateController(context, fixture.TargetId).Accept(fixture.SwapId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        await using var persisted = database.CreateContext();
        var slots = await ReadSlotsAsync(persisted);
        var transfer = slots.Single(slot => slot.Id == fixture.SourceId);
        Assert.Equal((fixture.TargetId, "09:00", "15:00", 3),
            (transfer.EmployeeId!.Value, transfer.FromTime, transfer.ToTime, transfer.SlotNo));
        var remainder = Assert.Single(slots, slot => slot.EmployeeId == fixture.OwnerId);
        Assert.Equal(("15:00", "22:00", 3), (remainder.FromTime, remainder.ToTime, remainder.SlotNo));
        AssertUniqueKeys(slots);
    }

    [Fact]
    public async Task Accept_MiddlePartialSwap_AllocatesBothRemainders()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context, offeredTo: "18:00");
        context.ScheduleSlots.AddRange(
            TestDataFactory.CreateDalSlot(fixture.ScheduleId, 23, 1, null, "15:00", "18:00"),
            TestDataFactory.CreateDalSlot(fixture.ScheduleId, 23, 3, null, "15:00", "18:00"),
            TestDataFactory.CreateDalSlot(fixture.ScheduleId, 23, 1, null, "18:00", "22:00"));
        await context.SaveChangesAsync();
        var before = await ReadSlotsAsync(context);

        var result = await CreateController(context, fixture.TargetId).Accept(fixture.SwapId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        await using var persisted = database.CreateContext();
        var after = await ReadSlotsAsync(persisted);
        Assert.Equal(before.Length + 2, after.Length);
        var transfer = after.Single(slot => slot.Id == fixture.SourceId);
        Assert.Equal((fixture.TargetId, "15:00", "18:00", 2),
            (transfer.EmployeeId!.Value, transfer.FromTime, transfer.ToTime, transfer.SlotNo));
        var remainders = after.Where(slot => slot.EmployeeId == fixture.OwnerId).OrderBy(slot => slot.FromTime).ToArray();
        Assert.Equal([("09:00", "15:00", 3), ("18:00", "22:00", 2)],
            remainders.Select(slot => (slot.FromTime, slot.ToTime, slot.SlotNo)));
        Assert.Equal(13, remainders.Sum(DurationHours) + DurationHours(transfer));
        Assert.Equal(before.Where(slot => slot.Id != fixture.SourceId),
            after.Where(slot => before.Any(old => old.Id == slot.Id) && slot.Id != fixture.SourceId));
        AssertUniqueKeys(after);
    }

    [Fact]
    public async Task Accept_RecipientAlreadyWorksOfferedTime_RejectsWithoutMutation()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context);
        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(fixture.ScheduleId, 23, 1,
            fixture.TargetId, "14:00", "16:00"));
        await context.SaveChangesAsync();
        await AssertRejectedAsync(database, context, fixture, fixture.TargetId, "You already work during this time.");
    }

    [Fact]
    public async Task Accept_NotIntendedPrivateRecipient_RejectsWithoutMutation()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context);
        await AssertRejectedAsync(database, context, fixture, fixture.OtherId,
            "This private swap offer is for another employee.");
    }

    [Fact]
    public async Task Accept_AlreadyAcceptedRequest_DoesNotTransferTwice()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context);
        await CreateController(context, fixture.TargetId).Accept(fixture.SwapId, CancellationToken.None);
        await AssertRejectedAsync(database, context, fixture, fixture.TargetId,
            "This swap offer is no longer open.", ShiftSwapStatus.Accepted, expectedHistoryCount: 1);
    }

    [Theory]
    [InlineData(true, "The offered period must stay inside the selected shift.")]
    [InlineData(false, "This shift is no longer assigned to the employee who opened the swap.")]
    public async Task Accept_StaleOrEditedSourceSlot_RejectsWithoutMutation(bool editTime, string message)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context);
        var source = await context.ScheduleSlots.SingleAsync(slot => slot.Id == fixture.SourceId);
        if (editTime)
            source.ToTime = "20:00";
        else
            source.EmployeeId = fixture.OtherId;
        await context.SaveChangesAsync();
        await AssertRejectedAsync(database, context, fixture, fixture.TargetId, message);
    }

    [Fact]
    public async Task Accept_ManagerLockedSchedule_RejectsWithoutMutation()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context);
        var schedule = await context.Schedules.SingleAsync();
        var locks = new ManagerEditLockService();
        ((IScheduleEditLockService)locks).SetLocks("manager-connection", "Test manager",
            [new ScheduleEditLockTarget(schedule.ContainerId, schedule.Id)]);
        await AssertRejectedAsync(database, context, fixture, fixture.TargetId,
            "This schedule is being edited by a manager. Try again after the manager saves changes.", locks: locks);
    }

    [Fact]
    public async Task Accept_ManagerCreatedOpenShift_PreservesExistingPath()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context, sourceFrom: "15:00", sourceSlotNo: 5);
        var source = await context.ScheduleSlots.SingleAsync(slot => slot.Id == fixture.SourceId);
        source.EmployeeId = null;
        source.Status = SlotStatus.UNFURNISHED;
        var request = await context.ShiftSwapRequests.SingleAsync();
        request.FromEmployeeId = null;
        request.IsManagerCreated = true;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var before = await ReadSlotsAsync(context);

        var result = await CreateController(context, fixture.TargetId).Accept(fixture.SwapId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        await using var persisted = database.CreateContext();
        var after = await ReadSlotsAsync(persisted);
        Assert.Equal(before.Length, after.Length);
        Assert.Equal(before.Single(slot => slot.Id == fixture.SourceId) with
            { EmployeeId = fixture.TargetId, Status = SlotStatus.ASSIGNED },
            after.Single(slot => slot.Id == fixture.SourceId));
        Assert.Equal(ShiftSwapStatus.Accepted, (await persisted.ShiftSwapRequests.SingleAsync()).Status);
        Assert.True((await persisted.ShiftSwapHistories.SingleAsync()).IsManagerCreated);
        AssertUniqueKeys(after);
    }

    [Theory]
    [InlineData(true, "slot_no")]
    [InlineData(false, "employee_id")]
    public async Task Sqlite_UniqueSlotIndexes_RemainEnforced(bool duplicatePosition, string column)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedAsync(context);
        var before = await ReadSlotsAsync(context);
        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(fixture.ScheduleId, 23,
            duplicatePosition ? 1 : 2, duplicatePosition ? fixture.TargetId : fixture.OwnerId, "09:00", "22:00"));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        var sqlite = Assert.IsType<SqliteException>(error.InnerException);
        Assert.Equal(19, sqlite.SqliteErrorCode);
        Assert.Equal(2067, sqlite.SqliteExtendedErrorCode);
        Assert.Contains($"schedule_slot.{column}", sqlite.Message);
        await using var persisted = database.CreateContext();
        Assert.Equal(before, await ReadSlotsAsync(persisted));
    }

    private static async Task AssertRejectedAsync(SqliteTestDatabase database, AppDbContext context, Fixture fixture,
        int employeeId, string message, ShiftSwapStatus expectedStatus = ShiftSwapStatus.Open, int expectedHistoryCount = 0,
        IScheduleEditLockService? locks = null)
    {
        var before = await ReadSlotsAsync(context);
        var notifier = new RecordingNotifier(database, context, fixture.SwapId);
        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateController(context, employeeId, notifier, locks).Accept(fixture.SwapId, CancellationToken.None));
        Assert.Equal(message, error.Message);
        await using var persisted = database.CreateContext();
        Assert.Equal(before, await ReadSlotsAsync(persisted));
        var swap = await persisted.ShiftSwapRequests.SingleAsync();
        Assert.Equal(expectedStatus, swap.Status);
        if (expectedStatus == ShiftSwapStatus.Open)
        {
            Assert.Null(swap.AcceptedByEmployeeId);
            Assert.Null(swap.AcceptedAtUtc);
        }
        Assert.Equal(expectedHistoryCount, await persisted.ShiftSwapHistories.CountAsync());
        Assert.Empty(notifier.Events);
    }

    private sealed class RecordingNotifier(SqliteTestDatabase database, AppDbContext context, int swapId) : IRealtimeNotifier
    {
        public List<string> Events { get; } = [];
        public bool AllAfterCommit { get; private set; } = true;
        public bool AllSawPersistedAcceptance { get; private set; } = true;

        private async Task RecordAsync(string name)
        {
            Events.Add(name);
            AllAfterCommit &= context.Database.CurrentTransaction is null;
            await using var persisted = database.CreateContext();
            AllSawPersistedAcceptance &= await persisted.ShiftSwapRequests.AnyAsync(swap =>
                swap.Id == swapId && swap.Status == ShiftSwapStatus.Accepted) &&
                await persisted.ShiftSwapHistories.CountAsync(history => history.SourceShiftSwapRequestId == swapId) == 1;
        }

        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason) => RecordAsync("schedule");
        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason,
            int? shiftSwapId = null) => RecordAsync("swaps");
        public Task NotifyManagerDataChangedAsync(string resourceType, string? resourceId, string reason,
            int? containerId = null, int? graphId = null) => RecordAsync("manager");
        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry) => RecordAsync("log");
        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state) => RecordAsync("schedule-lock");
        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state) => RecordAsync("manager-lock");
    }

    private static EmployeeShiftSwapsController CreateController(
        AppDbContext context, int employeeId, IRealtimeNotifier? notifier = null, IScheduleEditLockService? locks = null)
    {
        var controller = new EmployeeShiftSwapsController(context, locks ?? new ManagerEditLockService(),
            new NoopWorkflowLogService(), notifier ?? new NoopRealtimeNotifier());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, $"employee-{employeeId}"),
                    new Claim(ClaimTypes.Role, AuthRoles.Employee),
                    new Claim("employee_id", employeeId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ], JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role)),
            },
        };
        return controller;
    }

    private static async Task<Fixture> SeedAsync(AppDbContext context,
        string offeredFrom = "15:00", string offeredTo = "22:00", string sourceFrom = "09:00", int sourceSlotNo = 1)
    {
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var names = new[] { "Anastasia", "Oleh", "Olena", "Anna", "Margarita", "Wiktoria", "Iryna" };
        var employees = names.Select(name => TestDataFactory.CreateDalEmployee(name, "Test", email: null)).ToArray();
        context.AddRange(container, shop);
        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();
        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "F35", 2026, 10);
        schedule.PublicationStatus = SchedulePublicationStatus.Public;
        schedule.AllowSwap = true;
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();
        context.ScheduleEmployees.AddRange(employees.Select((employee, index) => new ScheduleEmployeeModel
        {
            ScheduleId = schedule.Id, EmployeeId = employee.Id, DisplayOrder = index, MinHoursMonth = 80,
        }));
        var source = TestDataFactory.CreateDalSlot(schedule.Id, 23, sourceSlotNo, employees[0].Id, sourceFrom, "22:00");
        context.ScheduleSlots.AddRange(source,
            TestDataFactory.CreateDalSlot(schedule.Id, 23, 1, employees[2].Id, "09:00", "15:00"),
            TestDataFactory.CreateDalSlot(schedule.Id, 23, 2, employees[3].Id, "09:00", "15:00"),
            TestDataFactory.CreateDalSlot(schedule.Id, 23, 1, employees[4].Id, "15:00", "22:00"),
            TestDataFactory.CreateDalSlot(schedule.Id, 23, 2, employees[5].Id, "15:00", "22:00"),
            TestDataFactory.CreateDalSlot(schedule.Id, 23, 1, employees[6].Id, "16:30", "22:00"),
            TestDataFactory.CreateDalSlot(schedule.Id, 21, 1, employees[1].Id, "15:00", "22:00"),
            TestDataFactory.CreateDalSlot(schedule.Id, 22, 1, employees[1].Id, "15:00", "22:00"));
        await context.SaveChangesAsync();
        var swap = new ShiftSwapRequestModel
        {
            ScheduleId = schedule.Id, ScheduleSlotId = source.Id, FromEmployeeId = employees[0].Id,
            TargetEmployeeId = employees[1].Id, Visibility = ShiftSwapVisibility.Private,
            OfferedFromTime = offeredFrom, OfferedToTime = offeredTo, CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        context.ShiftSwapRequests.Add(swap);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new Fixture(schedule.Id, source.Id, swap.Id, employees[0].Id, employees[1].Id, employees[6].Id);
    }

    private static async Task<SlotState[]> ReadSlotsAsync(AppDbContext context)
        => (await context.ScheduleSlots.AsNoTracking().OrderBy(slot => slot.Id).ToListAsync())
            .Select(slot => new SlotState(slot.Id, slot.ScheduleId, slot.DayOfMonth, slot.SlotNo,
                slot.EmployeeId, slot.Status, slot.FromTime, slot.ToTime)).ToArray();

    private static void AssertUniqueKeys(SlotState[] slots)
    {
        Assert.Equal(slots.Length, slots.Select(slot =>
            (slot.ScheduleId, slot.Day, slot.FromTime, slot.ToTime, slot.SlotNo)).Distinct().Count());
        var assigned = slots.Where(slot => slot.EmployeeId.HasValue).ToArray();
        Assert.Equal(assigned.Length, assigned.Select(slot =>
            (slot.ScheduleId, slot.Day, slot.FromTime, slot.ToTime, slot.EmployeeId)).Distinct().Count());
    }

    private static double DurationHours(SlotState slot)
        => (TimeSpan.Parse(slot.ToTime) - TimeSpan.Parse(slot.FromTime)).TotalHours;

    private sealed record Fixture(int ScheduleId, int SourceId, int SwapId, int OwnerId, int TargetId, int OtherId);
    private sealed record SlotState(int Id, int ScheduleId, int Day, int SlotNo, int? EmployeeId,
        SlotStatus Status, string FromTime, string ToTime);
}
