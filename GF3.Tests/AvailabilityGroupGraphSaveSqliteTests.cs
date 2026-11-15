using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DalKind = DataAccessLayer.Models.Enums.AvailabilityKind;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace GF3.Tests;

public sealed class AvailabilityGroupGraphSaveSqliteTests
{
    [Theory]
    [InlineData(2026, 2, 28)]
    [InlineData(2028, 2, 29)]
    [InlineData(2026, 11, 30)]
    [InlineData(2026, 12, 31)]
    public async Task SaveGraphAsync_ReconcilesFullSnapshotsWithoutReplacingIds(int year, int month, int dayCount)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var employees = await SeedEmployeesAsync(context, 6);
        var service = CreateService(context);
        var group = TestDataFactory.CreateAvailabilityGroupModel(year: year, month: month);
        group.PublicationStatus = AvailabilityPublicationStatus.Public;
        group.VisibleFromUtc = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.FromHours(2));
        group.VisibleToUtc = group.VisibleFromUtc.Value.AddDays(dayCount);
        var payload = employees.Take(5).Select(id => (id, Month(dayCount))).ToList();
        payload[0].Item2[0].Kind = AvailabilityKind.ANY;
        payload[0].Item2[0].IntervalStr = "09:00 - 10:00";
        payload[0].Item2[1].IntervalStr = "  Training note  ";
        payload[0].Item2[2].Kind = AvailabilityKind.INT;
        payload[0].Item2[2].IntervalStr = " 9:00-13:30 ";

        var created = await service.SaveGraphAsync(group, payload, []);
        Assert.True(created.Changed);
        Assert.Empty(created.SourceGroupIds);
        group.Id = created.Id;
        var createdGroup = await context.AvailabilityGroups.AsNoTracking().SingleAsync();
        Assert.Equal(group.VisibleFromUtc, createdGroup.VisibleFromUtc);
        Assert.Equal(group.VisibleToUtc, createdGroup.VisibleToUtc);
        Assert.NotNull(createdGroup.PublishedAtUtc);

        var stamped = await context.AvailabilityGroupMembers.SingleAsync(member => member.EmployeeId == employees[0]);
        var modifiedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        stamped.EmployeeLastModifiedAtUtc = modifiedAt;
        await context.SaveChangesAsync();

        var before = await service.LoadFullAsync(group.Id);
        Assert.Equal(5 * dayCount, before.days.Count);
        var originalDays = before.days.Where(day => day.AvailabilityGroupMemberId == stamped.Id)
            .ToDictionary(day => day.DayOfMonth);
        Assert.Null(originalDays[1].IntervalStr);
        Assert.Equal("Training note", originalDays[2].IntervalStr);
        Assert.Equal("09:00 - 13:30", originalDays[3].IntervalStr);

        group.Name = "Updated availability";
        payload[0].Item2[1].IntervalStr = "Updated note";
        var removedId = before.members[^1].Id;
        payload.RemoveAt(4);
        payload.Reverse();
        payload.Add((employees[5], Month(dayCount)));
        var updated = await service.SaveGraphAsync(group, payload, []);
        var repeated = await service.SaveGraphAsync(group, payload, []);
        Assert.Equal(group.Id, updated.Id);
        Assert.True(updated.Changed);
        Assert.False(repeated.Changed);

        await using var verification = database.CreateContext();
        var after = await CreateService(verification).LoadFullAsync(group.Id);
        Assert.Equal(group.Name, after.group.Name);
        Assert.Equal(payload.Select(member => member.id), after.members.Select(member => member.EmployeeId));
        var retained = Assert.Single(after.members, member => member.EmployeeId == employees[0]);
        Assert.Equal(stamped.Id, retained.Id);
        Assert.Equal(modifiedAt, retained.EmployeeLastModifiedAtUtc);
        Assert.Equal(createdGroup.PublishedAtUtc, (await verification.AvailabilityGroups.SingleAsync()).PublishedAtUtc);
        Assert.Equal(5 * dayCount, after.days.Count);
        Assert.Equal(after.days.Count, after.days.Select(day => (day.AvailabilityGroupMemberId, day.DayOfMonth)).Distinct().Count());
        Assert.False(await verification.AvailabilityGroupDays.AnyAsync(day => day.AvailabilityGroupMemberId == removedId));
        var savedDays = after.days.Where(day => day.AvailabilityGroupMemberId == retained.Id).OrderBy(day => day.DayOfMonth).ToArray();
        Assert.Equal(originalDays.Values.OrderBy(day => day.DayOfMonth).Select(day => day.Id), savedDays.Select(day => day.Id));
        Assert.Equal("Updated note", savedDays[1].IntervalStr);
    }

    [Fact]
    public async Task SaveGraphAsync_RollsBackGroupMembersAndEarlierDayInsertsOnDatabaseFailure()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var employees = await SeedEmployeesAsync(context, 2);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER fail_availability_graph_create
            BEFORE INSERT ON availability_group_day
            WHEN NEW.day_of_month = 5 AND EXISTS (SELECT 1 FROM availability_group_day WHERE day_of_month = 1)
            BEGIN
                SELECT RAISE(ABORT, 'forced failure after first day');
            END;
            """);

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => CreateService(context).SaveGraphAsync(
            TestDataFactory.CreateAvailabilityGroupModel(month: 12),
            employees.Select(id => (id, Month(31))).ToList(), []));
        Assert.Contains("forced failure after first day", error.GetBaseException().Message);

        await using var verification = database.CreateContext();
        Assert.Empty(await verification.AvailabilityGroups.ToListAsync());
        Assert.Empty(await verification.AvailabilityGroupMembers.ToListAsync());
        Assert.Empty(await verification.AvailabilityGroupDays.ToListAsync());
    }

    [Fact]
    public async Task SaveGraphAsync_RollsBackCfaAndTargetEditsThenCommitsValidTransferWithStableIds()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var employees = await SeedEmployeesAsync(context, 2);
        var service = CreateService(context);
        var source = await service.CreateAsync(TestDataFactory.CreateAvailabilityGroupModel(name: "Source", month: 12));
        var target = await service.CreateAsync(TestDataFactory.CreateAvailabilityGroupModel(name: "Target", month: 12));
        var sourceMember = await service.CreateMemberAsync(source.Id, new() { EmployeeId = employees[0] });
        var targetMember = await service.CreateMemberAsync(target.Id, new() { EmployeeId = employees[0] });
        var sourceSlot = await service.CreateSlotAsync(source.Id,
            TestDataFactory.CreateAvailabilityDayModel(3, AvailabilityKind.INT, "9:00 - 15:00", memberId: sourceMember.Id));
        var targetSlot = await service.CreateSlotAsync(target.Id,
            TestDataFactory.CreateAvailabilityDayModel(3, AvailabilityKind.NONE, memberId: targetMember.Id));
        target.Name = "Target changed";
        var days = Month(31);
        days[2].Kind = AvailabilityKind.INT;
        days[2].IntervalStr = "09:00 - 16:00";
        var payload = new List<(int, IList<AvailabilityGroupDayModel>)> { (employees[0], days), (employees[1], Month(31)) };

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SaveGraphAsync(target, payload, [new(employees[0], source.Id, [4])]));
        await VerifyRollbackAsync();
        var stale = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SaveGraphAsync(target, payload, [new(employees[0], source.Id, [3])]));
        Assert.Contains("source availability changed", stale.Message);
        await VerifyRollbackAsync();
        await Assert.ThrowsAsync<BusinessLogicLayer.Common.ValidationException>(() =>
            service.SaveGraphAsync(target, payload, [new(employees[0], target.Id, [3])]));
        target.Month = 11;
        await Assert.ThrowsAsync<BusinessLogicLayer.Common.ValidationException>(() =>
            service.SaveGraphAsync(target, [(employees[0], Month(30))], [new(employees[0], source.Id, [3])]));
        target.Month = 12;

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER fail_availability_graph_update
            BEFORE INSERT ON availability_group_day
            WHEN NEW.day_of_month = 5 AND EXISTS (SELECT 1 FROM availability_group_day_transfer)
            BEGIN
                SELECT RAISE(ABORT, 'forced failure after CFA');
            END;
            """);
        days[2].IntervalStr = "09:00 - 15:00";
        var error = await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.SaveGraphAsync(target, payload, [new(employees[0], source.Id, [3])]));
        Assert.Contains("forced failure after CFA", error.GetBaseException().Message);
        await VerifyRollbackAsync();

        await context.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_availability_graph_update");
        var saved = await service.SaveGraphAsync(target, payload, [new(employees[0], source.Id, [3, 3])]);
        Assert.True(saved.Changed);
        Assert.Equal([source.Id], saved.SourceGroupIds);
        Assert.False((await service.SaveGraphAsync(target, payload, [])).Changed);
        await using var verification = database.CreateContext();
        var fromDay = await verification.AvailabilityGroupDays.SingleAsync(day => day.Id == sourceSlot.Id);
        var toDay = await verification.AvailabilityGroupDays.SingleAsync(day => day.Id == targetSlot.Id);
        Assert.Equal(DalKind.NONE, fromDay.Kind);
        Assert.Null(fromDay.IntervalStr);
        Assert.Equal(DalKind.INT, toDay.Kind);
        Assert.Equal("09:00 - 15:00", toDay.IntervalStr);
        var link = Assert.Single(await verification.AvailabilityGroupDayTransfers.ToListAsync());
        Assert.Equal((sourceMember.Id, targetMember.Id, 3), (link.SourceMemberId, link.TargetMemberId, link.DayOfMonth));
        Assert.Equal(62, await verification.AvailabilityGroupDays.CountAsync(
            day => day.AvailabilityGroupMember.AvailabilityGroupId == target.Id));

        async Task VerifyRollbackAsync()
        {
            await using var read = database.CreateContext();
            Assert.Equal("Target", (await read.AvailabilityGroups.SingleAsync(group => group.Id == target.Id)).Name);
            Assert.Equal(2, await read.AvailabilityGroupMembers.CountAsync());
            Assert.Equal(2, await read.AvailabilityGroupDays.CountAsync());
            var from = await read.AvailabilityGroupDays.SingleAsync(day => day.Id == sourceSlot.Id);
            Assert.Equal(DalKind.INT, from.Kind);
            Assert.Equal("9:00 - 15:00", from.IntervalStr);
            Assert.Equal(DalKind.NONE, (await read.AvailabilityGroupDays.SingleAsync(day => day.Id == targetSlot.Id)).Kind);
            Assert.Empty(await read.AvailabilityGroupDayTransfers.ToListAsync());
        }
    }

    private static async Task<int[]> SeedEmployeesAsync(AppDbContext context, int count)
    {
        var employees = Enumerable.Range(1, count).Select(index => TestDataFactory.CreateDalEmployee($"Employee {index}")).ToArray();
        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();
        return employees.Select(employee => employee.Id).ToArray();
    }

    private static IList<AvailabilityGroupDayModel> Month(int count) => Enumerable.Range(1, count)
        .Select(day => TestDataFactory.CreateAvailabilityDayModel(day, AvailabilityKind.NONE)).ToList();

    private static AvailabilityGroupService CreateService(AppDbContext context)
    {
        var transfer = new AvailabilityGroupTransferRepository(context);
        return new AvailabilityGroupService(new AvailabilityGroupRepository(context, transfer),
            new AvailabilityGroupMemberRepository(context), new AvailabilityGroupDayRepository(context), transfer);
    }
}
