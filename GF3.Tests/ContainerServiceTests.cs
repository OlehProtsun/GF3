using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DalEnums = DataAccessLayer.Models.Enums;

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
    public async Task UpdateGraphPublicationAsync_UpdatesOnlyContainerSchedules_AndPreservesSwapWhenPrivate()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context, new FakeScheduleGenerator());

        var container = TestDataFactory.CreateDalContainer();
        var otherContainer = TestDataFactory.CreateDalContainer("Other Container");
        var shop = TestDataFactory.CreateDalShop();
        context.AddRange(container, otherContainer, shop);
        await context.SaveChangesAsync();

        var first = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "First");
        var second = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Second");
        var unrelated = TestDataFactory.CreateDalSchedule(otherContainer.Id, shop.Id, "Unrelated");
        first.AllowSwap = true;
        second.AllowSwap = true;
        unrelated.AllowSwap = true;
        context.AddRange(first, second, unrelated);
        await context.SaveChangesAsync();

        var publishedCount = await service.UpdateGraphPublicationAsync(
            container.Id,
            SchedulePublicationStatus.Public,
            allowSwap: false);

        context.ChangeTracker.Clear();
        var published = await context.Schedules.OrderBy(schedule => schedule.Id).ToListAsync();
        Assert.Equal(2, publishedCount);
        Assert.All(published.Where(schedule => schedule.ContainerId == container.Id), schedule =>
        {
            Assert.Equal(DalEnums.SchedulePublicationStatus.Public, schedule.PublicationStatus);
            Assert.False(schedule.AllowSwap);
        });
        Assert.Equal(DalEnums.SchedulePublicationStatus.Private, published.Single(schedule => schedule.ContainerId == otherContainer.Id).PublicationStatus);

        await service.UpdateGraphPublicationAsync(container.Id, SchedulePublicationStatus.Private, allowSwap: null);

        context.ChangeTracker.Clear();
        var privateSchedules = await context.Schedules.Where(schedule => schedule.ContainerId == container.Id).ToListAsync();
        Assert.All(privateSchedules, schedule =>
        {
            Assert.Equal(DalEnums.SchedulePublicationStatus.Private, schedule.PublicationStatus);
            Assert.False(schedule.AllowSwap);
        });
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
    public async Task SchedulePresets_AreGlobal_AndSurviveOriginDeletion()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context, new FakeScheduleGenerator());
        var a = TestDataFactory.CreateDalContainer("A");
        var b = TestDataFactory.CreateDalContainer("B");
        var c = TestDataFactory.CreateDalContainer("C");
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee();
        context.AddRange(a, b, c, shop, employee);
        await context.SaveChangesAsync();
        SchedulePresetModel Input() => new()
        {
            Name = "Same name", ScheduleName = "Schedule", ShopId = shop.Id, Year = 2026, Month = 4,
            PeoplePerShift = 1, Shift1Time = "08:00 - 16:00", Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160, MaxConsecutiveDays = 5, MaxConsecutiveFull = 2, MaxFullPerMonth = 4,
            Employees = [new() { EmployeeId = employee.Id, MinHoursMonth = 80 }],
        };
        var first = await service.CreateSchedulePresetAsync(a.Id, Input());
        var second = await service.CreateSchedulePresetAsync(b.Id, Input());
        Assert.NotEqual(first.Id, second.Id);
        foreach (var origin in new[] { a.Id, b.Id })
        {
            var presets = (await service.GetSchedulePresetsAsync(origin))!;
            Assert.Equal(new[] { first.Id, second.Id }.Order(), presets.Select(p => p.Id).Order());
            Assert.Equal(a.Id, presets.Single(p => p.Id == first.Id).ContainerId);
            Assert.Equal(b.Id, presets.Single(p => p.Id == second.Id).ContainerId);
            Assert.All(presets, p => { Assert.Equal(employee.Id, Assert.Single(p.Employees).EmployeeId); Assert.Equal(80, p.Employees.Single().MinHoursMonth); });
        }
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateSchedulePresetAsync(a.Id, Input()));
        Assert.Null(await service.GetSchedulePresetsAsync(int.MaxValue));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateSchedulePresetAsync(int.MaxValue, Input()));
        Assert.True((await service.TryDeleteAsync(a.Id)).Succeeded);
        var retained = (await service.GetSchedulePresetsAsync(b.Id))!.Single(p => p.Id == first.Id);
        Assert.Null(retained.ContainerId);
        Assert.Equal(first.Employees.Single().Id, Assert.Single(retained.Employees).Id);
        Assert.Equal(80, retained.Employees.Single().MinHoursMonth);
        Assert.True((await service.TryDeleteAsync(b.Id)).Succeeded);
        var orphans = (await service.GetSchedulePresetsAsync(c.Id))!;
        Assert.Equal(2, orphans.Count);
        Assert.All(orphans, p => Assert.Null(p.ContainerId));
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
    public async Task GetPublishedGraphsForEmployeeAsync_FiltersPublishedGraphsAndNormalizesCanceledReadTokens()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context, new FakeScheduleGenerator());

        var container = TestDataFactory.CreateDalContainer("Employee Container");
        var shop = TestDataFactory.CreateDalShop("Employee Shop");
        var employee = TestDataFactory.CreateDalEmployee("Zoe", "Young");
        var otherEmployee = TestDataFactory.CreateDalEmployee("Adam", "Blue");
        context.AddRange(container, shop, employee, otherEmployee);
        await context.SaveChangesAsync();

        var visibleMay = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Visible May", year: 2026, month: 5);
        visibleMay.PublicationStatus = DalEnums.SchedulePublicationStatus.Public;
        var visibleApril = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Visible April", year: 2026, month: 4);
        visibleApril.PublicationStatus = DalEnums.SchedulePublicationStatus.Public;
        var privateGraph = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Private June", year: 2026, month: 6);
        privateGraph.PublicationStatus = DalEnums.SchedulePublicationStatus.Private;
        var otherEmployeeGraph = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Other Employee", year: 2026, month: 7);
        otherEmployeeGraph.PublicationStatus = DalEnums.SchedulePublicationStatus.Public;
        context.Schedules.AddRange(visibleMay, visibleApril, privateGraph, otherEmployeeGraph);
        await context.SaveChangesAsync();

        context.ScheduleEmployees.AddRange(
            new DataAccessLayer.Models.ScheduleEmployeeModel
            {
                ScheduleId = visibleMay.Id,
                EmployeeId = employee.Id,
                DisplayOrder = 2,
            },
            new DataAccessLayer.Models.ScheduleEmployeeModel
            {
                ScheduleId = visibleApril.Id,
                EmployeeId = employee.Id,
                DisplayOrder = 1,
            },
            new DataAccessLayer.Models.ScheduleEmployeeModel
            {
                ScheduleId = privateGraph.Id,
                EmployeeId = employee.Id,
                DisplayOrder = 1,
            },
            new DataAccessLayer.Models.ScheduleEmployeeModel
            {
                ScheduleId = otherEmployeeGraph.Id,
                EmployeeId = otherEmployee.Id,
                DisplayOrder = 1,
            });
        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(visibleMay.Id, 10, 1, employee.Id, "08:00", "12:00"));
        await context.SaveChangesAsync();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var visibleGraphs = await service.GetPublishedGraphsForEmployeeAsync(employee.Id, cts.Token);

        Assert.Equal(["Visible May", "Visible April"], visibleGraphs.Select(graph => graph.Name).ToArray());
        Assert.All(visibleGraphs, graph => Assert.Equal(SchedulePublicationStatus.Public, graph.PublicationStatus));
        Assert.All(visibleGraphs, graph => Assert.Equal("Employee Container", graph.Container?.Name));
        Assert.All(visibleGraphs, graph => Assert.Equal("Employee Shop", graph.Shop?.Name));
        Assert.Single(visibleGraphs[0].Employees);
        Assert.Single(visibleGraphs[0].Slots);

        var invalidEmployee = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetPublishedGraphsForEmployeeAsync(0));
        Assert.Equal(["Employee is required."], invalidEmployee.Errors["employeeId"]);
    }

    [Fact]
    public async Task ReplaceGraphSlotsAsync_ValidatesSplitShiftsAndRebuildsSlotNumbers()
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
                TestDataFactory.CreateScheduleSlotModel(1, 9, null, "08:00", "12:00"),
            ]);

        var stored = await service.GetGraphSlotsAsync(container.Id, graph.Id);

        Assert.NotNull(stored);
        Assert.Equal(2, stored!.Count);
        Assert.Equal([1, 2], stored.Select(slot => slot.SlotNo).ToArray());
        Assert.Equal(SlotStatus.ASSIGNED, stored[0].Status);
        Assert.Equal(SlotStatus.UNFURNISHED, stored[1].Status);

        var duplicate = await Assert.ThrowsAsync<ValidationException>(() => service.ReplaceGraphSlotsAsync(
            container.Id,
            graph.Id,
            [
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "09:00", "15:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 2, employee.Id, "09:00", "15:00"),
            ]));
        Assert.Equal(
            "The same time range cannot be entered more than once for one employee and day.",
            duplicate.Message);

        var overlap = await Assert.ThrowsAsync<ValidationException>(() => service.ReplaceGraphSlotsAsync(
            container.Id,
            graph.Id,
            [
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "09:00", "15:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 2, employee.Id, "14:00", "18:00"),
            ]));
        Assert.Equal("Time ranges for one employee and day cannot overlap.", overlap.Message);

        var tooMany = await Assert.ThrowsAsync<ValidationException>(() => service.ReplaceGraphSlotsAsync(
            container.Id,
            graph.Id,
            [
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "08:00", "09:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "10:00", "11:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "12:00", "13:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "14:00", "15:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "16:00", "17:00"),
            ]));
        Assert.Equal("An employee can have no more than 4 time ranges in one day.", tooMany.Message);

        await service.ReplaceGraphSlotsAsync(
            container.Id,
            graph.Id,
            [
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "08:00", "09:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "09:00", "10:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "12:00", "13:00"),
                TestDataFactory.CreateScheduleSlotModel(1, 1, employee.Id, "14:00", "15:00"),
            ]);
        Assert.Equal(4, (await service.GetGraphSlotsAsync(container.Id, graph.Id))?.Count);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealGeneration_PreviewDryRunOverwriteAndFailure_PreserveDatabaseContracts(bool priority)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var (containerId, graphId, employeeId) = await SeedGenerationAsync(database);
        await using var context = database.CreateContext();
        if (priority) await context.ScheduleEmployees.Where(e => e.ScheduleId == graphId).ExecuteUpdateAsync(s => s.SetProperty(e => e.IsPriority, true));
        var legacy = new ScheduleGenerator();
        IScheduleGenerator engine = priority ? new PriorityAwareScheduleGenerator(legacy, new(legacy), context) : legacy;
        var service = CreateService(context, engine);
        var graph = (await service.GetGraphByIdAsync(containerId, graphId))!;
        var employees = (await service.GetGraphEmployeesAsync(containerId, graphId))!;
        var before = await GenerationSnapshotAsync(database);
        var preview = await service.GenerateGraphPreviewAsync(containerId, graph, employees, null);
        var dryRun = await service.GenerateGraphAsync(containerId, graphId, true, true, null);
        Assert.Equal(56, preview.GeneratedSlotsCount);
        Assert.Equal(0, preview.WrittenSlotsCount);
        Assert.Equal(0, dryRun.WrittenSlotsCount);
        Assert.Equal(ScheduleGenerationScenarioMatrixTests.Normalize(preview.Slots), ScheduleGenerationScenarioMatrixTests.Normalize(dryRun.Slots));
        Assert.Equal(before, await GenerationSnapshotAsync(database));
        Assert.All(preview.Slots.Where(s => s.DayOfMonth == 1), s => Assert.Null(s.EmployeeId));

        var saved = await service.GenerateGraphAsync(containerId, graphId, true, false, null);
        Assert.Equal(56, saved.GeneratedSlotsCount);
        Assert.True(saved.WrittenSlotsCount > 0);
        var persisted = await GenerationSnapshotAsync(database);
        var repeat = await service.GenerateGraphAsync(containerId, graphId, true, false, null);
        Assert.Equal(0, repeat.WrittenSlotsCount);
        Assert.Equal(persisted, await GenerationSnapshotAsync(database));
        await Assert.ThrowsAsync<ValidationException>(() => service.GenerateGraphAsync(containerId, graphId, false, false, null));
        Assert.Equal(persisted, await GenerationSnapshotAsync(database));

        await using var fresh = database.CreateContext();
        Assert.Equal(56, await fresh.ScheduleSlots.CountAsync(s => s.ScheduleId == graphId));
        Assert.All(await fresh.ScheduleSlots.Where(s => s.EmployeeId != null).ToListAsync(), s => Assert.Equal(employeeId, s.EmployeeId));
        var failingService = CreateService(fresh, new ThrowingScheduleGenerator());
        await Assert.ThrowsAsync<InvalidOperationException>(() => failingService.GenerateGraphAsync(containerId, graphId, true, false, null));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => failingService.GenerateGraphAsync(containerId, graphId, true, false, null, canceled.Token));
        Assert.Equal(persisted, await GenerationSnapshotAsync(database));
        using var during = new CancellationTokenSource();
        var freshLegacy = new ScheduleGenerator();
        var real = CreateService(fresh, priority ? new PriorityAwareScheduleGenerator(freshLegacy, new(freshLegacy), fresh) : freshLegacy);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => real.GenerateGraphAsync(containerId, graphId, true, false,
            new CancelGenerationProgress(during), during.Token));
        Assert.Equal(persisted, await GenerationSnapshotAsync(database));
        var afterFailure = await real.GenerateGraphAsync(containerId, graphId, true, false, null);
        Assert.Equal(0, afterFailure.WrittenSlotsCount);
    }

    [Theory]
    [InlineData(1, false, 0)] [InlineData(1, true, 1)]
    [InlineData(6, false, 2)] [InlineData(6, true, 3)]
    [InlineData(8, false, 3)] [InlineData(8, true, 0)]
    [InlineData(10, false, 1)] [InlineData(10, true, 2)]
    [InlineData(12, false, 2)] [InlineData(12, true, 3)] [InlineData(0, true, 2)]
    public async Task FreshPreviewSaveReopenAndDeleteRecreateAgree(int hours, bool weekendsOff, int priorityCount)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var (containerId, obsoleteId, employeeId) = await SeedGenerationAsync(database);
        await using var context = database.CreateContext();
        if (hours == 0) await context.PriorityGenerationSettings.ExecuteDeleteAsync();
        else await context.PriorityGenerationSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.ShiftHours, hours).SetProperty(x => x.WeekendsOff, weekendsOff));
        var group = await context.AvailabilityGroups.Include(g => g.Members).SingleAsync();
        for (var index = 0; index < 2; index++)
        {
            var added = TestDataFactory.CreateDalEmployee($"Synthetic{index}", phone: null, email: null);
            context.Employees.Add(added);
            await context.SaveChangesAsync();
            group.Members.Add(new() { EmployeeId = added.Id });
        }
        await context.SaveChangesAsync();
        var legacy = new ScheduleGenerator();
        var service = CreateService(context, new PriorityAwareScheduleGenerator(legacy, new(legacy), context));
        var graph = (await service.GetGraphByIdAsync(containerId, obsoleteId))!;
        await service.GenerateGraphAsync(containerId, obsoleteId, true, false, null);
        await service.DeleteGraphAsync(containerId, obsoleteId);
        graph.Id = 0; graph.Name = "Fresh synthetic";
        graph.Shift1Time = "09:00 - 15:00"; graph.Shift2Time = "15:00 - 22:00"; graph.MaxHoursPerEmpMonth = 80;
        var employees = group.Members.OrderBy(m => m.EmployeeId).Select((member, index) => new ScheduleEmployeeModel {
            EmployeeId = member.EmployeeId, DisplayOrder = index, MinHoursMonth = 180, IsPriority = index < priorityCount }).ToArray();
        var before = await GenerationSnapshotAsync(database);
        var preview = await service.GenerateGraphPreviewAsync(containerId, graph, employees, null);
        Assert.Equal(before, await GenerationSnapshotAsync(database));
        Assert.Equal(0, preview.WrittenSlotsCount);
        var groupModel = new AvailabilityGroupModel { Id = group.Id, Year = 2026, Month = 2,
            Members = [new() { EmployeeId = employeeId, Days = [new() { DayOfMonth = 1, Kind = AvailabilityKind.NONE }] }] };
        var check = ScheduleGenerationOracle.Check(new("fresh", graph, employees, [groupModel]), preview.Slots.ToArray(), hours == 0 ? 8 : hours, weekendsOff);
        Assert.Empty(check.HardViolations);
        Assert.True(check.Metrics.TotalMinHourDeficitMinutes > 0);
        Assert.Contains(preview.Slots, s => s.Status == SlotStatus.UNFURNISHED);
        var created = await service.CreateGraphAsync(containerId, graph);
        foreach (var employee in employees) await service.AddGraphEmployeeAsync(containerId, created.Id, employee);
        context.ChangeTracker.Clear();
        var reopened = (await service.GetGraphEmployeesAsync(containerId, created.Id))!;
        Assert.Equal(employees.Select(e => e.IsPriority), reopened.OrderBy(e => e.DisplayOrder).Select(e => e.IsPriority));
        var saved = await service.GenerateGraphAsync(containerId, created.Id, true, false, null);
        Assert.Equal(ScheduleGenerationScenarioMatrixTests.Normalize(preview.Slots), ScheduleGenerationScenarioMatrixTests.Normalize(saved.Slots));
        var persisted = await GenerationSnapshotAsync(database);
        var repeat = await service.GenerateGraphAsync(containerId, created.Id, true, false, null);
        Assert.Equal(0, repeat.WrittenSlotsCount);
        Assert.Equal(persisted, await GenerationSnapshotAsync(database));
        await service.DeleteGraphAsync(containerId, created.Id);
        graph.Id = 0;
        var recreatedPreview = await service.GenerateGraphPreviewAsync(containerId, graph, employees, null);
        Assert.Equal(ScheduleGenerationScenarioMatrixTests.Normalize(preview.Slots), ScheduleGenerationScenarioMatrixTests.Normalize(recreatedPreview.Slots));
        var recreated = await service.CreateGraphAsync(containerId, graph);
        foreach (var employee in employees) await service.AddGraphEmployeeAsync(containerId, recreated.Id, employee);
        var regenerated = await service.GenerateGraphAsync(containerId, recreated.Id, true, false, null);
        Assert.Equal(ScheduleGenerationScenarioMatrixTests.Normalize(preview.Slots), ScheduleGenerationScenarioMatrixTests.Normalize(regenerated.Slots));
    }

    [Fact]
    public async Task RealGeneration_OverwritePreservesProtectedManualOfferAndReservedNumber()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var (containerId, graphId, _) = await SeedGenerationAsync(database);
        int slotId;
        await using (var seed = database.CreateContext())
        {
            var manual = TestDataFactory.CreateDalSlot(graphId, 2, 1, null, "08:00", "16:00");
            seed.ScheduleSlots.Add(manual);
            await seed.SaveChangesAsync();
            slotId = manual.Id;
            seed.ShiftSwapRequests.Add(new DataAccessLayer.Models.ShiftSwapRequestModel
            {
                ScheduleId = graphId, ScheduleSlotId = slotId, IsManagerCreated = true,
                Status = DalEnums.ShiftSwapStatus.Open, CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await seed.SaveChangesAsync();
        }
        await using (var context = database.CreateContext())
        {
            var service = CreateService(context, new ScheduleGenerator());
            await service.GenerateGraphAsync(containerId, graphId, true, false, null);
            var before = await GenerationSnapshotAsync(database);
            await service.GenerateGraphAsync(containerId, graphId, true, false, null);
            Assert.Equal(before, await GenerationSnapshotAsync(database));
        }
        await using var fresh = database.CreateContext();
        var original = await fresh.ScheduleSlots.SingleAsync(s => s.Id == slotId);
        Assert.Null(original.EmployeeId);
        Assert.Equal("08:00", original.FromTime);
        Assert.Equal("16:00", original.ToTime);
        Assert.Equal(slotId, (await fresh.ShiftSwapRequests.SingleAsync()).ScheduleSlotId);
        var rows = await fresh.ScheduleSlots.ToListAsync();
        Assert.Equal(rows.Count, rows.Select(s => (s.DayOfMonth, s.SlotNo, s.FromTime, s.ToTime)).Distinct().Count());
    }

    internal static async Task<(int ContainerId, int GraphId, int EmployeeId)> SeedGenerationAsync(SqliteTestDatabase database)
    {
        await using var context = database.CreateContext();
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee();
        var group = new DataAccessLayer.Models.AvailabilityGroupModel { Name = "synthetic", Year = 2026, Month = 2 };
        context.AddRange(container, shop, employee, group);
        await context.SaveChangesAsync();
        group.Members.Add(new DataAccessLayer.Models.AvailabilityGroupMemberModel
        {
            EmployeeId = employee.Id,
            Days = [new DataAccessLayer.Models.AvailabilityGroupDayModel { DayOfMonth = 1, Kind = DalEnums.AvailabilityKind.NONE }]
        });
        var graph = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, year: 2026, month: 2, availabilityGroupId: group.Id);
        graph.MaxHoursPerEmpMonth = 400;
        graph.MaxConsecutiveDays = graph.MaxConsecutiveFull = graph.MaxFullPerMonth = 31;
        graph.Employees.Add(new DataAccessLayer.Models.ScheduleEmployeeModel { EmployeeId = employee.Id, MinHoursMonth = 0 });
        context.Schedules.Add(graph);
        await context.SaveChangesAsync();
        return (container.Id, graph.Id, employee.Id);
    }

    [Fact]
    public async Task RealGeneration_DatabaseFailureAfterDelete_RollsBackReplacement()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var (containerId, graphId, employeeId) = await SeedGenerationAsync(database);
        await using (var seed = database.CreateContext())
        {
            seed.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(graphId, 2, 9, employeeId, "08:00", "09:00"));
            await seed.SaveChangesAsync();
            await seed.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_synthetic_generation BEFORE INSERT ON schedule_slot BEGIN SELECT RAISE(ABORT, 'synthetic insert failure'); END;");
        }
        var before = await GenerationSnapshotAsync(database);
        await using var context = database.CreateContext();
        var service = CreateService(context, new ScheduleGenerator());
        await Assert.ThrowsAsync<ValidationException>(() => service.GenerateGraphAsync(containerId, graphId, true, false, null));
        Assert.Equal(before, await GenerationSnapshotAsync(database));
    }

    [Fact]
    public async Task OfflineReplay_SyntheticDiskFixture_IsReadOnlyRedactedAndAgreesWithOracle()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var (containerId, graphId, employeeId) = await SeedGenerationAsync(database);
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "GF3.sln"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var project = Path.Combine(repository.FullName, "tools", "AlgorithmStressHarness", "AlgorithmStressHarness.csproj");
        var build = await RunReplayProcessAsync(repository.FullName, "build", project, "--disable-build-servers", "-m:1", "-p:UseSharedCompilation=false", "-p:OutputPath=bin/ReplayVerification/");
        Assert.True(build.Code == 0, "Replay harness build failed: " + build.Output);
        var dll = Path.Combine(repository.FullName, "tools", "AlgorithmStressHarness", "bin", "ReplayVerification", "AlgorithmStressHarness.dll");
        var output = Path.Combine(Path.GetTempPath(), "GF3.ReplayReports", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try
        {
            var before = await GenerationSnapshotAsync(database);
            var hash = await HashDatabaseFileAsync(database.DatabasePath);
            Assert.True(File.Exists(database.DatabasePath + "-wal") && new FileInfo(database.DatabasePath + "-wal").Length > 0);
            var replay = await RunReplayProcessAsync(repository.FullName, dll, "--db", database.DatabasePath, "--graph", graphId.ToString(), "--output", output);
            Assert.True(replay.Code == 0, replay.Output);
            Assert.Equal(before, await GenerationSnapshotAsync(database));
            Assert.Equal(hash, await HashDatabaseFileAsync(database.DatabasePath));
            var summaryText = await File.ReadAllTextAsync(Path.Combine(output, "summary.json"));
            using var summary = System.Text.Json.JsonDocument.Parse(summaryText);
            var report = summary.RootElement.GetProperty("Cases")[0];
            Assert.True(report.GetProperty("RepeatStable").GetBoolean());
            Assert.Equal(3, report.GetProperty("DurationsMilliseconds").GetArrayLength());
            Assert.Empty(report.GetProperty("HardViolations").EnumerateObject());
            await using var context = database.CreateContext();
            var service = CreateService(context, new ScheduleGenerator());
            var schedule = (await service.GetGraphByIdAsync(containerId, graphId))!;
            var employees = (await service.GetGraphEmployeesAsync(containerId, graphId))!;
            var group = TestDataFactory.CreateAvailabilityGroupModel(year: 2026, month: 2);
            group.Members.Add(new AvailabilityGroupMemberModel { EmployeeId = employeeId, Days = [TestDataFactory.CreateAvailabilityDayModel(1, AvailabilityKind.NONE)] });
            var preview = await service.GenerateGraphPreviewAsync(containerId, schedule, employees, null);
            var expected = ScheduleGenerationOracle.Check(new("disk-fixture", schedule, employees, [group]), preview.Slots.ToArray());
            Assert.Empty(expected.HardViolations);
            var generated = report.GetProperty("Generated");
            var quality = report.GetProperty("Quality");
            Assert.True(quality.GetProperty("CapacityBoundsApplicable").GetBoolean());
            Assert.Equal(28 * 12 * 60, quality.GetProperty("DemandMinutes").GetInt32());
            Assert.Equal(27 * 12 * 60, quality.GetProperty("OptimisticCapacityMinutes").GetInt32());
            Assert.Equal(12 * 60, quality.GetProperty("CoverageGapLowerBoundMinutes").GetInt32());
            Assert.Equal(0, quality.GetProperty("CoverageGapAboveLowerBoundMinutes").GetInt32());
            Assert.Equal(0, quality.GetProperty("MinimumDeficitLowerBoundMinutes").GetInt32());
            Assert.Equal(expected.Metrics.CoverageGapMinutes, generated.GetProperty("CoverageGapMinutes").GetInt32());
            Assert.Equal(expected.Metrics.TotalAssignedMinutes, generated.GetProperty("TotalAssignedMinutes").GetInt32());
            Assert.Equal(expected.Metrics.TotalMinHourDeficitMinutes, generated.GetProperty("TotalMinHourDeficitMinutes").GetInt32());
            await context.ScheduleEmployees.Where(e => e.ScheduleId == graphId).ExecuteUpdateAsync(s => s.SetProperty(e => e.IsPriority, true));
            await context.PriorityGenerationSettings.ExecuteDeleteAsync();
            var selectedGroupId = await context.Schedules.Where(s => s.Id == graphId).Select(s => s.AvailabilityGroupId).SingleAsync();
            var selectedMember = await context.AvailabilityGroupMembers.SingleAsync(m => m.AvailabilityGroupId == selectedGroupId);
            context.AvailabilityGroupDays.Add(new() { AvailabilityGroupMemberId = selectedMember.Id, DayOfMonth = 2,
                Kind = DalEnums.AvailabilityKind.INT, IntervalStr = "09:00 - 15:00" });
            await context.SaveChangesAsync();
            var appBefore = await GenerationSnapshotAsync(database);
            var appHash = await HashDatabaseFileAsync(database.DatabasePath);
            foreach (var variant in new[] { "stored", "none", "first", "first-two", "all" })
            {
                var appReplay = await RunReplayProcessAsync(repository.FullName, dll, "--db", database.DatabasePath,
                    "--graph", graphId.ToString(), "--engine", "app", "--variant", variant, "--output", output);
                Assert.True(appReplay.Code == 0, appReplay.Output);
                using var app = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "summary.json")));
                Assert.Equal("app", app.RootElement.GetProperty("Engine").GetString());
                Assert.True(app.RootElement.GetProperty("FingerprintUnchanged").GetBoolean());
                Assert.Equal("ok", app.RootElement.GetProperty("QuickCheckAfter").GetString());
                var appCase = app.RootElement.GetProperty("Cases")[0];
                Assert.Equal(variant, appCase.GetProperty("Variant").GetString());
                Assert.Equal(variant == "none" ? 0 : 1, appCase.GetProperty("PriorityCount").GetInt32());
                Assert.Equal(8, appCase.GetProperty("PriorityShiftHours").GetInt32());
                Assert.Empty(appCase.GetProperty("HardViolations").EnumerateObject());
                Assert.True(appCase.GetProperty("RepeatStable").GetBoolean());
                Assert.Equal(appBefore, await GenerationSnapshotAsync(database));
                Assert.Equal(appHash, await HashDatabaseFileAsync(database.DatabasePath));
            }
            foreach (var (engine, variant) in new[] { ("legacy", "all"), ("unknown", "stored"), ("app", "unknown") })
            {
                var invalidVariant = await RunReplayProcessAsync(repository.FullName, dll, "--db", database.DatabasePath,
                    "--all-graphs", "--engine", engine, "--variant", variant, "--output", output);
                Assert.Equal(2, invalidVariant.Code);
            }
            foreach (var file in new[] { "summary.json", "cases.csv", "failures.json" })
            {
                var text = await File.ReadAllTextAsync(Path.Combine(output, file));
                foreach (var forbidden in new[] { "John", "Smith", "john.smith@example.com", "123456789", database.DatabasePath, "EmployeeId", "GraphId", "ContainerId", "IntervalStr" })
                    Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
            Assert.Equal("[]", (await File.ReadAllTextAsync(Path.Combine(output, "failures.json"))).Trim());
            var help = await RunReplayProcessAsync(repository.FullName, dll, "--help");
            Assert.Equal(0, help.Code);
            var missing = await RunReplayProcessAsync(repository.FullName, dll, "--db", Path.Combine(database.RootPath, "absent.db"), "--all-graphs", "--output", output);
            Assert.NotEqual(0, missing.Code);
            Assert.False(File.Exists(Path.Combine(database.RootPath, "absent.db")));
            var relative = await RunReplayProcessAsync(repository.FullName, dll, "--db", "relative.db", "--all-graphs", "--output", output);
            Assert.NotEqual(0, relative.Code);
            var invalidOutput = await RunReplayProcessAsync(repository.FullName, dll, "--db", database.DatabasePath, "--all-graphs", "--output", repository.FullName);
            Assert.NotEqual(0, invalidOutput.Code);
            var sourceOutput = await RunReplayProcessAsync(repository.FullName, dll, "--db", database.DatabasePath, "--all-graphs", "--output", database.RootPath);
            Assert.NotEqual(0, sourceOutput.Code);
            var invalidRepeat = await RunReplayProcessAsync(repository.FullName, dll, "--db", database.DatabasePath, "--all-graphs", "--repeat", "11", "--output", output);
            Assert.NotEqual(0, invalidRepeat.Code);
            var unsupported = Path.Combine(database.RootPath, "unsupported.db");
            await File.WriteAllBytesAsync(unsupported, []);
            var schema = await RunReplayProcessAsync(repository.FullName, dll, "--db", unsupported, "--all-graphs", "--output", output);
            Assert.NotEqual(0, schema.Code);
            Assert.Equal(0, new FileInfo(unsupported).Length);
            Assert.Equal(appBefore, await GenerationSnapshotAsync(database));
            await context.Database.OpenConnectionAsync();
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF");
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE schedule SET availability_group_id={int.MaxValue} WHERE id={graphId}");
            var missingBefore = await GenerationSnapshotAsync(database);
            var missingGroup = await RunReplayProcessAsync(repository.FullName, dll, "--db", database.DatabasePath,
                "--graph", graphId.ToString(), "--engine", "app", "--output", output);
            Assert.Equal(1, missingGroup.Code);
            Assert.Contains("missing-selected-availability", await File.ReadAllTextAsync(Path.Combine(output, "failures.json")));
            Assert.Equal(missingBefore, await GenerationSnapshotAsync(database));
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE schedule SET availability_group_id={selectedGroupId} WHERE id={graphId}");
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON");
        }
        finally
        {
            // This directory was created above, under the fixed test report root.
            var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "GF3.ReplayReports")) + Path.DirectorySeparatorChar;
            Assert.StartsWith(parent, Path.GetFullPath(output));
            Directory.Delete(output, true);
        }
    }

    private static async Task<(int Code, string Output)> RunReplayProcessAsync(string workingDirectory, params string[] args)
    {
        var start = new System.Diagnostics.ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in args) start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        return (process.ExitCode, await stdout + await stderr);
    }

    private static async Task<string> HashDatabaseFileAsync(string path)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(file));
    }

    internal static async Task<string> GenerationSnapshotAsync(SqliteTestDatabase database)
    {
        await using var context = database.CreateContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        var tables = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        }
        var rows = new List<string>();
        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM \"{table.Replace("\"", "\"\"")}\"";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                rows.Add(table + System.Text.Json.JsonSerializer.Serialize(values));
            }
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(string.Join("\n", rows.Order(StringComparer.Ordinal)));
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }

    private sealed class ThrowingScheduleGenerator : IScheduleGenerator
    {
        public Task<IList<ScheduleSlotModel>> GenerateAsync(ScheduleModel schedule, IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees, IProgress<int>? progress = null, CancellationToken ct = default)
            => throw new InvalidOperationException("Injected generator failure.");
    }

    private sealed class CancelGenerationProgress(CancellationTokenSource source) : IProgress<int>
    {
        public void Report(int value) { if (value >= 35) source.Cancel(); }
    }

    internal static ContainerService CreateService(
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
