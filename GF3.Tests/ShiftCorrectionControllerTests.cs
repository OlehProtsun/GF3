using System.Security.Claims;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.ShiftCorrections;
using WebApi.Controllers;
using WebApi.Realtime;

namespace GF3.Tests;

public sealed class ShiftCorrectionControllerTests
{
    [Theory]
    [InlineData("12:00", 2026, 8, true)]
    [InlineData("12:30", 2026, 8, false)]
    [InlineData("12:00", 2026, 9, false)]
    [InlineData("12:00", 2027, 8, false)]
    public async Task Create_ChecksOtherPublishedSchedulesByCalendarDate(string otherFrom, int year, int month, bool rejects)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        await AddOtherScheduleAsync(db, fixture, otherFrom, year, month);
        var controller = new EmployeeShiftCorrectionsController(db, new ManagerEditLockService(), new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetEmployeeUser(controller, fixture.EmployeeId);
        Task<ActionResult<ShiftCorrectionRequestDto>> Create() => controller.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId, ScheduleSlotId = fixture.MorningSlotId,
            RequestedFromTime = "08:00", RequestedToTime = "12:30",
        }, CancellationToken.None);
        if (rejects)
        {
            await Assert.ThrowsAsync<ValidationException>(Create);
            Assert.False(await db.ShiftCorrectionRequests.AnyAsync());
        }
        else Assert.IsType<CreatedAtActionResult>((await Create()).Result);
    }

    [Fact]
    public async Task Approve_RechecksConflictsIntroducedAfterSubmission()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var employee = new EmployeeShiftCorrectionsController(db, new ManagerEditLockService(), new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetEmployeeUser(employee, fixture.EmployeeId);
        var created = Assert.IsType<ShiftCorrectionRequestDto>(Assert.IsType<CreatedAtActionResult>((await employee.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId, ScheduleSlotId = fixture.MorningSlotId,
            RequestedFromTime = "08:00", RequestedToTime = "12:30",
        }, CancellationToken.None)).Result).Value);
        await AddOtherScheduleAsync(db, fixture, "12:00", 2026, 8);
        var manager = new ManagerShiftCorrectionsController(db, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetManagerUser(manager, fixture.ManagerId);
        await Assert.ThrowsAsync<ValidationException>(() => manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id,
            new ApproveShiftCorrectionRequest(), CancellationToken.None));
        db.ChangeTracker.Clear();
        Assert.Equal(ShiftCorrectionStatus.Pending, (await db.ShiftCorrectionRequests.SingleAsync()).Status);
        Assert.Equal("12:00", (await db.ScheduleSlots.SingleAsync(slot => slot.Id == fixture.MorningSlotId)).ToTime);
    }

    [Fact]
    public async Task CreateAndApprove_SucceedDespitePostCommitFailures_AndRetryDoesNotApplyTwice()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var logs = System.Reflection.DispatchProxy.Create<WebApi.Services.IWorkflowLogService, FailingPostCommitProxy>();
        var notifier = System.Reflection.DispatchProxy.Create<IRealtimeNotifier, FailingPostCommitProxy>();
        var employee = new EmployeeShiftCorrectionsController(db, new ManagerEditLockService(), logs, notifier);
        SetEmployeeUser(employee, fixture.EmployeeId);
        var created = Assert.IsType<ShiftCorrectionRequestDto>(Assert.IsType<CreatedAtActionResult>((await employee.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId, ScheduleSlotId = fixture.MorningSlotId,
            RequestedFromTime = "08:00", RequestedToTime = "12:30",
        }, CancellationToken.None)).Result).Value);
        var manager = new ManagerShiftCorrectionsController(db, logs, notifier);
        SetManagerUser(manager, fixture.ManagerId);
        var result = await manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id, new ApproveShiftCorrectionRequest(), CancellationToken.None);
        Assert.Equal("approved", Assert.IsType<ShiftCorrectionRequestDto>(Assert.IsType<OkObjectResult>(result.Result).Value).Status);
        await Assert.ThrowsAsync<ValidationException>(() => manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id,
            new ApproveShiftCorrectionRequest(), CancellationToken.None));
        Assert.Single(await db.ShiftCorrectionRequests.ToListAsync());
        Assert.Equal("12:30", (await db.ScheduleSlots.SingleAsync(slot => slot.Id == fixture.MorningSlotId)).ToTime);
    }

    [Theory]
    [InlineData("legacy", null)]
    [InlineData("missing", "no longer exists")]
    [InlineData("ambiguous", "cannot be uniquely identified")]
    [InlineData("hours", "changed after")]
    [InlineData("employee", "changed after")]
    [InlineData("day", "changed after")]
    [InlineData("collision", "conflicts with another shift position")]
    [InlineData("color", "color")]
    public async Task Approve_ResolvesOnlyUniqueUnchangedSnapshot(string scenario, string? error)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var employee = new EmployeeShiftCorrectionsController(db, new ManagerEditLockService(), new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetEmployeeUser(employee, fixture.EmployeeId);
        var created = Assert.IsType<ShiftCorrectionRequestDto>(Assert.IsType<CreatedAtActionResult>((await employee.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId, ScheduleSlotId = fixture.MorningSlotId,
            RequestedFromTime = "08:00", RequestedToTime = "12:30",
        }, CancellationToken.None)).Result).Value);
        var correction = await db.ShiftCorrectionRequests.SingleAsync();
        correction.CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-2);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var original = await db.ScheduleSlots.SingleAsync(slot => slot.Id == fixture.MorningSlotId);
        if (scenario is "hours" or "employee" or "day")
        {
            if (scenario == "hours") original.ToTime = "11:30";
            if (scenario == "employee")
            {
                original.EmployeeId = null;
                original.Status = SlotStatus.UNFURNISHED;
            }
            if (scenario == "day") original.DayOfMonth = 11;
            // A matching alternative must never rescue an existing but modified original ID.
            db.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(fixture.ScheduleId, 12, 3, fixture.EmployeeId, "08:00", "12:00"));
        }
        else
        {
            db.ScheduleSlots.Remove(original);
            await db.SaveChangesAsync();
            if (scenario != "missing")
                db.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(fixture.ScheduleId, 12, 1, fixture.EmployeeId, "08:00", "12:00"));
            if (scenario == "ambiguous")
            {
                // Simulate legacy inconsistent data; the current schema forbids this duplicate.
                await db.Database.ExecuteSqlRawAsync("DROP INDEX ux_slot_unique_emp_per_time");
                db.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(fixture.ScheduleId, 12, 3, fixture.EmployeeId, "08:00", "12:00"));
            }
            if (scenario == "collision")
                db.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(fixture.ScheduleId, 12, 1, null, "08:00", "12:30"));
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var before = await db.ScheduleSlots.AsNoTracking().OrderBy(slot => slot.Id)
            .Select(slot => new { slot.Id, slot.EmployeeId, slot.DayOfMonth, slot.FromTime, slot.ToTime }).ToListAsync();
        var manager = new ManagerShiftCorrectionsController(db, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetManagerUser(manager, fixture.ManagerId);
        var approval = new ApproveShiftCorrectionRequest { HighlightColor = scenario == "color" ? "invalid" : "#DBEAFE" };
        if (error is not null)
        {
            var failure = await Assert.ThrowsAsync<ValidationException>(() => manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id, approval, CancellationToken.None));
            Assert.Contains(error, failure.Message, StringComparison.OrdinalIgnoreCase);
            db.ChangeTracker.Clear();
            var pending = await db.ShiftCorrectionRequests.AsNoTracking().SingleAsync();
            Assert.Equal(ShiftCorrectionStatus.Pending, pending.Status);
            Assert.Equal(fixture.MorningSlotId, pending.ScheduleSlotId);
            Assert.Null(pending.ReviewedAtUtc);
            Assert.Equal(before, await db.ScheduleSlots.AsNoTracking().OrderBy(slot => slot.Id)
                .Select(slot => new { slot.Id, slot.EmployeeId, slot.DayOfMonth, slot.FromTime, slot.ToTime }).ToListAsync());
            Assert.Null((await db.ScheduleCellStyles.AsNoTracking().SingleAsync()).BackgroundColorArgb);
            Assert.False(await db.ManagerShiftCorrectionSettings.AnyAsync());
        }
        else
        {
            await manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id, approval, CancellationToken.None);
            db.ChangeTracker.Clear();
            var approved = await db.ShiftCorrectionRequests.AsNoTracking().SingleAsync();
            Assert.Equal(ShiftCorrectionStatus.Approved, approved.Status);
            Assert.NotEqual(fixture.MorningSlotId, approved.ScheduleSlotId);
            Assert.NotNull(approved.ReviewedAtUtc);
            Assert.Equal(fixture.ManagerId, approved.ReviewedByManagerId);
            Assert.Equal("12:30", (await db.ScheduleSlots.AsNoTracking().SingleAsync(slot => slot.Id == approved.ScheduleSlotId)).ToTime);
            Assert.Equal("19:00", (await db.ScheduleSlots.AsNoTracking().SingleAsync(slot => slot.Id == fixture.AfternoonSlotId)).ToTime);
            Assert.Equal(unchecked((int)0xFFDBEAFE), (await db.ScheduleCellStyles.AsNoTracking().SingleAsync()).BackgroundColorArgb);
        }
    }

    [Fact]
    public async Task Create_BlocksLogicalDuplicateWithStaleId_ButAllowsDifferentShifts()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var employee = new EmployeeShiftCorrectionsController(db, new ManagerEditLockService(), new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetEmployeeUser(employee, fixture.EmployeeId);
        var request = new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId, ScheduleSlotId = fixture.MorningSlotId,
            RequestedFromTime = "08:00", RequestedToTime = "12:30",
        };
        await employee.Create(request, CancellationToken.None);
        db.ChangeTracker.Clear();
        await db.ScheduleSlots.Where(slot => slot.Id == fixture.MorningSlotId).ExecuteDeleteAsync();
        var replacement = TestDataFactory.CreateDalSlot(fixture.ScheduleId, 12, 1, fixture.EmployeeId, "08:00", "12:00");
        var anotherDay = TestDataFactory.CreateDalSlot(fixture.ScheduleId, 13, 1, fixture.EmployeeId, "08:00", "12:00");
        db.ScheduleSlots.AddRange(replacement, anotherDay);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ValidationException>(() => employee.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId, ScheduleSlotId = replacement.Id,
            RequestedFromTime = "08:00", RequestedToTime = "12:30",
        }, CancellationToken.None));
        await employee.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId, ScheduleSlotId = fixture.AfternoonSlotId,
            RequestedFromTime = "15:00", RequestedToTime = "19:30",
        }, CancellationToken.None);
        await employee.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId, ScheduleSlotId = anotherDay.Id,
            RequestedFromTime = "08:00", RequestedToTime = "12:30",
        }, CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.Equal(3, await db.ShiftCorrectionRequests.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Approve_RejectsResolvedRequestsInvalidManagerAndWrongSchedule()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var employee = new EmployeeShiftCorrectionsController(db, new ManagerEditLockService(), new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetEmployeeUser(employee, fixture.EmployeeId);
        var created = Assert.IsType<ShiftCorrectionRequestDto>(Assert.IsType<CreatedAtActionResult>((await employee.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId, ScheduleSlotId = fixture.MorningSlotId,
            RequestedFromTime = "08:00", RequestedToTime = "12:30",
        }, CancellationToken.None)).Result).Value);
        var manager = new ManagerShiftCorrectionsController(db, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetManagerUser(manager, 0);
        await Assert.ThrowsAsync<BadHttpRequestException>(() => manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id, new(), CancellationToken.None));
        db.ChangeTracker.Clear();
        SetManagerUser(manager, fixture.ManagerId);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => manager.Approve(fixture.ContainerId + 1, fixture.ScheduleId, created.Id, new(), CancellationToken.None));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => manager.Approve(fixture.ContainerId, fixture.ScheduleId + 1, created.Id, new(), CancellationToken.None));
        await manager.Reject(fixture.ContainerId, fixture.ScheduleId, created.Id, CancellationToken.None);
        await Assert.ThrowsAsync<ValidationException>(() => manager.Approve(fixture.ContainerId, fixture.ScheduleId, created.Id, new(), CancellationToken.None));
        db.ChangeTracker.Clear();
        Assert.Equal("12:00", (await db.ScheduleSlots.AsNoTracking().SingleAsync(slot => slot.Id == fixture.MorningSlotId)).ToTime);
        Assert.Equal(ShiftCorrectionStatus.Rejected, (await db.ShiftCorrectionRequests.AsNoTracking().SingleAsync()).Status);
    }

    public class FailingPostCommitProxy : System.Reflection.DispatchProxy
    {
        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
            => throw new InvalidOperationException("Injected post-commit outage");
    }

    private static async Task AddOtherScheduleAsync(DataAccessLayer.Models.DataBaseContext.AppDbContext db, Fixture fixture, string from, int year, int month)
    {
        var original = await db.Schedules.SingleAsync(schedule => schedule.Id == fixture.ScheduleId);
        var other = TestDataFactory.CreateDalSchedule(fixture.ContainerId, original.ShopId, year: year, month: month);
        other.PublicationStatus = SchedulePublicationStatus.Public;
        db.Schedules.Add(other);
        await db.SaveChangesAsync();
        db.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(other.Id, 12, 1, fixture.EmployeeId, from, "14:00"));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAndApprove_SplitShiftUpdatesOnlyRequestedSlotAndMarksCell()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var lockService = new ManagerEditLockService();

        var employeeController = new EmployeeShiftCorrectionsController(
            db, lockService, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetEmployeeUser(employeeController, fixture.EmployeeId);

        var createResult = await employeeController.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.MorningSlotId,
            RequestedFromTime = "08:00",
            RequestedToTime = "12:30",
        }, CancellationToken.None);
        var created = Assert.IsType<ShiftCorrectionRequestDto>(
            Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);
        Assert.Equal("pending", created.Status);

        var repository = new DataAccessLayer.Repositories.ScheduleSlotRepository(db);
        var unchangedSlots = await repository.GetByScheduleAsync(fixture.ScheduleId);
        await repository.ReplaceForScheduleAsync(fixture.ScheduleId, unchangedSlots, overwrite: true);
        Assert.Equal(new[] { fixture.MorningSlotId, fixture.AfternoonSlotId },
            (await repository.GetByScheduleAsync(fixture.ScheduleId)).Select(slot => slot.Id));

        var employeeListResult = await employeeController.GetMine(CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftCorrectionRequestDto>>(
            Assert.IsType<OkObjectResult>(employeeListResult.Result).Value)).Id);

        var managerController = new ManagerShiftCorrectionsController(
            db, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetManagerUser(managerController, fixture.ManagerId);
        var managerListResult = await managerController.GetGraphRequests(
            fixture.ContainerId, fixture.ScheduleId, CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftCorrectionRequestDto>>(
            Assert.IsType<OkObjectResult>(managerListResult.Result).Value)).Id);
        var approveResult = await managerController.Approve(
            fixture.ContainerId,
            fixture.ScheduleId,
            created.Id,
            new ApproveShiftCorrectionRequest { HighlightColor = "#FDE68A" },
            CancellationToken.None);

        var approved = Assert.IsType<ShiftCorrectionRequestDto>(Assert.IsType<OkObjectResult>(approveResult.Result).Value);
        Assert.Equal("approved", approved.Status);
        db.ChangeTracker.Clear();
        var morning = await db.ScheduleSlots.AsNoTracking().SingleAsync(slot => slot.Id == fixture.MorningSlotId);
        var afternoon = await db.ScheduleSlots.AsNoTracking().SingleAsync(slot => slot.Id == fixture.AfternoonSlotId);
        var style = await db.ScheduleCellStyles.AsNoTracking().SingleAsync(item =>
            item.ScheduleId == fixture.ScheduleId && item.EmployeeId == fixture.EmployeeId && item.DayOfMonth == 12);
        Assert.Equal("08:00", morning.FromTime);
        Assert.Equal("12:30", morning.ToTime);
        Assert.Equal(("15:00", "19:00"), (afternoon.FromTime, afternoon.ToTime));
        Assert.Equal(unchecked((int)0xFFFDE68A), style.BackgroundColorArgb);
        Assert.Equal(-16777216, style.TextColorArgb);
        Assert.Equal("#FDE68A", (await db.ManagerShiftCorrectionSettings.AsNoTracking().SingleAsync()).HighlightColor);
    }

    [Fact]
    public async Task Create_OverlappingSplitShiftIsRejectedAndDoesNotCreateRequest()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await SeedAsync(db);
        var controller = new EmployeeShiftCorrectionsController(
            db, new ManagerEditLockService(), new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetEmployeeUser(controller, fixture.EmployeeId);

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(new CreateShiftCorrectionRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.MorningSlotId,
            RequestedFromTime = "08:00",
            RequestedToTime = "16:00",
        }, CancellationToken.None));

        Assert.False(await db.ShiftCorrectionRequests.AnyAsync());
    }

    private static async Task<Fixture> SeedAsync(DataAccessLayer.Models.DataBaseContext.AppDbContext db)
    {
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee("Zoe", "Young", email: "zoe-correction@example.com");
        var now = DateTimeOffset.UtcNow;
        var manager = new ManagerAccountModel
        {
            Username = $"manager-{Guid.NewGuid():N}",
            DisplayName = "Ada Manager",
            PasswordHash = "test",
            PasswordUpdatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.AddRange(container, shop, employee, manager);
        await db.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, name: "August Schedule", year: 2026, month: 8);
        schedule.PublicationStatus = SchedulePublicationStatus.Public;
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync();
        db.ScheduleEmployees.Add(new ScheduleEmployeeModel
        {
            ScheduleId = schedule.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
            MinHoursMonth = 80,
        });
        var morning = TestDataFactory.CreateDalSlot(schedule.Id, 12, 1, employee.Id, "08:00", "12:00");
        var afternoon = TestDataFactory.CreateDalSlot(schedule.Id, 12, 2, employee.Id, "15:00", "19:00");
        db.ScheduleSlots.AddRange(morning, afternoon);
        db.ScheduleCellStyles.Add(new ScheduleCellStyleModel
        {
            ScheduleId = schedule.Id,
            EmployeeId = employee.Id,
            DayOfMonth = 12,
            TextColorArgb = -16777216,
        });
        await db.SaveChangesAsync();
        return new Fixture(container.Id, schedule.Id, employee.Id, manager.Id, morning.Id, afternoon.Id);
    }

    private static void SetEmployeeUser(ControllerBase controller, int employeeId) => SetUser(controller,
        AuthRoles.Employee, "employee_id", employeeId, $"employee-{employeeId}");

    private static void SetManagerUser(ControllerBase controller, int managerId) => SetUser(controller,
        AuthRoles.Manager, "manager_id", managerId, $"manager-{managerId}");

    private static void SetUser(ControllerBase controller, string role, string idClaim, int id, string name)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.Name, name),
                    new Claim(ClaimTypes.Role, role),
                    new Claim(idClaim, id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ], JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role)),
            },
        };
    }

    private sealed record Fixture(int ContainerId, int ScheduleId, int EmployeeId, int ManagerId, int MorningSlotId, int AfternoonSlotId);
}
