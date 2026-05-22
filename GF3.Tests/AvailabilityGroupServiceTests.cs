using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DalEnums = DataAccessLayer.Models.Enums;

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
    public async Task GetPublishedForEmployeeAsync_ReturnsOnlyStartedPublicGroupsForAssignedEmployee()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var now = new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero);
        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown", email: "alice@example.com");
        var otherEmployee = TestDataFactory.CreateDalEmployee("Bob", "Smith", email: "bob@example.com");
        context.Employees.AddRange(employee, otherEmployee);
        await context.SaveChangesAsync();

        var openGroup = await CreatePublishedGroupAsync(service, "Open April", now.AddDays(-1), now.AddDays(1));
        var openMember = await service.CreateMemberAsync(openGroup.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id });
        await service.CreateSlotAsync(openGroup.Id, TestDataFactory.CreateAvailabilityDayModel(9, AvailabilityKind.NONE, memberId: openMember.Id));
        await service.CreateSlotAsync(openGroup.Id, TestDataFactory.CreateAvailabilityDayModel(2, AvailabilityKind.ANY, memberId: openMember.Id));

        var expiredGroup = await CreatePublishedGroupAsync(service, "Expired April", now.AddDays(-10), now.AddDays(-1));
        await service.CreateMemberAsync(expiredGroup.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id });

        var futureGroup = await CreatePublishedGroupAsync(service, "Future April", now.AddDays(1), now.AddDays(5));
        await service.CreateMemberAsync(futureGroup.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id });

        var otherOnlyGroup = await CreatePublishedGroupAsync(service, "Other April", now.AddDays(-1), now.AddDays(1));
        await service.CreateMemberAsync(otherOnlyGroup.Id, new AvailabilityGroupMemberModel { EmployeeId = otherEmployee.Id });

        var privateGroup = await service.CreateAsync(new AvailabilityGroupModel
        {
            Name = "Private April",
            Year = 2026,
            Month = 4,
            PublicationStatus = AvailabilityPublicationStatus.Private,
        });
        await service.CreateMemberAsync(privateGroup.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id });

        var published = await service.GetPublishedForEmployeeAsync(employee.Id, now);

        Assert.Equal([openGroup.Id, expiredGroup.Id], published.Select(item => item.Group.Id).ToArray());
        Assert.True(published[0].CanSubmit);
        Assert.False(published[1].CanSubmit);
        Assert.Equal(employee.Id, published[0].Member.EmployeeId);
        Assert.Equal([2, 9], published[0].Days.Select(day => day.DayOfMonth).ToArray());

        var missingFuture = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetPublishedForEmployeeByIdAsync(employee.Id, futureGroup.Id, now));
        Assert.Equal($"Published availability group with id {futureGroup.Id} was not found for the current employee.", missingFuture.Message);
    }

    [Fact]
    public async Task SaveEmployeeAvailabilityAsync_ReplacesOnlyCurrentMemberDaysAndStampsMember()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var now = new DateTimeOffset(2026, 4, 15, 14, 30, 0, TimeSpan.FromHours(2));
        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown", email: "alice-save@example.com");
        var otherEmployee = TestDataFactory.CreateDalEmployee("Bob", "Smith", email: "bob-save@example.com");
        context.Employees.AddRange(employee, otherEmployee);
        await context.SaveChangesAsync();

        var group = await CreatePublishedGroupAsync(service, "Save April", now.AddDays(-1), now.AddDays(1));
        var member = await service.CreateMemberAsync(group.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id, DisplayOrder = 4 });
        var otherMember = await service.CreateMemberAsync(group.Id, new AvailabilityGroupMemberModel { EmployeeId = otherEmployee.Id, DisplayOrder = 5 });
        await service.CreateSlotAsync(group.Id, TestDataFactory.CreateAvailabilityDayModel(3, AvailabilityKind.ANY, memberId: member.Id));
        await service.CreateSlotAsync(group.Id, TestDataFactory.CreateAvailabilityDayModel(4, AvailabilityKind.NONE, memberId: otherMember.Id));

        var saved = await service.SaveEmployeeAvailabilityAsync(
            employee.Id,
            group.Id,
            [
                TestDataFactory.CreateAvailabilityDayModel(6, AvailabilityKind.ANY, "08:00 - 18:00", id: 123, memberId: 999),
                TestDataFactory.CreateAvailabilityDayModel(5, AvailabilityKind.INT, " 09:00 - 13:00 "),
            ],
            now);

        var persistedMember = await context.AvailabilityGroupMembers.AsNoTracking().SingleAsync(item => item.Id == member.Id);
        var persistedDays = await context.AvailabilityGroupDays.AsNoTracking()
            .OrderBy(day => day.AvailabilityGroupMemberId)
            .ThenBy(day => day.DayOfMonth)
            .ToListAsync();

        Assert.Equal(now.ToUniversalTime(), persistedMember.EmployeeLastModifiedAtUtc);
        Assert.Equal([5, 6], saved.Days.Select(day => day.DayOfMonth).ToArray());
        Assert.Equal("09:00 - 13:00", saved.Days.Single(day => day.DayOfMonth == 5).IntervalStr);
        Assert.Null(saved.Days.Single(day => day.DayOfMonth == 6).IntervalStr);
        Assert.All(saved.Days, day => Assert.Equal(member.Id, day.AvailabilityGroupMemberId));
        Assert.DoesNotContain(persistedDays, day => day.AvailabilityGroupMemberId == member.Id && day.DayOfMonth == 3);
        Assert.Contains(persistedDays, day =>
            day.AvailabilityGroupMemberId == member.Id &&
            day.DayOfMonth == 5 &&
            day.Kind == DalEnums.AvailabilityKind.INT &&
            day.IntervalStr == "09:00 - 13:00");
        Assert.Contains(persistedDays, day =>
            day.AvailabilityGroupMemberId == otherMember.Id &&
            day.DayOfMonth == 4 &&
            day.Kind == DalEnums.AvailabilityKind.NONE);
    }

    [Fact]
    public async Task SaveEmployeeAvailabilityAsync_RejectsClosedWindowAndInvalidDayPayloads()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = CreateService(context);

        var now = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);
        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var openFebruary = await CreatePublishedGroupAsync(service, "Open February", now.AddDays(-1), now.AddDays(1), year: 2026, month: 2);
        await service.CreateMemberAsync(openFebruary.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id });

        var duplicateDay = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SaveEmployeeAvailabilityAsync(
                employee.Id,
                openFebruary.Id,
                [
                    TestDataFactory.CreateAvailabilityDayModel(7, AvailabilityKind.ANY),
                    TestDataFactory.CreateAvailabilityDayModel(7, AvailabilityKind.NONE),
                ],
                now));
        Assert.Equal(["A slot for this day already exists for the current employee."], duplicateDay.Errors[nameof(AvailabilityGroupDayModel.DayOfMonth)]);

        var missingInterval = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SaveEmployeeAvailabilityAsync(
                employee.Id,
                openFebruary.Id,
                [TestDataFactory.CreateAvailabilityDayModel(8, AvailabilityKind.INT, " ")],
                now));
        Assert.Equal(["Interval is required for interval availability."], missingInterval.Errors[nameof(AvailabilityGroupDayModel.IntervalStr)]);

        var outsideMonth = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SaveEmployeeAvailabilityAsync(
                employee.Id,
                openFebruary.Id,
                [TestDataFactory.CreateAvailabilityDayModel(30, AvailabilityKind.ANY)],
                now));
        Assert.Equal(["Day of month is outside the availability month."], outsideMonth.Errors[nameof(AvailabilityGroupDayModel.DayOfMonth)]);

        var closedGroup = await CreatePublishedGroupAsync(service, "Closed February", now.AddDays(-10), now.AddDays(-1), year: 2026, month: 2);
        await service.CreateMemberAsync(closedGroup.Id, new AvailabilityGroupMemberModel { EmployeeId = employee.Id });

        var closedWindow = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SaveEmployeeAvailabilityAsync(
                employee.Id,
                closedGroup.Id,
                [TestDataFactory.CreateAvailabilityDayModel(9, AvailabilityKind.ANY)],
                now));
        Assert.Equal(["The time for editing this availability has expired."], closedWindow.Errors[nameof(AvailabilityGroupModel.VisibleToUtc)]);
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

    private static Task<AvailabilityGroupModel> CreatePublishedGroupAsync(
        AvailabilityGroupService service,
        string name,
        DateTimeOffset visibleFromUtc,
        DateTimeOffset visibleToUtc,
        int year = 2026,
        int month = 4)
        => service.CreateAsync(new AvailabilityGroupModel
        {
            Name = name,
            Year = year,
            Month = month,
            PublicationStatus = AvailabilityPublicationStatus.Public,
            VisibleFromUtc = visibleFromUtc,
            VisibleToUtc = visibleToUtc,
        });
}
