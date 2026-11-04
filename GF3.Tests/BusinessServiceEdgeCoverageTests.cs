using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using GF3.Tests.Infrastructure;
using DalModels = DataAccessLayer.Models;

namespace GF3.Tests;

public sealed class BusinessServiceEdgeCoverageTests
{
    [Fact]
    public async Task ContainerService_GraphCrudAndLookupOperations_HandleOwnershipAndNullPaths()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateContainerService(context, new ConfigurableScheduleGenerator());

        var container = TestDataFactory.CreateDalContainer("Main Container");
        var otherContainer = TestDataFactory.CreateDalContainer("Other Container");
        var shop = TestDataFactory.CreateDalShop();
        context.AddRange(container, otherContainer, shop);
        await context.SaveChangesAsync();

        var created = await service.CreateGraphAsync(
            container.Id,
            TestDataFactory.CreateScheduleModel(
                containerId: 0,
                shopId: shop.Id,
                name: "April Graph",
                availabilityGroupId: null));

        var graphs = await service.GetGraphsAsync(container.Id);
        var byId = await service.GetGraphByIdAsync(container.Id, created.Id);
        var missingContainerGraphs = await service.GetGraphsAsync(9999);
        var wrongOwnerGraph = await service.GetGraphByIdAsync(otherContainer.Id, created.Id);
        var missingPresets = await service.GetSchedulePresetsAsync(9999);

        Assert.Single(graphs ?? throw new InvalidOperationException("Expected graph list."));
        Assert.NotNull(byId);
        Assert.Equal(container.Id, byId!.ContainerId);
        Assert.Null(missingContainerGraphs);
        Assert.Null(wrongOwnerGraph);
        Assert.Null(missingPresets);

        created.Name = "Updated Graph";
        created.Month = 5;
        await service.UpdateGraphAsync(container.Id, created.Id, created);

        var updated = await service.GetGraphByIdAsync(container.Id, created.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Graph", updated!.Name);
        Assert.Equal(5, updated.Month);

        var ownershipException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateGraphAsync(otherContainer.Id, created.Id, created));
        Assert.Equal("Graph not found in container", ownershipException.Message);

