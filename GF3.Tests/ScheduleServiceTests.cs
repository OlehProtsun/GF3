using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class ScheduleServiceTests
{
    [Fact]
    public async Task SaveWithDetailsAsync_NormalizesAndReplacesNestedCollections()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee1 = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        var employee2 = TestDataFactory.CreateDalEmployee("Bob", "Smith");
        var availabilityGroup = new DataAccessLayer.Models.AvailabilityGroupModel { Name = "Group", Year = 2026, Month = 4 };
        context.AddRange(container, shop, employee1, employee2, availabilityGroup);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var schedule = TestDataFactory.CreateScheduleModel(containerId: container.Id, shopId: shop.Id, availabilityGroupId: availabilityGroup.Id);
        schedule.Note = "  Important note  ";
        schedule.Shift1Time = "8:00–16:00";
        schedule.Shift2Time = "16:00-20:00";

        await service.SaveWithDetailsAsync(
            schedule,
            [TestDataFactory.CreateScheduleEmployeeModel(employee1.Id, displayOrder: 3)],
            [TestDataFactory.CreateScheduleSlotModel(1, 1, employee1.Id, "08:00", "12:00")],
            [
                new ScheduleCellStyleModel { DayOfMonth = 1, EmployeeId = employee1.Id, BackgroundColorArgb = null, TextColorArgb = null },
                new ScheduleCellStyleModel { DayOfMonth = 2, EmployeeId = employee1.Id, BackgroundColorArgb = 10, TextColorArgb = null },
            ]);

        var persisted = Assert.Single(await service.GetAllAsync());
        schedule.Id = persisted.Id;

        await service.SaveWithDetailsAsync(
            schedule,
            [TestDataFactory.CreateScheduleEmployeeModel(employee2.Id, displayOrder: 1)],
            [TestDataFactory.CreateScheduleSlotModel(2, 1, employee2.Id, "12:00", "16:00")],
            [new ScheduleCellStyleModel { DayOfMonth = 3, EmployeeId = employee2.Id, BackgroundColorArgb = 22, TextColorArgb = 33 }]);

        var detailed = await service.GetDetailedAsync(persisted.Id);

        Assert.NotNull(detailed);
        Assert.Equal("Important note", detailed!.Note);
        Assert.Equal("08:00 - 16:00", detailed.Shift1Time);
        Assert.Equal("16:00 - 20:00", detailed.Shift2Time);
        Assert.Single(detailed.Employees);
        Assert.Equal(employee2.Id, detailed.Employees.Single().EmployeeId);
        Assert.Single(detailed.Slots);
        Assert.Equal(2, detailed.Slots.Single().DayOfMonth);
        Assert.Single(detailed.CellStyles);
        Assert.Equal(3, detailed.CellStyles.Single().DayOfMonth);
    }

    [Fact]
    public async Task SaveWithDetailsAsync_RequiresAvailabilityGroupAndSlots()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var schedule = TestDataFactory.CreateScheduleModel(availabilityGroupId: null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SaveWithDetailsAsync(
                schedule,
                employees: [],
                slots: [],
                cellStyles: []));

        Assert.Equal(
            "You can't save a schedule until something has been generated. Please run generation first.",
            exception.Message);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidShiftFormat()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var schedule = TestDataFactory.CreateScheduleModel();
        schedule.Shift1Time = "08:00";

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(schedule));

        Assert.Equal("Shift1 format must be HH:mm - HH:mm.", exception.Message);
    }

    private static ScheduleService CreateService(DataAccessLayer.Models.DataBaseContext.AppDbContext context)
        => new(
            new DataAccessLayer.Repositories.ScheduleRepository(context),
            new DataAccessLayer.Repositories.ScheduleEmployeeRepository(context),
            new DataAccessLayer.Repositories.ScheduleSlotRepository(context),
            new DataAccessLayer.Repositories.ScheduleCellStyleRepository(context));
}
