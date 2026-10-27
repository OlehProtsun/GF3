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
        for (var repeat = 0; repeat < 3; repeat++)
            Assert.Equal(Tuples(expected), Tuples(await wrapper.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)));
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var configured = database.CreateContext();
        var configuredWrapper = new PriorityAwareScheduleGenerator(legacy, new(legacy), configured);
        foreach (var hours in new[] { 8, 12 })
        {
            await configured.PriorityGenerationSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.ShiftHours, hours).SetProperty(x => x.WeekendsOff, hours == 8));
            for (var repeat = 0; repeat < 3; repeat++)
                Assert.Equal(Tuples(expected), Tuples(await configuredWrapper.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)));
        }
        output.WriteLine($"LEGACY case={id},sha256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join(';', Tuples(expected)))))}");
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
    [InlineData(1, "09:00 - 16:00", "15:00 - 22:00", true)]
    [InlineData(6, "09:00 - 16:00", "15:00 - 22:00", true)]
    [InlineData(8, "09:00 - 16:00", "15:00 - 22:00", true)]
    [InlineData(10, "09:00 - 16:00", "15:00 - 22:00", true)]
    [InlineData(12, "09:00 - 16:00", "15:00 - 22:00", true)]
    [InlineData(8, "09:00 - 13:00", "15:00 - 22:00", false)]
    public async Task ContiguousEnvelopeSupportsExactDurationButNeverBridgesAGap(int hours, string first, string second, bool feasible)
    {
        var scenario = Fixture(2026, 4, null, hours);
        scenario.Schedule.Shift1Time = first; scenario.Schedule.Shift2Time = second;
        scenario.Schedule.MaxHoursPerEmpMonth = hours;
        scenario = scenario with { Employees = scenario.Employees.Take(1).ToArray() };
        var slots = await Generate(scenario, hours, true);
        Check(scenario, slots, hours, true);
        Assert.Equal(feasible ? 1 : 0, slots.Count(s => s.EmployeeId == 1));
        Assert.Equal(Tuples(slots), Tuples(await Generate(scenario, hours, true)));
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
    public async Task MissingSelectedMemberAndDuplicateEnrollmentPreserveCanonicalOutput()
    {
        var scenario = Fixture(2026, 4, null, 8);
        Assert.Empty(scenario.AvailabilityGroups[0].Members);
        var expected = await Generate(scenario, 8, true);
        Assert.Contains(expected, s => s.EmployeeId == 1);
        var duplicate = new ScheduleEmployeeModel { EmployeeId = 1, IsPriority = false, MinHoursMonth = 180, DisplayOrder = 99 };
        var actual = await Generate(scenario with { Employees = scenario.Employees.Append(duplicate).Reverse().ToArray() }, 8, true);
        Assert.Equal(Tuples(expected), Tuples(actual));
        Check(scenario, actual, 8, true);
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
        foreach (var staffing in new[] { 1, 2, 3, 4 })
        foreach (var month in new[] { 2, 4, 7, 11 }) yield return [ count, staffing, 2026, month ];
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
        output.WriteLine($"n={count},staffing={staffing},month={month},baselineMs={baselineMs:F0},priorityMedianMs={durations.Order().ElementAt(1):F0},priorityMaxMs={durations.Max():F0},baselineAllocated={baselineBytes},priorityAllocated={allocated.Average():F0}");
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

    [Theory]
    [InlineData("08:00 - 14:00", null, "09:00", "17:00")]
    [InlineData("16:00 - 23:00", null, "14:00", "22:00")]
    [InlineData("09:00 - 15:00", "10:00 - 22:00", "10:00", "18:00")]
    [InlineData("16:00 - 23:00", "09:00 - 21:00", "13:00", "21:00")]
    public async Task BoundaryAnchorsClampToOpeningAndExternalFreeWindow(string positive, string? external, string start, string end)
    {
        var scenario = Fixture(2026, 4, positive, 8);
        scenario.Schedule.Shift2Time = "15:00 - 22:00";
        scenario = scenario with { Employees = scenario.Employees.Take(1).ToArray() };
        scenario.Schedule.MaxHoursPerEmpMonth = 8;
        if (external is not null)
            scenario = scenario with { AvailabilityGroups = scenario.AvailabilityGroups.Append(new AvailabilityGroupModel {
                Id = 0, Name = "Saved schedule commitments", Year = 2026, Month = 4,
                Members = [new() { EmployeeId = 1, Days = Enumerable.Range(1, 30).Select(day => new AvailabilityGroupDayModel {
                    DayOfMonth = day, Kind = AvailabilityKind.INT, IntervalStr = external }).ToList() }] }).ToArray() };
        string[]? reference = null;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            var slots = await Generate(scenario, 8, true);
            var assigned = Assert.Single(slots, s => s.EmployeeId == 1);
            Assert.Equal((start, end), (assigned.FromTime, assigned.ToTime));
            Check(scenario, slots, 8, true);
            if (reference is not null) Assert.Equal(reference, Tuples(slots));
            reference = Tuples(slots);
        }
    }

    [Theory]
    [InlineData(0, 180, 400)] [InlineData(2, 180, 400)] [InlineData(12, 180, 400)]
    [InlineData(2, 1000000, 16)] [InlineData(2, 0, 400)]
    public async Task NovemberInfeasibleMinimaReturnSafeDeterministicPartial(int priorityCount, int target, int cap)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, 11, 12, 4, 49);
        scenario.Schedule.Shift1Time = "09:00 - 15:00";
        scenario.Schedule.Shift2Time = "15:00 - 22:00";
        scenario.Schedule.MaxHoursPerEmpMonth = cap;
        foreach (var employee in scenario.Employees) { employee.IsPriority = employee.EmployeeId <= priorityCount; employee.MinHoursMonth = target; }
        string[]? reference = null;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var legacy = new ScheduleGenerator();
            var slots = priorityCount == 0
                ? await legacy.GenerateAsync(scenario.Schedule, [], scenario.Employees, ct: bounded.Token)
                : await new PriorityScheduleGenerator(legacy).GenerateAsync(scenario.Schedule, [], scenario.Employees, 8, true, null, bounded.Token);
            Check(scenario, slots, 8, true);
            Assert.Contains(slots, s => s.EmployeeId.HasValue);
            if (priorityCount > 0)
                Assert.All(scenario.Employees.Where(e => e.IsPriority), e => Assert.True(slots.Where(s => s.EmployeeId == e.EmployeeId).Sum(Duration) <= 168 * 60));
            var metrics = ScheduleGenerationOracle.Check(scenario, slots.ToArray(), 8, true).Metrics;
            if (target >= 180) Assert.True(metrics.TotalMinHourDeficitMinutes > 0);
            output.WriteLine($"November priority={priorityCount}, cap={cap}, target={target}, assigned={metrics.TotalAssignedMinutes}, gap={metrics.CoverageGapMinutes}, deficit={metrics.TotalMinHourDeficitMinutes}, sha256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join(';', Tuples(slots)))))}");
            if (reference is not null) Assert.Equal(reference, Tuples(slots));
            reference = Tuples(slots);
        }
    }

    [Fact]
    public async Task ConstructionDoesNotStopAtTheOptionalSearchBudget()
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, 11, 20, 4, 49);
        scenario.Schedule.Shift1Time = "09:00 - 15:00"; scenario.Schedule.Shift2Time = "15:00 - 22:00";
        foreach (var employee in scenario.Employees) { employee.IsPriority = true; employee.MinHoursMonth = 180; }
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var slots = await new PriorityScheduleGenerator(new()).GenerateAsync(scenario.Schedule, [], scenario.Employees, 8, true, null, bounded.Token);
        Check(scenario, slots, 8, true);
        // Witness: four distinct employees work 09-17 on each of the 21 weekdays.
        // Every pair of 8h shifts in a 13h envelope overlaps, so four is also the daily maximum.
        Assert.Equal(21 * 4, slots.Count(s => s.EmployeeId.HasValue));
    }

    [Theory]
    [InlineData(11)] [InlineData(29)] [InlineData(47)]
    public async Task OptionalBudgetDoesNotWasteValidationOnNonImprovingMoves(int seed)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 11, 12, 4, AvailabilityPattern.MixedPartial, seed);
        scenario.Schedule.Shift1Time = "09:00 - 15:00"; scenario.Schedule.Shift2Time = "15:00 - 22:00";
        scenario.Schedule.MaxHoursPerEmpMonth = 200; scenario.Schedule.MaxConsecutiveDays = 5;
        scenario.Schedule.MaxConsecutiveFull = 2; scenario.Schedule.MaxFullPerMonth = 5;
        foreach (var employee in scenario.Employees) { employee.IsPriority = employee.EmployeeId <= 2; employee.MinHoursMonth = 180; }
        var slots = (await Generate(scenario, 8, true)).ToArray();
        Check(scenario, slots, 8, true);
        var potential = scenario.Employees.ToDictionary(e => e.EmployeeId, e => Math.Max(1, Math.Min(200 * 60,
            Enumerable.Range(1, 30).Sum(day => { var w = ScheduleGenerationOracle.Window(scenario, e.EmployeeId, day);
                return e.IsPriority ? w.To > w.From && new DateOnly(2026, 11, day).DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) ? 480 : 0
                    : Math.Max(0, Math.Min(1320, w.To) - Math.Max(540, w.From)); }))));
        double[] Score(ScheduleSlotModel[] state)
        {
            var totals = scenario.Employees.ToDictionary(e => e.EmployeeId, e => state.Where(s => s.EmployeeId == e.EmployeeId).Sum(Duration));
            return [scenario.Employees.Where(e => e.IsPriority).Sum(e => Math.Max(0, 10800 - totals[e.EmployeeId])),
                -totals.Values.Sum(), scenario.Employees.Where(e => !e.IsPriority).Sum(e => Math.Max(0, 10800 - totals[e.EmployeeId])),
                scenario.Employees.Sum(e => Math.Pow(Math.Max(0, 10800 - totals[e.EmployeeId]) / (double)Math.Min(potential[e.EmployeeId], 10800), 2)),
                scenario.Employees.Where(e => !e.IsPriority).Sum(e => Math.Pow(totals[e.EmployeeId] / (double)potential[e.EmployeeId], 2)),
                scenario.Employees.Sum(e => Math.Pow(ScheduleGenerationOracle.LongestRun(state.Where(s => s.EmployeeId == e.EmployeeId).Select(s => s.DayOfMonth)), 2))];
        }
        bool Better(double[] left, double[] right)
        {
            for (var index = 0; index < left.Length; index++) { var comparison = left[index].CompareTo(right[index]); if (comparison != 0) return comparison < 0; }
            return false;
        }
        var baseline = Score(slots);
        output.WriteLine($"QUALITY seed={seed},vector={string.Join(',', baseline)}");
        // Fixed constructive vacancy-handover witnesses: the earlier fill stopped at
        // 74,520 / 71,460 minutes although these higher coverage totals are feasible.
        if (seed == 29) Assert.True(-baseline[1] >= 74580);
        if (seed == 47) Assert.True(-baseline[1] >= 71580);
        var movable = Enumerable.Range(0, slots.Length).Where(i => slots[i].EmployeeId is > 2).ToArray();
        bool Safe(int employee, ScheduleSlotModel[] state)
        {
            var own = Enumerable.Range(0, state.Length).Where(i => state[i].EmployeeId == employee).ToArray();
            if (own.Sum(i => Duration(state[i])) > 200 * 60 || ScheduleGenerationOracle.LongestRun(own.Select(i => state[i].DayOfMonth)) > 5) return false;
            var full = new List<int>();
            foreach (var day in own.GroupBy(i => state[i].DayOfMonth))
            {
                if (day.Select(i => i / 4 % 2).Distinct().Count() != day.Count()) return false;
                var window = ScheduleGenerationOracle.Window(scenario, employee, day.Key);
                var end = -1;
                foreach (var index in day.OrderBy(i => TimeSpan.Parse(state[i].FromTime)))
                {
                    var from = (int)TimeSpan.Parse(state[index].FromTime).TotalMinutes;
                    var to = (int)TimeSpan.Parse(state[index].ToTime).TotalMinutes;
                    if (from < window.From || to > window.To || from < end) return false;
                    end = to;
                }
                if (day.Count() > 1) full.Add(day.Key);
            }
            return full.Count <= 5 && ScheduleGenerationOracle.LongestRun(full) <= 2;
        }
        foreach (var first in movable)
        foreach (var second in movable.Where(index => index > first && slots[index].EmployeeId != slots[first].EmployeeId))
        {
            var trial = slots.Select(s => new ScheduleSlotModel { DayOfMonth = s.DayOfMonth, SlotNo = s.SlotNo, EmployeeId = s.EmployeeId,
                Status = s.Status, FromTime = s.FromTime, ToTime = s.ToTime }).ToArray();
            (trial[first].EmployeeId, trial[second].EmployeeId) = (trial[second].EmployeeId, trial[first].EmployeeId);
            var quality = Score(trial);
            if (!Better(quality, baseline)) continue;
            if (!Safe(trial[first].EmployeeId!.Value, trial) || !Safe(trial[second].EmployeeId!.Value, trial)) continue;
            Assert.Empty(ScheduleGenerationOracle.Check(scenario, trial, 8, true).HardViolations);
            Assert.Fail($"Independent improving pair remains: seed={seed}, baseline={string.Join(',', baseline)}, witness={string.Join(',', quality)}");
        }
    }

    [Fact]
    public async Task CancellationDuringConstructionPreservesInputAndAllowsRetry()
    {
        var scenario = Fixture(2026, 11, null, 180);
        var before = System.Text.Json.JsonSerializer.Serialize(scenario);
        using var bounded = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PriorityScheduleGenerator(new()).GenerateAsync(
            scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, 8, true, null, bounded.Token));
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(scenario));
        Check(scenario, await Generate(scenario, 8, true), 8, true);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RejectedHalfSwapsAndBoundedSearchLeaveAValidStableState(bool large)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, 7, large ? 20 : 3, large ? 4 : 1, 49);
        scenario.Schedule.Shift1Time = "09:00 - 15:00"; scenario.Schedule.Shift2Time = "15:00 - 22:00";
        scenario.Employees[0].IsPriority = true;
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 7 };
        foreach (var employee in scenario.Employees)
            group.Members.Add(new() { EmployeeId = employee.EmployeeId, Days = Enumerable.Range(1, 31).Select(day => new AvailabilityGroupDayModel {
                DayOfMonth = day, Kind = employee.IsPriority ? AvailabilityKind.NONE : large ? AvailabilityKind.ANY : AvailabilityKind.INT,
                IntervalStr = employee.IsPriority || large ? null : employee.EmployeeId == 2 ? "09:00 - 15:00" : "15:00 - 22:00" }).ToList() });
        scenario = scenario with { AvailabilityGroups = [group] };
        string[]? reference = null;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var slots = await new PriorityScheduleGenerator(new()).GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups,
                scenario.Employees, 8, true, null, bounded.Token);
            Check(scenario, slots, 8, true);
            Assert.Equal(0, ScheduleGenerationOracle.Check(scenario, slots.ToArray(), 8, true).Metrics.CoverageGapMinutes);
            if (reference is not null) Assert.Equal(reference, Tuples(slots));
            reference = Tuples(slots);
        }
    }

    [Theory]
    [InlineData(1, 1, 2)] [InlineData(2, 1, 2)] [InlineData(1, 2, 3)]
    public async Task TinyIndependentEnumerationAttainsBestQuality(int activeDays, int staffing, int count)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, 4, count, staffing, 49);
        scenario.Schedule.Shift1Time = "09:00 - 15:00"; scenario.Schedule.Shift2Time = "15:00 - 21:00";
        scenario.Schedule.MaxHoursPerEmpMonth = 8;
        foreach (var employee in scenario.Employees) { employee.IsPriority = employee.EmployeeId == 1; employee.MinHoursMonth = 8; }
        scenario = scenario with { AvailabilityGroups = [new() { Id = 1, Year = 2026, Month = 4,
            Members = scenario.Employees.Select(e => new AvailabilityGroupMemberModel { EmployeeId = e.EmployeeId,
                Days = Enumerable.Range(activeDays + 1, 30 - activeDays).Select(day => new AvailabilityGroupDayModel { DayOfMonth = day, Kind = AvailabilityKind.NONE }).ToList() }).ToList() }] };
        var states = 0;
        var state = new List<(int Day, int Shift, int Employee, int From, int To)>();
        double[]? best = null;
        double[] Score(int[] worked)
        {
            var deficits = worked.Select(minutes => Math.Max(0, 480 - minutes)).ToArray();
            return [deficits[0], activeDays * staffing * 720 - worked.Sum(), deficits.Skip(1).Sum(),
                deficits.Sum(d => Math.Pow(d / 480.0, 2)), worked.Skip(1).Sum(m => Math.Pow(m / 480.0, 2))];
        }
        int CompareScore(double[] left, double[] right)
        {
            for (var i = 0; i < left.Length; i++) { var comparison = left[i].CompareTo(right[i]); if (comparison != 0) return comparison; }
            return 0;
        }
        void Visit(int position)
        {
            Assert.True(++states <= 200_000, "INCONCLUSIVE: independent reference reached its state cap.");
            if (position == activeDays * 2 * staffing)
            {
                var quality = Score(Enumerable.Range(1, count).Select(id => state.Where(s => s.Employee == id).Sum(s => s.To - s.From)).ToArray());
                if (best is null || CompareScore(quality, best) < 0) best = quality;
                return;
            }
            Visit(position + 1);
            var day = position / (2 * staffing) + 1;
            var shift = position / staffing % 2;
            var low = shift == 0 ? 540 : 900; var high = low + 360;
            for (var employee = 1; employee <= count; employee++)
            {
                if (state.Any(s => s.Day == day && s.Employee == employee && (s.Shift == shift || employee == 1))) continue;
                var endpoints = Enumerable.Range(0, 7).Select(i => 540 + i * 120).ToArray();
                foreach (var from in endpoints)
                foreach (var to in endpoints.Where(to => to > from))
                {
                    if (employee == 1 ? to - from != 480 || from >= high || to <= low : from < low || to > high) continue;
                    if (state.Where(s => s.Employee == employee).Sum(s => s.To - s.From) + to - from > 480) continue;
                    if (state.Any(s => s.Day == day && s.Employee == employee && from < s.To && s.From < to)) continue;
                    if (Enumerable.Range(from, to - from).Any(minute => state.Count(s => s.Day == day && s.From <= minute && minute < s.To) >= staffing)) continue;
                    state.Add((day, shift, employee, from, to));
                    Visit(position + 1);
                    state.RemoveAt(state.Count - 1);
                }
            }
        }
        Visit(0);
        var slots = await Generate(scenario, 8, true);
        Check(scenario, slots, 8, true);
        var actual = Score(scenario.Employees.Select(e => slots.Where(s => s.EmployeeId == e.EmployeeId).Sum(Duration)).ToArray());
        output.WriteLine($"EXACT days={activeDays},staffing={staffing},employees={count},states={states},best={string.Join(',', best!)},actual={string.Join(',', actual)}");
        Assert.Equal(best, actual);
    }

    private static readonly int[] PairwiseLevels = [5, 5, 4, 5, 2, 4, 9];
    // Deterministic greedy two-way cover; the following test verifies every declared pair.
    private static readonly int[][] PairwiseRows =
    [
        [0, 0, 0, 0, 0, 0, 0],
        [0, 1, 1, 1, 1, 1, 1],
        [1, 0, 2, 2, 1, 2, 2],
        [1, 1, 3, 3, 0, 3, 3],
        [2, 2, 0, 4, 1, 3, 4],
        [3, 3, 1, 4, 0, 2, 5],
        [2, 4, 2, 0, 0, 1, 6],
        [3, 4, 3, 1, 1, 0, 7],
        [4, 2, 1, 2, 0, 0, 8],
        [4, 3, 0, 3, 1, 1, 0],
        [0, 2, 2, 3, 0, 2, 7],
        [0, 3, 3, 0, 1, 3, 8],
        [1, 3, 2, 1, 0, 0, 4],
        [2, 0, 3, 2, 1, 1, 5],
        [3, 0, 1, 3, 1, 3, 6],
        [4, 4, 0, 1, 1, 2, 3],
        [4, 0, 2, 4, 0, 3, 1],
        [1, 2, 1, 0, 0, 1, 2],
        [4, 1, 3, 0, 0, 2, 4],
        [1, 1, 0, 4, 0, 0, 6],
        [3, 1, 2, 2, 0, 3, 0],
        [0, 4, 3, 4, 0, 3, 2],
        [2, 4, 1, 3, 0, 0, 1],
        [2, 2, 3, 1, 0, 2, 0],
        [2, 3, 0, 2, 0, 1, 7],
        [3, 0, 0, 1, 0, 1, 8],
        [0, 4, 1, 2, 0, 1, 4],
        [1, 4, 0, 1, 0, 3, 5],
        [3, 2, 0, 0, 0, 2, 1],
        [0, 0, 1, 4, 0, 1, 3],
        [2, 1, 0, 1, 0, 0, 2],
        [0, 1, 2, 0, 0, 0, 5],
        [0, 2, 3, 1, 0, 2, 6],
        [1, 0, 1, 0, 0, 3, 7],
        [1, 1, 2, 3, 0, 2, 8],
        [2, 2, 2, 0, 0, 0, 3],
        [1, 3, 3, 2, 0, 0, 1],
        [1, 4, 1, 4, 0, 0, 0],
        [2, 4, 0, 4, 0, 0, 8],
        [3, 0, 0, 3, 0, 0, 4],
        [3, 3, 0, 2, 0, 0, 3],
        [3, 3, 0, 3, 0, 0, 2],
        [4, 1, 0, 4, 0, 0, 7],
        [4, 2, 0, 3, 0, 0, 5],
        [4, 3, 0, 2, 0, 0, 6],
        [4, 0, 0, 0, 0, 0, 2]
    ];

    public static IEnumerable<object[]> PairwiseCases() => PairwiseRows.Select((row, index) =>
        new object[] { index, row[0], row[1], row[2], row[3], row[4], row[5], row[6] });

    [Fact]
    public void PairwiseCoverContainsEveryDeclaredFactorPair()
    {
        for (var first = 0; first < PairwiseLevels.Length; first++)
        for (var second = first + 1; second < PairwiseLevels.Length; second++)
        for (var a = 0; a < PairwiseLevels[first]; a++)
        for (var b = 0; b < PairwiseLevels[second]; b++)
            Assert.Contains(PairwiseRows, row => row[first] == a && row[second] == b);
    }

    [Theory]
    [MemberData(nameof(PairwiseCases))]
    public async Task PairwisePriorityBoundaryMatrix(int index, int calendar, int size, int staff, int duration, int weekends, int selection, int pattern)
    {
        var (year, month) = new[] { (2026, 2), (2028, 2), (2026, 4), (2026, 7), (2026, 11) }[calendar];
        var count = new[] { 1, 3, 5, 10, 20 }[size];
        var hours = new[] { 1, 6, 8, 10, 12 }[duration];
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(year, month, count, staff + 1, (AvailabilityPattern)pattern, 49);
        scenario.Schedule.Shift1Time = "09:00 - 15:00"; scenario.Schedule.Shift2Time = "15:00 - 22:00";
        scenario.Schedule.MaxHoursPerEmpMonth = 80; scenario.Schedule.MaxConsecutiveDays = 3;
        foreach (var employee in scenario.Employees)
        {
            employee.IsPriority = selection == 3 || employee.EmployeeId <= selection;
            employee.MinHoursMonth = employee.EmployeeId % 2 == 0 ? 180 : 0;
        }
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);
        var legacy = new ScheduleGenerator(); var wrapper = new PriorityAwareScheduleGenerator(legacy, new(legacy), db);
        string[]? reference = null;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var slots = selection == 0 ? await wrapper.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, ct: bounded.Token)
                : await new PriorityScheduleGenerator(legacy).GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, hours, weekends == 1, null, bounded.Token);
            Check(scenario, slots, hours, weekends == 1);
            if (pattern == (int)AvailabilityPattern.AllAvailable) Assert.Contains(slots, s => s.EmployeeId.HasValue);
            if (reference is not null) Assert.Equal(reference, Tuples(slots));
            reference = Tuples(slots);
        }
        if (selection == 0)
            Assert.Equal(Tuples(await legacy.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)), reference);
        else
            Assert.Equal(reference, Tuples(await Generate(scenario with { Employees = scenario.Employees.Reverse().ToArray(),
                AvailabilityGroups = scenario.AvailabilityGroups.Reverse().ToArray() }, hours, weekends == 1)));
        output.WriteLine($"PAIRWISE case={index},calendar={calendar},size={size},staffing={staff + 1},hours={hours},weekendsOff={weekends == 1},selection={selection},pattern={pattern},hard=0");
    }

    [Theory]
    [Trait("Category", "GenerationStress")]
    [Trait("Category", "LocalOnly")]
    [MemberData(nameof(ScheduleGenerationScenarioMatrixTests.StressSeeds), MemberType = typeof(ScheduleGenerationScenarioMatrixTests))]
    public async Task PriorityFixedSeedsRemainSafeAndDeterministic(int seed)
    {
        var months = new[] { (2026, 2), (2028, 2), (2026, 4), (2026, 7), (2026, 11) };
        var (year, month) = months[seed % months.Length];
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(year, month, 1 + seed % 20, 1 + seed % 4,
            (AvailabilityPattern)(seed % 9), seed);
        scenario.Schedule.Shift1Time = "09:00 - 15:00"; scenario.Schedule.Shift2Time = "15:00 - 22:00";
        scenario.Schedule.MaxHoursPerEmpMonth = 80 + seed % 5 * 40; scenario.Schedule.MaxConsecutiveDays = 1 + seed % 5;
        var hours = new[] { 1, 6, 8, 10, 12 }[seed % 5];
        foreach (var employee in scenario.Employees) { employee.IsPriority = seed % 4 == 0 || employee.EmployeeId <= 1 + seed % 2; employee.MinHoursMonth = 180; }
        string[]? reference = null;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var slots = await new PriorityScheduleGenerator(new()).GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups,
                scenario.Employees, hours, seed % 2 == 0, null, bounded.Token);
            Check(scenario, slots, hours, seed % 2 == 0);
            if (reference is not null) Assert.Equal(reference, Tuples(slots));
            reference = Tuples(slots);
        }
    }

    [Fact]
    public Task PathologicalSeed1139CompletesWithinTheStressBound() => PriorityFixedSeedsRemainSafeAndDeterministic(1139);

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
