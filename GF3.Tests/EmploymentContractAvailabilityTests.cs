using System.Net;
using System.Net.Http.Json;
using BusinessLogicLayer.Availability;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Contracts.EmployeeAvailability;
using BllKind = BusinessLogicLayer.Contracts.Enums.AvailabilityKind;
using DalKind = DataAccessLayer.Models.Enums.AvailabilityKind;
using DalPublication = DataAccessLayer.Models.Enums.AvailabilityPublicationStatus;

namespace GF3.Tests;

public sealed class EmploymentContractAvailabilityTests
{
    [Theory]
    [InlineData(2026,2,28)][InlineData(2028,2,29)][InlineData(2026,4,30)][InlineData(2026,1,31)]
    public async Task CalendarHasEveryDayAndCorrectWeekdays(int year, int month, int days)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var policy = new EmploymentContractPolicyService(db); var calendar = policy.BuildAutomaticWeekdayDays(year, month);
        Assert.Equal(days, calendar.Count); Assert.Equal(Enumerable.Range(1, days), calendar.Select(x => x.DayOfMonth));
        Assert.All(calendar, day => Assert.Equal(new DateOnly(year,month,day.DayOfMonth).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? BllKind.NONE : BllKind.ANY, day.Kind));
        Assert.Empty(await db.AvailabilityGroupDays.ToListAsync());
    }

    [Fact]
    public async Task ReadOverlayOwnMemberPutForbiddenAndToggleRestoresRawRowsWithoutTimestampWrites()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        await using (var db = host.Database.CreateContext())
        {
            (await db.Employees.SingleAsync()).ContractType = "o_prace";
            var group = await db.AvailabilityGroups.SingleAsync(); group.PublicationStatus = DalPublication.Public;
            group.VisibleFromUtc = DateTimeOffset.UtcNow.AddDays(-1); group.VisibleToUtc = DateTimeOffset.UtcNow.AddDays(1);
            group.Members.Add(new() { EmployeeId = 1, Days = [new() { DayOfMonth = 1, Kind = DalKind.NONE }] });
            await db.SaveChangesAsync();
        }
        await host.Login("employee");
        var automatic = (await host.Client.GetFromJsonAsync<EmployeeAvailabilityGroupDto>("/api/employee-availability/1"))!;
        Assert.True(automatic.IsAutomatic); Assert.False(automatic.CanSubmit); Assert.Equal(30, automatic.Slots.Count);
        Assert.Equal(BllKind.ANY, automatic.Slots[0].Kind);
        Assert.Equal(automatic.Slots.Select(x => x.Kind), Assert.Single(automatic.Members).Slots.Select(x => x.Kind));
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync("/api/employee-availability/1/slots", new { slots = Array.Empty<object>() })).StatusCode);
        await using (var db = host.Database.CreateContext())
        {
            Assert.Equal(DalKind.NONE, (await db.AvailabilityGroupDays.SingleAsync()).Kind);
            Assert.Null((await db.AvailabilityGroupMembers.SingleAsync()).EmployeeLastModifiedAtUtc);
            await db.EmploymentContractSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.AutomaticWeekdayAvailability, false));
        }
        var manual = (await host.Client.GetFromJsonAsync<EmployeeAvailabilityGroupDto>("/api/employee-availability/1"))!;
        Assert.False(manual.IsAutomatic); Assert.True(manual.CanSubmit); Assert.Equal(BllKind.NONE, Assert.Single(manual.Slots).Kind);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task BothAlgorithmsHonorHardEmploymentCalendarAndExternalConflicts(bool priority)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        var policy = new EmploymentContractPolicyService(db);
        var schedule = TestDataFactory.CreateScheduleModel(year:2026, month:4, availabilityGroupId:null);
        var employee = TestDataFactory.CreateScheduleEmployeeModel(1); employee.IsPriority = priority;
        var group = EmploymentWeekdayAvailabilityBuilder.Build(2026,4,[1],policy);
        var external = new AvailabilityGroupModel { Year = 2026, Month = 4, Name = "Saved schedule commitments",
            Members = [new() { EmployeeId = 1, Days = [new() { DayOfMonth = 1, Kind = BllKind.NONE }] }] };
        var legacy = new ScheduleGenerator();
        var slots = priority ? await new PriorityScheduleGenerator(legacy).GenerateAsync(schedule,[group,external],[employee],8,false,null,default,respectAvailability:false)
            : await legacy.GenerateAsync(schedule,[group,external],[employee]);
        var assigned = slots.Where(x => x.EmployeeId == 1).ToArray(); Assert.NotEmpty(assigned);
        Assert.All(assigned, slot => { Assert.NotEqual(1,slot.DayOfMonth); Assert.DoesNotContain(new DateOnly(2026,4,slot.DayOfMonth).DayOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }); });
    }

    [Theory]
    [InlineData(false,false)][InlineData(true,false)][InlineData(false,true)][InlineData(true,true)]
    public async Task ContainerGenerationDerivesCalendarWithoutSavedAvailabilityGroup(bool priority, bool savedGroup)
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        await using (var db = host.Database.CreateContext())
        {
            (await db.Employees.SingleAsync()).ContractType = "o_prace";
            db.ScheduleEmployees.Add(new() { ScheduleId = 1, EmployeeId = 1, IsPriority = priority });
            if (savedGroup)
            {
                var graph = await db.Schedules.SingleAsync(); graph.AvailabilityGroupId = 1;
                var group = await db.AvailabilityGroups.SingleAsync();
                group.Members.Add(new() { EmployeeId = 1, Days = Enumerable.Range(1,30).Select(day => new DataAccessLayer.Models.AvailabilityGroupDayModel { DayOfMonth = day, Kind = DalKind.NONE }).ToList() });
                var ordinary = TestDataFactory.CreateDalEmployee("Ordinary", "Worker"); db.Add(ordinary); await db.SaveChangesAsync();
                graph.Employees.Add(new() { EmployeeId = ordinary.Id });
                group.Members.Add(new() { EmployeeId = ordinary.Id, Days = Enumerable.Range(1,30).Select(day => new DataAccessLayer.Models.AvailabilityGroupDayModel { DayOfMonth = day, Kind = DalKind.NONE }).ToList() });
            }
            await db.PriorityGenerationSettings.ExecuteUpdateAsync(x => x.SetProperty(v => v.RespectAvailability, false).SetProperty(v => v.WeekendsOff, false));
            await db.SaveChangesAsync();
        }
        using var scope = host.App.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IContainerService>().GenerateGraphAsync(1,1,true,true,null);
        var assigned = result!.Slots.Where(x => x.EmployeeId == 1).ToArray(); Assert.NotEmpty(assigned);
        Assert.All(assigned, slot => Assert.DoesNotContain(new DateOnly(2026,4,slot.DayOfMonth).DayOfWeek,new[] { DayOfWeek.Saturday,DayOfWeek.Sunday }));
        await using var check = host.Database.CreateContext();
        if (savedGroup)
        {
            Assert.Equal(60,await check.AvailabilityGroupDays.CountAsync());
            Assert.All(await check.AvailabilityGroupDays.ToListAsync(),day => Assert.Equal(DalKind.NONE,day.Kind));
            Assert.DoesNotContain(result.Slots,slot => slot.EmployeeId == 2);
            Assert.All(await check.AvailabilityGroupMembers.ToListAsync(),member => Assert.Null(member.EmployeeLastModifiedAtUtc));
        }
        else Assert.Empty(await check.AvailabilityGroupDays.ToListAsync());
    }
}
