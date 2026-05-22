using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DalEnums = DataAccessLayer.Models.Enums;

namespace GF3.Tests;

public sealed class DataAccessRepositoryTests
{
    [Fact]
    public async Task GenericRepository_AddAndUpdate_DetachesEntities_AndSupportsTrackedUpdates()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();

        await using var context = database.CreateContext();
        var repository = new GenericRepository<DataAccessLayer.Models.ContainerModel>(context);
        var created = await repository.AddAsync(TestDataFactory.CreateDalContainer("Container A"));

        Assert.Equal(EntityState.Detached, context.Entry(created).State);

        var tracked = await context.Containers.FirstAsync();
        Assert.Equal("Container A", tracked.Name);

        await repository.UpdateAsync(new DataAccessLayer.Models.ContainerModel
        {
            Id = tracked.Id,
            Name = "Container B",
            Note = "Updated"
        });

        var updated = await context.Containers.AsNoTracking().SingleAsync();
        Assert.Equal("Container B", updated.Name);
        Assert.Equal("Updated", updated.Note);
    }

    [Fact]
    public async Task EmployeeRepository_SearchAndReferenceChecks_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee(" Alice ", " Brown ", email: "Alice@Example.com");
        var employee2 = TestDataFactory.CreateDalEmployee("Bob", "Smith", phone: "555-77");
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        context.AddRange(employee, employee2, container, shop);
        await context.SaveChangesAsync();

        var availabilityGroup = new DataAccessLayer.Models.AvailabilityGroupModel { Name = "Group", Year = 2026, Month = 4 };
        context.AvailabilityGroups.Add(availabilityGroup);
        await context.SaveChangesAsync();

        var member = new DataAccessLayer.Models.AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = availabilityGroup.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        };

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        context.AvailabilityGroupMembers.Add(member);
        context.ScheduleEmployees.Add(new DataAccessLayer.Models.ScheduleEmployeeModel { ScheduleId = schedule.Id, EmployeeId = employee2.Id });
        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(schedule.Id, 1, 1, employee.Id, "08:00", "12:00"));
        await context.SaveChangesAsync();

        var repository = new EmployeeRepository(context);

        var searchResult = await repository.GetByValueAsync("alice");

        Assert.Single(searchResult);
        Assert.True(await repository.ExistsByNameAsync("alice", "brown"));
        Assert.False(await repository.ExistsByNameAsync("alice", "brown", excludeId: employee.Id));
        Assert.True(await repository.HasAvailabilityReferencesAsync(employee.Id));
        Assert.True(await repository.HasScheduleReferencesAsync(employee.Id));
        Assert.True(await repository.HasScheduleReferencesAsync(employee2.Id));
        Assert.Equal(employee.Id, searchResult[0].Id);
    }

    [Fact]
    public async Task ShopRepository_SearchAndReferenceChecks_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop(" Mega Store ", "Broadway 10", "Open 24/7");
        var shop2 = TestDataFactory.CreateDalShop("Mini Store", "Second street");
        context.AddRange(container, shop, shop2);
        await context.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        var preset = new DataAccessLayer.Models.SchedulePresetModel
        {
            ContainerId = container.Id,
            Name = "Preset A",
            ScheduleName = "Sched",
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
        };

        context.Schedules.Add(schedule);
        context.SchedulePresets.Add(preset);
        await context.SaveChangesAsync();

        var repository = new ShopRepository(context);
        var searchResult = await repository.GetByValueAsync("open");

        Assert.Single(searchResult);
        Assert.True(await repository.ExistsByNameAsync(" mega store "));
        Assert.False(await repository.ExistsByNameAsync(" mega store ", excludeId: shop.Id));
        Assert.True(await repository.HasScheduleReferencesAsync(shop.Id));
        Assert.True(await repository.HasSchedulePresetReferencesAsync(shop.Id));
    }

    [Fact]
    public async Task BindRepository_GetActiveAndUpsertByKey_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var repository = new BindRepository(context);
        await repository.AddAsync(new DataAccessLayer.Models.BindModel { Key = "B", Value = "-", IsActive = false });
        var active = await repository.UpsertByKeyAsync(new DataAccessLayer.Models.BindModel { Key = " A ", Value = " + ", IsActive = true });

        var updated = await repository.UpsertByKeyAsync(new DataAccessLayer.Models.BindModel { Key = "A", Value = "08:00 - 12:00", IsActive = true });
        var activeBinds = await repository.GetActiveAsync();

        Assert.Equal("A", active.Key);
        Assert.Equal(active.Id, updated.Id);
        Assert.Single(activeBinds);
        Assert.Equal("08:00 - 12:00", activeBinds[0].Value);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpsertByKeyAsync(new DataAccessLayer.Models.BindModel
        {
            Id = 999,
            Key = "A",
            Value = "+",
            IsActive = true,
        }));
    }

    [Fact]
    public async Task ContainerRepository_GetAllAndSearch_IncludeSchedules()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer(" North Hub ", "Priority region");
        var shop = TestDataFactory.CreateDalShop();
        context.AddRange(container, shop);
        await context.SaveChangesAsync();

        context.Schedules.Add(TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Alpha"));
        await context.SaveChangesAsync();

        var repository = new ContainerRepository(context);
        var all = await repository.GetAllAsync();
        var search = await repository.GetByValueAsync("priority");

        Assert.Single(all);
        Assert.Single(all[0].Schedules);
        Assert.Single(search);
        Assert.True(await repository.ExistsByNameAsync(" north hub "));
        Assert.True(await repository.HasScheduleReferencesAsync(container.Id));
    }

    [Fact]
    public async Task AvailabilityGroupRepository_GetFullAndSearch_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var group = new DataAccessLayer.Models.AvailabilityGroupModel { Name = "April", Year = 2026, Month = 4 };
        context.AvailabilityGroups.Add(group);
        await context.SaveChangesAsync();

        var member = new DataAccessLayer.Models.AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = group.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        };
        context.AvailabilityGroupMembers.Add(member);
        await context.SaveChangesAsync();

        context.AvailabilityGroupDays.Add(new DataAccessLayer.Models.AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = member.Id,
            DayOfMonth = 3,
            Kind = DalEnums.AvailabilityKind.ANY,
        });
        await context.SaveChangesAsync();

        var repository = new AvailabilityGroupRepository(context);
        var full = await repository.GetFullByIdAsync(group.Id);
        var search = await repository.GetByValueAsync("alice");

        Assert.NotNull(full);
        Assert.Single(full!.Members);
        Assert.Single(full.Members.First().Days);
        Assert.Single(search);
        Assert.True(await repository.ExistsByNameAsync(" april ", 2026, 4));
    }

    [Fact]
    public async Task ScheduleRepository_GetDetailedAndSearch_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer("Container");
        var shop = TestDataFactory.CreateDalShop("Shop");
        var employee = TestDataFactory.CreateDalEmployee();
        context.AddRange(container, shop, employee);
        await context.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Night Shift");
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        context.ScheduleEmployees.Add(new DataAccessLayer.Models.ScheduleEmployeeModel
        {
            ScheduleId = schedule.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 1,
        });
        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(schedule.Id, 2, 1, employee.Id, "08:00", "12:00"));
        await context.SaveChangesAsync();

        var repository = new ScheduleRepository(context);
        var detailed = await repository.GetDetailedAsync(schedule.Id);
        var byContainer = await repository.GetByContainerAsync(container.Id, "night");
        var byValue = await repository.GetByValueAsync("shop");

        Assert.NotNull(detailed);
        Assert.Equal("Container", detailed!.Container!.Name);
        Assert.Equal("Shop", detailed.Shop!.Name);
        Assert.Single(detailed.Employees);
        Assert.Single(detailed.Slots);
        Assert.Single(byContainer);
        Assert.Single(byValue);
    }

    [Fact]
    public async Task ScheduleRepository_GetPublishedForEmployee_FiltersAndOrdersVisibleSchedules()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer("Published Container");
        var shop = TestDataFactory.CreateDalShop("Published Shop");
        var employee = TestDataFactory.CreateDalEmployee("Visible", "Worker", email: "visible@example.com");
        var otherEmployee = TestDataFactory.CreateDalEmployee("Other", "Worker", email: "other.worker@example.com");
        context.AddRange(container, shop, employee, otherEmployee);
        await context.SaveChangesAsync();

        var april = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "April Published", year: 2026, month: 4);
        april.PublicationStatus = DalEnums.SchedulePublicationStatus.Public;
        var may = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "May Published", year: 2026, month: 5);
        may.PublicationStatus = DalEnums.SchedulePublicationStatus.Public;
        var privateSchedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "June Private", year: 2026, month: 6);
        privateSchedule.PublicationStatus = DalEnums.SchedulePublicationStatus.Private;
        var otherSchedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id, "Other Published", year: 2027, month: 1);
        otherSchedule.PublicationStatus = DalEnums.SchedulePublicationStatus.Public;
        context.Schedules.AddRange(april, may, privateSchedule, otherSchedule);
        await context.SaveChangesAsync();

        context.ScheduleEmployees.AddRange(
            new DataAccessLayer.Models.ScheduleEmployeeModel { ScheduleId = april.Id, EmployeeId = employee.Id, DisplayOrder = 0 },
            new DataAccessLayer.Models.ScheduleEmployeeModel { ScheduleId = may.Id, EmployeeId = employee.Id, DisplayOrder = 0 },
            new DataAccessLayer.Models.ScheduleEmployeeModel { ScheduleId = privateSchedule.Id, EmployeeId = employee.Id, DisplayOrder = 0 },
            new DataAccessLayer.Models.ScheduleEmployeeModel { ScheduleId = otherSchedule.Id, EmployeeId = otherEmployee.Id, DisplayOrder = 0 });
        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(may.Id, 1, 1, employee.Id, "08:00", "12:00"));
        await context.SaveChangesAsync();

        var repository = new ScheduleRepository(context);

        var published = await repository.GetPublishedForEmployeeAsync(employee.Id);

        Assert.Equal(["May Published", "April Published"], published.Select(schedule => schedule.Name));
        Assert.All(published, schedule => Assert.Equal(DalEnums.SchedulePublicationStatus.Public, schedule.PublicationStatus));
        Assert.All(published, schedule => Assert.Equal("Published Container", schedule.Container.Name));
        Assert.All(published, schedule => Assert.Equal("Published Shop", schedule.Shop.Name));
        Assert.Single(published[0].Employees);
        Assert.Single(published[0].Slots);
    }

    [Fact]
    public async Task ScheduleRepository_SearchSupportsWhitespaceNumericNoteAndContainerScope()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var firstContainer = TestDataFactory.CreateDalContainer("North Region");
        var secondContainer = TestDataFactory.CreateDalContainer("South Region");
        var shop = TestDataFactory.CreateDalShop("Numeric Shop");
        context.AddRange(firstContainer, secondContainer, shop);
        await context.SaveChangesAsync();

        var first = TestDataFactory.CreateDalSchedule(firstContainer.Id, shop.Id, "Early Plan", year: 2026, month: 5);
        first.Note = "Contains audit token";
        var second = TestDataFactory.CreateDalSchedule(secondContainer.Id, shop.Id, "Later Plan", year: 2027, month: 6);
        context.Schedules.AddRange(first, second);
        await context.SaveChangesAsync();

        var repository = new ScheduleRepository(context);

        var allFromWhitespace = await repository.GetByValueAsync("   ");
        var byYear = await repository.GetByValueAsync("2027");
        var byMonth = await repository.GetByValueAsync("5");
        var byNoteInContainer = await repository.GetByContainerAsync(firstContainer.Id, " audit ");
        var allInContainer = await repository.GetByContainerAsync(firstContainer.Id);

        Assert.Equal(2, allFromWhitespace.Count);
        Assert.Equal(["Later Plan"], byYear.Select(schedule => schedule.Name));
        Assert.Equal(["Early Plan"], byMonth.Select(schedule => schedule.Name));
        Assert.Equal(["Early Plan"], byNoteInContainer.Select(schedule => schedule.Name));
        Assert.Equal(["Early Plan"], allInContainer.Select(schedule => schedule.Name));
    }

    [Fact]
    public async Task ScheduleSlotRepository_ReplaceForSchedule_OverwriteAndAppend_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee();
        context.AddRange(container, shop, employee);
        await context.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        var repository = new ScheduleSlotRepository(context);
        var firstWritten = await repository.ReplaceForScheduleAsync(
            schedule.Id,
            [TestDataFactory.CreateDalSlot(schedule.Id, 1, 1, employee.Id, "08:00", "12:00")],
            overwrite: true);

        var secondWritten = await repository.ReplaceForScheduleAsync(
            schedule.Id,
            [TestDataFactory.CreateDalSlot(schedule.Id, 2, 1, null, "12:00", "16:00")],
            overwrite: false);

        var all = await repository.GetByScheduleAsync(schedule.Id);

        Assert.Equal(1, firstWritten);
        Assert.Equal(1, secondWritten);
        Assert.Equal(2, all.Count);
        Assert.Equal([1, 2], all.Select(slot => slot.DayOfMonth).ToArray());
    }

    [Fact]
    public async Task ScheduleSlotRepository_ReplaceForSchedule_PreservesOpenManagerManualOffer()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee();
        context.AddRange(container, shop, employee);
        await context.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        var protectedManualSlot = TestDataFactory.CreateDalSlot(schedule.Id, 3, 2, null, "10:00", "14:00");
        context.ScheduleSlots.Add(protectedManualSlot);
        await context.SaveChangesAsync();

        context.ShiftSwapRequests.Add(new DataAccessLayer.Models.ShiftSwapRequestModel
        {
            ScheduleId = schedule.Id,
            ScheduleSlotId = protectedManualSlot.Id,
            OfferedFromTime = protectedManualSlot.FromTime,
            OfferedToTime = protectedManualSlot.ToTime,
            FromEmployeeId = null,
            TargetEmployeeId = null,
            Visibility = DalEnums.ShiftSwapVisibility.Public,
            Status = DalEnums.ShiftSwapStatus.Open,
            IsManagerCreated = true,
            ManualColumnId = 101,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var repository = new ScheduleSlotRepository(context);
        await repository.ReplaceForScheduleAsync(
            schedule.Id,
            [
                new DataAccessLayer.Models.ScheduleSlotModel
                {
                    Id = protectedManualSlot.Id,
                    ScheduleId = schedule.Id,
                    DayOfMonth = protectedManualSlot.DayOfMonth,
                    SlotNo = protectedManualSlot.SlotNo,
                    EmployeeId = null,
                    Status = protectedManualSlot.Status,
                    FromTime = protectedManualSlot.FromTime,
                    ToTime = protectedManualSlot.ToTime,
                },
                TestDataFactory.CreateDalSlot(schedule.Id, 3, 1, employee.Id, "10:00", "14:00"),
                TestDataFactory.CreateDalSlot(schedule.Id, 4, 1, employee.Id, "08:00", "12:00"),
            ],
            overwrite: true);

        var slots = await context.ScheduleSlots
            .AsNoTracking()
            .Where(slot => slot.ScheduleId == schedule.Id)
            .OrderBy(slot => slot.DayOfMonth)
            .ThenBy(slot => slot.SlotNo)
            .ToListAsync();
        var swap = await context.ShiftSwapRequests.AsNoTracking().SingleAsync();

        Assert.Contains(slots, slot => slot.Id == protectedManualSlot.Id && slot.EmployeeId is null);
        Assert.Equal(3, slots.Count);
        Assert.Equal(protectedManualSlot.Id, swap.ScheduleSlotId);
        Assert.Equal(DalEnums.ShiftSwapStatus.Open, swap.Status);
        Assert.Equal([1, 2], slots.Where(slot => slot.DayOfMonth == 3).Select(slot => slot.SlotNo).ToArray());
    }

    [Fact]
    public async Task AvailabilityGroupDayRepository_AddRangeDeleteByMemberIdAndGetByGroup_Work()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee();
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var group = new DataAccessLayer.Models.AvailabilityGroupModel { Name = "Group", Year = 2026, Month = 4 };
        context.AvailabilityGroups.Add(group);
        await context.SaveChangesAsync();

        var member = new DataAccessLayer.Models.AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = group.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        };
        context.AvailabilityGroupMembers.Add(member);
        await context.SaveChangesAsync();

        var repository = new AvailabilityGroupDayRepository(context);
        await repository.AddRangeAsync(
            [
                new DataAccessLayer.Models.AvailabilityGroupDayModel { AvailabilityGroupMemberId = member.Id, DayOfMonth = 1, Kind = DalEnums.AvailabilityKind.ANY },
                new DataAccessLayer.Models.AvailabilityGroupDayModel { AvailabilityGroupMemberId = member.Id, DayOfMonth = 2, Kind = DalEnums.AvailabilityKind.NONE },
            ]);

        var daysByGroup = await repository.GetByGroupIdAsync(group.Id);
        Assert.Equal(2, daysByGroup.Count);

        await repository.DeleteByMemberIdAsync(member.Id);
        Assert.Empty(await repository.GetByMemberIdAsync(member.Id));
    }

    [Fact]
    public async Task SchedulePresetRepository_ExistsByName_UsesTrimmedInput()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        context.AddRange(container, shop);
        await context.SaveChangesAsync();

        context.SchedulePresets.Add(new DataAccessLayer.Models.SchedulePresetModel
        {
            ContainerId = container.Id,
            Name = "Preset A",
            ScheduleName = "Schedule",
            ShopId = shop.Id,
            Year = 2026,
            Month = 4,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 1,
            MaxConsecutiveDays = 1,
            MaxConsecutiveFull = 1,
            MaxFullPerMonth = 1,
        });
        await context.SaveChangesAsync();

        var repository = new SchedulePresetRepository(context);

        Assert.True(await repository.ExistsByNameAsync(container.Id, "  Preset A  "));
        Assert.False(await repository.ExistsByNameAsync(container.Id, "  "));
    }

    [Fact]
    public async Task SchedulePresetRepository_GetAllAndByContainer_OrderByNameAndIncludeEmployees()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var firstContainer = TestDataFactory.CreateDalContainer("First");
        var secondContainer = TestDataFactory.CreateDalContainer("Second");
        var shop = TestDataFactory.CreateDalShop();
        var employee = TestDataFactory.CreateDalEmployee("Preset", "Worker");
        context.AddRange(firstContainer, secondContainer, shop, employee);
        await context.SaveChangesAsync();

        var beta = CreateSchedulePreset(firstContainer.Id, shop.Id, "Beta");
        var alpha = CreateSchedulePreset(firstContainer.Id, shop.Id, "Alpha");
        var gamma = CreateSchedulePreset(secondContainer.Id, shop.Id, "Gamma");
        context.SchedulePresets.AddRange(beta, alpha, gamma);
        await context.SaveChangesAsync();

        context.SchedulePresetEmployees.AddRange(
            new DataAccessLayer.Models.SchedulePresetEmployeeModel
            {
                SchedulePresetId = alpha.Id,
                EmployeeId = employee.Id,
                MinHoursMonth = 80,
            },
            new DataAccessLayer.Models.SchedulePresetEmployeeModel
            {
                SchedulePresetId = beta.Id,
                EmployeeId = employee.Id,
                MinHoursMonth = 60,
            });
        await context.SaveChangesAsync();

        var repository = new SchedulePresetRepository(context);

        var all = await repository.GetAllAsync();
        var byContainer = await repository.GetByContainerAsync(firstContainer.Id);
        var duplicateInFirst = await repository.ExistsByNameAsync(firstContainer.Id, "Alpha");
        var duplicateExcluded = await repository.ExistsByNameAsync(firstContainer.Id, "Alpha", alpha.Id);
        var sameNameOtherContainer = await repository.ExistsByNameAsync(secondContainer.Id, "Alpha");

        Assert.Equal(["Alpha", "Beta", "Gamma"], all.Select(preset => preset.Name));
        Assert.Equal(["Alpha", "Beta"], byContainer.Select(preset => preset.Name));
        Assert.Equal(80, Assert.Single(byContainer[0].Employees).MinHoursMonth);
        Assert.True(duplicateInFirst);
        Assert.False(duplicateExcluded);
        Assert.False(sameNameOtherContainer);
        Assert.Equal(EntityState.Detached, context.Entry(byContainer[0]).State);
    }

    [Fact]
    public async Task AppDbContext_EnforcesUniqueEmployeeName_AndSlotStatusPair()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();

        await using (var context = database.CreateContext())
        {
            context.Employees.Add(TestDataFactory.CreateDalEmployee("Jane", "Doe"));
            await context.SaveChangesAsync();
            context.Employees.Add(TestDataFactory.CreateDalEmployee("Jane", "Doe", email: "other@example.com"));
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using var context2 = database.CreateContext();
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        context2.AddRange(container, shop);
        await context2.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        context2.Schedules.Add(schedule);
        await context2.SaveChangesAsync();

        context2.ScheduleSlots.Add(new DataAccessLayer.Models.ScheduleSlotModel
        {
            ScheduleId = schedule.Id,
            DayOfMonth = 1,
            SlotNo = 1,
            EmployeeId = null,
            Status = DalEnums.SlotStatus.ASSIGNED,
            FromTime = "08:00",
            ToTime = "12:00",
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context2.SaveChangesAsync());
    }

    private static DataAccessLayer.Models.SchedulePresetModel CreateSchedulePreset(int containerId, int shopId, string name)
        => new()
        {
            ContainerId = containerId,
            Name = name,
            ScheduleName = $"{name} Schedule",
            ShopId = shopId,
            Year = 2026,
            Month = 4,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 120,
            MaxConsecutiveDays = 4,
            MaxConsecutiveFull = 2,
            MaxFullPerMonth = 8,
        };
}
