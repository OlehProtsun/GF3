using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using WebApi.ShiftSwaps;

namespace GF3.Tests;

public sealed class EmploymentContractSwapRulesTests
{
    [Theory]
    [InlineData("o_prace", "zlecenie", true, false, true)]
    [InlineData("o_prace", "o_prace", true, false, true)]
    [InlineData("zlecenie", "o_prace", true, false, true)]
    [InlineData("zlecenie", "zlecenie", true, false, false)]
    [InlineData("o_prace", "zlecenie", false, false, false)]
    [InlineData("o_prace", "o_prace", false, false, false)]
    [InlineData("zlecenie", "o_prace", false, false, false)]
    [InlineData("zlecenie", "zlecenie", false, false, false)]
    [InlineData("o_prace", "zlecenie", true, true, false)]
    [InlineData("o_prace", "o_prace", true, true, true)]
    public void PickupApprovalUsesEitherParticipantAndIgnoresAStoredOwnerForManagerOffers(
        string owner, string recipient, bool enabled, bool manual, bool expected)
    {
        var offer = new ShiftSwapRequestModel { FromEmployeeId = 1, IsManagerCreated = manual };
        Assert.Equal(expected, EmploymentSwapRules.RequiresPickupApproval(offer, 2,
            new EmploymentContractSettingsSnapshot(RequirePickupApproval: enabled), new Dictionary<int, string> { [1] = owner, [2] = recipient }));
    }

    [Theory]
    [InlineData("09:00","17:00","09:00","17:00",true)]
    [InlineData("09:00","16:59","09:00","16:59",false)]
    [InlineData("09:00","17:01","09:00","17:01",false)]
    [InlineData("09:00","21:00","09:00","17:00",false)]
    [InlineData("09:00","17:00","10:00","17:00",false)]
    [InlineData("16:00","24:00","16:00","24:00",false)]
    [InlineData("xx:xx","17:00","09:00","17:00",false)]
    public void WholeEightHoursMustMatchConcreteShift(string from, string to, string selectedFrom, string selectedTo, bool allowed)
    {
        var slot = new ScheduleSlotModel { FromTime = from, ToTime = to };
        Assert.Equal(allowed, EmploymentSwapRules.IsWholeEightHourShift(slot, new(selectedFrom, selectedTo)));
        if (allowed) EmploymentSwapRules.ValidateWholeEightHourShift(slot, new(selectedFrom, selectedTo), 1);
        else Assert.Throws<ValidationException>(() => EmploymentSwapRules.ValidateWholeEightHourShift(slot, new(selectedFrom, selectedTo), 1));
    }

    [Theory]
    [InlineData(null,true)][InlineData(0,false)][InlineData(8,true)][InlineData(16,true)]
    public async Task FloorUsesMaxEnrollmentAndDistinctPublicSlots(int? target, bool expected)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var fixture = await ShiftSwapScenarioFactory.SeedAsync(db, new());
        var source = await db.Schedules.FindAsync(fixture.SourceSchedule);
        (await db.Employees.FindAsync(fixture.Owner))!.ContractType = "o_prace";
        (await db.ScheduleEmployees.SingleAsync(x => x.ScheduleId == source!.Id && x.EmployeeId == fixture.Owner)).MinHoursMonth = target;
        var sibling = TestDataFactory.CreateDalSchedule(source!.ContainerId, source.ShopId, "Sibling", source.Year, source.Month);
        sibling.PublicationStatus = SchedulePublicationStatus.Public;
        sibling.Employees.Add(new() { EmployeeId = fixture.Owner, MinHoursMonth = target.HasValue ? target + 8 : null });
        db.Add(sibling); await db.SaveChangesAsync();
        db.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(sibling.Id, 3, 1, fixture.Owner, "09:00", "17:00"));
        await db.SaveChangesAsync(); var policy = new EmploymentContractPolicyService(db);
        var minimum = await policy.GetShopMonthMinimumAsync(fixture.Owner, source.ShopId, source.Year, source.Month);
        Assert.Equal(960, minimum.CurrentMinutes);
        Assert.Equal(target.HasValue ? (target + 8) * 60 : null, minimum.MinimumMinutes);
        var check = await EmploymentSwapRules.CheckOutgoingMinimumAsync(fixture.Owner, source, new("09:00","17:00"), null, null, policy, default);
        Assert.Equal(expected, check!.RequiresException);
        Assert.Null(await EmploymentSwapRules.CheckOutgoingMinimumAsync(fixture.Owner, source, new("09:00","17:00"), source, new("09:00","17:00"), policy, default));
        Assert.False((await policy.GetShopMonthMinimumAsync(fixture.Owner, source.ShopId, source.Year + 1, source.Month)).IsConfigured);
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RequireBelowMinimumGiveApproval, false));
        Assert.Null(await EmploymentSwapRules.CheckOutgoingMinimumAsync(fixture.Owner, source, new("09:00","17:00"), null, null, policy, default));
    }
    public static IEnumerable<object[]> ContractMatrix()
    {
        foreach (var owner in new[] { false,true }) foreach (var receiver in new[] { false,true })
        foreach (var enabled in new[] { false,true }) foreach (var minutes in new[] { 479,480,720 })
            yield return [owner,receiver,enabled,minutes];
    }

    [Theory, MemberData(nameof(ContractMatrix))]
    public async Task WholeShiftPolicyAppliesToEitherParticipantAndCanBeDisabled(bool owner, bool receiver, bool enabled, int minutes)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var f = await ShiftSwapScenarioFactory.SeedAsync(db,new());
        (await db.Employees.FindAsync(f.Owner))!.ContractType = owner ? "o_prace" : "zlecenie";
        (await db.Employees.FindAsync(f.Target))!.ContractType = receiver ? "o_prace" : "zlecenie";
        var slot = (await db.ScheduleSlots.FindAsync(f.SourceSlot))!;
        slot.ToTime = minutes == 479 ? "16:59" : minutes == 720 ? "21:00" : "17:00";
        await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RestrictSwapsToEightHours, enabled)
            .SetProperty(v => v.RequirePickupApproval, false).SetProperty(v => v.RequireBelowMinimumGiveApproval, false));
        await db.SaveChangesAsync();
        var command = ShiftSwapScenarioFactory.Request(f,new(Scope: SwapScope.Private));
        command.ToTime = minutes == 479 ? "16:59" : "17:00";
        var engine = ContractTestFactory.Engine(db);
        if (enabled && (owner || receiver) && minutes != 480)
            await Assert.ThrowsAsync<ValidationException>(() => engine.Create(f.Owner,ContractTestFactory.Employee(f.Owner),command,default));
        else
        {
            var offer = await engine.Create(f.Owner,ContractTestFactory.Employee(f.Owner),command,default);
            await engine.Accept(f.Target,ContractTestFactory.Employee(f.Target),offer.Id,default);
            Assert.Single(await db.ShiftSwapHistories.ToListAsync());
        }
    }

    [Fact]
    public void UnassignedManualShiftIsValidatedForTheReceiver()
    {
        var slot = new ScheduleSlotModel { EmployeeId = null, FromTime = "09:00",ToTime = "17:00" };
        EmploymentSwapRules.ValidateWholeEightHourShift(slot,new("09:00","17:00"),2);
        slot.ToTime = "21:00";
        Assert.Throws<ValidationException>(() => EmploymentSwapRules.ValidateWholeEightHourShift(slot,new("09:00","17:00"),2));
    }

}
