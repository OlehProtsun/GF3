using System.Diagnostics;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using GF3.Tests.Infrastructure;
using Xunit.Abstractions;

namespace GF3.Tests;

public sealed class ScheduleGenerationScenarioMatrixTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> DefaultCases()
    {
        foreach (var (year, month) in new[] { (2026, 2), (2028, 2), (2026, 4), (2026, 7) })
            foreach (var pattern in Enum.GetValues<AvailabilityPattern>())
                yield return new object[] { $"calendar-{year}-{month}-{pattern}", year, month, (int)pattern, 3, 1, 11, "normal" };
        foreach (var seed in new[] { 11, 29, 47 })
            foreach (var mode in new[] { "empty", "one", "staffing2", "staffing3", "single", "overlap", "separated", "hours", "streak1", "streak2", "streak5", "full", "override", "impossible", "unequal", "groups", "short", "offset", "duplicate", "weekends" })
                yield return new object[] { $"{mode}-seed{seed}", 2026, 2, 0, 5, 1, seed, mode };
    }

    [Theory]
    [MemberData(nameof(DefaultCases))]
    public async Task BoundedCases_HaveNoHardViolations(string id, int year, int month, int pattern,
        int count, int staffing, int seed, string mode)
    {
        var scenario = Build(id, year, month, (AvailabilityPattern)pattern, count, staffing, seed, mode);
        await GenerateAndCheck(scenario);
    }

    public static IEnumerable<object[]> StressSeeds() => Enumerable.Range(1000, 200).Select(seed => new object[] { seed });

    [Theory]
    [Trait("Category", "GenerationStress")]
    [Trait("Category", "LocalOnly")]
    [MemberData(nameof(StressSeeds))]
    public async Task ExtendedFixedSeeds_HaveNoHardViolations(int seed)
    {
        var calendars = new[] { (2026, 2), (2028, 2), (2026, 4), (2026, 7) };
        var (year, month) = calendars[seed % 4];
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(year, month, 1 + seed % 20,
            1 + seed % 3, (AvailabilityPattern)(seed % 9), seed);
        scenario = ScheduleGenerationScenarioFactory.WithLimits(scenario, 80 + seed % 5 * 40,
            1 + seed % 5, 1 + seed % 3, 2 + seed % 8, seed % 4 * 40);
        await GenerateAndCheck(scenario);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(29)]
    [InlineData(47)]
    public async Task RepeatedInput_IsStable_AndPermutationsRemainSafe(int seed)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 2, 5, 1, AvailabilityPattern.MixedPartial, seed);
        string[]? baseline = null;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            var slots = await GenerateAndCheck(ScheduleGenerationScenarioFactory.Copy(scenario));
            var tuples = Normalize(slots);
            if (baseline is not null) Assert.Equal(baseline, tuples);
            baseline = tuples;
        }
        var copy = ScheduleGenerationScenarioFactory.Copy(scenario);
        await GenerateAndCheck(copy with { Employees = copy.Employees.Reverse().ToArray(), AvailabilityGroups = copy.AvailabilityGroups.Reverse().ToArray() });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task SmallExactReference_FindsCompleteCoverage(int employees)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 2, employees, 1, AvailabilityPattern.AllUnavailable, 11);
        foreach (var member in scenario.AvailabilityGroups[0].Members)
            member.Days.First().Kind = AvailabilityKind.ANY;
        scenario = ScheduleGenerationScenarioFactory.WithShiftWindows(scenario, "08:00 - 09:00", "09:00 - 10:00");
        scenario = ScheduleGenerationScenarioFactory.WithLimits(scenario, maxHours: 2);
        var slots = await GenerateAndCheck(scenario);
        var best = ExactCoveredMinutes(scenario, [(480, 540), (540, 600)], stateCap: 1000);
        Assert.Equal(120, best);
        Assert.Equal(best, slots.Where(s => s.DayOfMonth == 1 && s.EmployeeId.HasValue)
            .Sum(s => { ScheduleGenerationOracle.ReadTime(s.FromTime, out var from); ScheduleGenerationOracle.ReadTime(s.ToTime, out var to); return to - from; }));
    }

    private static int ExactCoveredMinutes(GenerationScenario scenario, (int From, int To)[] shifts, int stateCap)
    {
        var states = 0;
        var best = 0;
        var assigned = new List<ScheduleSlotModel>();
        void Visit(int position)
        {
            Assert.True(++states <= stateCap, "Exact reference exceeded its state cap.");
            if (position == shifts.Length)
            {
                var result = ScheduleGenerationOracle.Check(scenario, assigned);
                if (result.HardViolations.All(v => v.Code == "PositionCount")) best = Math.Max(best, result.Metrics.TotalAssignedMinutes);
                return;
            }
            var shift = shifts[position];
            foreach (int? employee in new int?[] { null }.Concat(scenario.Employees.Select(e => (int?)e.EmployeeId)))
            {
                assigned.Add(TestDataFactory.CreateScheduleSlotModel(1, 1, employee,
                    $"{shift.From / 60:00}:{shift.From % 60:00}", $"{shift.To / 60:00}:{shift.To % 60:00}"));
                Visit(position + 1);
                assigned.RemoveAt(assigned.Count - 1);
            }
        }
        Visit(0);
        return best;
    }

    private async Task<IReadOnlyList<ScheduleSlotModel>> GenerateAndCheck(GenerationScenario scenario)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var watch = Stopwatch.StartNew();
        var slots = (await new ScheduleGenerator().GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, ct: cancellation.Token)).ToArray();
        watch.Stop();
        var result = ScheduleGenerationOracle.Check(scenario, slots);
        result = result with { Metrics = result.Metrics with { ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds } };
        output.WriteLine($"{scenario.ScenarioId}: ms={watch.Elapsed.TotalMilliseconds:F1}, assignedMinutes={result.Metrics.TotalAssignedMinutes}, gapMinutes={result.Metrics.CoverageGapMinutes}, minDeficitMinutes={result.Metrics.TotalMinHourDeficitMinutes}, hard={result.HardViolations.Count}");
        Assert.True(result.HardViolations.Count == 0, string.Join("; ", result.HardViolations.Select(v => $"{v.ScenarioId}:{v.Code}:day{v.DayOfMonth}:employeeOrdinal{v.EmployeeOrdinal}")));
        return slots;
    }

    [Fact]
    public async Task RepresentativeBenchmarks_ThreeRepeatsRemainSafeAndStable()
    {
        foreach (var count in new[] { 5, 10, 20 })
            foreach (var month in new[] { 2, 4, 7 })
                foreach (var staffing in new[] { 1, 2, 3 })
                {
                    var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, month, count, staffing, 47);
                    var durations = new List<double>();
                    string[]? normalized = null;
                    GenerationMetrics? metrics = null;
                    for (var repeat = 0; repeat < 3; repeat++)
                    {
                        var copy = ScheduleGenerationScenarioFactory.Copy(scenario);
                        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var watch = Stopwatch.StartNew();
                        var slots = (await new ScheduleGenerator().GenerateAsync(copy.Schedule, copy.AvailabilityGroups, copy.Employees, ct: cancellation.Token)).ToArray();
                        durations.Add(watch.Elapsed.TotalMilliseconds);
                        var result = ScheduleGenerationOracle.Check(copy, slots);
                        Assert.Empty(result.HardViolations);
                        if (normalized is not null) Assert.Equal(normalized, Normalize(slots));
                        normalized = Normalize(slots);
                        metrics = result.Metrics;
                    }
                    output.WriteLine(FormattableString.Invariant($"BENCH n={count} days={DateTime.DaysInMonth(2026, month)} p={staffing} medianMs={durations.Order().ElementAt(1):F3} maxMs={durations.Max():F3} hard=0 conflict={metrics!.ConflictDays} gap={metrics.CoverageGapMinutes} deficit={metrics.TotalMinHourDeficitMinutes} peakBytes={Process.GetCurrentProcess().PeakWorkingSet64}"));
                }
    }

    internal static string[] Normalize(IEnumerable<ScheduleSlotModel> slots) => slots
        .Select(s => $"{s.DayOfMonth:D2}|{s.SlotNo}|{s.EmployeeId}|{s.Status}|{s.FromTime}|{s.ToTime}").Order(StringComparer.Ordinal).ToArray();

    private static GenerationScenario Build(string id, int year, int month, AvailabilityPattern pattern, int count, int staffing, int seed, string mode)
    {
        if (mode == "empty") count = 0;
        if (mode == "one") count = 1;
        if (mode == "staffing2") staffing = 2;
        if (mode == "staffing3") staffing = 3;
        if (mode == "override") pattern = AvailabilityPattern.ExplicitFullDay;
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(year, month, count, staffing, pattern, seed);
        scenario = mode switch
        {
            "single" => ScheduleGenerationScenarioFactory.WithShiftWindows(scenario, "08:00 - 16:00", null),
            "overlap" => ScheduleGenerationScenarioFactory.WithShiftWindows(scenario, "08:00 - 17:00", "16:00 - 20:00"),
            "separated" => ScheduleGenerationScenarioFactory.WithShiftWindows(scenario, "08:00 - 12:00", "16:00 - 20:00"),
            "hours" => ScheduleGenerationScenarioFactory.WithLimits(scenario, maxHours: 80, minimumHours: 70),
            "streak1" => ScheduleGenerationScenarioFactory.WithLimits(scenario, maxWorkStreak: 1),
            "streak2" => ScheduleGenerationScenarioFactory.WithLimits(scenario, maxWorkStreak: 2),
            "streak5" => ScheduleGenerationScenarioFactory.WithLimits(scenario, maxWorkStreak: 5),
            "full" => ScheduleGenerationScenarioFactory.WithLimits(scenario, maxFullStreak: 1, maxFullDays: 2),
            "override" => ScheduleGenerationScenarioFactory.WithLimits(scenario, maxFullStreak: 1, maxFullDays: 1, minimumHours: 100),
            "impossible" => ScheduleGenerationScenarioFactory.WithLimits(scenario, maxHours: 16, minimumHours: 400),
            _ => scenario
        };
        if (mode == "unequal")
            foreach (var employee in scenario.Employees) employee.MinHoursMonth = employee.EmployeeId * 20;
        if (mode == "weekends")
            foreach (var member in scenario.AvailabilityGroups[0].Members.Where(m => m.EmployeeId % 2 == 1))
                foreach (var day in member.Days.Where(d => new DateTime(year, month, d.DayOfMonth).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
                    day.Kind = AvailabilityKind.NONE;
        if (mode == "groups")
        {
            var second = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(year, month, count, staffing, AvailabilityPattern.MorningOnly, seed);
            scenario = scenario with { AvailabilityGroups = scenario.AvailabilityGroups.Concat(second.AvailabilityGroups).ToArray() };
        }
        if (mode is "short" or "offset" or "duplicate")
            foreach (var member in scenario.AvailabilityGroups[0].Members)
            {
                foreach (var day in member.Days) { day.Kind = AvailabilityKind.INT; day.IntervalStr = mode == "short" ? "10:00 - 11:00" : "12:00 - 18:00"; }
                if (mode == "duplicate") member.Days.Add(TestDataFactory.CreateAvailabilityDayModel(1, AvailabilityKind.NONE));
            }
        return scenario with { ScenarioId = id };
    }
}
