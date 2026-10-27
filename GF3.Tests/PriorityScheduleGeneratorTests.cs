using System.Diagnostics;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace GF3.Tests;

public sealed class PriorityScheduleGeneratorTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> LegacyCases() => ScheduleGenerationScenarioMatrixTests.DefaultCases();

    [Theory]
    [MemberData(nameof(LegacyCases))]
    public async Task NoPriorityIsIdenticalWithoutSettingsTable(string id, int year, int month, int pattern,
        int count, int staffing, int seed, string mode)
    {
        var scenario = ScheduleGenerationScenarioMatrixTests.Build(id, year, month, (AvailabilityPattern)pattern, count, staffing, seed, mode);
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);
        var legacy = new ScheduleGenerator();
        var wrapper = new PriorityAwareScheduleGenerator(legacy, new(legacy), db);
        var expected = await legacy.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees);
        var actual = await wrapper.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees);
        Assert.Equal(Tuples(expected), Tuples(actual));
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Theory]
    [InlineData("09:00 - 15:00", "09:00", "17:00", "17:00", "21:00")]
    [InlineData("15:00 - 21:00", "13:00", "21:00", "09:00", "13:00")]
    public async Task ExactEightHourHandovers(string positive, string start, string end, string normalStart, string normalEnd)
    {
        var scenario = Fixture(2026, 4, positive, 8);
        scenario = scenario with { Employees = scenario.Employees.Take(2).ToArray() };
        for (var repeat = 0; repeat < 3; repeat++)
        {
            var slots = await Generate(scenario, 8, true);
            var day = slots.Where(s => s.DayOfMonth == 1).ToArray();
            var priority = Assert.Single(day, s => s.EmployeeId == 1);
            Assert.Equal((start, end), (priority.FromTime, priority.ToTime));
            var regular = Assert.Single(day, s => s.EmployeeId == 2);
            Assert.Equal((normalStart, normalEnd), (regular.FromTime, regular.ToTime));
            Check(scenario, slots, 8, true);
        }
    }

    [Theory]
    [InlineData(6)] [InlineData(8)] [InlineData(10)] [InlineData(12)]
    public async Task ExactDurationsTargetsDeterminismAndPermutation(int hours)
    {
        var scenario = Fixture(2028, 2, null, 70);
        scenario.Schedule.MaxHoursPerEmpMonth = 160;
        string[]? previous = null;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            var slots = await Generate(scenario, hours, true);
            Check(scenario, slots, hours, true);
            Assert.True(slots.Where(s => s.EmployeeId == 1).Sum(Duration) >= 70 * 60);
            if (previous is not null) Assert.Equal(previous, Tuples(slots));
            previous = Tuples(slots);
        }
        Assert.Equal(previous, Tuples(await Generate(scenario with { Employees = scenario.Employees.Reverse().ToArray() }, hours, true)));
    }

    [Theory]
    [InlineData("NONE")] [InlineData("invalid")] [InlineData("09:00 - 10:00")]
    public async Task ImpossiblePriorityRemainsUnfilled(string availability)
    {
        var scenario = Fixture(2026, 4, availability, 80);
        if (availability == "09:00 - 10:00") scenario.Schedule.Shift2Time = "";
        var slots = await Generate(scenario, 8, true);
        Assert.DoesNotContain(slots, s => s.EmployeeId == 1);
        Check(scenario, slots, 8, true);
    }

    [Fact]
    public async Task ExternalConstraintsAreNeverExtendedAndNameAloneIsNotTrusted()
    {
        var scenario = Fixture(2026, 4, "09:00 - 15:00", 8);
        var external = new AvailabilityGroupModel { Id = 0, Name = "Saved schedule commitments", Year = 2026, Month = 4,
            Members = [new() { EmployeeId = 1, Days = [new() { DayOfMonth = 1, Kind = AvailabilityKind.INT, IntervalStr = "09:00 - 15:00" }] }] };
        scenario = scenario with { AvailabilityGroups = scenario.AvailabilityGroups.Append(external).ToArray() };
        Assert.DoesNotContain(await Generate(scenario, 8, true), s => s.EmployeeId == 1 && s.DayOfMonth == 1);
        external.Id = 99;
        Assert.Contains(await Generate(scenario, 8, true), s => s.EmployeeId == 1 && s.DayOfMonth == 1);
    }

    [Theory]
    [InlineData(2026, 2)] [InlineData(2028, 2)] [InlineData(2026, 4)] [InlineData(2026, 7)]
    public async Task WeekendsAndCapsAreHard(int year, int month)
    {
        var scenario = Fixture(year, month, null, 1000);
        scenario.Schedule.MaxHoursPerEmpMonth = 70;
        scenario.Schedule.MaxConsecutiveDays = 2;
        foreach (var weekendsOff in new[] { true, false })
            Check(scenario, await Generate(scenario, 8, weekendsOff), 8, weekendsOff);
    }

    [Fact]
    public async Task CancellationAndSettingsSnapshot()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = Fixture(2026, 4, null, 16);
        var legacy = new ScheduleGenerator();
        var wrapper = new PriorityAwareScheduleGenerator(legacy, new(legacy), db);
        await db.PriorityGenerationSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.ShiftHours, 6));
        var slots = await wrapper.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees);
        Assert.All(slots.Where(s => s.EmployeeId == 1), s => Assert.Equal(360, Duration(s)));
        await db.PriorityGenerationSettings.ExecuteDeleteAsync();
        slots = await wrapper.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees);
        Assert.All(slots.Where(s => s.EmployeeId == 1), s => Assert.Equal(480, Duration(s)));
        Assert.Empty(await db.PriorityGenerationSettings.ToListAsync());
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wrapper.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, ct: cancelled.Token));
        Assert.Empty(await db.ScheduleSlots.ToListAsync());
    }

    public static IEnumerable<object[]> Benchmarks()
    {
        foreach (var count in new[] { 5, 10, 20 })
        foreach (var staffing in new[] { 1, 2, 3 })
        foreach (var month in new[] { 2, 4, 7 }) yield return [ count, staffing, 2026, month ];
    }

    [Theory]
    [MemberData(nameof(Benchmarks))]
    public async Task BoundedBenchmark(int count, int staffing, int year, int month)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(year, month, count, staffing, 11);
        scenario.Schedule.Shift1Time = "09:00 - 15:00"; scenario.Schedule.Shift2Time = "15:00 - 21:00";
        foreach (var employee in scenario.Employees) { employee.IsPriority = employee.EmployeeId <= 2; employee.MinHoursMonth = 70; }
        var before = GC.GetTotalAllocatedBytes(); var watch = Stopwatch.StartNew();
        await new ScheduleGenerator().GenerateAsync(scenario.Schedule, [], scenario.Employees);
        var baselineMs = watch.Elapsed.TotalMilliseconds; var baselineBytes = GC.GetTotalAllocatedBytes() - before;
        var durations = new List<double>(); var allocated = new List<long>();
        string[]? reference = null;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            before = GC.GetTotalAllocatedBytes(); watch.Restart();
            var slots = await Generate(scenario, 8, true);
            durations.Add(watch.Elapsed.TotalMilliseconds); allocated.Add(GC.GetTotalAllocatedBytes() - before);
            Check(scenario, slots, 8, true);
            var tuples = Tuples(slots);
            if (reference is not null) Assert.Equal(reference, tuples);
            reference = tuples;
            Assert.True(durations[^1] < Math.Max(5_000, baselineMs * 20));
        }
        output.WriteLine($"n={count},staffing={staffing},month={month},baselineMs={baselineMs:F0},priorityMs={durations.Average():F0},priorityMaxMs={durations.Max():F0},baselineAllocated={baselineBytes},priorityAllocated={allocated.Average():F0}");
    }

    public static IEnumerable<object[]> PriorityMatrixCases()
    {
        foreach (var pattern in Enum.GetValues<AvailabilityPattern>())
        foreach (var staffing in new[] { 1, 2, 4 })
        foreach (var allPriority in new[] { false, true })
            yield return [ (int)pattern, staffing, allPriority ];
    }

    [Theory]
    [MemberData(nameof(PriorityMatrixCases))]
    public async Task PriorityMatrixPreservesIndependentHardInvariants(int pattern, int staffing, bool allPriority)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 4, 5, staffing, (AvailabilityPattern)pattern, 29);
        scenario.Schedule.MaxConsecutiveDays = 3; scenario.Schedule.MaxFullPerMonth = 4; scenario.Schedule.MaxConsecutiveFull = 2;
        foreach (var employee in scenario.Employees) { employee.IsPriority = allPriority || employee.EmployeeId <= 2; employee.MinHoursMonth = employee.EmployeeId % 2 == 0 ? 70 : 0; }
        string[]? reference = null;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            var slots = await Generate(scenario, 8, true);
            Check(scenario, slots, 8, true);
            var tuples = Tuples(slots);
            if (reference is not null) Assert.Equal(reference, tuples);
            reference = tuples;
        }
        var permuted = await Generate(scenario with { Employees = scenario.Employees.Reverse().ToArray(), AvailabilityGroups = scenario.AvailabilityGroups.Reverse().ToArray() }, 8, true);
        Assert.Equal(reference, Tuples(permuted));
    }

    [Fact]
    public async Task NoPriorityIgnoresDifferentStoredSettingsAndCancellationMatchesLegacy()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, 2, 3, 1, 47);
        var legacy = new ScheduleGenerator(); var wrapper = new PriorityAwareScheduleGenerator(legacy, new(legacy), db);
        var expected = Tuples(await legacy.GenerateAsync(scenario.Schedule, [], scenario.Employees));
        foreach (var hours in new[] { 1, 8, 12 })
        {
            await db.PriorityGenerationSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.ShiftHours, hours).SetProperty(x => x.WeekendsOff, hours == 8));
            Assert.Equal(expected, Tuples(await wrapper.GenerateAsync(scenario.Schedule, [], scenario.Employees)));
        }
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wrapper.GenerateAsync(scenario.Schedule, [], scenario.Employees, ct: cancelled.Token));
    }

    [Fact]
    public async Task DuplicateWindowsIntersectAndRealShiftGapsRemainHard()
    {
        var scenario = Fixture(2026, 4, "09:00 - 15:00", 8);
        var member = scenario.AvailabilityGroups[0].Members.Single();
        member.Days.Add(new() { DayOfMonth = 1, Kind = AvailabilityKind.INT, IntervalStr = "10:00 - 14:00" });
        var slots = await Generate(scenario, 8, true);
        Assert.Contains(slots, s => s.DayOfMonth == 1 && s.EmployeeId == 1 && s.FromTime == "10:00" && s.ToTime == "18:00");
        Check(scenario, slots, 8, true);
        member.Days.Add(new() { DayOfMonth = 1, Kind = AvailabilityKind.NONE });
        Assert.DoesNotContain(await Generate(scenario, 8, true), s => s.DayOfMonth == 1 && s.EmployeeId == 1);
        scenario.Schedule.Shift2Time = "16:00 - 21:00";
        Assert.DoesNotContain(await Generate(scenario, 8, true), s => s.EmployeeId == 1);
    }

    [Fact]
    public async Task ScarcePriorityTargetsAndOrdinaryTargetsReachConstructivelyFeasibleReference()
    {
        var scenario = Fixture(2026, 4, null, 8);
        scenario.Employees[1].IsPriority = true; scenario.Employees[1].MinHoursMonth = 8;
        scenario.Employees[2].MinHoursMonth = 4;
        scenario.Schedule.MaxHoursPerEmpMonth = 8;
        var scarce = new AvailabilityGroupMemberModel { EmployeeId = 1, Days = Enumerable.Range(2, 29)
            .Select(day => new AvailabilityGroupDayModel { DayOfMonth = day, Kind = AvailabilityKind.NONE }).ToList() };
        scenario.AvailabilityGroups[0].Members.Add(scarce);
        var slots = await Generate(scenario, 8, true);
        Check(scenario, slots, 8, true);
        // Independent witness: employee 1 day 1 09-17; employee 2 day 2 09-17; employee 3 day 1 17-21.
        Assert.Equal(480, slots.Where(s => s.EmployeeId == 1).Sum(Duration));
        Assert.Equal(480, slots.Where(s => s.EmployeeId == 2).Sum(Duration));
        Assert.True(slots.Where(s => s.EmployeeId == 3).Sum(Duration) >= 240);
    }

    private static GenerationScenario Fixture(int year, int month, string? positive, int target)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(year, month, 3, 1, 11);
        scenario.Schedule.Shift1Time = "09:00 - 15:00"; scenario.Schedule.Shift2Time = "15:00 - 21:00";
        scenario.Employees[0].IsPriority = true; scenario.Employees[0].MinHoursMonth = target;
        var group = new AvailabilityGroupModel { Id = 1, Year = year, Month = month };
        if (positive is not null)
            group.Members.Add(new() { EmployeeId = 1, Days = Enumerable.Range(1, DateTime.DaysInMonth(year, month)).Select(day => new AvailabilityGroupDayModel {
                DayOfMonth = day, Kind = positive == "NONE" ? AvailabilityKind.NONE : AvailabilityKind.INT, IntervalStr = positive }).ToList() });
        return scenario with { AvailabilityGroups = [group] };
    }

    private static async Task<IList<ScheduleSlotModel>> Generate(GenerationScenario scenario, int hours, bool weekendsOff) =>
        await new PriorityScheduleGenerator(new()).GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, hours, weekendsOff, null, default);

    private static int Duration(ScheduleSlotModel slot) => (int)(TimeSpan.Parse(slot.ToTime) - TimeSpan.Parse(slot.FromTime)).TotalMinutes;
    private static string[] Tuples(IEnumerable<ScheduleSlotModel> slots) => slots.Select(s => $"{s.DayOfMonth}:{s.SlotNo}:{s.FromTime}:{s.ToTime}:{s.Status}:{s.EmployeeId}").ToArray();

    private static void Check(GenerationScenario scenario, IEnumerable<ScheduleSlotModel> slots, int hours, bool weekendsOff)
    {
        var array = slots.ToArray();
        var result = ScheduleGenerationOracle.Check(scenario, array, hours, weekendsOff);
        Assert.True(result.HardViolations.Count == 0, string.Join(",", result.HardViolations.Select(v => $"{v.Code}:day{v.DayOfMonth}:emp{v.EmployeeOrdinal}")));
    }
}
