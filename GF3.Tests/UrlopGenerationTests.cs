using BusinessLogicLayer.Contracts.Urlop;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GF3.Tests;

public sealed class UrlopGenerationTests
{
    [Theory]
    [InlineData(false, true, false)][InlineData(true, true, false)][InlineData(true, false, false)]
    [InlineData(false, true, true)][InlineData(true, true, true)][InlineData(true, false, true)]
    public async Task ApprovedIsHardNoneForPreviewDryRunAndWrite(bool priority, bool respectAvailability, bool automatic)
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(new UrlopTestClock());
        await using (var db = host.Database.CreateContext())
        {
            db.ScheduleEmployees.Add(new() { ScheduleId = 1, EmployeeId = 1, IsPriority = priority });
            (await db.Employees.SingleAsync()).ContractType = automatic ? "o_prace" : "zlecenie";
            var graph = await db.Schedules.SingleAsync(); graph.AvailabilityGroupId = 1;
            (await db.AvailabilityGroups.SingleAsync()).Members.Add(new() { EmployeeId = 1,
                Days = Enumerable.Range(1, 30).Select(day => new AvailabilityGroupDayModel { DayOfMonth = day, Kind = DataAccessLayer.Models.Enums.AvailabilityKind.ANY }).ToList() });
            await db.PriorityGenerationSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RespectAvailability, respectAvailability).SetProperty(v => v.WeekendsOff, false));
            await db.SaveChangesAsync();
            await UrlopTestData.Service(db).CreateApprovedAsync(1, 1, 1, 2026, 4, [3, 6, 7], default);
        }
        using var scope = host.App.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContainerService>();
        var dryRun = await service.GenerateGraphAsync(1, 1, true, true, null);
        var graphModel = (await service.GetGraphByIdAsync(1, 1))!;
        var employees = (await service.GetGraphEmployeesAsync(1, 1))!;
        var preview = await service.GenerateGraphPreviewAsync(1, graphModel, employees, null);
        var written = await service.GenerateGraphAsync(1, 1, true, false, null);
        foreach (var result in new[] { dryRun, preview, written })
        {
            Assert.Contains(result.Slots, x => x.EmployeeId == 1);
            Assert.DoesNotContain(result.Slots, x => x.EmployeeId == 1 && new[] { 3, 6, 7 }.Contains(x.DayOfMonth));
        }
        await using var check = host.Database.CreateContext();
        Assert.Equal(written.Slots.Count, await check.ScheduleSlots.CountAsync());
        Assert.All(await check.AvailabilityGroupDays.ToListAsync(), x => Assert.Equal(DataAccessLayer.Models.Enums.AvailabilityKind.ANY, x.Kind));
        Assert.Equal(3, await check.UrlopDays.CountAsync());
        Assert.DoesNotContain("UR", (await check.Schedules.SingleAsync()).Note ?? "");
    }

    [Theory]
    [InlineData("pending", 1)][InlineData("rejected", 1)][InlineData("approved", 2)]
    public async Task NonApprovedAndOtherShopDoNotAffectGeneratedSlots(string status, int vacationShop)
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(new UrlopTestClock());
        await using (var db = host.Database.CreateContext())
        {
            db.ScheduleEmployees.Add(new() { ScheduleId = 1, EmployeeId = 1 });
            db.Shops.Add(TestDataFactory.CreateDalShop("B")); await db.SaveChangesAsync();
        }
        using var scope = host.App.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContainerService>();
        var baseline = await service.GenerateGraphAsync(1, 1, true, true, null);
        await using (var db = host.Database.CreateContext())
        {
            db.UrlopDays.Add(new() { RequestId = Guid.NewGuid(), ShopId = vacationShop, EmployeeId = 1, Date = new(2026, 4, 3), Status = status, CreatedAtUtc = new UrlopTestClock().GetUtcNow() });
            await db.SaveChangesAsync();
        }
        var after = await service.GenerateGraphAsync(1, 1, true, true, null);
        Assert.Equal(baseline.Slots.Select(x => (x.EmployeeId, x.DayOfMonth, x.FromTime, x.ToTime)),
            after.Slots.Select(x => (x.EmployeeId, x.DayOfMonth, x.FromTime, x.ToTime)));
    }
}
