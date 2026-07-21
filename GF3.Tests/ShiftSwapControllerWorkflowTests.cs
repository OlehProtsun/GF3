using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Controllers;
using WebApi.Realtime;

namespace GF3.Tests;

public sealed class ShiftSwapControllerWorkflowTests
{
    [Fact]
    public async Task EmployeeShiftSwaps_GetEmployees_ExcludesCurrentEmployeeAndSortsByName()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var thirdEmployee = TestDataFactory.CreateDalEmployee("Amy", "Able", email: "amy@example.com");
        context.Employees.Add(thirdEmployee);
        await context.SaveChangesAsync();
        var controller = new EmployeeShiftSwapsController(
            context,
            new ManagerEditLockService(),
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(controller, fixture.OwnerEmployeeId);

        var result = await controller.GetEmployees(CancellationToken.None);

        var employees = Assert.IsAssignableFrom<IEnumerable<ShiftSwapEmployeeDto>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).ToList();
        Assert.Equal([thirdEmployee.Id, fixture.TargetEmployeeId], employees.Select(employee => employee.Id));
        Assert.DoesNotContain(employees, employee => employee.Id == fixture.OwnerEmployeeId);
        Assert.Equal(["Amy Able", "Target Worker"], employees.Select(employee => employee.DisplayName));
    }

    [Fact]
    public async Task EmployeeShiftSwaps_CreateVisibleAndAcceptPartialSwap_UpdateSlotsAndDtos()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var lockService = new ManagerEditLockService();

        var ownerController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(ownerController, fixture.OwnerEmployeeId);

        var createResult = await ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
            FromTime = "10:00",
            ToTime = "14:00",
        }, CancellationToken.None);

        var createdDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);
        Assert.Equal("open", createdDto.Status);
        Assert.Equal("public", createdDto.Visibility);
        Assert.True(createdDto.CanCancel);
        Assert.False(createdDto.CanAccept);
        Assert.Equal(4, createdDto.ShiftHours);
        Assert.Equal(8, createdDto.CurrentEmployeeHoursBefore);
        Assert.Equal(4, createdDto.CurrentEmployeeHoursAfter);

        var targetController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(targetController, fixture.TargetEmployeeId);

        var visibleResult = await targetController.GetVisible(CancellationToken.None);
        var visibleDto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(visibleResult.Result).Value));
        Assert.True(visibleDto.CanAccept);
        Assert.False(visibleDto.CanCancel);
        Assert.Equal(4, visibleDto.CurrentEmployeeHoursAfter);

        var acceptResult = await targetController.Accept(createdDto.Id, CancellationToken.None);
        var acceptedDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>(acceptResult.Result).Value);

        var slots = await context.ScheduleSlots
            .Where(slot => slot.ScheduleId == fixture.ScheduleId)
            .OrderBy(slot => slot.FromTime)
            .ToListAsync();
        var swap = await context.ShiftSwapRequests.SingleAsync();

        Assert.Equal("accepted", acceptedDto.Status);
        Assert.Equal(fixture.TargetEmployeeId, acceptedDto.AcceptedByEmployeeId);
        Assert.False(acceptedDto.CanAccept);
        Assert.False(acceptedDto.CanCancel);
        Assert.Equal(ShiftSwapStatus.Accepted, swap.Status);
        Assert.Equal(fixture.TargetEmployeeId, swap.AcceptedByEmployeeId);
        Assert.Equal(
            [
                (fixture.OwnerEmployeeId, "08:00", "10:00"),
                (fixture.TargetEmployeeId, "10:00", "14:00"),
                (fixture.OwnerEmployeeId, "14:00", "16:00"),
            ],
            slots.Select(slot => (slot.EmployeeId.GetValueOrDefault(), slot.FromTime, slot.ToTime)));

        var history = await context.ShiftSwapHistories.SingleAsync();
        Assert.Equal(createdDto.Id, history.SourceShiftSwapRequestId);
        Assert.Contains("\"employeeName\":\"Owner Worker\"", history.BeforeSnapshotJson, StringComparison.Ordinal);
        Assert.Contains("\"employeeName\":\"Target Worker\"", history.AfterSnapshotJson, StringComparison.Ordinal);

        var containerLogController = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(containerLogController, managerId: 3);
        var containerLogResult = await containerLogController.GetContainerSwaps(
            fixture.ContainerId,
            CancellationToken.None);
        var containerLogEntry = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(containerLogResult.Result).Value));
        Assert.Equal(createdDto.Id, containerLogEntry.Id);
        Assert.Equal("accepted", containerLogEntry.Status);
        Assert.NotNull(containerLogEntry.BeforeSnapshot);
        Assert.NotNull(containerLogEntry.AfterSnapshot);

        var replacementSlots = await context.ScheduleSlots
            .AsNoTracking()
            .Where(item => item.ScheduleId == fixture.ScheduleId)
            .Select(item => new ScheduleSlotModel
            {
                DayOfMonth = item.DayOfMonth,
                SlotNo = item.SlotNo,
                EmployeeId = item.EmployeeId,
                Status = item.Status,
                FromTime = item.FromTime,
                ToTime = item.ToTime,
            })
            .ToListAsync();
        var repository = new ScheduleSlotRepository(context);
        await repository.ReplaceForScheduleAsync(fixture.ScheduleId, replacementSlots, overwrite: true);

        Assert.False(await context.ShiftSwapRequests.AnyAsync());
        Assert.Single(await context.ShiftSwapHistories.ToListAsync());

        var logController = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(logController, managerId: 3);

        var logResult = await logController.GetGraphLog(
            fixture.ContainerId,
            fixture.ScheduleId,
            CancellationToken.None);
        var logEntry = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(logResult.Result).Value));

        Assert.Equal("accepted", logEntry.Status);
        Assert.Equal(createdDto.Id, logEntry.Id);
        Assert.NotEmpty(Assert.IsType<ShiftSwapScheduleSnapshotDto>(logEntry.BeforeSnapshot).Rows);
        Assert.NotEmpty(Assert.IsType<ShiftSwapScheduleSnapshotDto>(logEntry.AfterSnapshot).Rows);
    }

    [Fact]
    public async Task ShiftSwapLogsController_Container_DeleteAcceptedSwapRemovesRequestAndHistory()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var lockService = new ManagerEditLockService();
        var ownerController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(ownerController, fixture.OwnerEmployeeId);
        var createResult = await ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
        }, CancellationToken.None);
        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);

        var targetController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(targetController, fixture.TargetEmployeeId);
        await targetController.Accept(created.Id, CancellationToken.None);

        var managerController = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(managerController, managerId: 3);
        var deleteResult = await managerController.DeleteContainerSwap(
            fixture.ContainerId,
            created.Id,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(deleteResult);
        Assert.False(await context.ShiftSwapRequests.AnyAsync(item => item.Id == created.Id));
        Assert.False(await context.ShiftSwapHistories.AnyAsync(item => item.SourceShiftSwapRequestId == created.Id));
    }

    [Fact]
    public async Task EmployeeShiftSwaps_CreatePrivateOfferForEmployeeOutsideSchedule_AddsEmployeeWhenAccepted()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var outsider = TestDataFactory.CreateDalEmployee("Private", "Receiver", email: "private@example.com");
        context.Employees.Add(outsider);
        await context.SaveChangesAsync();
        var lockService = new ManagerEditLockService();
        var ownerController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(ownerController, fixture.OwnerEmployeeId);

        var createResult = await ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
            TargetEmployeeId = outsider.Id,
        }, CancellationToken.None);

        var createdDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);
        Assert.Equal("private", createdDto.Visibility);
        Assert.Equal(outsider.Id, createdDto.TargetEmployeeId);
        Assert.Equal("Private Receiver", createdDto.TargetEmployeeName);

        var unrelatedController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(unrelatedController, fixture.TargetEmployeeId);
        var unrelatedVisibleResult = await unrelatedController.GetVisible(CancellationToken.None);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(unrelatedVisibleResult.Result).Value));

        var outsiderController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(outsiderController, outsider.Id);
        var visibleResult = await outsiderController.GetVisible(CancellationToken.None);
        var visibleDto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(visibleResult.Result).Value));
        Assert.True(visibleDto.CanAccept);
        Assert.Equal(0, visibleDto.CurrentEmployeeHoursBefore);
        Assert.Equal(8, visibleDto.CurrentEmployeeHoursAfter);

        var acceptResult = await outsiderController.Accept(createdDto.Id, CancellationToken.None);
        var acceptedDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>(acceptResult.Result).Value);
        var slot = await context.ScheduleSlots.SingleAsync(slot => slot.Id == fixture.OwnerSlotId);
        var scheduleEmployees = await context.ScheduleEmployees
            .Where(employee => employee.ScheduleId == fixture.ScheduleId)
            .OrderBy(employee => employee.DisplayOrder)
            .ToListAsync();

        Assert.Equal("accepted", acceptedDto.Status);
        Assert.Equal(outsider.Id, slot.EmployeeId);
        Assert.Equal([fixture.OwnerEmployeeId, fixture.TargetEmployeeId, outsider.Id], scheduleEmployees.Select(employee => employee.EmployeeId));
        Assert.Equal([0, 1, 2], scheduleEmployees.Select(employee => employee.DisplayOrder));
    }

    [Fact]
    public async Task EmployeeShiftSwaps_CreateRejectsInvalidSessionScheduleSlotOwnershipAndTarget()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var targetSlot = TestDataFactory.CreateDalSlot(
            fixture.ScheduleId,
            dayOfMonth: 2,
            slotNo: 1,
            fixture.TargetEmployeeId,
            "08:00",
            "12:00");
        context.ScheduleSlots.Add(targetSlot);
        await context.SaveChangesAsync();
        var controller = new EmployeeShiftSwapsController(
            context,
            new ManagerEditLockService(),
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(controller, fixture.OwnerEmployeeId);
        var invalidSessionController = new EmployeeShiftSwapsController(
            context,
            new ManagerEditLockService(),
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetUser(invalidSessionController, new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "employee")
        ], JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role)));

        var invalidSession = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            invalidSessionController.Create(new CreateEmployeeShiftSwapRequest(), CancellationToken.None));
        var missingSchedule = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = 999,
                ScheduleSlotId = fixture.OwnerSlotId,
            }, CancellationToken.None));
        var missingSlot = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = fixture.ScheduleId,
                ScheduleSlotId = 999,
            }, CancellationToken.None));
        var notOwner = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = fixture.ScheduleId,
                ScheduleSlotId = targetSlot.Id,
            }, CancellationToken.None));
        var selfTarget = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = fixture.ScheduleId,
                ScheduleSlotId = fixture.OwnerSlotId,
                TargetEmployeeId = fixture.OwnerEmployeeId,
            }, CancellationToken.None));
        var missingTarget = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = fixture.ScheduleId,
                ScheduleSlotId = fixture.OwnerSlotId,
                TargetEmployeeId = 999,
            }, CancellationToken.None));

        Assert.Equal("The current employee session is invalid.", invalidSession.Message);
        Assert.Equal(["Published schedule was not found for your account."], missingSchedule.Errors["scheduleId"]);
        Assert.Equal(["Shift was not found in this schedule."], missingSlot.Errors[nameof(CreateEmployeeShiftSwapRequest.ScheduleSlotId)]);
        Assert.Equal(["You can only offer your own assigned shift."], notOwner.Errors[nameof(CreateEmployeeShiftSwapRequest.ScheduleSlotId)]);
        Assert.Equal(["Choose another employee or publish this swap for everyone."], selfTarget.Errors[nameof(CreateEmployeeShiftSwapRequest.TargetEmployeeId)]);
        Assert.Equal(["The selected employee was not found."], missingTarget.Errors[nameof(CreateEmployeeShiftSwapRequest.TargetEmployeeId)]);
    }

    [Fact]
    public async Task EmployeeShiftSwaps_CreateRejectsBadTimeTextAndOutsidePeriod()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var controller = new EmployeeShiftSwapsController(
            context,
            new ManagerEditLockService(),
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(controller, fixture.OwnerEmployeeId);

        var badTime = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = fixture.ScheduleId,
                ScheduleSlotId = fixture.OwnerSlotId,
                FromTime = "bad",
                ToTime = "10:00",
            }, CancellationToken.None));
        var outsidePeriod = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = fixture.ScheduleId,
                ScheduleSlotId = fixture.OwnerSlotId,
                FromTime = "07:00",
                ToTime = "09:00",
            }, CancellationToken.None));

        Assert.Equal(["Use HH:mm time format."], badTime.Errors[nameof(CreateEmployeeShiftSwapRequest.FromTime)]);
        Assert.Equal("The offered period must stay inside the selected shift.", outsidePeriod.Message);
    }

    [Fact]
    public async Task EmployeeShiftSwaps_CancelRequiresOwnerAndMarksOpenOfferCancelled()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var lockService = new ManagerEditLockService();
        var ownerController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(ownerController, fixture.OwnerEmployeeId);
        var createResult = await ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
        }, CancellationToken.None);
        var createdDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);

        var targetController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(targetController, fixture.TargetEmployeeId);
        var notOwnerException = await Assert.ThrowsAsync<ValidationException>(() =>
            targetController.Cancel(createdDto.Id, CancellationToken.None));

        var cancelResult = await ownerController.Cancel(createdDto.Id, CancellationToken.None);
        var cancelledDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>(cancelResult.Result).Value);

        Assert.Equal("Only the employee who opened this swap can cancel it.", notOwnerException.Message);
        Assert.Equal("cancelled", cancelledDto.Status);
        Assert.False(cancelledDto.CanCancel);
        Assert.Equal(ShiftSwapStatus.Cancelled, (await context.ShiftSwapRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task EmployeeShiftSwaps_RejectsDuplicateOpenOfferAndOverlappingAcceptance()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(
            fixture.ScheduleId,
            dayOfMonth: 1,
            slotNo: 2,
            fixture.TargetEmployeeId,
            "11:00",
            "12:00"));
        await context.SaveChangesAsync();
        var lockService = new ManagerEditLockService();
        var ownerController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(ownerController, fixture.OwnerEmployeeId);

        var createResult = await ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
            FromTime = "10:00",
            ToTime = "14:00",
        }, CancellationToken.None);
        var createdDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);

        var duplicateException = await Assert.ThrowsAsync<ValidationException>(() =>
            ownerController.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = fixture.ScheduleId,
                ScheduleSlotId = fixture.OwnerSlotId,
            }, CancellationToken.None));

        var targetController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(targetController, fixture.TargetEmployeeId);
        var visibleResult = await targetController.GetVisible(CancellationToken.None);
        var visibleDto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(visibleResult.Result).Value));
        var overlapException = await Assert.ThrowsAsync<ValidationException>(() =>
            targetController.Accept(createdDto.Id, CancellationToken.None));

        Assert.False(visibleDto.CanAccept);
        Assert.Equal("You already work during this time.", visibleDto.AcceptanceUnavailableReason);

        Assert.Equal(["This shift already has an open swap offer."], duplicateException.Errors[nameof(CreateEmployeeShiftSwapRequest.ScheduleSlotId)]);
        Assert.Equal("You already work during this time.", overlapException.Message);
    }

    [Fact]
    public async Task EmployeeShiftSwaps_GetVisible_AggregatesMonthAndBlocksOverlapAcrossSchedules()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var secondShop = TestDataFactory.CreateDalShop("Second Shop");
        context.Shops.Add(secondShop);
        await context.SaveChangesAsync();
        var secondSchedule = TestDataFactory.CreateDalSchedule(
            fixture.ContainerId,
            secondShop.Id,
            name: "Second May Schedule",
            year: 2026,
            month: 5);
        secondSchedule.PublicationStatus = SchedulePublicationStatus.Public;
        context.Schedules.Add(secondSchedule);
        await context.SaveChangesAsync();
        context.ScheduleEmployees.Add(new ScheduleEmployeeModel
        {
            ScheduleId = secondSchedule.Id,
            EmployeeId = fixture.TargetEmployeeId,
            DisplayOrder = 0,
            MinHoursMonth = 80,
        });
        context.ScheduleSlots.AddRange(
            TestDataFactory.CreateDalSlot(secondSchedule.Id, 1, 1, fixture.TargetEmployeeId, "11:00", "12:00"),
            TestDataFactory.CreateDalSlot(secondSchedule.Id, 2, 1, fixture.TargetEmployeeId, "17:00", "20:00"));
        await context.SaveChangesAsync();

        var lockService = new ManagerEditLockService();
        var ownerController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(ownerController, fixture.OwnerEmployeeId);
        var createResult = await ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
            FromTime = "10:00",
            ToTime = "14:00",
        }, CancellationToken.None);
        var createdDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);

        var targetController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(targetController, fixture.TargetEmployeeId);
        var visibleResult = await targetController.GetVisible(CancellationToken.None);
        var visibleDto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(visibleResult.Result).Value));

        Assert.Equal(createdDto.Id, visibleDto.Id);
        Assert.False(visibleDto.CanAccept);
        Assert.Equal("You already work during this time.", visibleDto.AcceptanceUnavailableReason);
        Assert.Equal(4, visibleDto.CurrentEmployeeHoursBefore);
        Assert.Equal(4, visibleDto.CurrentEmployeeHoursAfter);
        Assert.Equal(2, visibleDto.CurrentEmployeeWorkDaysBefore);
        Assert.Equal(2, visibleDto.CurrentEmployeeWorkDaysAfter);
        Assert.Equal(29, visibleDto.CurrentEmployeeFreeDaysBefore);
        Assert.Equal(29, visibleDto.CurrentEmployeeFreeDaysAfter);
    }

    [Fact]
    public async Task EmployeeShiftSwaps_GetVisible_MarksOpenOffersLocked_WhenScheduleEditLockActive()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var lockService = new ManagerEditLockService();

        var ownerController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(ownerController, fixture.OwnerEmployeeId);
        var createResult = await ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
        }, CancellationToken.None);
        var createdDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);

        ((IScheduleEditLockService)lockService).SetLocks(
            "manager-connection",
            "Manager",
            [new ScheduleEditLockTarget(fixture.ContainerId, fixture.ScheduleId)]);
        var targetController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(targetController, fixture.TargetEmployeeId);

        var visibleResult = await targetController.GetVisible(CancellationToken.None);

        var visibleDto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(visibleResult.Result).Value));
        Assert.False(createdDto.IsScheduleLocked);
        Assert.Equal(createdDto.Id, visibleDto.Id);
        Assert.True(visibleDto.IsScheduleLocked);
        Assert.False(visibleDto.CanAccept);
    }

    [Fact]
    public async Task ShiftSwaps_DisabledScheduleRejectsEmployeeCreateAcceptAndManagerManualOffer()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var lockService = new ManagerEditLockService();
        var ownerController = new EmployeeShiftSwapsController(context, lockService, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetEmployeeUser(ownerController, fixture.OwnerEmployeeId);

        var createResult = await ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
        }, CancellationToken.None);
        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);

        var schedule = await context.Schedules.SingleAsync(item => item.Id == fixture.ScheduleId);
        schedule.AllowSwap = false;
        await context.SaveChangesAsync();

        var createBlocked = await Assert.ThrowsAsync<ValidationException>(() => ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
        }, CancellationToken.None));

        var targetController = new EmployeeShiftSwapsController(context, lockService, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetEmployeeUser(targetController, fixture.TargetEmployeeId);
        var acceptBlocked = await Assert.ThrowsAsync<ValidationException>(() => targetController.Accept(created.Id, CancellationToken.None));

        var managerController = new ShiftSwapLogsController(context, new NoopWorkflowLogService(), new NoopRealtimeNotifier());
        SetManagerUser(managerController, managerId: 3);
        var manualBlocked = await Assert.ThrowsAsync<ValidationException>(() => managerController.CreateManualOffer(
            fixture.ContainerId,
            fixture.ScheduleId,
            new CreateManagerShiftSwapRequest { ManualColumnId = 2, DayOfMonth = 2, FromTime = "09:00", ToTime = "13:00" },
            CancellationToken.None));

        Assert.Equal("Swaps are not allowed for this schedule.", createBlocked.Message);
        Assert.Equal("Swaps are not allowed for this schedule.", acceptBlocked.Message);
        Assert.Equal("Swaps are not allowed for this schedule.", manualBlocked.Message);
    }

    [Fact]
    public async Task ShiftSwaps_AreScopedBySharedContainerAcrossScheduleMonths()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var baseSchedule = await context.Schedules.SingleAsync(schedule => schedule.Id == fixture.ScheduleId);

        var sharedSchedule = TestDataFactory.CreateDalSchedule(
            fixture.ContainerId,
            baseSchedule.ShopId,
            name: "June schedule in shared container",
            year: 2026,
            month: 6);
        sharedSchedule.PublicationStatus = SchedulePublicationStatus.Public;

        var otherContainer = TestDataFactory.CreateDalContainer("Other container");
        var otherShop = TestDataFactory.CreateDalShop("Other shop");
        var otherEmployee = TestDataFactory.CreateDalEmployee("Other", "Worker", email: "other@example.com");
        context.AddRange(sharedSchedule, otherContainer, otherShop, otherEmployee);
        await context.SaveChangesAsync();

        var otherSchedule = TestDataFactory.CreateDalSchedule(
            otherContainer.Id,
            otherShop.Id,
            name: "Other container schedule",
            year: 2026,
            month: 7);
        otherSchedule.PublicationStatus = SchedulePublicationStatus.Public;
        context.Schedules.Add(otherSchedule);
        await context.SaveChangesAsync();

        var sharedSlot = TestDataFactory.CreateDalSlot(sharedSchedule.Id, 3, 1, fixture.OwnerEmployeeId, "09:00", "13:00");
        var otherSlot = TestDataFactory.CreateDalSlot(otherSchedule.Id, 4, 1, otherEmployee.Id, "09:00", "13:00");
        context.ScheduleEmployees.AddRange(
            new ScheduleEmployeeModel
            {
                ScheduleId = sharedSchedule.Id,
                EmployeeId = fixture.OwnerEmployeeId,
                DisplayOrder = 0,
            },
            new ScheduleEmployeeModel
            {
                ScheduleId = otherSchedule.Id,
                EmployeeId = otherEmployee.Id,
                DisplayOrder = 0,
            });
        context.ScheduleSlots.AddRange(sharedSlot, otherSlot);
        await context.SaveChangesAsync();

        var lockService = new ManagerEditLockService();
        var ownerController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(ownerController, fixture.OwnerEmployeeId);
        var sharedCreateResult = await ownerController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = sharedSchedule.Id,
            ScheduleSlotId = sharedSlot.Id,
        }, CancellationToken.None);
        var sharedSwap = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(sharedCreateResult.Result).Value);

        var otherController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(otherController, otherEmployee.Id);
        var otherCreateResult = await otherController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = otherSchedule.Id,
            ScheduleSlotId = otherSlot.Id,
        }, CancellationToken.None);
        var otherSwap = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(otherCreateResult.Result).Value);

        var targetController = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(targetController, fixture.TargetEmployeeId);
        var visibleResult = await targetController.GetVisible(CancellationToken.None);
        var visibleSwap = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(visibleResult.Result).Value));

        Assert.Equal(sharedSwap.Id, visibleSwap.Id);
        Assert.True(visibleSwap.CanAccept);
        var otherContainerError = await Assert.ThrowsAsync<ValidationException>(() =>
            targetController.Accept(otherSwap.Id, CancellationToken.None));
        Assert.Equal("This swap belongs to another container.", otherContainerError.Message);

        var managerController = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(managerController, managerId: 3);
        var managerResult = await managerController.GetContainerSwaps(fixture.ContainerId, CancellationToken.None);
        var managerSwap = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(managerResult.Result).Value));
        Assert.Equal(sharedSwap.Id, managerSwap.Id);
    }

    [Fact]
    public async Task EmployeeShiftSwaps_RejectsLockedSchedulesAndInvalidPeriods()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var lockService = new ManagerEditLockService();
        ((IScheduleEditLockService)lockService).SetLocks(
            "manager-connection",
            "Manager",
            [new ScheduleEditLockTarget(fixture.ContainerId, fixture.ScheduleId)]);

        var controller = new EmployeeShiftSwapsController(
            context,
            lockService,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(controller, fixture.OwnerEmployeeId);

        var lockedException = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = fixture.ScheduleId,
                ScheduleSlotId = fixture.OwnerSlotId,
                FromTime = "10:00",
                ToTime = "12:00",
            }, CancellationToken.None));

        ((IScheduleEditLockService)lockService).ReleaseConnection("manager-connection");
        var invalidPeriodException = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.Create(new CreateEmployeeShiftSwapRequest
            {
                ScheduleId = fixture.ScheduleId,
                ScheduleSlotId = fixture.OwnerSlotId,
                FromTime = "15:00",
                ToTime = "10:00",
            }, CancellationToken.None));

        Assert.Equal("This schedule is being edited by a manager. Try again after the manager saves changes.", lockedException.Message);
        Assert.Equal(["The end time must be after the start time."], invalidPeriodException.Errors[nameof(CreateEmployeeShiftSwapRequest.ToTime)]);
    }

    [Fact]
    public async Task ShiftSwapLogsController_CreateListAndCancelManualOffers()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context, note: "Planner note\n\n<!--GF3_GRAPH_META:{\"m\":[[2,\"Open counter\",{\"5\":\"09:00\"}]]}-->");
        var controller = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(controller, managerId: 3);

        var createResult = await controller.CreateManualOffer(
            fixture.ContainerId,
            fixture.ScheduleId,
            new CreateManagerShiftSwapRequest
            {
                ManualColumnId = 2,
                DayOfMonth = 5,
                FromTime = "9:00",
                ToTime = "13:30",
            },
            CancellationToken.None);

        var createdDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);
        Assert.True(createdDto.IsManagerCreated);
        Assert.Equal("Open counter", createdDto.ManualColumnName);
        Assert.Equal("Open counter", createdDto.FromEmployeeName);
        Assert.Equal("09:00", createdDto.FromTime);
        Assert.Equal("13:30", createdDto.ToTime);
        Assert.Equal(4.5, createdDto.ShiftHours);
        Assert.True(createdDto.CanCancel);

        var logResult = await controller.GetGraphLog(fixture.ContainerId, fixture.ScheduleId, CancellationToken.None);
        var logDto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(logResult.Result).Value));
        Assert.Equal(createdDto.Id, logDto.Id);

        var cancelResult = await controller.CancelManualOffer(
            fixture.ContainerId,
            fixture.ScheduleId,
            createdDto.Id,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(cancelResult);
        Assert.False(await context.ShiftSwapRequests.AnyAsync());
        Assert.DoesNotContain(await context.ScheduleSlots.ToListAsync(), slot => slot.Id == createdDto.ScheduleSlotId);
    }

    [Fact]
    public async Task ShiftSwapLogsController_CreateManualOfferRejectsDuplicateDayAndMissingTarget()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context, note: "Planner note\n\n<!--GF3_GRAPH_META:{\"m\":[[2,\"Open counter\",{\"5\":\"09:00\",\"6\":\"10:00\"}]]}-->");
        var controller = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(controller, managerId: 3);

        await controller.CreateManualOffer(
            fixture.ContainerId,
            fixture.ScheduleId,
            new CreateManagerShiftSwapRequest
            {
                ManualColumnId = 2,
                DayOfMonth = 5,
                FromTime = "09:00",
                ToTime = "13:00",
            },
            CancellationToken.None);

        var duplicate = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.CreateManualOffer(
                fixture.ContainerId,
                fixture.ScheduleId,
                new CreateManagerShiftSwapRequest
                {
                    ManualColumnId = 2,
                    DayOfMonth = 5,
                    FromTime = "10:00",
                    ToTime = "14:00",
                },
                CancellationToken.None));
        var missingTarget = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.CreateManualOffer(
                fixture.ContainerId,
                fixture.ScheduleId,
                new CreateManagerShiftSwapRequest
                {
                    ManualColumnId = 2,
                    DayOfMonth = 6,
                    FromTime = "10:00",
                    ToTime = "14:00",
                    TargetEmployeeId = 999,
                },
                CancellationToken.None));

        Assert.Equal(["This manual shift already has an open swap offer."], duplicate.Errors[nameof(CreateManagerShiftSwapRequest.DayOfMonth)]);
        Assert.Equal(["The selected employee was not found."], missingTarget.Errors[nameof(CreateManagerShiftSwapRequest.TargetEmployeeId)]);
        Assert.Single(await context.ShiftSwapRequests.ToListAsync());
    }

    [Fact]
    public async Task EmployeeShiftSwaps_AcceptManagerCreatedOffer_AssignsSlotAndRemovesManualCell()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(
            context,
            note: "Planner note\n\n<!--GF3_GRAPH_META:{\"m\":[[2,\"Open counter\",{\"5\":\"09:00\"}]]}-->");
        var managerController = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(managerController, managerId: 3);
        var createResult = await managerController.CreateManualOffer(
            fixture.ContainerId,
            fixture.ScheduleId,
            new CreateManagerShiftSwapRequest
            {
                ManualColumnId = 2,
                DayOfMonth = 5,
                FromTime = "09:00",
                ToTime = "13:00",
            },
            CancellationToken.None);
        var createdDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);

        var employeeController = new EmployeeShiftSwapsController(
            context,
            new ManagerEditLockService(),
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(employeeController, fixture.TargetEmployeeId);
        var acceptResult = await employeeController.Accept(createdDto.Id, CancellationToken.None);

        var acceptedDto = Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>(acceptResult.Result).Value);
        var slot = await context.ScheduleSlots.SingleAsync(item => item.Id == createdDto.ScheduleSlotId);
        var schedule = await context.Schedules.SingleAsync(item => item.Id == fixture.ScheduleId);

        Assert.Equal("accepted", acceptedDto.Status);
        Assert.Equal(fixture.TargetEmployeeId, slot.EmployeeId);
        Assert.Equal(SlotStatus.ASSIGNED, slot.Status);
        Assert.Contains("b64:", schedule.Note, StringComparison.Ordinal);
        Assert.DoesNotContain("\"5\"", DecodeGraphMetaSuffix(schedule.Note!));
    }

    [Fact]
    public async Task ShiftSwapLogsController_ValidatesManualOfferInputsAndMissingGraph()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var controller = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(controller, managerId: 3);

        var missingManualColumn = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.CreateManualOffer(
                fixture.ContainerId,
                fixture.ScheduleId,
                new CreateManagerShiftSwapRequest
                {
                    ManualColumnId = 0,
                    DayOfMonth = 1,
                    FromTime = "09:00",
                    ToTime = "13:00",
                },
                CancellationToken.None));

        var invalidDay = await Assert.ThrowsAsync<ValidationException>(() =>
            controller.CreateManualOffer(
                fixture.ContainerId,
                fixture.ScheduleId,
                new CreateManagerShiftSwapRequest
                {
                    ManualColumnId = 2,
                    DayOfMonth = 32,
                    FromTime = "09:00",
                    ToTime = "13:00",
                },
                CancellationToken.None));

        var missingGraph = await controller.GetGraphLog(fixture.ContainerId, graphId: 999, CancellationToken.None);

        Assert.Equal(["Choose a manual column shift."], missingManualColumn.Errors[nameof(CreateManagerShiftSwapRequest.ManualColumnId)]);
        Assert.Equal(["Choose a valid day in this schedule month."], invalidDay.Errors[nameof(CreateManagerShiftSwapRequest.DayOfMonth)]);
        var notFound = Assert.IsType<NotFoundObjectResult>(missingGraph.Result);
        Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<ProblemDetails>(notFound.Value).Status);
    }

    [Fact]
    public async Task ShiftSwapLogsController_Container_CanListCancelAndDeleteEmployeeOfferWithoutDeletingSlot()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context);
        var employeeController = new EmployeeShiftSwapsController(
            context,
            new ManagerEditLockService(),
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetEmployeeUser(employeeController, fixture.OwnerEmployeeId);
        var createResult = await employeeController.Create(new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = fixture.ScheduleId,
            ScheduleSlotId = fixture.OwnerSlotId,
            FromTime = "10:00",
            ToTime = "14:00",
            TargetEmployeeId = fixture.TargetEmployeeId,
        }, CancellationToken.None);
        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);

        var managerController = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(managerController, managerId: 3);

        var listResult = await managerController.GetContainerSwaps(fixture.ContainerId, CancellationToken.None);
        var listed = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(
            Assert.IsType<OkObjectResult>(listResult.Result).Value));
        Assert.Equal(created.Id, listed.Id);
        Assert.True(listed.CanCancel);
        Assert.Equal("Target Worker", listed.TargetEmployeeName);

        var cancelResult = await managerController.CancelContainerSwap(
            fixture.ContainerId,
            created.Id,
            CancellationToken.None);
        var cancelled = Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>(cancelResult.Result).Value);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.True(await context.ScheduleSlots.AnyAsync(slot => slot.Id == fixture.OwnerSlotId));

        var deleteResult = await managerController.DeleteContainerSwap(
            fixture.ContainerId,
            created.Id,
            CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);
        Assert.False(await context.ShiftSwapRequests.AnyAsync(request => request.Id == created.Id));
        Assert.True(await context.ScheduleSlots.AnyAsync(slot => slot.Id == fixture.OwnerSlotId));
    }

    [Fact]
    public async Task ShiftSwapLogsController_Container_CancellingManagerOfferRemovesItsManualSlot()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var fixture = await SeedPublishedScheduleAsync(context, note: "Planner note\n\n<!--GF3_GRAPH_META:{\"m\":[[2,\"Open counter\",{\"5\":\"09:00\"}]]}-->");
        var controller = new ShiftSwapLogsController(
            context,
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(controller, managerId: 3);
        var createResult = await controller.CreateManualOffer(
            fixture.ContainerId,
            fixture.ScheduleId,
            new CreateManagerShiftSwapRequest
            {
                ManualColumnId = 2,
                DayOfMonth = 5,
                FromTime = "09:00",
                ToTime = "13:00",
            },
            CancellationToken.None);
        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);

        var cancelResult = await controller.CancelContainerSwap(
            fixture.ContainerId,
            created.Id,
            CancellationToken.None);

        Assert.IsType<ShiftSwapDto>(Assert.IsType<OkObjectResult>(cancelResult.Result).Value);
        Assert.False(await context.ShiftSwapRequests.AnyAsync(request => request.Id == created.Id));
        Assert.False(await context.ScheduleSlots.AnyAsync(slot => slot.Id == created.ScheduleSlotId));
    }

    private static async Task<ScheduleFixture> SeedPublishedScheduleAsync(
        AppDbContext context,
        string? note = null)
    {
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var owner = TestDataFactory.CreateDalEmployee("Owner", "Worker", email: "owner@example.com");
        var target = TestDataFactory.CreateDalEmployee("Target", "Worker", email: "target@example.com");
        context.AddRange(container, shop, owner, target);
        await context.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, name: "May Schedule", year: 2026, month: 5);
        schedule.PublicationStatus = SchedulePublicationStatus.Public;
        schedule.Note = note;
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        context.ScheduleEmployees.AddRange(
            new ScheduleEmployeeModel
            {
                ScheduleId = schedule.Id,
                EmployeeId = owner.Id,
                DisplayOrder = 0,
                MinHoursMonth = 80,
            },
            new ScheduleEmployeeModel
            {
                ScheduleId = schedule.Id,
                EmployeeId = target.Id,
                DisplayOrder = 1,
                MinHoursMonth = 80,
            });
        var ownerSlot = TestDataFactory.CreateDalSlot(schedule.Id, dayOfMonth: 1, slotNo: 1, owner.Id, "08:00", "16:00");
        context.ScheduleSlots.Add(ownerSlot);
        await context.SaveChangesAsync();

        return new ScheduleFixture(container.Id, schedule.Id, owner.Id, target.Id, ownerSlot.Id);
    }

    private static void SetEmployeeUser(ControllerBase controller, int employeeId)
        => SetUser(controller, new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, $"employee-{employeeId}"),
            new Claim(ClaimTypes.Role, AuthRoles.Employee),
            new Claim("employee_id", employeeId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        ], JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role)));

    private static void SetManagerUser(ControllerBase controller, int managerId)
        => SetUser(controller, new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, $"manager-{managerId}"),
            new Claim(ClaimTypes.Role, AuthRoles.Manager),
            new Claim("manager_id", managerId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        ], JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role)));

    private static void SetUser(ControllerBase controller, ClaimsPrincipal user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user,
            },
        };
    }

    private static string DecodeGraphMetaSuffix(string note)
    {
        var match = Regex.Match(note, @"\[\[GF3_GRAPH_META:b64:([\s\S]*?)\]\]$");
        Assert.True(match.Success);
        var value = match.Groups[1].Value
            .Replace('-', '+')
            .Replace('_', '/')
            .PadRight((int)Math.Ceiling(match.Groups[1].Value.Length / 4d) * 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }

    private sealed record ScheduleFixture(
        int ContainerId,
        int ScheduleId,
        int OwnerEmployeeId,
        int TargetEmployeeId,
        int OwnerSlotId);
}
