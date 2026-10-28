using System.Diagnostics;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Schedule;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace GF3.Tests;

public sealed class PrioritySchedulingDeepAuditTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> MatrixCases() => Enumerable.Range(0, PriorityAuditScenarios.PairwiseRows.Length).Select(i => new object[] { i });

    [Fact]
    public void BroadMatrixCoversEveryDeclaredFactorPair()
    {
        var rows = PriorityAuditScenarios.PairwiseRows;
        for (var first = 0; first < PriorityAuditScenarios.Levels.Length; first++)
        for (var second = first + 1; second < PriorityAuditScenarios.Levels.Length; second++)
        for (var a = 0; a < PriorityAuditScenarios.Levels[first]; a++)
        for (var b = 0; b < PriorityAuditScenarios.Levels[second]; b++)
            Assert.Contains(rows, row => row[first] == a && row[second] == b);
        output.WriteLine($"PAIRWISE cases={rows.Length},dimensions={PriorityAuditScenarios.Levels.Length}");
    }

    [Theory]
    [MemberData(nameof(MatrixCases))]
    public async Task BroadMatrixIsSafeDeterministicAndInstrumentationIsTransparent(int index)
    {
        var input = PriorityAuditScenarios.Build(index);
        var external = ExternalScheduleAvailabilityBuilder.Build(input.Schedule, input.External, input.Employees.Select(e => e.EmployeeId));
        var groups = external is null ? input.Groups : input.Groups.Append(external).ToArray();
        var scenario = new GenerationScenario(input.Id, input.Schedule, input.Employees, groups, input.External);
        var original = System.Text.Json.JsonSerializer.Serialize(input);
        string[]? reference = null;
        var audit = new PriorityGenerationAudit();
        for (var repeat = 0; repeat < 3; repeat++)
        {
            using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var legacy = new ScheduleGenerator();
            var slots = input.Employees.Any(e => e.IsPriority)
                ? await new PriorityScheduleGenerator(legacy).GenerateAuditedAsync(input.Schedule, groups,
                    input.Employees, input.Hours, input.Weekends, input.Respect, audit, bounded.Token)
                : await legacy.GenerateAsync(input.Schedule, groups, input.Employees, ct: bounded.Token);
            Assert.Empty(ScheduleGenerationOracle.Check(scenario, slots.ToArray(), input.Hours, input.Weekends, input.Respect).HardViolations);
            var normalized = Normalize(slots);
            if (reference is not null) Assert.Equal(reference, normalized);
            reference = normalized;
        }
        var ordinary = new ScheduleGenerator();
        var plain = input.Employees.Any(e => e.IsPriority)
            ? await new PriorityScheduleGenerator(ordinary).GenerateAsync(input.Schedule, groups, input.Employees,
                input.Hours, input.Weekends, null, default, input.Respect)
            : await ordinary.GenerateAsync(input.Schedule, groups, input.Employees);
        Assert.Equal(reference, Normalize(plain));
        var permuted = input.Employees.Any(e => e.IsPriority)
            ? await new PriorityScheduleGenerator(ordinary).GenerateAsync(input.Schedule, groups.Reverse().ToArray(), input.Employees.Reverse().ToArray(),
                input.Hours, input.Weekends, null, default, input.Respect)
            : await ordinary.GenerateAsync(input.Schedule, groups, input.Employees);
        Assert.Equal(reference, Normalize(permuted));
        Assert.Equal(original, System.Text.Json.JsonSerializer.Serialize(input));
        Assert.InRange(audit.Evaluations, 0, 20_000); Assert.InRange(audit.Trials, 0, 64);
        Assert.InRange(audit.CoordinatedEvaluations, 0, 10_000); Assert.InRange(audit.CoordinatedTrials, 0, 16);
        Assert.InRange(audit.RestartTrials, 0, 2);
        output.WriteLine($"MATRIX case={index},factors={string.Join(',', input.Factors)},evals={audit.Evaluations},trials={audit.Trials},limited={audit.BudgetExhausted || audit.TrialLimitReached}");
    }

    public static IEnumerable<object[]> TargetCases()
    {
        foreach (var target in new[] { 0, 1, 7, 40, 80, 120, 160, 168, 169, 170, 180, 200 })
        foreach (var respect in new[] { true, false })
        foreach (var weekends in new[] { true, false }) yield return [target, respect, weekends];
    }

    [Theory]
    [MemberData(nameof(TargetCases))]
    public async Task NovemberExactDurationSeparatesImpossibleTargetFromAvoidableDeficit(int target, bool respect, bool weekends)
    {
        var input = PriorityAuditScenarios.Screenshot(target);
        var employee = input.Employees.Take(1).ToArray();
        var generator = new PriorityScheduleGenerator(new());
        var slots = await generator.GenerateAsync(input.Schedule, [], employee, 8, weekends, null, default, respect);
        var check = ScheduleGenerationOracle.Check(new("november-bound", input.Schedule, employee, []), slots.ToArray(), 8, weekends, respect);
        Assert.Empty(check.HardViolations);
        var assigned = slots.Where(s => s.EmployeeId.HasValue).ToArray();
        Assert.All(assigned, s => Assert.Equal(TimeSpan.FromHours(8), TimeSpan.Parse(s.ToTime) - TimeSpan.Parse(s.FromTime)));
        var upperHours = weekends || !respect ? 168 : 200;
        Assert.Equal(upperHours * 60, check.Metrics.TotalAssignedMinutes);
        Assert.Equal(Math.Max(0, target - upperHours) * 60, check.Metrics.TotalMinHourDeficitMinutes);
        if (target == 170 && (weekends || !respect)) Assert.Equal(120, check.Metrics.TotalMinHourDeficitMinutes);
    }

    [Theory]
    [InlineData(true, true, 6)] [InlineData(false, true, 6)]
    [InlineData(true, false, 8)] [InlineData(false, false, 8)]
    [InlineData(true, true, 10)] [InlineData(false, true, 10)]
    [InlineData(true, false, 12)] [InlineData(false, false, 12)]
    public async Task StoredSettingsRouteMatchesAuditedPolicy(bool respect, bool weekends, int hours)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        await db.PriorityGenerationSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.RespectAvailability, respect)
            .SetProperty(x => x.WeekendsOff, weekends).SetProperty(x => x.ShiftHours, hours));
        var input = PriorityAuditScenarios.Screenshot();
        var roster = input.Employees.Take(2).ToArray();
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 11,
            Members = roster.Select(e => new AvailabilityGroupMemberModel { EmployeeId = e.EmployeeId,
                Days = Enumerable.Range(1, 30).Select(day => new AvailabilityGroupDayModel { DayOfMonth = day, Kind = AvailabilityKind.NONE }).ToList() }).ToList() };
        var legacy = new ScheduleGenerator(); var priority = new PriorityScheduleGenerator(legacy);
        var expected = await priority.GenerateAsync(input.Schedule, [group], roster, hours, weekends, null, default, respect);
        var actual = await new PriorityAwareScheduleGenerator(legacy, priority, db).GenerateAsync(input.Schedule, [group], roster);
        Assert.Equal(Normalize(expected), Normalize(actual));
        Assert.DoesNotContain(actual, s => s.EmployeeId == roster[1].EmployeeId);
    }

    [Fact]
    public async Task PriorityReservationPreservesConstructivelyAttainableWorkdays()
    {
        var input = PriorityAuditScenarios.Screenshot(48);
        input.Schedule.Month = 4; input.Schedule.PeoplePerShift = 1;
        input.Schedule.MaxConsecutiveDays = 2; input.Schedule.MaxHoursPerEmpMonth = 48;
        var roster = input.Employees.Take(1).ToArray();
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 4,
            Members = [new() { EmployeeId = roster[0].EmployeeId, Days = Enumerable.Range(9, 22)
                .Select(day => new AvailabilityGroupDayModel { DayOfMonth = day, Kind = AvailabilityKind.NONE }).ToList() }] };
        var slots = await new PriorityScheduleGenerator(new()).GenerateAsync(input.Schedule, [group], roster, 8, false, null, default, true);
        var checkedOutput = ScheduleGenerationOracle.Check(new("attainable-eight-day-witness", input.Schedule, roster, [group]), slots.ToArray(), 8, false);
        Assert.Empty(checkedOutput.HardViolations);
        // Independent witness: days 1,2 / 4,5 / 7,8, each with one exact eight-hour shift.
        Assert.Equal(6 * 480, checkedOutput.Metrics.TotalAssignedMinutes);
        Assert.Equal(0, checkedOutput.Metrics.TotalMinHourDeficitMinutes);
    }

    [Fact]
    public async Task ForcedCounterfactualIsOptInAndProtectsOriginalInput()
    {
        var scenario = Tiny("09:00 - 13:00", "17:00 - 21:00");
        var generator = new PriorityScheduleGenerator(new());
        var plain = await generator.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, 8, true, null, default);
        var audit = new PriorityGenerationAudit();
        audit.FixedAnchors[(1, 2)] = new(780, 1260);
        var trial = await generator.GenerateAuditedAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, 8, true, true, audit);
        Assert.Empty(ScheduleGenerationOracle.Check(scenario, trial.ToArray(), 8).HardViolations);
        Assert.Contains(trial, s => s.EmployeeId == 1 && s.DayOfMonth == 2 && s.FromTime == "13:00");
        Assert.Equal(Normalize(plain), Normalize(await generator.GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, 8, true, null, default)));
    }

    [Theory]
    [InlineData(0)] [InlineData(4)] [InlineData(8)]
    public async Task TinyEarlyLateReferenceEnumeratesCompleteOrdinaryAssignments(int regularTarget)
    {
        var scenario = Tiny("09:00 - 13:00", "17:00 - 21:00", regularTarget);
        var slots = await new PriorityScheduleGenerator(new()).GenerateAsync(scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, 8, true, null, default);
        var state = new List<(int Position, int Employee, int From, int To)>();
        double[]? best = null; var visited = 0; var complete = 0;
        void Visit(int position)
        {
            Assert.True(++visited <= 200_000, "INCONCLUSIVE - independent reference state bound exhausted");
            if (position == 4)
            {
                if (state.Count(s => s.Employee == 1) != 2) return;
                var outputSlots = Enumerable.Range(1, 30).SelectMany(day => Enumerable.Range(0, 2).Select(shift =>
                    new ScheduleSlotModel { DayOfMonth = day, SlotNo = 1, FromTime = shift == 0 ? "09:00" : "15:00",
                        ToTime = shift == 0 ? "15:00" : "21:00", Status = SlotStatus.UNFURNISHED })).ToArray();
                foreach (var item in state)
                {
                    var slot = outputSlots[item.Position]; slot.EmployeeId = item.Employee; slot.Status = SlotStatus.ASSIGNED;
                    slot.FromTime = $"{item.From / 60:00}:{item.From % 60:00}"; slot.ToTime = $"{item.To / 60:00}:{item.To % 60:00}";
                }
                var vector = ScheduleGenerationOracle.CanonicalQuality(scenario, outputSlots);
                if (best is null || Compare(vector, best) < 0) best = vector;
                complete++; return;
            }
            Visit(position + 1);
            var day = position / 2 + 1; var low = position % 2 == 0 ? 540 : 900; var high = low + 360;
            foreach (var employee in scenario.Employees)
            {
                var window = ScheduleGenerationOracle.Window(scenario, employee.EmployeeId, day);
                var endpoints = new[] { 540, 780, 900, 1020, 1260 };
                foreach (var from in endpoints)
                foreach (var to in endpoints.Where(to => to > from))
                {
                    if (employee.IsPriority ? to - from != 480 || from >= high || to <= low
                        : from < low || to > high || from < window.From || to > window.To) continue;
                    if (state.Where(s => s.Employee == employee.EmployeeId).Sum(s => s.To - s.From) + to - from > 960) continue;
                    if (state.Any(s => s.Position / 2 + 1 == day && (s.Employee == employee.EmployeeId && employee.IsPriority || from < s.To && s.From < to))) continue;
                    state.Add((position, employee.EmployeeId, from, to)); Visit(position + 1); state.RemoveAt(state.Count - 1);
                }
            }
        }
        Visit(0);
        Assert.NotNull(best);
        var actual = ScheduleGenerationOracle.CanonicalQuality(scenario, slots.ToArray());
        Assert.Equal(best, actual);
        output.WriteLine($"OPTIMAL WITHIN REFERENCE SPACE target={regularTarget},visited={visited},complete={complete},vector=[{string.Join(',', actual)}]");
    }

    [Fact]
    public async Task CoordinatedAnchorsReleaseMonthlyCapacityThatSingleDayMovesCannotImprove()
    {
        var s = PriorityAuditScenarios.Screenshot(16);
        s.Schedule.Month = 4; s.Schedule.PeoplePerShift = 1; s.Schedule.MaxHoursPerEmpMonth = 16;
        var roster = s.Employees.Take(3).ToArray();
        foreach (var e in roster) e.MinHoursMonth = 16;
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 4 };
        foreach (var e in roster)
        {
            var member = new AvailabilityGroupMemberModel { EmployeeId = e.EmployeeId };
            for (var day = 1; day <= 30; day++)
            {
                string? window = e.IsPriority ? null : day switch {
                    1 => e.EmployeeId == 2 ? "18:00 - 21:00" : "09:00 - 13:00",
                    2 => e.EmployeeId == 2 ? "09:00 - 13:00" : "18:00 - 21:00",
                    3 or 4 => e.EmployeeId == 2 ? "09:00 - 14:00" : "16:00 - 21:00", _ => null };
                member.Days.Add(new() { DayOfMonth = day, Kind = e.IsPriority && day <= 2 ? AvailabilityKind.ANY : window is null ? AvailabilityKind.NONE : AvailabilityKind.INT,
                    IntervalStr = window });
            }
            group.Members.Add(member);
        }
        var scenario = new GenerationScenario("coordinated-monthly-capacity", s.Schedule, roster, [group]);
        var generator = new PriorityScheduleGenerator(new());
        var slots = await generator.GenerateAsync(s.Schedule, [group], roster, 8, true, null, default);
        var witness = new PriorityGenerationAudit(); witness.FixedAnchors[(1, 1)] = new(780, 1260); witness.FixedAnchors[(1, 2)] = new(780, 1260);
        var alternative = await generator.GenerateAuditedAsync(s.Schedule, [group], roster, 8, true, true, witness);
        var expected = ScheduleGenerationOracle.CanonicalQuality(scenario, alternative.ToArray());
        var actual = ScheduleGenerationOracle.CanonicalQuality(scenario, slots.ToArray());
        Assert.Equal(44 * 60, alternative.Where(x => x.EmployeeId.HasValue).Sum(x => (int)(TimeSpan.Parse(x.ToTime) - TimeSpan.Parse(x.FromTime)).TotalMinutes));
        Assert.True(Compare(actual, expected) <= 0, $"VALID BUT SUBOPTIMAL: actual=[{string.Join(',', actual)}],witness=[{string.Join(',', expected)}]");
    }

    private static int Compare(double[] left, double[] right)
    {
        for (var i = 0; i < left.Length; i++) { var value = left[i].CompareTo(right[i]); if (value != 0) return value; }
        return 0;
    }

    [Fact]
    public async Task ImpossiblePriorityTargetsPermitAValidOwnershipTransferWithLowerNormalizedDeficit()
    {
        var input = PriorityAuditScenarios.Screenshot();
        input.Schedule.Month = 4; input.Schedule.PeoplePerShift = 1; input.Schedule.MaxHoursPerEmpMonth = 24;
        var roster = input.Employees.Take(2).ToArray();
        roster[0].IsPriority = roster[1].IsPriority = true;
        roster[0].MinHoursMonth = 30; roster[1].MinHoursMonth = 40;
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 4,
            Members = roster.Select(e => new AvailabilityGroupMemberModel { EmployeeId = e.EmployeeId,
                Days = Enumerable.Range(1, 30).Select(day => new AvailabilityGroupDayModel { DayOfMonth = day,
                    Kind = day <= 3 ? AvailabilityKind.ANY : AvailabilityKind.NONE }).ToList() }).ToList() };
        var scenario = new GenerationScenario("priority-ownership-deficit", input.Schedule, roster, [group]);
        var slots = (await new PriorityScheduleGenerator(new()).GenerateAsync(input.Schedule, [group], roster, 8, true, null, default)).ToArray();
        Assert.Empty(ScheduleGenerationOracle.Check(scenario, slots, 8).HardViolations);
        var witness = System.Text.Json.JsonSerializer.Deserialize<ScheduleSlotModel[]>(System.Text.Json.JsonSerializer.Serialize(slots))!;
        var firstHours = witness.Count(s => s.EmployeeId == 1) * 8;
        if (firstHours > 8)
        {
            var moved = witness.First(s => s.EmployeeId == 1 && !witness.Any(t => t.DayOfMonth == s.DayOfMonth && t.EmployeeId == 2));
            moved.EmployeeId = 2;
        }
        Assert.Empty(ScheduleGenerationOracle.Check(scenario, witness, 8).HardViolations);
        var actual = ScheduleGenerationOracle.CanonicalQuality(scenario, slots);
        var expected = ScheduleGenerationOracle.CanonicalQuality(scenario, witness);
        Assert.True(Compare(actual, expected) <= 0, $"VALID BUT SUBOPTIMAL: actual=[{string.Join(',', actual)}],witness=[{string.Join(',', expected)}]");
        Assert.Equal(8, slots.Count(s => s.EmployeeId == 1) * 8);
        Assert.Equal(16, slots.Count(s => s.EmployeeId == 2) * 8);
    }

    [Theory]
    [InlineData(11)] [InlineData(29)] [InlineData(47)]
    public async Task MonthlyCounterfactualReusesGuidanceAndNeverRegressesTheAcceptedVector(int seed)
    {
        var scenario = ScheduleGenerationScenarioFactory.CreateAvailabilityPattern(2026, 11, 12, 4, AvailabilityPattern.MixedPartial, seed);
        scenario.Schedule.Shift1Time = "09:00 - 15:00"; scenario.Schedule.Shift2Time = "15:00 - 21:00";
        scenario.Schedule.MaxHoursPerEmpMonth = 200; scenario.Schedule.MaxConsecutiveDays = 5;
        scenario.Schedule.MaxConsecutiveFull = 2; scenario.Schedule.MaxFullPerMonth = 5;
        foreach (var e in scenario.Employees) { e.IsPriority = e.EmployeeId == 1; e.MinHoursMonth = e.IsPriority ? 170 : 180; }
        var audit = new PriorityGenerationAudit();
        var slots = await new PriorityScheduleGenerator(new()).GenerateAuditedAsync(scenario.Schedule, scenario.AvailabilityGroups,
            scenario.Employees, 8, true, true, audit);
        Assert.Empty(ScheduleGenerationOracle.Check(scenario, slots.ToArray(), 8).HardViolations);
        Assert.Equal(1, audit.LegacyGuidanceBuilds);
        Assert.Equal(2, audit.RestartTrials);
        Assert.True(Compare(audit.FinalObjective!, audit.BeforeRestarts!) <= 0);
        double[] retainedBaseline = seed switch
        {
            11 => [2520, 12900, 52980, 2.48256407810248, 3.6935839055901116, 191, 1],
            29 => [3000, 11460, 51060, 2.448698433142763, 3.864196190589985, 197, 1],
            47 => [1080, 13620, 55140, 3.098103727592898, 3.8767704753817145, 223, 2],
            _ => throw new ArgumentOutOfRangeException(nameof(seed))
        };
        Assert.True(Compare(audit.FinalObjective!, retainedBaseline) <= 0,
            $"Regression against retained feasible vector: [{string.Join(',', audit.FinalObjective!)}]");
        output.WriteLine($"RESTART seed={seed},accepted={audit.AcceptedRestarts},before=[{string.Join(',', audit.BeforeRestarts!)}],after=[{string.Join(',', audit.FinalObjective!)}]");
    }

    [Fact]
    public async Task CancellationAtMonthlyCounterfactualBoundaryLeavesNoMutatedInput()
    {
        var scenario = Tiny("09:00 - 13:00", "17:00 - 21:00");
        var before = System.Text.Json.JsonSerializer.Serialize(scenario);
        using var canceled = new CancellationTokenSource();
        var audit = new PriorityGenerationAudit { BeforeRestart = canceled.Cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PriorityScheduleGenerator(new()).GenerateAuditedAsync(
            scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, 8, true, true, audit, canceled.Token));
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(scenario));
        Assert.Empty(ScheduleGenerationOracle.Check(scenario, (await new PriorityScheduleGenerator(new()).GenerateAsync(
            scenario.Schedule, scenario.AvailabilityGroups, scenario.Employees, 8, true, null, default)).ToArray(), 8).HardViolations);
    }

    internal static GenerationScenario Tiny(string firstOrdinary, string secondOrdinary, int regularTarget = 4)
    {
        var s = PriorityAuditScenarios.Screenshot();
        s.Schedule.Month = 4; s.Schedule.PeoplePerShift = 1; s.Schedule.MaxHoursPerEmpMonth = 16;
        var roster = s.Employees.Take(3).ToArray();
        roster[0].MinHoursMonth = 16; roster[1].MinHoursMonth = regularTarget; roster[2].MinHoursMonth = 0;
        var group = new AvailabilityGroupModel { Id = 1, Year = 2026, Month = 4,
            Members = roster.Select(e => new AvailabilityGroupMemberModel { EmployeeId = e.EmployeeId,
                Days = Enumerable.Range(1, 30).Select(day => new AvailabilityGroupDayModel { DayOfMonth = day,
                    Kind = day > 2 ? AvailabilityKind.NONE : e.IsPriority ? AvailabilityKind.ANY : AvailabilityKind.INT,
                    IntervalStr = day > 2 || e.IsPriority ? null : (day + e.EmployeeId) % 2 == 0 ? firstOrdinary : secondOrdinary }).ToList() }).ToList() };
        return new("tiny-counterfactual", s.Schedule, roster, [group]);
    }

    private static string[] Normalize(IEnumerable<ScheduleSlotModel> slots) => slots.Select(s => $"{s.DayOfMonth}:{s.SlotNo}:{s.EmployeeId}:{s.FromTime}:{s.ToTime}:{s.Status}").ToArray();
}
