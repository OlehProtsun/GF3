using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Generators;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class ScheduleGenerationInvariantTests
{
    [Theory]
    [InlineData("Enrollment")]
    [InlineData("Overlap")]
    [InlineData("Availability")]
    [InlineData("MaxHours")]
    [InlineData("Date")]
    [InlineData("DuplicatePosition")]
    [InlineData("UnfurnishedOwner")]
    [InlineData("ExternalOverlap")]
    [InlineData("TemplateBounds")]
    public void Oracle_DetectsMalformedAssignments(string code)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 2, 1, 1, AvailabilityPattern.MorningOnly, 11);
        scenario.Schedule.MaxHoursPerEmpMonth = 1;
        var first = TestDataFactory.CreateScheduleSlotModel(1, 1, 1, "08:00", "16:00");
        var second = TestDataFactory.CreateScheduleSlotModel(1, 1, 1, "15:00", "20:00");
        if (code == "Enrollment") first.EmployeeId = 999;
        if (code == "Date") first.DayOfMonth = 32;
        if (code == "TemplateBounds") { first.FromTime = "01:00"; first.ToTime = "02:00"; }
        if (code == "DuplicatePosition") scenario.Schedule.PeoplePerShift = 2;
        if (code == "UnfurnishedOwner") first.Status = SlotStatus.UNFURNISHED;
        if (code == "ExternalOverlap") scenario = ScheduleGenerationScenarioFactory.WithExternalCommitments(scenario, [first]);
        var result = ScheduleGenerationOracle.Check(scenario, [first, second]);
        Assert.Contains(result.HardViolations, v => v.Code == code);
    }

    [Fact]
    public void Oracle_TouchingAssignmentsPass_AndCoverageUsesWorkedIntervals()
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, 2, 1, 1, 11);
        var slots = Enumerable.Range(1, 28).SelectMany(day => new[]
        {
            TestDataFactory.CreateScheduleSlotModel(day, 1, 1, "08:00", "16:00"),
            TestDataFactory.CreateScheduleSlotModel(day, 1, 1, "16:00", "20:00")
        }).ToArray();
        var result = ScheduleGenerationOracle.Check(scenario, slots);
        Assert.Empty(result.HardViolations);
        Assert.Equal(0, result.Metrics.CoverageGapMinutes);
        Assert.Equal(28 * 12 * 60, result.Metrics.TotalAssignedMinutes);
    }

    [Fact]
    public void Factory_ReturnsFreshCollections()
    {
        var source = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 2, 2, 1, AvailabilityPattern.AllAvailable, 11);
        var copy = ScheduleGenerationScenarioFactory.WithLimits(source, minimumHours: 100);
        copy.AvailabilityGroups[0].Members.First().Days.Clear();
        Assert.Equal(28, source.AvailabilityGroups[0].Members.First().Days.Count);
        Assert.Equal(0, source.Employees[0].MinHoursMonth);
    }

    [Fact]
    public async Task Cancellation_PreCanceledAndDuringGeneration()
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 7, 20, 3, AvailabilityPattern.MixedPartial, 47);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ScheduleGenerator().GenerateAsync(
            scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, ct: canceled.Token));
        using var running = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ScheduleGenerator().GenerateAsync(
            scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, new CancelProgress(running), running.Token));
    }

    [Theory]
    [InlineData(1899, 2, 1, "08:00 - 16:00", false)]
    [InlineData(2026, 0, 1, "08:00 - 16:00", false)]
    [InlineData(2026, 13, 1, "08:00 - 16:00", false)]
    [InlineData(2026, 2, 0, "08:00 - 16:00", false)]
    [InlineData(2026, 2, 1, "", false)]
    [InlineData(2026, 2, 1, "invalid", true)]
    [InlineData(2026, 2, 1, "bad - time", true)]
    [InlineData(2026, 2, 1, "16:00 - 08:00", true)]
    public async Task InvalidConfiguration_PreservesExistingOutcome(int year, int month, int staffing, string shift, bool throws)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(year, month, 1, staffing, 11);
        scenario.Schedule.Shift1Time = shift;
        scenario.Schedule.Shift2Time = "";
        var task = new ScheduleGenerator().GenerateAsync(scenario.Schedule, [], scenario.Employees);
        if (throws) await Assert.ThrowsAsync<InvalidOperationException>(() => task);
        else Assert.Empty(await task);
    }

    private sealed class CancelProgress(CancellationTokenSource source) : IProgress<int>
    {
        public void Report(int value) { if (value >= 35) source.Cancel(); }
    }

    public static IEnumerable<object[]> PartialCases() => Enumerable.Range(1, 6).SelectMany(n => Enumerable.Range(1, 3).Select(p => new object[] { n, p }));

    [Theory]
    [MemberData(nameof(PartialCases))]
    public async Task PartialShiftSwap_CountsActualMaterializedHoursTowardMonthlyMaximum(int count, int staffing)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2028, 2, count, staffing, AvailabilityPattern.MixedPartial, 1085);
        scenario = ScheduleGenerationScenarioFactory.WithLimits(scenario, maxHours: 80, maxWorkStreak: 1, maxFullStreak: 3, maxFullDays: 7, minimumHours: 40);
        var slots = (await new ScheduleGenerator().GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)).ToArray();
        var result = ScheduleGenerationOracle.Check(scenario, slots);
        Assert.Empty(result.HardViolations);
    }

    [Fact]
    public async Task ExplicitFullDayOverride_IsReviewed_WhileControlEnforcesFullDayLimits()
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 2, 1, 1, AvailabilityPattern.ExplicitFullDay, 11);
        foreach (var day in scenario.AvailabilityGroups[0].Members.First().Days.Where(d => d.DayOfMonth > 4)) day.Kind = AvailabilityKind.NONE;
        scenario = ScheduleGenerationScenarioFactory.WithLimits(scenario, maxFullStreak: 1, maxFullDays: 1, minimumHours: 48);
        var slots = (await new ScheduleGenerator().GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)).ToArray();
        var result = ScheduleGenerationOracle.Check(scenario, slots);
        Assert.Empty(result.HardViolations);
        Assert.Equal(48 * 60, result.Metrics.TotalAssignedMinutes);
        Assert.True(result.Metrics.FullDayOverrideReviews > 0);
        var control = ScheduleGenerationScenarioFactory.WithLimits(scenario, maxFullStreak: 1, maxFullDays: 1);
        var controlSlots = (await new ScheduleGenerator().GenerateAsync(control.Schedule, control.AvailabilityGroups, control.Employees)).ToArray();
        var check = ScheduleGenerationOracle.Check(control, controlSlots);
        Assert.Empty(check.HardViolations);
        Assert.Equal(0, check.Metrics.FullDayOverrideReviews);
        Assert.True(controlSlots.Where(s => s.EmployeeId.HasValue).GroupBy(s => s.DayOfMonth).Count(g => g.Count() == 2) <= 1);
    }

    [Fact]
    public async Task MultipleGroupsAndDuplicateDayEntries_IntersectAllWindows()
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 2, 2, 1, AvailabilityPattern.ExplicitFullDay, 29);
        var other = ScheduleGenerationScenarioFactory.Copy(scenario).AvailabilityGroups[0];
        foreach (var member in scenario.AvailabilityGroups[0].Members)
            foreach (var day in member.Days) day.IntervalStr = "08:00 - 18:00";
        foreach (var member in other.Members)
        {
            foreach (var day in member.Days) day.IntervalStr = "10:00 - 20:00";
            member.Days.Add(TestDataFactory.CreateAvailabilityDayModel(1, AvailabilityKind.INT, "14:00 - 17:00"));
        }
        scenario = scenario with { AvailabilityGroups = scenario.AvailabilityGroups.Concat(new[] { other }).ToArray() };
        var slots = (await new ScheduleGenerator().GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)).ToArray();
        Assert.Empty(ScheduleGenerationOracle.Check(scenario, slots).HardViolations);
        Assert.Contains(slots, s => s.EmployeeId.HasValue);
        Assert.All(slots.Where(s => s.EmployeeId.HasValue && s.DayOfMonth == 1), s =>
        {
            Assert.True(TimeSpan.Parse(s.FromTime) >= TimeSpan.FromHours(14));
            Assert.True(TimeSpan.Parse(s.ToTime) <= TimeSpan.FromHours(17));
        });
    }

    [Theory]
    [InlineData("08:00 - 14:00", "14:00 - 20:00", 12)]
    [InlineData("08:00 - 14:00", "12:00 - 20:00", 14)]
    [InlineData("08:00 - 12:00", "16:00 - 20:00", 8)]
    public async Task DisjointPartialWindows_PreserveAvailabilityAndReportUnavoidableGap(string firstShift, string secondShift, int dailyDemandHours)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 2, 2, 1, AvailabilityPattern.AllUnavailable, 47);
        scenario = ScheduleGenerationScenarioFactory.WithShiftWindows(scenario, firstShift, secondShift);
        var members = scenario.AvailabilityGroups[0].Members.ToArray();
        members[0].Days.First().Kind = AvailabilityKind.INT;
        members[0].Days.First().IntervalStr = "08:00 - 12:00";
        members[1].Days.First().Kind = AvailabilityKind.INT;
        members[1].Days.First().IntervalStr = "16:00 - 20:00";
        var slots = (await new ScheduleGenerator().GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)).ToArray();
        var result = ScheduleGenerationOracle.Check(scenario, slots);
        Assert.Empty(result.HardViolations);
        Assert.Equal(8 * 60, result.Metrics.TotalAssignedMinutes);
        Assert.Equal((28 * dailyDemandHours - 8) * 60, result.Metrics.CoverageGapMinutes);
    }

    [Fact]
    public async Task DisjointPartialWindows_KeepActualHoursWithinMonthlyCap()
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 2, 2, 1, AvailabilityPattern.AllAvailable, 47);
        scenario = ScheduleGenerationScenarioFactory.WithShiftWindows(scenario, "08:00 - 12:00", "16:00 - 20:00");
        scenario = ScheduleGenerationScenarioFactory.WithLimits(scenario, maxHours: 8);
        var members = scenario.AvailabilityGroups[0].Members.ToArray();
        foreach (var day in members[0].Days) { day.Kind = AvailabilityKind.INT; day.IntervalStr = "08:00 - 12:00"; }
        foreach (var day in members[1].Days) { day.Kind = AvailabilityKind.INT; day.IntervalStr = "16:00 - 20:00"; }
        var slots = (await new ScheduleGenerator().GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)).ToArray();
        var result = ScheduleGenerationOracle.Check(scenario, slots);
        Assert.Empty(result.HardViolations);
        Assert.Equal(16 * 60, result.Metrics.TotalAssignedMinutes);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(5, true)]
    public async Task MinimumHours_FixedIntervalTransfersCannotImproveEarlierScore(int employeeCount, bool unequalTargets)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAllAvailable(2026, 2, employeeCount, 1, 29);
        scenario = ScheduleGenerationScenarioFactory.WithLimits(scenario, maxHours: 160, maxWorkStreak: 5, maxFullStreak: 3, maxFullDays: 10, minimumHours: 120);
        if (unequalTargets)
            foreach (var employee in scenario.Employees) employee.MinHoursMonth = 40 + employee.EmployeeId * 20;
        var slots = (await new ScheduleGenerator().GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees)).ToArray();
        var baseline = ScheduleGenerationOracle.Check(scenario, slots);
        Assert.Empty(baseline.HardViolations);
        var totals = scenario.Employees.ToDictionary(e => e.EmployeeId, e => slots.Where(s => s.EmployeeId == e.EmployeeId)
            .Sum(s => { ScheduleGenerationOracle.ReadTime(s.FromTime, out var from); ScheduleGenerationOracle.ReadTime(s.ToTime, out var to); return to - from; }));
        foreach (var slot in slots.Where(s => s.EmployeeId.HasValue))
        {
            var donor = slot.EmployeeId!.Value;
            ScheduleGenerationOracle.ReadTime(slot.FromTime, out var from);
            ScheduleGenerationOracle.ReadTime(slot.ToTime, out var to);
            foreach (var receiver in scenario.Employees.Where(e => e.EmployeeId != donor))
            {
                var deficits = scenario.Employees.Select(e => Math.Max(0, (e.MinHoursMonth ?? 0) * 60 - totals[e.EmployeeId]
                    + (e.EmployeeId == donor ? to - from : e.EmployeeId == receiver.EmployeeId ? from - to : 0))).ToArray();
                if (deficits.Sum() > baseline.Metrics.TotalMinHourDeficitMinutes || deficits.Sum() == baseline.Metrics.TotalMinHourDeficitMinutes
                    && deficits.Sum(d => (long)d * d) >= baseline.Metrics.TotalMinHourDeficitSquared) continue;
                slot.EmployeeId = receiver.EmployeeId;
                var candidate = ScheduleGenerationOracle.Check(scenario, slots);
                slot.EmployeeId = donor;
                Assert.False(candidate.HardViolations.Count == 0,
                    $"Safe one-slot transfer improves minimum score for n={employeeCount}, unequal={unequalTargets}: {baseline.Metrics.TotalMinHourDeficitMinutes}/{baseline.Metrics.TotalMinHourDeficitSquared} -> {candidate.Metrics.TotalMinHourDeficitMinutes}/{candidate.Metrics.TotalMinHourDeficitSquared}.");
            }
        }
    }
}