        var missingContainerException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateGraphAsync(
                9999,
                TestDataFactory.CreateScheduleModel(containerId: 0, shopId: shop.Id, availabilityGroupId: null)));
        Assert.Equal("Container with id 9999 was not found.", missingContainerException.Message);

        await service.DeleteGraphAsync(container.Id, created.Id);
        Assert.Null(await service.GetGraphByIdAsync(container.Id, created.Id));
    }

    [Fact]
    public async Task ContainerService_GraphSlotOperations_RejectDuplicateAndOwnershipMismatch()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateContainerService(context, new ConfigurableScheduleGenerator());

        var container = TestDataFactory.CreateDalContainer("Main Container");
        var otherContainer = TestDataFactory.CreateDalContainer("Other Container");
        var shop = TestDataFactory.CreateDalShop();
        context.AddRange(container, otherContainer, shop);
        await context.SaveChangesAsync();

        var graph = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, availabilityGroupId: null);
        var otherGraph = TestDataFactory.CreateDalSchedule(otherContainer.Id, shop.Id, name: "Other Graph", availabilityGroupId: null);
        context.Schedules.AddRange(graph, otherGraph);
        await context.SaveChangesAsync();

        var createdSlot = await service.CreateGraphSlotAsync(container.Id, graph.Id, new ScheduleSlotModel
        {
            DayOfMonth = 1,
            SlotNo = 1,
            EmployeeId = null,
            Status = SlotStatus.UNFURNISHED,
            FromTime = "08:00",
            ToTime = "12:00",
        });

        var duplicateException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateGraphSlotAsync(container.Id, graph.Id, new ScheduleSlotModel
            {
                DayOfMonth = 1,
                SlotNo = 1,
                EmployeeId = null,
                Status = SlotStatus.UNFURNISHED,
                FromTime = "08:00",
                ToTime = "12:00",
            }));
        Assert.Equal("Duplicate slot for the same time/day", duplicateException.Message);

        await service.UpdateGraphSlotAsync(container.Id, graph.Id, createdSlot.Id, new ScheduleSlotModel
        {
            DayOfMonth = 1,
            SlotNo = 1,
            EmployeeId = null,
            Status = SlotStatus.UNFURNISHED,
            FromTime = "09:00",
            ToTime = "13:00",
        });

        var slots = await service.GetGraphSlotsAsync(container.Id, graph.Id);
        Assert.NotNull(slots);
        Assert.Single(slots!);
        Assert.Equal("09:00", slots[0].FromTime);
        Assert.Equal("13:00", slots[0].ToTime);
        Assert.Null(await service.GetGraphSlotsAsync(otherContainer.Id, graph.Id));

        var otherSlot = await service.CreateGraphSlotAsync(otherContainer.Id, otherGraph.Id, new ScheduleSlotModel
        {
            DayOfMonth = 2,
            SlotNo = 1,
            EmployeeId = null,
            Status = SlotStatus.UNFURNISHED,
            FromTime = "10:00",
            ToTime = "14:00",
        });

        var wrongOwnerException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateGraphSlotAsync(container.Id, graph.Id, otherSlot.Id, new ScheduleSlotModel
            {
                DayOfMonth = 2,
                SlotNo = 1,
                EmployeeId = null,
                Status = SlotStatus.UNFURNISHED,
                FromTime = "10:00",
                ToTime = "14:00",
            }));
        Assert.Equal("Slot not found in graph.", wrongOwnerException.Message);

        await service.DeleteGraphSlotAsync(container.Id, graph.Id, createdSlot.Id);
        var emptySlots = await service.GetGraphSlotsAsync(container.Id, graph.Id);
        Assert.NotNull(emptySlots);
        Assert.Empty(emptySlots!);
    }

    [Fact]
    public async Task ContainerService_GenerateGraphAsync_StoresSlots_AndRejectsDuplicateWritesOrMissingAvailability()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var generator = new ConfigurableScheduleGenerator
        {
            SlotsToReturn =
            [
                new ScheduleSlotModel
                {
                    DayOfMonth = 1,
                    SlotNo = 1,
                    EmployeeId = 1,
                    Status = SlotStatus.ASSIGNED,
                    FromTime = "08:00",
                    ToTime = "12:00",
                }
            ]
        };
        var service = CreateContainerService(context, generator);

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee();
        var availabilityGroup = new DalModels.AvailabilityGroupModel { Name = "Avail", Year = 2026, Month = 4 };
        context.AddRange(container, shop, employee, availabilityGroup);
        await context.SaveChangesAsync();

        var graph = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, availabilityGroupId: availabilityGroup.Id);
        context.Schedules.Add(graph);
        await context.SaveChangesAsync();
        context.ScheduleEmployees.Add(new DalModels.ScheduleEmployeeModel
        {
            ScheduleId = graph.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
            MinHoursMonth = 80,
        });
        await context.SaveChangesAsync();

        generator.SlotsToReturn =
        [
            new ScheduleSlotModel
            {
                DayOfMonth = 1,
                SlotNo = 1,
                EmployeeId = employee.Id,
                Status = SlotStatus.ASSIGNED,
                FromTime = "08:00",
                ToTime = "12:00",
            }
        ];

        var generated = await service.GenerateGraphAsync(container.Id, graph.Id, overwrite: true, dryRun: false, progress: null);

        Assert.Equal(1, generated.GeneratedSlotsCount);
        Assert.True(generated.WrittenSlotsCount > 0);
        Assert.Equal(1, generator.LastAvailabilityCount);
        Assert.Equal(1, generator.LastEmployeeCount);
        var storedSlots = await service.GetGraphSlotsAsync(container.Id, graph.Id);
        Assert.NotNull(storedSlots);
        Assert.Single(storedSlots!);

        var duplicateException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GenerateGraphAsync(container.Id, graph.Id, overwrite: false, dryRun: false, progress: null));
        Assert.Equal("Duplicate slot constraint violated. Try overwrite=true.", duplicateException.Message);

        var missingAvailabilityException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GenerateGraphPreviewAsync(
                container.Id,
                TestDataFactory.CreateScheduleModel(containerId: container.Id, shopId: shop.Id, availabilityGroupId: 999),
                [new ScheduleEmployeeModel { EmployeeId = employee.Id, DisplayOrder = 0 }],
                progress: null));
        Assert.Equal("Availability group with id 999 was not found.", missingAvailabilityException.Message);
    }

    [Fact]
    public async Task ContainerService_PreviewReplacementEmployeeAndStylePaths_ValidateErrorsAndOwnership()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateContainerService(context, new ConfigurableScheduleGenerator());

        var container = TestDataFactory.CreateDalContainer("Main Container");
        var otherContainer = TestDataFactory.CreateDalContainer("Other Container");
        var shop = TestDataFactory.CreateDalShop();
        var employee1 = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        var employee2 = TestDataFactory.CreateDalEmployee("Bob", "Smith");
        context.AddRange(container, otherContainer, shop, employee1, employee2);
        await context.SaveChangesAsync();

        var graph = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, availabilityGroupId: null);
        var otherGraph = TestDataFactory.CreateDalSchedule(otherContainer.Id, shop.Id, name: "Other Graph", availabilityGroupId: null);
        context.Schedules.AddRange(graph, otherGraph);
        await context.SaveChangesAsync();

        var missingContainerPreviewException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GenerateGraphPreviewAsync(
                9999,
                TestDataFactory.CreateScheduleModel(containerId: 9999, shopId: shop.Id, availabilityGroupId: null),
                [],
                progress: null));
        Assert.Equal("Container with id 9999 was not found.", missingContainerPreviewException.Message);

        var replacementException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ReplaceGraphSlotsAsync(
                container.Id,
                graph.Id,
                [
                    new ScheduleSlotModel
                    {
                        DayOfMonth = 1,
                        SlotNo = 1,
                        EmployeeId = null,
                        Status = SlotStatus.UNFURNISHED,
                        FromTime = "12:00",
                        ToTime = "08:00",
                    }
                ]));
        Assert.Equal("Could not save schedule slots because the database rejected the slot set.", replacementException.Message);
        context.ChangeTracker.Clear();

        var graphEmployee1 = await service.AddGraphEmployeeAsync(container.Id, graph.Id, new ScheduleEmployeeModel
        {
            EmployeeId = employee1.Id,
            DisplayOrder = 2,
        });
        var graphEmployee2 = await service.AddGraphEmployeeAsync(container.Id, graph.Id, new ScheduleEmployeeModel
        {
            EmployeeId = employee2.Id,
            DisplayOrder = 1,
        });

        var graphEmployees = await service.GetGraphEmployeesAsync(container.Id, graph.Id);
        Assert.NotNull(graphEmployees);
        Assert.Equal([employee2.Id, employee1.Id], graphEmployees!.Select(x => x.EmployeeId).ToArray());
        Assert.Null(await service.GetGraphEmployeesAsync(otherContainer.Id, graph.Id));

        await service.UpdateGraphEmployeeAsync(container.Id, graph.Id, graphEmployee1.Id, new ScheduleEmployeeModel
        {
            EmployeeId = employee1.Id,
            DisplayOrder = 0,
        });

        var duplicateEmployeeException = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateGraphEmployeeAsync(container.Id, graph.Id, graphEmployee2.Id, new ScheduleEmployeeModel
            {
                EmployeeId = employee1.Id,
                DisplayOrder = 3,
            }));
        Assert.Equal("This employee is already added to the graph.", duplicateEmployeeException.Message);

        var otherGraphEmployee = await service.AddGraphEmployeeAsync(otherContainer.Id, otherGraph.Id, new ScheduleEmployeeModel
        {
            EmployeeId = employee1.Id,
            DisplayOrder = 0,
        });

        var wrongGraphEmployeeException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RemoveGraphEmployeeAsync(container.Id, graph.Id, otherGraphEmployee.Id));
        Assert.Equal("Graph employee not found in graph.", wrongGraphEmployeeException.Message);

        var style = await service.UpsertGraphCellStyleAsync(container.Id, graph.Id, new ScheduleCellStyleModel
        {
            DayOfMonth = 1,
            EmployeeId = employee1.Id,
            BackgroundColorArgb = 10,
            TextColorArgb = 20,
        });
        var otherStyle = await service.UpsertGraphCellStyleAsync(otherContainer.Id, otherGraph.Id, new ScheduleCellStyleModel
        {
            DayOfMonth = 1,
            EmployeeId = employee1.Id,
            BackgroundColorArgb = 30,
            TextColorArgb = 40,
        });

        Assert.Null(await service.GetGraphCellStylesAsync(otherContainer.Id, graph.Id));

        var wrongStyleException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.DeleteGraphCellStyleAsync(container.Id, graph.Id, otherStyle.Id));
        Assert.Equal("Cell style not found in graph.", wrongStyleException.Message);

        await service.DeleteGraphCellStyleAsync(container.Id, graph.Id, style.Id);
        var remainingStyles = await service.GetGraphCellStylesAsync(container.Id, graph.Id);
        Assert.NotNull(remainingStyles);
        Assert.Empty(remainingStyles!);

        await service.RemoveGraphEmployeeAsync(container.Id, graph.Id, graphEmployee1.Id);
        await service.RemoveGraphEmployeeAsync(container.Id, graph.Id, graphEmployee2.Id);
        var remainingEmployees = await service.GetGraphEmployeesAsync(container.Id, graph.Id);
        Assert.NotNull(remainingEmployees);
        Assert.Empty(remainingEmployees!);
    }

    [Fact]
    public async Task ScheduleService_CrudQueryAndDelete_NormalizeSearchAndReturnNullForMissingDetails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateScheduleService(context);

        var container = TestDataFactory.CreateDalContainer("Main Container");
        var shop = TestDataFactory.CreateDalShop("Mega Shop");
        context.AddRange(container, shop);
        await context.SaveChangesAsync();

        var created = await service.CreateAsync(new ScheduleModel
        {
            ContainerId = container.Id,
            ShopId = shop.Id,
            Name = "April Graph",
            Year = 2026,
            Month = 4,
            PeoplePerShift = 1,
            Shift1Time = "8:00-16:00",
            Shift2Time = "16:00—20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            AvailabilityGroupId = null,
            Note = "  Dashboard note  ",
        });

        Assert.Equal("08:00 - 16:00", created.Shift1Time);
        Assert.Equal("16:00 - 20:00", created.Shift2Time);
        Assert.Equal("Dashboard note", created.Note);

        created.Note = "  Updated note  ";
        created.Month = 5;
        created.Shift1Time = "9:00 - 17:00";
        created.Shift2Time = "17:00 - 21:00";
        await service.UpdateAsync(created);

        var byContainer = await service.GetByContainerAsync(container.Id, "updated");
        var byValue = await service.GetByValueAsync("main container");
        var byMonth = await service.GetByValueAsync("5");
        var detailed = await service.GetDetailedAsync(created.Id);

        Assert.Single(byContainer);
        Assert.Single(byValue);
        Assert.Single(byMonth);
        Assert.NotNull(detailed);
        Assert.Equal("Updated note", detailed!.Note);
        Assert.Equal("09:00 - 17:00", detailed.Shift1Time);
        Assert.Equal("17:00 - 21:00", detailed.Shift2Time);
        Assert.Null(await service.GetDetailedAsync(9999));

        await service.DeleteAsync(created.Id);
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task ScheduleService_Create_RejectsMissingAndReversedShiftValues()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateScheduleService(context);

        var missingShift2 = TestDataFactory.CreateScheduleModel(availabilityGroupId: null);
        missingShift2.Shift2Time = " ";

        var missingShift2Exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(missingShift2));
        Assert.Equal("Shift2 is required.", missingShift2Exception.Message);

        var reversedShift = TestDataFactory.CreateScheduleModel(availabilityGroupId: null);
        reversedShift.Shift1Time = "16:00 - 08:00";

        var reversedShiftException = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(reversedShift));
        Assert.Equal("Shift1 end must be later than start.", reversedShiftException.Message);
    }

    [Fact]
    public async Task EmployeeAndShopServices_UpdateSearchAndDeleteGuards_CoverRemainingValidationPaths()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employeeService = new EmployeeService(new DataAccessLayer.Repositories.EmployeeRepository(context));
        var shopService = new ShopService(new DataAccessLayer.Repositories.ShopRepository(context));

        var alice = await employeeService.CreateAsync(TestDataFactory.CreateEmployeeModel(firstName: "Alice", lastName: "Brown"));
        var bob = await employeeService.CreateAsync(TestDataFactory.CreateEmployeeModel(firstName: "Bob", lastName: "Smith"));

        alice.FirstName = " Alice ";
        alice.LastName = " Cooper ";
        await employeeService.UpdateAsync(alice);

        var employeesByValue = await employeeService.GetByValueAsync("cooper");
        Assert.Single(employeesByValue);
        Assert.Equal("Cooper", employeesByValue[0].LastName);

        var duplicateEmployeeException = await Assert.ThrowsAsync<ValidationException>(() =>
            employeeService.UpdateAsync(new EmployeeModel
            {
                Id = alice.Id,
                FirstName = "Bob",
                LastName = "Smith",
            }));
        Assert.Equal("An employee with the same first and last name already exists.", duplicateEmployeeException.Message);

        var emptyFirstNameException = await Assert.ThrowsAsync<ValidationException>(() =>
            employeeService.UpdateAsync(new EmployeeModel
            {
                Id = alice.Id,
                FirstName = " ",
                LastName = "Whatever",
            }));
        Assert.Equal("First name is required.", emptyFirstNameException.Message);

        var container = TestDataFactory.CreateDalContainer();
        context.Containers.Add(container);
        await context.SaveChangesAsync();

        var mainShop = await shopService.CreateAsync(TestDataFactory.CreateShopModel(name: "Mega Shop", address: "Main Street", description: "  Desc  "));
        var auxShop = await shopService.CreateAsync(TestDataFactory.CreateShopModel(name: "Second Shop", address: "Second Street", description: null));

        context.SchedulePresets.Add(new DalModels.SchedulePresetModel
        {
            ContainerId = container.Id,
            Name = "Preset A",
            ScheduleName = "Preset Graph",
            ShopId = mainShop.Id,
            Year = 2026,
            Month = 4,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
        });
        await context.SaveChangesAsync();

        var blockedDeleteResult = await shopService.TryDeleteAsync(mainShop.Id);
        Assert.False(blockedDeleteResult.Succeeded);
        Assert.Equal(
            "To delete this shop, first delete all schedules and schedule presets where this shop is used.",
            blockedDeleteResult.Message);

        await Assert.ThrowsAsync<ValidationException>(() => shopService.DeleteAsync(mainShop.Id));

        auxShop.Name = new string('X', 201);
        var tooLongException = await Assert.ThrowsAsync<ValidationException>(() => shopService.UpdateAsync(auxShop));
        Assert.Equal("Shop name or address is too long.", tooLongException.Message);

        auxShop.Name = "Second Shop";
        auxShop.Address = " ";
        var emptyAddressException = await Assert.ThrowsAsync<ValidationException>(() => shopService.UpdateAsync(auxShop));
        Assert.Equal("Shop address is required.", emptyAddressException.Message);

        auxShop.Address = " Updated Street ";
        auxShop.Description = "  Updated  ";
        await shopService.UpdateAsync(auxShop);

        var shopsByValue = await shopService.GetByValueAsync("updated");
        Assert.Single(shopsByValue);
        Assert.Equal("Updated Street", shopsByValue[0].Address);
        Assert.Equal("Updated", shopsByValue[0].Description);
    }

    private static ContainerService CreateContainerService(
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

    private static ScheduleService CreateScheduleService(DataAccessLayer.Models.DataBaseContext.AppDbContext context)
        => new(
            new DataAccessLayer.Repositories.ScheduleRepository(context),
            new DataAccessLayer.Repositories.ScheduleEmployeeRepository(context),
            new DataAccessLayer.Repositories.ScheduleSlotRepository(context),
            new DataAccessLayer.Repositories.ScheduleCellStyleRepository(context));

    private sealed class ConfigurableScheduleGenerator : IScheduleGenerator
    {
        public int LastAvailabilityCount { get; private set; }

        public int LastEmployeeCount { get; private set; }

        public IList<ScheduleSlotModel> SlotsToReturn { get; set; } = [];

        public Task<IList<ScheduleSlotModel>> GenerateAsync(
            ScheduleModel schedule,
            IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress = null,
            CancellationToken ct = default)
        {
            LastAvailabilityCount = availabilities.Count();
            LastEmployeeCount = employees.Count();

            IList<ScheduleSlotModel> result = SlotsToReturn
                .Select(slot => new ScheduleSlotModel
                {
                    ScheduleId = schedule.Id,
                    DayOfMonth = slot.DayOfMonth,
                    SlotNo = slot.SlotNo,
                    EmployeeId = slot.EmployeeId,
                    Status = slot.Status,
                    FromTime = slot.FromTime,
                    ToTime = slot.ToTime,
                })
                .ToList();

            return Task.FromResult(result);
        }
    }
}
