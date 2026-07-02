using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DalAvailabilityKind = DataAccessLayer.Models.Enums.AvailabilityKind;
using DalPublicationStatus = DataAccessLayer.Models.Enums.AvailabilityPublicationStatus;

namespace GF3.Tests;

public sealed class AvailabilityGroupTransferServiceTests
{
    [Fact]
    public async Task GetPreviewSourcesAsync_FindsExistingMonthAvailability_WithoutPersistedTarget()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee("Amin", "Operator");
        var sourceGroup = new AvailabilityGroupModel { Name = "F35", Year = 2026, Month = 7 };
        context.AddRange(employee, sourceGroup);
        await context.SaveChangesAsync();

        var sourceMember = new AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = sourceGroup.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        };
        context.Add(sourceMember);
        await context.SaveChangesAsync();
        context.AvailabilityGroupDays.Add(new AvailabilityGroupDayModel
        {
            AvailabilityGroupMemberId = sourceMember.Id,
            DayOfMonth = 3,
            Kind = DalAvailabilityKind.INT,
            IntervalStr = "09:00 - 15:00",
        });
        await context.SaveChangesAsync();

        var service = new AvailabilityGroupTransferService(
            new AvailabilityGroupRepository(context),
            new AvailabilityGroupMemberRepository(context),
            new AvailabilityGroupTransferRepository(context));

        var source = Assert.Single(await service.GetPreviewSourcesAsync(
            [employee.Id],
            2026,
            7,
            targetGroupId: null));

        Assert.Equal(sourceGroup.Id, source.GroupId);
        Assert.Equal("F35", source.GroupName);
        var day = Assert.Single(source.Days);
        Assert.Equal(3, day.DayOfMonth);
        Assert.Equal(AvailabilityKind.INT, day.Kind);
        Assert.Equal("09:00 - 15:00", day.IntervalStr);
        Assert.True(day.CanTransfer);
    }

    [Fact]
    public async Task TransferDaysAsync_MovesFilledDayAtomically_AndReturnsSourceHint()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee("Amin", "Operator");
        var sourceGroup = new AvailabilityGroupModel
        {
            Name = "Availability 1",
            Year = 2026,
            Month = 7,
            PublicationStatus = DalPublicationStatus.Public,
            VisibleFromUtc = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            VisibleToUtc = new DateTimeOffset(2026, 7, 31, 23, 59, 0, TimeSpan.Zero),
        };
        var targetGroup = new AvailabilityGroupModel { Name = "F35", Year = 2026, Month = 7 };
        context.AddRange(employee, sourceGroup, targetGroup);
        await context.SaveChangesAsync();

        var sourceMember = new AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = sourceGroup.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        };
        var targetMember = new AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = targetGroup.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        };
        context.AddRange(sourceMember, targetMember);
        await context.SaveChangesAsync();

        context.AvailabilityGroupDays.AddRange(
            new AvailabilityGroupDayModel
            {
                AvailabilityGroupMemberId = sourceMember.Id,
                DayOfMonth = 3,
                Kind = DalAvailabilityKind.INT,
                IntervalStr = "09:00 - 15:00",
            },
            new AvailabilityGroupDayModel
            {
                AvailabilityGroupMemberId = targetMember.Id,
                DayOfMonth = 3,
                Kind = DalAvailabilityKind.NONE,
            });
        await context.SaveChangesAsync();

        var service = new AvailabilityGroupTransferService(
            new AvailabilityGroupRepository(context),
            new AvailabilityGroupMemberRepository(context),
            new AvailabilityGroupTransferRepository(context));

        var source = Assert.Single(await service.GetSourcesAsync(targetGroup.Id, targetMember.Id));
        Assert.True(Assert.Single(source.Days).CanTransfer);

        var result = await service.TransferDaysAsync(targetGroup.Id, targetMember.Id, sourceGroup.Id, [3]);

        var employeeAvailabilityService = new AvailabilityGroupService(
            new AvailabilityGroupRepository(context),
            new AvailabilityGroupMemberRepository(context),
            new AvailabilityGroupDayRepository(context),
            new AvailabilityGroupTransferRepository(context));
        await employeeAvailabilityService.SaveEmployeeAvailabilityAsync(
            employee.Id,
            sourceGroup.Id,
            [new BusinessLogicLayer.Contracts.Models.AvailabilityGroupDayModel
            {
                DayOfMonth = 3,
                Kind = AvailabilityKind.ANY,
            }],
            new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
        context.ChangeTracker.Clear();

        var sourceDay = await context.AvailabilityGroupDays
            .AsNoTracking()
            .SingleAsync(day => day.AvailabilityGroupMemberId == sourceMember.Id && day.DayOfMonth == 3);
        var targetDay = await context.AvailabilityGroupDays
            .AsNoTracking()
            .SingleAsync(day => day.AvailabilityGroupMemberId == targetMember.Id && day.DayOfMonth == 3);
        var hint = Assert.Single(await service.GetHintsAsync(sourceGroup.Id));

        Assert.Equal([3], result.DayOfMonths);
        Assert.Equal(DalAvailabilityKind.NONE, sourceDay.Kind);
        Assert.Null(sourceDay.IntervalStr);
        Assert.Equal(DalAvailabilityKind.INT, targetDay.Kind);
        Assert.Equal("09:00 - 15:00", targetDay.IntervalStr);
        Assert.Equal(AvailabilityKind.INT, hint.Kind);
        Assert.Equal("F35", hint.TargetGroupName);
        Assert.Equal("09:00 - 15:00", hint.IntervalStr);
    }
    [Fact]
    public async Task TransferDaysAsync_CanMovePreviouslyTransferredDayAcrossSameMonth()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee("Amin", "Operator");
        var originGroup = new AvailabilityGroupModel { Name = "Origin", Year = 2026, Month = 8 };
        var middleGroup = new AvailabilityGroupModel { Name = "Middle", Year = 2026, Month = 8 };
        var destinationGroup = new AvailabilityGroupModel { Name = "Destination", Year = 2026, Month = 8 };
        context.AddRange(employee, originGroup, middleGroup, destinationGroup);
        await context.SaveChangesAsync();

        var originMember = new AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = originGroup.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        };
        var middleMember = new AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = middleGroup.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        };
        var destinationMember = new AvailabilityGroupMemberModel
        {
            AvailabilityGroupId = destinationGroup.Id,
            EmployeeId = employee.Id,
            DisplayOrder = 0,
        };
        context.AddRange(originMember, middleMember, destinationMember);
        await context.SaveChangesAsync();

        context.AvailabilityGroupDays.AddRange(
            new AvailabilityGroupDayModel
            {
                AvailabilityGroupMemberId = originMember.Id,
                DayOfMonth = 3,
                Kind = DalAvailabilityKind.INT,
                IntervalStr = "09:00 - 15:00",
            },
            new AvailabilityGroupDayModel
            {
                AvailabilityGroupMemberId = middleMember.Id,
                DayOfMonth = 3,
                Kind = DalAvailabilityKind.NONE,
            },
            new AvailabilityGroupDayModel
            {
                AvailabilityGroupMemberId = destinationMember.Id,
                DayOfMonth = 3,
                Kind = DalAvailabilityKind.NONE,
            });
        await context.SaveChangesAsync();

        var service = new AvailabilityGroupTransferService(
            new AvailabilityGroupRepository(context),
            new AvailabilityGroupMemberRepository(context),
            new AvailabilityGroupTransferRepository(context));

        await service.TransferDaysAsync(middleGroup.Id, middleMember.Id, originGroup.Id, [3]);

        var reversePreview = await service.GetPreviewSourcesAsync(
            [employee.Id],
            2026,
            8,
            originGroup.Id);
        var middlePreview = Assert.Single(reversePreview, source => source.GroupId == middleGroup.Id);
        Assert.True(Assert.Single(middlePreview.Days).CanTransfer);

        var newGroupPreview = await service.GetPreviewSourcesAsync(
            [employee.Id],
            2026,
            8,
            targetGroupId: null);
        var middleForNewGroup = Assert.Single(newGroupPreview, source => source.GroupId == middleGroup.Id);
        Assert.True(Assert.Single(middleForNewGroup.Days).CanTransfer);

        var persistedReverseSources = await service.GetSourcesAsync(originGroup.Id, originMember.Id);
        var persistedMiddleSource = Assert.Single(
            persistedReverseSources, source => source.GroupId == middleGroup.Id);
        Assert.True(Assert.Single(persistedMiddleSource.Days).CanTransfer);

        await service.TransferDaysAsync(destinationGroup.Id, destinationMember.Id, middleGroup.Id, [3]);
        context.ChangeTracker.Clear();

        var movedLink = Assert.Single(await context.AvailabilityGroupDayTransfers.AsNoTracking().ToListAsync());
        Assert.Equal(originMember.Id, movedLink.SourceMemberId);
        Assert.Equal(destinationMember.Id, movedLink.TargetMemberId);

        var moveAgainPreview = await service.GetPreviewSourcesAsync(
            [employee.Id],
            2026,
            8,
            middleGroup.Id);
        var destinationPreview = Assert.Single(
            moveAgainPreview, source => source.GroupId == destinationGroup.Id);
        Assert.True(Assert.Single(destinationPreview.Days).CanTransfer);

        await service.TransferDaysAsync(originGroup.Id, originMember.Id, destinationGroup.Id, [3]);
        context.ChangeTracker.Clear();

        Assert.Empty(await context.AvailabilityGroupDayTransfers.AsNoTracking().ToListAsync());

        var finalDays = await context.AvailabilityGroupDays
            .AsNoTracking()
            .Where(day => day.DayOfMonth == 3)
            .ToDictionaryAsync(day => day.AvailabilityGroupMemberId);
        Assert.Equal(DalAvailabilityKind.INT, finalDays[originMember.Id].Kind);
        Assert.Equal("09:00 - 15:00", finalDays[originMember.Id].IntervalStr);
        Assert.Equal(DalAvailabilityKind.NONE, finalDays[middleMember.Id].Kind);
        Assert.Equal(DalAvailabilityKind.NONE, finalDays[destinationMember.Id].Kind);
    }
}
