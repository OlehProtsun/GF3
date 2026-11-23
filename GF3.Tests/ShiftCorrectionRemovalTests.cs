using System.Text.Json;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ShiftCorrections;
using WebApi.Realtime;
using static GF3.Tests.ShiftCorrectionControllerTests;

namespace GF3.Tests;

public sealed class ShiftCorrectionRemovalTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemovalSnapshotsBothShiftsAndOnlyApprovalUnassignsThem(bool reject)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var colleague = TestDataFactory.CreateDalEmployee("Other", "Worker");
        var shop = TestDataFactory.CreateDalShop("Other shop");
        db.AddRange(colleague, shop);
        await db.SaveChangesAsync();
        var other = TestDataFactory.CreateDalSchedule(fixture.ContainerId, shop.Id);
        db.Add(other);
        await db.SaveChangesAsync();
        db.AddRange(TestDataFactory.CreateDalSlot(fixture.ScheduleId, 12, 3, colleague.Id, "09:00", "14:00"),
            TestDataFactory.CreateDalSlot(fixture.ScheduleId, 13, 1, fixture.EmployeeId, "09:00", "14:00"),
            TestDataFactory.CreateDalSlot(other.Id, 12, 1, fixture.EmployeeId, "20:00", "23:00"));
        await db.SaveChangesAsync();
        var before = await Slots(db);

        var created = await Create(db, fixture);
        Assert.Equal("pending", created.Status);
        Assert.Equal("removeShift", created.RequestType);
        Assert.False(created.IsDayOffRequest);
        Assert.Equal("Fixture note", created.Comment);
        Assert.Null(created.ScheduleSlotId);
        Assert.Null(created.RequestedFromTime);
        Assert.Equal([new(fixture.MorningSlotId, "08:00", "12:00"), new(fixture.AfternoonSlotId, "15:00", "19:00")], created.OriginalShifts);
        var persisted = await db.ShiftCorrectionRequests.AsNoTracking().SingleAsync();
        Assert.Equal(ShiftCorrectionOperation.RemoveShift, persisted.Operation);
        using var snapshot = JsonDocument.Parse(persisted.RemovalSnapshotJson!);
        Assert.Equal(created.OriginalShifts, snapshot.RootElement.GetProperty("Shifts").Deserialize<ShiftCorrectionOriginalShiftDto[]>());
        Assert.Equal(before, await Slots(db));
        var duplicate = await EmployeeController(db, fixture).Create(Removal(fixture), default);
        Assert.IsType<ConflictObjectResult>(duplicate.Result);

        var manager = ManagerController(db, fixture);
        var listed = Assert.IsAssignableFrom<IReadOnlyList<ShiftCorrectionRequestDto>>(
            Assert.IsType<OkObjectResult>((await manager.GetGraphRequests(fixture.ContainerId, fixture.ScheduleId, default)).Result).Value);
        Assert.Equal(created.OriginalShifts, Assert.Single(listed).OriginalShifts);
        var response = reject
            ? await manager.Reject(fixture.ContainerId, fixture.ScheduleId, created.Id, default)
            : await manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id, new(), default);
        var reviewed = Assert.IsType<ShiftCorrectionRequestDto>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(reject ? "rejected" : "approved", reviewed.Status);
        Assert.Equal(created.OriginalShifts, reviewed.OriginalShifts);
        Assert.NotNull(reviewed.ReviewedAtUtc);
        Assert.Equal(before.Select(slot => !reject && (slot.Id == fixture.MorningSlotId || slot.Id == fixture.AfternoonSlotId)
            ? (slot.Id, (int?)null, SlotStatus.UNFURNISHED, slot.From, slot.To) : slot), await Slots(db));
        await Assert.ThrowsAsync<ValidationException>(() => manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id, new(), default));
    }

    [Theory]
    [InlineData("private")]
    [InlineData("foreign")]
    [InlineData("deleted")]
    [InlineData("locked")]
    [InlineData("empty")]
    [InlineData("invalid-day")]
    [InlineData("times")]
    [InlineData("open-swap")]
    public async Task RemovalRejectsIneligibleRequestsWithoutWriting(string reason)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var locks = new ManagerEditLockService();
        if (reason == "private") (await db.Schedules.SingleAsync()).PublicationStatus = SchedulePublicationStatus.Private;
        if (reason == "foreign") db.ScheduleEmployees.Remove(await db.ScheduleEmployees.SingleAsync());
        if (reason == "deleted") (await db.Employees.SingleAsync()).IsDeleted = true;
        if (reason == "locked") locks.SetLocks("fixture-lock", fixture.ManagerId, "Fixture manager",
            [ManagerEditLockTargets.Schedule(fixture.ContainerId, fixture.ScheduleId)]);
        if (reason == "open-swap") db.ShiftSwapRequests.Add(OpenSwap(fixture));
        await db.SaveChangesAsync();
        var before = await Slots(db);
        var request = Removal(fixture, reason == "empty" ? 13 : reason == "invalid-day" ? 32 : 12,
            reason == "times" ? "10:00" : null);

        await Assert.ThrowsAsync<ValidationException>(() => EmployeeController(db, fixture, locks).Create(request, default));
        Assert.Empty(await db.ShiftCorrectionRequests.ToListAsync());
        Assert.Equal(before, await Slots(db));
    }

    [Theory]
    [InlineData("changed-time")]
    [InlineData("added-shift")]
    [InlineData("open-swap")]
    [InlineData("write-failure")]
    public async Task ApprovalRejectsStaleRemovalOrRollsBackFailedWrite(string reason)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var created = await Create(db, fixture);
        if (reason == "changed-time") (await db.ScheduleSlots.FindAsync(fixture.MorningSlotId))!.ToTime = "12:30";
        if (reason == "added-shift") db.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(fixture.ScheduleId, 12, 3, fixture.EmployeeId, "20:00", "23:00"));
        if (reason == "open-swap") db.ShiftSwapRequests.Add(OpenSwap(fixture));
        await db.SaveChangesAsync();
        var before = await Slots(db);
        var manager = ManagerController(db, fixture);
        Task<ActionResult<ShiftCorrectionRequestDto>> Approve() => manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id, new(), default);

        if (reason == "write-failure")
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER reject_removal BEFORE UPDATE OF status ON shift_correction_request
                BEGIN SELECT RAISE(ABORT, 'fixture rejection'); END;
                """);
            await Assert.ThrowsAsync<DbUpdateException>(Approve);
        }
        else Assert.IsType<ConflictObjectResult>((await Approve()).Result);

        db.ChangeTracker.Clear();
        var pending = await db.ShiftCorrectionRequests.SingleAsync();
        Assert.Equal(ShiftCorrectionStatus.Pending, pending.Status);
        Assert.Null(pending.ReviewedAtUtc);
        Assert.Equal(before, await Slots(db));
        Assert.Null((await db.ScheduleCellStyles.SingleAsync()).BackgroundColorArgb);
    }

    [Fact]
    public async Task ConcurrentApproveAndRejectLeaveOneTerminalStatus()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var created = await Create(db, fixture);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var approve = Review(true);
        var reject = Review(false);
        start.SetResult();
        var reviewed = Assert.Single((await Task.WhenAll(approve, reject)).OfType<ShiftCorrectionRequestDto>());
        var stored = await db.ShiftCorrectionRequests.AsNoTracking().SingleAsync();
        Assert.Equal(reviewed.Status, stored.Status.ToString().ToLowerInvariant());
        Assert.All(await Slots(db), slot => Assert.Equal(reviewed.Status == "approved" ? null : fixture.EmployeeId, slot.Owner));

        async Task<ShiftCorrectionRequestDto?> Review(bool approving)
        {
            await start.Task;
            await using var reviewDb = database.CreateContext();
            var manager = ManagerController(reviewDb, fixture);
            try
            {
                var result = approving
                    ? await manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id, new(), default)
                    : await manager.Reject(fixture.ContainerId, fixture.ScheduleId, created.Id, default);
                if (result.Result is OkObjectResult ok) return Assert.IsType<ShiftCorrectionRequestDto>(ok.Value);
                Assert.IsType<ConflictObjectResult>(result.Result);
                return null;
            }
            catch (ValidationException error)
            {
                Assert.Contains("reviewed", error.Message, StringComparison.OrdinalIgnoreCase);
                return null;
            }
        }
    }

    private static CreateShiftCorrectionRequest Removal(Fixture fixture, int day = 12, string? from = null) => new()
    {
        ScheduleId = fixture.ScheduleId, DayOfMonth = day, RequestType = "removeShift", Comment = " Fixture note ", RequestedFromTime = from,
    };

    private static async Task<ShiftCorrectionRequestDto> Create(AppDbContext db, Fixture fixture)
        => Assert.IsType<ShiftCorrectionRequestDto>(Assert.IsType<CreatedAtActionResult>(
            (await EmployeeController(db, fixture).Create(Removal(fixture), default)).Result).Value);

    private static ShiftSwapRequestModel OpenSwap(Fixture fixture) => new()
    {
        ScheduleId = fixture.ScheduleId, ScheduleSlotId = fixture.MorningSlotId, FromEmployeeId = fixture.EmployeeId,
        Status = ShiftSwapStatus.Open, Visibility = ShiftSwapVisibility.Public, CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    private static async Task<(int Id, int? Owner, SlotStatus Status, string From, string To)[]> Slots(AppDbContext db)
        => (await db.ScheduleSlots.AsNoTracking().OrderBy(slot => slot.Id).ToListAsync())
            .Select(slot => (slot.Id, slot.EmployeeId, slot.Status, slot.FromTime, slot.ToTime)).ToArray();
}
