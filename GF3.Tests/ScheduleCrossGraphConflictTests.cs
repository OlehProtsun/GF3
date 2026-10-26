using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GF3.Tests;

public sealed class ScheduleCrossGraphConflictTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistedGenerations_WithSharedEmployee_NeverOverlapAcrossGraphs(bool parallel)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var (containerId, graphId, employeeId) = await ContainerServiceTests.SeedGenerationAsync(database);
        int secondId;
        int secondContainerId;
        await using (var seed = database.CreateContext())
        {
            var original = await seed.Schedules.SingleAsync(s => s.Id == graphId);
            var secondContainer = TestDataFactory.CreateDalContainer("second");
            var secondShop = TestDataFactory.CreateDalShop("second");
            seed.AddRange(secondContainer, secondShop);
            await seed.SaveChangesAsync();
            secondContainerId = secondContainer.Id;
            var second = TestDataFactory.CreateDalSchedule(secondContainer.Id, secondShop.Id, "second", 2026, 2, original.AvailabilityGroupId);
            second.MaxHoursPerEmpMonth = 400;
            second.MaxConsecutiveDays = second.MaxConsecutiveFull = second.MaxFullPerMonth = 31;
            second.Employees.Add(new DataAccessLayer.Models.ScheduleEmployeeModel { EmployeeId = employeeId });
            seed.Schedules.Add(second);
            await seed.SaveChangesAsync();
            secondId = second.Id;
        }
        var gate = new GenerationRendezvous();
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstService = ContainerServiceTests.CreateService(firstContext, parallel ? gate : new ScheduleGenerator());
        var secondService = ContainerServiceTests.CreateService(secondContext, parallel ? gate : new ScheduleGenerator());
        if (parallel)
            await Task.WhenAll(firstService.GenerateGraphAsync(containerId, graphId, true, false, null),
                secondService.GenerateGraphAsync(secondContainerId, secondId, true, false, null));
        else
        {
            await firstService.GenerateGraphAsync(containerId, graphId, true, false, null);
            await secondService.GenerateGraphAsync(secondContainerId, secondId, true, false, null);
        }
        await using var fresh = database.CreateContext();
        var rows = await fresh.ScheduleSlots.AsNoTracking().Where(s => s.EmployeeId == employeeId).ToListAsync();
        Assert.NotEmpty(rows);
        foreach (var day in rows.GroupBy(s => s.DayOfMonth))
        {
            var ordered = day.OrderBy(s => TimeSpan.Parse(s.FromTime)).ToArray();
            for (var index = 1; index < ordered.Length; index++)
                Assert.True(TimeSpan.Parse(ordered[index].FromTime) >= TimeSpan.Parse(ordered[index - 1].ToTime),
                    $"Saved cross-graph overlap on synthetic day {day.Key}; parallel={parallel}.");
        }
    }

    [Fact]
    public async Task SameGraphConcurrentGeneration_AndOwnPreview_RetainSingleConsistentSet()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var (containerId, graphId, _) = await ContainerServiceTests.SeedGenerationAsync(database);
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = ContainerServiceTests.CreateService(firstContext, new ScheduleGenerator());
        var second = ContainerServiceTests.CreateService(secondContext, new ScheduleGenerator());
        var results = await Task.WhenAll(first.GenerateGraphAsync(containerId, graphId, true, false, null),
            second.GenerateGraphAsync(containerId, graphId, true, false, null));
        Assert.Equal(ScheduleGenerationScenarioMatrixTests.Normalize(results[0].Slots), ScheduleGenerationScenarioMatrixTests.Normalize(results[1].Slots));
        var before = await ContainerServiceTests.GenerationSnapshotAsync(database);
        var preview = await first.GenerateGraphPreviewAsync(containerId, (await first.GetGraphByIdAsync(containerId, graphId))!,
            (await first.GetGraphEmployeesAsync(containerId, graphId))!, null);
        Assert.Equal(ScheduleGenerationScenarioMatrixTests.Normalize(results[0].Slots), ScheduleGenerationScenarioMatrixTests.Normalize(preview.Slots));
        Assert.Equal(before, await ContainerServiceTests.GenerationSnapshotAsync(database));
        await using var fresh = database.CreateContext();
        Assert.Equal(56, await fresh.ScheduleSlots.CountAsync());
    }

    private sealed class GenerationRendezvous : IScheduleGenerator
    {
        private readonly TaskCompletionSource bothEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int entries;
        public async Task<IList<ScheduleSlotModel>> GenerateAsync(ScheduleModel schedule, IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees, IProgress<int>? progress = null, CancellationToken ct = default)
        {
            if (Interlocked.Increment(ref entries) == 2) bothEntered.TrySetResult();
            // Expose the reload/generate/persist race deterministically; a serialized writer
            // may proceed after the bounded rendezvous without requiring a second entrant.
            await Task.WhenAny(bothEntered.Task, Task.Delay(250, ct));
            ct.ThrowIfCancellationRequested();
            return await new ScheduleGenerator().GenerateAsync(schedule, availabilities, employees, progress, ct);
        }
    }

    [Fact]
    public async Task Preview_UsesPartialCommitment_IgnoresOtherMonthAndOwnSlots_WithoutWrites()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var (containerId, graphId, employeeId) = await ContainerServiceTests.SeedGenerationAsync(database);
        await using (var seed = database.CreateContext())
        {
            var original = await seed.Schedules.SingleAsync(s => s.Id == graphId);
            var other = TestDataFactory.CreateDalSchedule(containerId, original.ShopId, "other", 2026, 2);
            var later = TestDataFactory.CreateDalSchedule(containerId, original.ShopId, "later", 2026, 3);
            seed.AddRange(other, later);
            await seed.SaveChangesAsync();
            seed.ScheduleSlots.AddRange(TestDataFactory.CreateDalSlot(other.Id, 2, 1, employeeId, "10:00", "12:00"),
                TestDataFactory.CreateDalSlot(later.Id, 3, 1, employeeId, "08:00", "20:00"),
                TestDataFactory.CreateDalSlot(graphId, 3, 1, employeeId, "08:00", "20:00"));
            await seed.SaveChangesAsync();
        }
        var before = await ContainerServiceTests.GenerationSnapshotAsync(database);
        await using var context = database.CreateContext();
        var service = ContainerServiceTests.CreateService(context, new ScheduleGenerator());
        var schedule = (await service.GetGraphByIdAsync(containerId, graphId))!;
        var employees = (await service.GetGraphEmployeesAsync(containerId, graphId))!;
        var result = await service.GenerateGraphPreviewAsync(containerId, schedule, employees, null);
        Assert.Equal(56, result.GeneratedSlotsCount);
        Assert.Equal(0, result.WrittenSlotsCount);
        Assert.All(result.Slots.Where(s => s.DayOfMonth == 2 && s.EmployeeId.HasValue), s => Assert.True(TimeSpan.Parse(s.FromTime) >= TimeSpan.FromHours(12)));
        Assert.Equal(12, result.Slots.Where(s => s.DayOfMonth == 3 && s.EmployeeId.HasValue)
            .Sum(s => (TimeSpan.Parse(s.ToTime) - TimeSpan.Parse(s.FromTime)).TotalHours));
        var group = TestDataFactory.CreateAvailabilityGroupModel(year: 2026, month: 2);
        group.Members.Add(new AvailabilityGroupMemberModel { EmployeeId = employeeId, Days = [TestDataFactory.CreateAvailabilityDayModel(1, AvailabilityKind.NONE)] });
        var scenario = new GenerationScenario("partial-external", schedule, employees, [group],
            [TestDataFactory.CreateScheduleSlotModel(2, 1, employeeId, "10:00", "12:00")]);
        Assert.Empty(ScheduleGenerationOracle.Check(scenario, result.Slots.ToArray()).HardViolations);
        Assert.Equal(before, await ContainerServiceTests.GenerationSnapshotAsync(database));
    }

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
