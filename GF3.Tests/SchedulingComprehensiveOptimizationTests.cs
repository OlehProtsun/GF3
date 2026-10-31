using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using GF3.Tests.Infrastructure;
using Xunit.Abstractions;

namespace GF3.Tests;

public sealed class SchedulingComprehensiveOptimizationTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Seeds() => Enumerable.Range(0, 48).Select(seed => new object[] { seed });

    internal static GenerationScenario Tiny(int seed)
    {
        var random = new Random(716000 + seed); var count = 2 + seed % 2; var active = 2 + seed % 2;
        var schedule = new ScheduleModel
        {
            Year = 2026,
            Month = 4,
            PeoplePerShift = 1,
            Shift1Time = "08:00 - 14:00",
            Shift2Time = "14:00 - 20:00",
            MaxHoursPerEmpMonth = new[] { 6, 12, 18, 24 }[seed % 4],
            MaxConsecutiveDays = 1 + seed % 3,
            MaxConsecutiveFull = 1 + seed % 2,
            MaxFullPerMonth = 1 + seed % 3
        };
        var roster = Enumerable.Range(1, count).Select(id => new ScheduleEmployeeModel { EmployeeId = id, DisplayOrder = id, MinHoursMonth = new[] { 0, 6, 12, 24, 36 }[random.Next(5)] }).ToArray();
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 4 };
        foreach (var employee in roster)
        {
            var member = new AvailabilityGroupMemberModel { EmployeeId = employee.EmployeeId };
            for (var day = 1; day <= 30; day++)
            {
                var choice = day > active ? 0 : random.Next(5); member.Days.Add(new()
                {
                    DayOfMonth = day,
                    Kind = choice == 0 ? AvailabilityKind.NONE : choice == 3 ? AvailabilityKind.ANY : AvailabilityKind.INT,
                    IntervalStr = choice switch { 1 => "08:00 - 14:00", 2 => "14:00 - 20:00", 4 => "10:00 - 18:00", _ => null }
                });
            }
            group.Members.Add(member);
        }
        return new($"legacy-reference-{seed}", schedule, roster, [group]);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public async Task LegacyMatchesIndependentSmallReference(int seed)
    {
        var s = Tiny(seed); var reference = LegacyScheduleReference.Enumerate(s.Schedule, s.Employees.ToArray(), s.AvailabilityGroups.ToArray(), 2 + seed % 2);
        var actual = (await new ScheduleGenerator().GenerateAsync(s.Schedule, s.AvailabilityGroups, s.Employees)).ToArray();
        Assert.Empty(ScheduleGenerationOracle.Check(s, actual).HardViolations);
        Assert.Empty(ScheduleGenerationOracle.Check(s, reference.Slots).HardViolations);
        var quality = LegacyScheduleReference.Score(s.Schedule, s.Employees, s.AvailabilityGroups, actual);
        output.WriteLine($"REFERENCE seed={seed},states={reference.States},actual=[{string.Join(',', quality)}],best=[{string.Join(',', reference.Score)}]");
        if (LegacyScheduleReference.Compare(quality, reference.Score) > 0)
        {
            output.WriteLine("INPUT " + System.Text.Json.JsonSerializer.Serialize(s.Employees.Select(e => new { e.EmployeeId, e.MinHoursMonth, Days = s.AvailabilityGroups.SelectMany(g => g.Members).Where(m => m.EmployeeId == e.EmployeeId).SelectMany(m => m.Days).Where(d => d.DayOfMonth <= 3) })));
            output.WriteLine("ACTUAL " + System.Text.Json.JsonSerializer.Serialize(actual.Where(s => s.EmployeeId.HasValue).Select(s => new { s.DayOfMonth, s.EmployeeId, s.FromTime, s.ToTime })));
            output.WriteLine("WITNESS " + System.Text.Json.JsonSerializer.Serialize(reference.Slots.Where(s => s.EmployeeId.HasValue).Select(s => new { s.DayOfMonth, s.EmployeeId, s.FromTime, s.ToTime })));
        }
        Assert.True(LegacyScheduleReference.Compare(quality, reference.Score) <= 0, $"VALID_BUT_SUBOPTIMAL seed={seed}: actual=[{string.Join(',', quality)}]; reference=[{string.Join(',', reference.Score)}]");
    }

    [Fact]
    public async Task LegacyExactMaterializationPreservesActualWindowAndHourAccounting()
    {
        const int seed = 0;
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, 4, 5, 2, seed);
        scenario.Schedule.Shift1Time = "09:00 - 17:00";
        scenario.Schedule.Shift2Time = "15:00 - 21:00";
        scenario.Schedule.MaxHoursPerEmpMonth = 6 + seed % 3 * 6;
        var random = new Random(716100 + seed);
        var windows = new[] { "09:00 - 12:00", "12:00 - 16:00", "16:00 - 17:00", "15:00 - 16:00", "15:00 - 21:00", "09:00 - 21:00" };
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 4 };
        foreach (var employee in scenario.Employees)
        {
            employee.MinHoursMonth = random.Next(4) * 6;
            group.Members.Add(new AvailabilityGroupMemberModel
            {
                EmployeeId = employee.EmployeeId,
                Days = Enumerable.Range(1, 30).Select(day => new AvailabilityGroupDayModel
                {
                    DayOfMonth = day,
                    Kind = day <= 4 ? AvailabilityKind.INT : AvailabilityKind.NONE,
                    IntervalStr = windows[random.Next(windows.Length)]
                }).ToList()
            });
        }
        scenario = scenario with { AvailabilityGroups = [group] };
        var slots = (await new ScheduleGenerator().GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)).ToArray();
        Assert.True(ScheduleGenerationOracle.Check(scenario, slots).HardViolations.Count == 0,
            $"Materialization failure seed={seed}");
    }

    [Fact]
    public async Task LegacyOverlappingTemplatesNeverEmitUnmaterializedPairs()
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, 4, 2, 1, 716049);
        scenario.Schedule.Shift1Time = "09:00 - 17:00";
        scenario.Schedule.Shift2Time = "15:00 - 21:00";
        scenario.Schedule.MaxHoursPerEmpMonth = 1;
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 4 };
        foreach (var employee in scenario.Employees)
        {
            employee.MinHoursMonth = 1;
            group.Members.Add(new AvailabilityGroupMemberModel
            {
                EmployeeId = employee.EmployeeId,
                Days = Enumerable.Range(1, 30).Select(day => new AvailabilityGroupDayModel
                {
                    DayOfMonth = day,
                    Kind = day == 1 ? AvailabilityKind.INT : AvailabilityKind.NONE,
                    IntervalStr = employee.EmployeeId == 1 ? "16:00 - 17:00" : "15:00 - 16:00"
                }).ToList()
            });
        }
        scenario = scenario with { AvailabilityGroups = [group] };
        var slots = (await new ScheduleGenerator().GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)).ToArray();
        Assert.Empty(ScheduleGenerationOracle.Check(scenario, slots).HardViolations);
        Assert.Equal(120, slots.Where(s => s.EmployeeId.HasValue).Sum(s => (TimeSpan.Parse(s.ToTime) - TimeSpan.Parse(s.FromTime)).TotalMinutes));
    }

    [Fact]
    public async Task PrioritySearchBudgetReachesAUsefulLateDateAfterEquivalentLanes()
    {
        var dates = new[] { 2, 3, 4, 5, 6, 9, 10, 11, 12, 13 };
        var schedule = new ScheduleModel
        {
            Year = 2026,
            Month = 11,
            PeoplePerShift = 4,
            Shift1Time = "09:00 - 15:00",
            Shift2Time = "15:00 - 21:00",
            MaxHoursPerEmpMonth = 200,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 2,
            MaxFullPerMonth = 5
        };
        var roster = Enumerable.Range(1, 5).Select(id => new ScheduleEmployeeModel { EmployeeId = id, DisplayOrder = id, IsPriority = id == 1, MinHoursMonth = id == 1 ? 80 : 4 }).ToArray();
        var group = new AvailabilityGroupModel
        {
            Id = 1,
            Year = 2026,
            Month = 11,
            Members = roster.Select(e => new AvailabilityGroupMemberModel
            {
                EmployeeId = e.EmployeeId,
                Days = Enumerable.Range(1, 30).Select(day => new AvailabilityGroupDayModel
                {
                    DayOfMonth = day,
                    Kind = e.IsPriority && dates.Contains(day) ? AvailabilityKind.ANY : !e.IsPriority && day == 13 ? AvailabilityKind.INT : AvailabilityKind.NONE,
                    IntervalStr = !e.IsPriority && day == 13 ? "09:00 - 13:00" : null
                }).ToList()
            }).ToList()
        };
        var slots = (await new PriorityScheduleGenerator(new()).GenerateAsync(schedule, [group], roster, 8, true, null, default)).ToArray();
        var result = ScheduleGenerationOracle.Check(new("priority-distinct-anchor-budget", schedule, roster, [group]), slots, 8);
        Assert.Empty(result.HardViolations);
        Assert.Equal(0, result.Metrics.TotalMinHourDeficitMinutes);
        Assert.Contains(slots, s => s.EmployeeId == 1 && s.DayOfMonth == 13 && s.FromTime == "13:00");
    }

    [Fact]
    public async Task LegacyNeverBridgesARealGapBetweenShiftTemplates()
    {
        var s = Tiny(0); s.Schedule.Shift1Time = "09:00 - 13:00"; s.Schedule.Shift2Time = "15:00 - 21:00";
        s.Schedule.MaxHoursPerEmpMonth = 24; s.Schedule.MaxConsecutiveDays = 5; s.Schedule.MaxFullPerMonth = 5;
        var employee = s.Employees.Take(1).ToArray(); employee[0].MinHoursMonth = 0;
        var group = new AvailabilityGroupModel
        {
            Id = 1,
            Year = 2026,
            Month = 4,
            Members = [new() {EmployeeId=employee[0].EmployeeId,
            Days=Enumerable.Range(1,30).Select(day=>new AvailabilityGroupDayModel {DayOfMonth=day,Kind=day==1?AvailabilityKind.ANY:AvailabilityKind.NONE}).ToList()}]
        };
        var slots = await new ScheduleGenerator().GenerateAsync(s.Schedule, [group], employee);
        var assigned = slots.Where(x => x.EmployeeId.HasValue).ToArray();
        Assert.All(assigned, x => Assert.False(TimeSpan.Parse(x.FromTime) < TimeSpan.FromHours(15) && TimeSpan.Parse(x.ToTime) > TimeSpan.FromHours(13)));
        Assert.Equal(600, assigned.Sum(x => (int)(TimeSpan.Parse(x.ToTime) - TimeSpan.Parse(x.FromTime)).TotalMinutes));
    }

    [Fact]
    public async Task LegacyCanonicalEnrollmentOrderSurvivesCallerPermutation()
    {
        static string[] Rows(IEnumerable<ScheduleSlotModel> slots) => slots.Select(s => $"{s.DayOfMonth}:{s.SlotNo}:{s.EmployeeId}:{s.FromTime}:{s.ToTime}:{s.Status}").ToArray();
        for (var seed = 0; seed < 48; seed++)
        {
            var s = Tiny(seed); var generator = new ScheduleGenerator();
            var first = await generator.GenerateAsync(s.Schedule, s.AvailabilityGroups, s.Employees);
            var reversed = await generator.GenerateAsync(s.Schedule, s.AvailabilityGroups.Reverse(), s.Employees.Reverse());
            output.WriteLine($"PERMUTATION seed={seed}");
            Assert.Equal(Rows(first), Rows(reversed));
        }
    }
}
