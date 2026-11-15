using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using WebApi.Auth;
using WebApi.Services;

namespace GF3.Tests;

public sealed class ScheduleLastUpdateServiceTests
{
    [Fact]
    public async Task GetLastUpdatesAsync_CombinesManagerLogsAndAcceptedSwapsWithoutSchemaChanges()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var firstContainer = TestDataFactory.CreateDalContainer("First Container");
        var secondContainer = TestDataFactory.CreateDalContainer("Second Container");
        var firstShop = TestDataFactory.CreateDalShop("First Shop");
        var secondShop = TestDataFactory.CreateDalShop("Second Shop");
        var employee = TestDataFactory.CreateDalEmployee("Anna", "Worker");
        context.AddRange(firstContainer, secondContainer, firstShop, secondShop, employee);
        await context.SaveChangesAsync();

        var firstSchedule = TestDataFactory.CreateDalSchedule(
            firstContainer.Id,
            firstShop.Id,
            "June Schedule",
            year: 2026,
            month: 6);
        var secondSchedule = TestDataFactory.CreateDalSchedule(
            secondContainer.Id,
            secondShop.Id,
            "June Schedule",
            year: 2026,
            month: 6);
        context.Schedules.AddRange(firstSchedule, secondSchedule);
        await context.SaveChangesAsync();

        var firstSlot = TestDataFactory.CreateDalSlot(
            firstSchedule.Id,
            dayOfMonth: 28,
            slotNo: 1,
            employee.Id,
            fromTime: "08:00",
            toTime: "16:00");
        context.ScheduleSlots.Add(firstSlot);
        await context.SaveChangesAsync();

        var firstManagerUpdate = new DateTimeOffset(2026, 6, 28, 8, 0, 0, TimeSpan.Zero);
        var secondManagerUpdate = firstManagerUpdate.AddHours(1);
        var acceptedSwapUpdate = secondManagerUpdate.AddHours(1);
        context.WorkflowLogEntries.AddRange(
            CreateManagerLog(
                firstManagerUpdate,
                "Updated schedule \"June Schedule\" for June 2026 at shop \"First Shop\" in container \"First Container\"."),
            CreateManagerLog(
                secondManagerUpdate,
                $"Saved schedule matrix for schedule #{secondSchedule.Id}."));
        context.ShiftSwapRequests.Add(new ShiftSwapRequestModel
        {
            ScheduleId = firstSchedule.Id,
            ScheduleSlotId = firstSlot.Id,
            FromEmployeeId = employee.Id,
            AcceptedByEmployeeId = employee.Id,
            IsManagerCreated = false,
            Visibility = ShiftSwapVisibility.Public,
            Status = ShiftSwapStatus.Accepted,
            CreatedAtUtc = firstManagerUpdate,
            AcceptedAtUtc = acceptedSwapUpdate,
        });
        await context.SaveChangesAsync();

        var service = new ScheduleLastUpdateService(context);
        var result = await service.GetLastUpdatesAsync([firstSchedule.Id, secondSchedule.Id]);

        Assert.Equal(acceptedSwapUpdate, result[firstSchedule.Id]);
        Assert.Equal(secondManagerUpdate, result[secondSchedule.Id]);
        Assert.Empty(await service.GetLastUpdatesAsync([]));
    }

    private static WorkflowLogEntryModel CreateManagerLog(DateTimeOffset occurredAtUtc, string action)
        => new()
        {
            OccurredAtUtc = occurredAtUtc,
            ActorRole = AuthRoles.Manager,
            ActorName = "Manager",
            Action = action,
        };
}
