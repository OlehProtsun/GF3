using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class AvailabilityGroupServiceTests
{
    [Fact]
    public async Task CreateMemberAndSlot_ValidateUniquenessAndNormalizeAnyIntervals()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var group = await service.CreateAsync(TestDataFactory.CreateAvailabilityGroupModel());
        var member = await service.CreateMemberAsync(group.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id });
        var slot = await service.CreateSlotAsync(group.Id, new AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = member.Id,
            DayOfMonth = 2,
            Kind = AvailabilityKind.ANY,
            IntervalStr = "08:00 - 16:00",
        });

        Assert.Equal(group.Id, member.AvailabilityGroupId);
        Assert.Null(slot.IntervalStr);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateMemberAsync(group.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id }));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateSlotAsync(group.Id, new AvailabilityGroupDayModel
            {
                AvailabilityGroupMemberId = member.Id,
                DayOfMonth = 2,
                Kind = AvailabilityKind.ANY,
            }));
    }

    [Fact]
    public async Task SaveGroupAsync_ReplacesMembersAndDays_FromPayload()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var employee1 = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        var employee2 = TestDataFactory.CreateDalEmployee("Bob", "Smith");
        var employee3 = TestDataFactory.CreateDalEmployee("Cara", "Stone");
        context.Employees.AddRange(employee1, employee2, employee3);
        await context.SaveChangesAsync();

        var createdGroup = await service.CreateAsync(TestDataFactory.CreateAvailabilityGroupModel(name: " April "));
        var member1 = await service.CreateMemberAsync(createdGroup.Id, new AvailabilityGroupMemberModel { EmployeeId = employee1.Id, DisplayOrder = 0 });
        var member2 = await service.CreateMemberAsync(createdGroup.Id, new AvailabilityGroupMemberModel { EmployeeId = employee2.Id, DisplayOrder = 1 });

        await service.CreateSlotAsync(createdGroup.Id, new AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = member1.Id,
            DayOfMonth = 1,
            Kind = AvailabilityKind.ANY,
        });
        await service.CreateSlotAsync(createdGroup.Id, new AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = member2.Id,
            DayOfMonth = 2,
            Kind = AvailabilityKind.NONE,
        });

        await service.SaveGroupAsync(
            new AvailabilityGroupModel
            {
                Id = createdGroup.Id,
                Name = " April Final ",
                Year = createdGroup.Year,
                Month = createdGroup.Month,
            },
            [
                (employee3.Id, (IList<AvailabilityGroupDayModel>)new List<AvailabilityGroupDayModel>
                {
                    TestDataFactory.CreateAvailabilityDayModel(7, AvailabilityKind.INT, "08:00 - 16:00"),
                }),
                (employee1.Id, (IList<AvailabilityGroupDayModel>)new List<AvailabilityGroupDayModel>
                {
                    TestDataFactory.CreateAvailabilityDayModel(5, AvailabilityKind.ANY),
                }),
            ]);

        var members = await service.GetMembersAsync(createdGroup.Id);
        var slots = await service.GetSlotsAsync(createdGroup.Id);
        var reloadedGroup = await service.GetAsync(createdGroup.Id);

        Assert.NotNull(reloadedGroup);
        Assert.Equal("April Final", reloadedGroup!.Name);
        Assert.Equal(2, members.Count);
        Assert.DoesNotContain(members, member => member.EmployeeId == employee2.Id);
        Assert.Equal([employee3.Id, employee1.Id], members.OrderBy(member => member.DisplayOrder).Select(member => member.EmployeeId).ToArray());
        Assert.Equal(2, slots.Count);
        Assert.Contains(slots, slot => slot.DayOfMonth == 7 && slot.Kind == AvailabilityKind.INT);
        Assert.Contains(slots, slot => slot.DayOfMonth == 5 && slot.Kind == AvailabilityKind.ANY);
    }

    [Fact]
    public async Task LoadFullAsync_ReturnsOrderedMembersAndDays()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var employee1 = TestDataFactory.CreateDalEmployee("Zoe", "Brown");
        var employee2 = TestDataFactory.CreateDalEmployee("Adam", "Adams");
        context.Employees.AddRange(employee1, employee2);
        await context.SaveChangesAsync();

        var group = await service.CreateAsync(TestDataFactory.CreateAvailabilityGroupModel());
        var member1 = await service.CreateMemberAsync(group.Id, new AvailabilityGroupMemberModel { EmployeeId = employee1.Id, DisplayOrder = 2 });
        var member2 = await service.CreateMemberAsync(group.Id, new AvailabilityGroupMemberModel { EmployeeId = employee2.Id, DisplayOrder = 1 });

        await service.CreateSlotAsync(group.Id, new AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = member1.Id,
            DayOfMonth = 10,
            Kind = AvailabilityKind.ANY,
        });

        var (_, members, days) = await service.LoadFullAsync(group.Id);

        Assert.Equal([employee2.Id, employee1.Id], members.Select(member => member.EmployeeId).ToArray());
        Assert.Single(days);
        Assert.Equal(10, days[0].DayOfMonth);
    }

    [Fact]
    public async Task UpdateSlot_RequiresIntervalForIntervalAvailability()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var employee = TestDataFactory.CreateDalEmployee();
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var group = await service.CreateAsync(TestDataFactory.CreateAvailabilityGroupModel());
        var member = await service.CreateMemberAsync(group.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id });
        var slot = await service.CreateSlotAsync(group.Id, new AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = member.Id,
            DayOfMonth = 1,
            Kind = AvailabilityKind.ANY,
        });

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSlotAsync(group.Id, slot.Id, new AvailabilityGroupDayModel
            {
                AvailabilityGroupMemberId = member.Id,
                DayOfMonth = 1,
                Kind = AvailabilityKind.INT,
                IntervalStr = " ",
            }));

        Assert.Equal(["Interval is required for interval availability."], exception.Errors[nameof(AvailabilityGroupDayModel.IntervalStr)]);
    }

    private static AvailabilityGroupService CreateService(DataAccessLayer.Models.DataBaseContext.AppDbContext context)
        => new(
            new DataAccessLayer.Repositories.AvailabilityGroupRepository(context),
            new DataAccessLayer.Repositories.AvailabilityGroupMemberRepository(context),
            new DataAccessLayer.Repositories.AvailabilityGroupDayRepository(context));
}
