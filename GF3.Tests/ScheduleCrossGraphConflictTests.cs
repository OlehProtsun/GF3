using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class ScheduleCrossGraphConflictTests
{
    [Fact]
    public async Task GeneratePreview_BlocksOverlapFromSavedSchedule_ButAllowsTouchingShift()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer("Main");
        var shop = TestDataFactory.CreateDalShop("Shop");
        var employee = TestDataFactory.CreateDalEmployee("Opr", "Employee");
        context.AddRange(container, shop, employee);
        await context.SaveChangesAsync();

        var savedSchedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Saved", year: 2026, month: 7);
        savedSchedule.Shift1Time = "09:00 - 15:00";
        savedSchedule.Shift2Time = "15:00 - 21:00";
        context.Schedules.Add(savedSchedule);
        await context.SaveChangesAsync();
        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(
            savedSchedule.Id,
            dayOfMonth: 1,
            slotNo: 1,
            employeeId: employee.Id,
            fromTime: "09:00",
            toTime: "15:00"));
        await context.SaveChangesAsync();

        var preview = TestDataFactory.CreateScheduleModel(containerId: container.Id, shopId: shop.Id, availabilityGroupId: null);
        preview.Id = 0;
        preview.Year = 2026;
        preview.Month = 7;
        preview.PeoplePerShift = 1;
        preview.Shift1Time = "09:00 - 15:00";
        preview.Shift2Time = "15:00 - 21:00";
        preview.MaxHoursPerEmpMonth = 400;
        preview.MaxConsecutiveDays = 31;
        preview.MaxConsecutiveFull = 31;
        preview.MaxFullPerMonth = 31;

        var service = new ContainerService(
            new ContainerRepository(context),
            new ScheduleRepository(context),
            new SchedulePresetRepository(context),
            new ScheduleSlotRepository(context),
            new ScheduleEmployeeRepository(context),
            new ScheduleCellStyleRepository(context),
            new AvailabilityGroupRepository(context),
            new ScheduleGenerator());

        var result = await service.GenerateGraphPreviewAsync(
            container.Id,
            preview,
            [new ScheduleEmployeeModel { EmployeeId = employee.Id, MinHoursMonth = 0 }],
            progress: null);
        var employeeDayOneSlots = result.Slots
            .Where(slot => slot.DayOfMonth == 1 && slot.EmployeeId == employee.Id && slot.Status == SlotStatus.ASSIGNED)
            .ToList();

        Assert.NotEmpty(employeeDayOneSlots);
        Assert.All(employeeDayOneSlots, slot => Assert.True(
            TimeSpan.Parse(slot.FromTime) >= TimeSpan.FromHours(15),
            $"Generated overlapping slot {slot.FromTime}-{slot.ToTime}."));
        Assert.Contains(employeeDayOneSlots, slot => slot.FromTime == "15:00" && slot.ToTime == "21:00");
    }
}
