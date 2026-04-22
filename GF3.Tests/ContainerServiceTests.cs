using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class ContainerServiceTests
{
    [Fact]
    public async Task ContainerCrud_ValidatesUniqueness_AndDeleteGuardsScheduleReferences()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context, new FakeScheduleGenerator());

        var created = await service.CreateAsync(new ContainerModel
        {
            Name = " Main Container ",
            Note = "  Note  ",
        });

        Assert.Equal("Main Container", created.Name);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new ContainerModel { Name = "main container" }));

        var shop = TestDataFactory.CreateDalShop();
        context.Shops.Add(shop);
        await context.SaveChangesAsync();
        context.Schedules.Add(TestDataFactory.CreateDalSchedule(created.Id, shop.Id));
        await context.SaveChangesAsync();

        var deleteResult = await service.TryDeleteAsync(created.Id);
        Assert.False(deleteResult.Succeeded);
        Assert.Equal("To delete this container, first delete all graphs that belong to it.", deleteResult.Message);
    }

    [Fact]
    public async Task CreateSchedulePresetAsync_NormalizesDeduplicatesAndRejectsDuplicateName()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context, new FakeScheduleGenerator());

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee();
        context.AddRange(container, shop, employee);
        await context.SaveChangesAsync();

        var created = await service.CreateSchedulePresetAsync(container.Id, new SchedulePresetModel
        {
            Name = "  Preset A  ",
            ScheduleName = "  Base Graph  ",
            ShopId = shop.Id,
            Year = 2026,
            Month = 4,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 10,
            MaxConsecutiveDays = 2,
            MaxConsecutiveFull = 2,
            MaxFullPerMonth = 2,
            Employees =
            [
                new SchedulePresetEmployeeModel { EmployeeId = 0, MinHoursMonth = 1 },
                new SchedulePresetEmployeeModel { EmployeeId = employee.Id, MinHoursMonth = 10 },
                new SchedulePresetEmployeeModel { EmployeeId = employee.Id, MinHoursMonth = 15 },
            ]
        });

        Assert.Equal("Preset A", created.Name);
        Assert.Equal("Base Graph", created.ScheduleName);
        Assert.Single(created.Employees);
        Assert.Equal(employee.Id, created.Employees.Single().EmployeeId);
        Assert.Equal(15, created.Employees.Single().MinHoursMonth);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateSchedulePresetAsync(container.Id, new SchedulePresetModel
            {
                Name = "Preset A",
                ScheduleName = "Other",
                ShopId = shop.Id,
                Year = 2026,
                Month = 4,
                PeoplePerShift = 1,
                Shift1Time = "08:00 - 16:00",
                Shift2Time = "16:00 - 20:00",
                MaxHoursPerEmpMonth = 10,
                MaxConsecutiveDays = 1,
                MaxConsecutiveFull = 1,
                MaxFullPerMonth = 1,
            }));
    }

    [Fact]
    public async Task GenerateGraphPreviewAsync_UsesNormalizedEmployeesAndAvailability()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var generator = new FakeScheduleGenerator();
        var service = CreateService(context, generator);

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee();
        context.AddRange(container, shop, employee);
        await context.SaveChangesAsync();

        var group = new DataAccessLayer.Models.AvailabilityGroupModel { Name = "Avail", Year = 2026, Month = 4 };
        context.AvailabilityGroups.Add(group);
        await context.SaveChangesAsync();

        var result = await service.GenerateGraphPreviewAsync(
            container.Id,
            TestDataFactory.CreateScheduleModel(containerId: container.Id, shopId: shop.Id, availabilityGroupId: group.Id),
            [
                TestDataFactory.CreateScheduleEmployeeModel(employee.Id, id: 5, displayOrder: 2),
                TestDataFactory.CreateScheduleEmployeeModel(employee.Id, id: 2, displayOrder: 1),
            ],
            progress: null);

        Assert.Equal(1, generator.LastEmployeeCount);
        Assert.Equal(1, generator.LastAvailabilityCount);
        Assert.Equal(1, result.GeneratedSlotsCount);
        Assert.Equal(0, result.WrittenSlotsCount);
        Assert.Single(result.Slots);
    }

    [Fact]
    public async Task ReplaceGraphSlotsAsync_DeduplicatesEmployeesAndRebuildsSlotNumbers()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context, new FakeScheduleGenerator());

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee();
        context.AddRange(container, shop, employee);
        await context.SaveChangesAsync();

        var graph = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        context.Schedules.Add(graph);
        await context.SaveChangesAsync();

        await service.ReplaceGraphSlotsAsync(
            container.Id,
            graph.Id,
            [
                TestDataFactory.CreateScheduleSlotModel(1, 7, employee.Id, "08:00", "12:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 4, employee.Id, "08:00", "12:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 9, null, "08:00", "12:00"),
            ]);

        var stored = await service.GetGraphSlotsAsync(container.Id, graph.Id);

        Assert.NotNull(stored);
        Assert.Equal(2, stored!.Count);
        Assert.Equal([1, 2], stored.Select(slot => slot.SlotNo).ToArray());
        Assert.Equal(SlotStatus.ASSIGNED, stored[0].Status);
        Assert.Equal(SlotStatus.UNFURNISHED, stored[1].Status);
    }

    [Fact]
    public async Task GraphEmployeeAndCellStyleOperations_ValidateDuplicateEmployee_AndUpdateExistingStyle()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context, new FakeScheduleGenerator());

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee();
        context.AddRange(container, shop, employee);
        await context.SaveChangesAsync();

        var graph = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        context.Schedules.Add(graph);
        await context.SaveChangesAsync();

        var graphEmployee = await service.AddGraphEmployeeAsync(container.Id, graph.Id, new ScheduleEmployeeModel
        {
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        });

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.AddGraphEmployeeAsync(container.Id, graph.Id, new ScheduleEmployeeModel
            {
                EmployeeId = employee.Id,
                DisplayOrder = 1,
            }));

        var createdStyle = await service.UpsertGraphCellStyleAsync(container.Id, graph.Id, new ScheduleCellStyleModel
        {
            DayOfMonth = 1,
            EmployeeId = employee.Id,
            BackgroundColorArgb = 10,
            TextColorArgb = null,
        });

        var updatedStyle = await service.UpsertGraphCellStyleAsync(container.Id, graph.Id, new ScheduleCellStyleModel
        {
            DayOfMonth = 1,
            EmployeeId = employee.Id,
            BackgroundColorArgb = 20,
            TextColorArgb = 30,
        });

        var styles = await service.GetGraphCellStylesAsync(container.Id, graph.Id);
        var style = Assert.Single(styles ?? throw new InvalidOperationException("Expected a stored style."));

        Assert.NotEqual(0, graphEmployee.Id);
        Assert.Equal(createdStyle.Id, updatedStyle.Id);
        Assert.Equal(20, style.BackgroundColorArgb);
        Assert.Equal(30, style.TextColorArgb);
    }

    private static ContainerService CreateService(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        IScheduleGenerator generator)
        => new(
            new DataAccessLayer.Repositories.ContainerRepository(context),
            new DataAccessLayer.Repositories.ScheduleRepository(context),
            new DataAccessLayer.Repositories.SchedulePresetRepository(context),
            new DataAccessLayer.Repositories.ScheduleSlotRepository(context),
            new DataAccessLayer.Repositories.ScheduleEmployeeRepository(context),
            new DataAccessLayer.Repositories.ScheduleCellStyleRepository(context),
            new DataAccessLayer.Repositories.AvailabilityGroupRepository(context),
            generator);

    private sealed class FakeScheduleGenerator : IScheduleGenerator
    {
        public int LastAvailabilityCount { get; private set; }

        public int LastEmployeeCount { get; private set; }

        public Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress = null,
            CancellationToken ct = default)
        {
            LastAvailabilityCount = availabilities.Count();
            LastEmployeeCount = employees.Count();

            var employeeId = employees.FirstOrDefault()?.EmployeeId;
            IList<ScheduleSlotModel> result =
            [
                new ScheduleSlotModel
                {
                    ScheduleId = schedule.Id,
                    DayOfMonth = 1,
                    SlotNo = 1,
                    EmployeeId = employeeId,
                    Status = employeeId.HasValue ? SlotStatus.ASSIGNED : SlotStatus.UNFURNISHED,
                    FromTime = "08:00",
                    ToTime = "12:00",
                }
            ];

            return Task.FromResult(result);
        }
    }
}
