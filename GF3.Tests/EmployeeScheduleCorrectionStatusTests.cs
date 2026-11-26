using BusinessLogicLayer;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Contracts.EmployeeSchedules;
using WebApi.Contracts.ShiftCorrections;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class EmployeeScheduleCorrectionStatusTests
{
    [Fact]
    public async Task OwnScheduleProjectsSortedDistinctApprovedDaysEvenBeyondHistoryLimit()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await EmployeePublicSchedulesTests.Seed(db);
        var ownGraph = fixture.Hidden[0];
        var colleague = await db.Employees.SingleAsync(employee => employee.Id != fixture.Owner);
        var schedule = await db.Schedules.SingleAsync(item => item.Id == ownGraph);
        schedule.Month = 4;
        var oldest = Request(29);
        db.Add(oldest);
        await db.SaveChangesAsync();
        for (var index = 0; index < 100; index++) db.Add(Request(5));
        var adjustment = Request(10);
        adjustment.ScheduleSlotId = await db.ScheduleSlots.Where(slot => slot.ScheduleId == ownGraph).Select(slot => slot.Id).SingleAsync();
        adjustment.OriginalFromTime = "08:00";
        adjustment.OriginalToTime = "12:00";
        var removal = Request(3);
        removal.Operation = ShiftCorrectionOperation.RemoveShift;
        removal.RemovalSnapshotJson = """{"ContainerId":1,"Year":2026,"Month":4,"Shifts":[{"SlotId":1,"FromTime":"08:00","ToTime":"16:00"}]}""";
        removal.RequestedFromTime = null;
        removal.RequestedToTime = null;
        db.AddRange(adjustment, removal, Request(31), Request(11, ShiftCorrectionStatus.Pending), Request(12, ShiftCorrectionStatus.Rejected));
        var otherEmployee = Request(13);
        otherEmployee.EmployeeId = colleague.Id;
        var otherSchedule = Request(14);
        otherSchedule.ScheduleId = fixture.Preview;
        db.AddRange(otherEmployee, otherSchedule);
        await db.SaveChangesAsync();

        using var services = new ServiceCollection().AddLogging().AddSingleton(TimeProvider.System)
            .AddBusinessLogicStack(database.ConnectionString).BuildServiceProvider();
        using var scope = services.CreateScope();
        var context = EmployeePublicSchedulesTests.Context(fixture.Owner);
        var controller = new EmployeeSchedulesController(scope.ServiceProvider.GetRequiredService<IContainerService>())
        {
            ControllerContext = context,
        };
        var history = new EmployeeShiftCorrectionsController(db, null!, null!, null!) { ControllerContext = context };
        var historyResult = await history.GetMine(default);
        var requests = Assert.IsAssignableFrom<IReadOnlyList<ShiftCorrectionRequestDto>>(Assert.IsType<OkObjectResult>(historyResult.Result).Value);
        Assert.Equal(100, requests.Count);
        Assert.DoesNotContain(requests, request => request.Id == oldest.Id);
        var manager = new ManagerShiftCorrectionsController(db, null!, null!);
        var graphRequests = await manager.GetGraphRequests(fixture.Container, ownGraph, default);
        Assert.DoesNotContain(Assert.IsAssignableFrom<IReadOnlyList<ShiftCorrectionRequestDto>>(
            Assert.IsType<OkObjectResult>(graphRequests.Result).Value), request => request.Id == oldest.Id);
        var exact = await manager.GetGraphRequest(fixture.Container, ownGraph, oldest.Id, default);
        Assert.Equal(oldest.Id, Assert.IsType<ShiftCorrectionRequestDto>(Assert.IsType<OkObjectResult>(exact.Result).Value).Id);
        var result = await controller.GetVisible(db, default);
        var own = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EmployeeScheduleDto>>(Assert.IsType<OkObjectResult>(result.Result).Value));
        Assert.Equal(ownGraph, own.Id);
        Assert.Equal(new[] { 3, 5, 10, 29 }, own.ApprovedCorrectionDays);
        Assert.DoesNotContain(own.Slots, slot => slot.DayOfMonth == 3);

        schedule.Month = 8;
        await db.SaveChangesAsync();
        var preview = await controller.GetPublicSchedule(fixture.Preview, db, default);
        Assert.Empty(Assert.IsType<EmployeeScheduleDto>(Assert.IsType<OkObjectResult>(preview.Result).Value).ApprovedCorrectionDays);

        ShiftCorrectionRequestModel Request(int day, ShiftCorrectionStatus status = ShiftCorrectionStatus.Approved) => new()
        {
            ScheduleId = ownGraph,
            EmployeeId = fixture.Owner,
            DayOfMonth = day,
            Status = status,
            RequestedFromTime = "08:00",
            RequestedToTime = "16:00",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
