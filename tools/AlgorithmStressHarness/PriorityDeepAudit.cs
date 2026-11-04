using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

internal sealed partial class SqliteReplaySource
{
    internal static (string Database, string Output) AuditPaths(string database, string output)
    {
        var options = Parse(["--db", database, "--all-graphs", "--output", output]);
        return (options.Database, options.Output);
    }

    internal async Task<ReplayInput> LoadReconstructionAsync(int rosterSourceGraph, int selectedGroup, int priorityEmployee, CancellationToken ct)
    {
        var source = await LoadAsync(rosterSourceGraph, ct);
        var group = await new AvailabilityGroupRepository(context).GetFullByIdAsync(selectedGroup, ct)
            ?? throw new ReplayInputException("audit-availability-not-found");
        var sourceTargets = source.Employees.ToDictionary(e => e.EmployeeId, e => e.MinHoursMonth);
        var employees = group.Members.OrderBy(m => m.DisplayOrder).ThenBy(m => m.EmployeeId)
            .Select((m, i) => new ScheduleEmployeeModel
            {
                EmployeeId = m.EmployeeId,
                DisplayOrder = i,
                IsPriority = m.EmployeeId == priorityEmployee,
                MinHoursMonth = m.EmployeeId == priorityEmployee ? 170 : sourceTargets.GetValueOrDefault(m.EmployeeId)
            }).ToArray();
        if (employees.Count(e => e.IsPriority) != 1) throw new ReplayInputException("audit-priority-member-not-found");
        var schedule = new ScheduleModel
        {
            Id = 0,
            ContainerId = source.Schedule.ContainerId,
            ShopId = source.Schedule.ShopId,
            Year = group.Year,
            Month = group.Month,
            AvailabilityGroupId = group.Id,
            PeoplePerShift = 4,
            Shift1Time = "09:00 - 15:00",
            Shift2Time = "15:00 - 21:00",
            MaxHoursPerEmpMonth = 200,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 2,
            MaxFullPerMonth = 5
        };
        var availability = new AvailabilityGroupModel
        {
            Id = group.Id,
            Year = group.Year,
            Month = group.Month,
            Members = group.Members.Select(m => new AvailabilityGroupMemberModel
            {
                EmployeeId = m.EmployeeId,
                Days = m.Days.Select(d => new AvailabilityGroupDayModel
                {
                    DayOfMonth = d.DayOfMonth,
                    Kind = (AvailabilityKind)(int)d.Kind,
                    IntervalStr = d.IntervalStr
                }).ToList()
            }).ToList()
        };
        var others = await new ScheduleRepository(context).GetByMonthWithSlotsAsync(group.Year, group.Month, 0, ct);
        var external = others.SelectMany(s => s.Slots).Select(s => new ScheduleSlotModel
        {
            EmployeeId = s.EmployeeId,
            DayOfMonth = s.DayOfMonth,
            FromTime = s.FromTime,
            ToTime = s.ToTime,
            Status = (SlotStatus)(int)s.Status
        }).ToArray();
        var settings = await context.PriorityGenerationSettings.AsNoTracking().SingleOrDefaultAsync(s => s.Id == 1, ct);
        return new(schedule, employees, [availability], external, [], settings?.ShiftHours ?? 8,
            settings?.WeekendsOff ?? true, settings?.RespectAvailability ?? true);
    }
}

internal static class PriorityDeepAudit
{
    private sealed record Result(IList<ScheduleSlotModel> Slots, object? Diagnostics);
    private delegate Task<Result> Generate(ReplayInput input, Dictionary<(int Employee, int Day), (int Start, int End)> fixedAnchors, bool diagnostics, CancellationToken ct);

    internal static Task<int> RunControlAsync(string[] args) => RunCoreAsync(args, async (input, _, _, ct) =>
    {
        var legacy = new ScheduleGenerator();
        var groups = WithExternal(input);
        var slots = input.Employees.Any(e => e.IsPriority)
            ? await new PriorityScheduleGenerator(legacy).GenerateAsync(input.Schedule, groups, input.Employees,
                input.PriorityShiftHours ?? 8, input.PriorityWeekendsOff, null, ct, input.PriorityRespectAvailability)
            : await legacy.GenerateAsync(input.Schedule, groups, input.Employees, ct: ct);
        return new(slots, null);
    }, true);

    internal static Task<int> RunAsync(string[] args) => RunCoreAsync(args, async (input, fixedAnchors, diagnostics, ct) =>
    {
        var audit = new PriorityGenerationAudit();
        foreach (var (key, time) in fixedAnchors) audit.FixedAnchors[key] = new(time.Start, time.End);
        var legacy = new ScheduleGenerator();
        var groups = WithExternal(input);
        var slots = input.Employees.Any(e => e.IsPriority)
            ? !diagnostics && fixedAnchors.Count == 0
                ? await new PriorityScheduleGenerator(legacy).GenerateAsync(input.Schedule, groups, input.Employees,
                    input.PriorityShiftHours ?? 8, input.PriorityWeekendsOff, null, ct, input.PriorityRespectAvailability)
                : await new PriorityScheduleGenerator(legacy).GenerateAuditedAsync(input.Schedule, groups, input.Employees,
                    input.PriorityShiftHours ?? 8, input.PriorityWeekendsOff, input.PriorityRespectAvailability, audit, ct)
            : await legacy.GenerateAsync(input.Schedule, groups, input.Employees, ct: ct);
        return new(slots, audit);
    }, false);

    // Reflection lets the same audit executable run the frozen pre-instrumentation assembly.
    internal static AvailabilityGroupModel[] WithExternal(ReplayInput input)
    {
        var builder = typeof(ScheduleGenerator).Assembly.GetType("BusinessLogicLayer.Schedule.ExternalScheduleAvailabilityBuilder")!;
        var external = (AvailabilityGroupModel?)builder.GetMethod("Build", BindingFlags.Static | BindingFlags.Public)!
            .Invoke(null, [input.Schedule, input.ExternalSlots, input.Employees.Select(e => e.EmployeeId)]);
        return external is null ? input.Groups.ToArray() : input.Groups.Append(external).ToArray();
    }

    private static T Copy<T>(T input) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(input))!;
    private static ReplayInput From(PriorityAuditScenario s) => new(s.Schedule, s.Employees, s.Groups, s.External, [], s.Hours, s.Weekends, s.Respect);

    private static async Task<int> RunCoreAsync(string[] args, Generate generate, bool control)
    {
        try
        {
            if (args.Length != 1 || !Path.IsPathFullyQualified(args[0])) throw new ReplayInputException("audit-requires-absolute-private-config");
            using var config = JsonDocument.Parse(await File.ReadAllTextAsync(args[0]));
            var root = config.RootElement;
            var traceOnly = root.TryGetProperty("traceOnly", out var traceFlag) && traceFlag.GetBoolean();
            var paths = SqliteReplaySource.AuditPaths(root.GetProperty("snapshot").GetString()!, root.GetProperty("reports").GetString()!);
            var cases = new List<object>();
            var hardCases = 0;
            async Task<IList<ScheduleSlotModel>> Run(string id, ReplayInput input, Dictionary<(int, int), (int, int)>? anchors = null, int[]? factors = null)
            {
                if (root.TryGetProperty("casePrefix", out var prefix) && !id.StartsWith(prefix.GetString()!, StringComparison.Ordinal)) return [];
                var times = new List<double>(); var allocations = new List<long>(); string? hash = null;
                object? diagnostic = null; IList<ScheduleSlotModel>? slots = null; ReplayMetrics? metrics = null; ReplayCase? report = null;
                var unchanged = JsonSerializer.Serialize(input);
                var repeats = anchors is null && !traceOnly ? 3 : 1;
                for (var repeat = anchors is null && !traceOnly ? -1 : 0; repeat < repeats; repeat++)
                {
                    using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var before = GC.GetTotalAllocatedBytes(); var watch = Stopwatch.StartNew();
                    var result = await generate(input, anchors ?? [], anchors is not null || traceOnly, bounded.Token);
                    watch.Stop(); var allocated = GC.GetTotalAllocatedBytes() - before;
                    if (JsonSerializer.Serialize(input) != unchanged) throw new ReplayInputException("audit-input-mutated");
                    if (repeat < 0) continue;
                    slots = result.Slots; diagnostic = result.Diagnostics;
                    metrics = ReplayChecks.Check(input, slots.ToArray());
                    if (metrics.Violations.Count > 0 && !control)
                    {
                        Console.Error.WriteLine($"Invariant failure {id}: {JsonSerializer.Serialize(metrics.Violations)}");
                        throw new ReplayInputException("audit-hard-invariant");
                    }
                    var next = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', ReplayChecks.Normalize(slots)))));
                    if (hash is not null && hash != next) throw new ReplayInputException("audit-nondeterminism");
                    hash = next; times.Add(watch.Elapsed.TotalMilliseconds); allocations.Add(allocated);
                }
                if (!control && anchors is null && !traceOnly)
                {
                    using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var instrumented = await generate(input, anchors ?? [], true, bounded.Token);
                    if (!ReplayChecks.Normalize(slots!).SequenceEqual(ReplayChecks.Normalize(instrumented.Slots)))
                        throw new ReplayInputException("audit-instrumentation-altered-output");
                    diagnostic = instrumented.Diagnostics;
                }
                report = ReplayCase.Create(id, input, metrics!, slots!.ToArray(), "app", "audit");
                if (metrics!.Violations.Count > 0) hardCases++;
                var fixedSatisfied = anchors is null || anchors.All(anchor => slots!.Any(s => s.EmployeeId == anchor.Key.Item1 &&
                    s.DayOfMonth == anchor.Key.Item2 && ReplayChecks.Time(s.FromTime, out var from) && from == anchor.Value.Item1 &&
                    ReplayChecks.Time(s.ToTime, out var to) && to == anchor.Value.Item2));
                cases.Add(new
                {
                    Id = id,
                    Factors = factors,
                    RespectAvailability = input.PriorityRespectAvailability,
                    WeekendsOff = input.PriorityWeekendsOff,
                    Hours = input.PriorityShiftHours,
                    PriorityCount = input.Employees.Count(e => e.IsPriority),
                    EmployeeCount = input.Employees.Count,
                    MeasuredRepetitions = repeats,
                    TraceOnly = traceOnly,
                    WarmupGenerations = anchors is null && !traceOnly ? 1 : 0,
                    DiagnosticGenerations = !control && anchors is null ? 1 : 0,
                    MedianMilliseconds = times.Order().ElementAt(times.Count / 2),
                    AllocatedBytes = allocations.Order().ElementAt(allocations.Count / 2),
                    MaximumMilliseconds = times.Max(),
                    DurationsMilliseconds = times,
                    PeakProcessBytes = Process.GetCurrentProcess().PeakWorkingSet64,
                    Hash = hash,
                    metrics!.TotalAssignedMinutes,
                    metrics.CoverageGapMinutes,
                    metrics.PriorityDeficitMinutes,
                    metrics.RegularDeficitMinutes,
                    Early = report.PriorityEarlyIntervals,
                    Late = report.PriorityLateIntervals,
                    Objective = report.ObjectiveVector,
                    LegacyObjective = input.Employees.Any(e => e.IsPriority) ? null : LegacyScheduleReference.Score(input.Schedule, input.Employees, WithExternal(input), slots!.ToArray()),
                    LocalQuality = report.LocalQuality,
                    HardViolations = metrics.Violations.Count,
                    ViolationCodes = metrics.Violations,
                    Classification = metrics.Violations.Count > 0 ? "HARD_VIOLATION" : report.Classification,
                    Diagnostics = diagnostic,
                    QualityBounds = report.Quality,
                    Profiles = Profiles(input, slots!, metrics),
                    FixedAnchorCount = anchors?.Count ?? 0,
                    CounterfactualStatus = anchors is null ? null : fixedSatisfied ? "FEASIBLE" : "NOT REALIZED - NOT AN IMPROVEMENT WITNESS"
                });
                Console.WriteLine($"Audit {id}: hard={metrics.Violations.Count}, priorities={input.Employees.Count(e => e.IsPriority)}, early={report.PriorityEarlyIntervals}, late={report.PriorityLateIntervals}, ms={times.Order().ElementAt(times.Count / 2):F1}");
                await File.WriteAllTextAsync(Path.Combine(paths.Output, control ? "deep-control.json" : "deep-candidate.json"),
                    JsonSerializer.Serialize(new
                    {
                        Control = control,
                        AssemblySha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(typeof(ScheduleGenerator).Assembly.Location))),
                        PairwiseCaseCount = PriorityAuditScenarios.PairwiseRows.Length,
                        Cases = cases
                    }, new JsonSerializerOptions { WriteIndented = true }));
                return slots!;
            }

            var section = root.TryGetProperty("section", out var selectedSection) ? selectedSection.GetString() : "all";
            if (section is not ("all" or "witnesses" or "matrix" or "screenshot" or "real" or "real-base" or "benchmark" or "optimization" or "verify-optimization" or "legacy" or "legacy-matrix" or "policy-matrix" or "legacy-benchmark" or "cancellation"))
                throw new ReplayInputException("audit-section-invalid");
            if (section == "legacy-matrix")
                for (var i = 0; i < PriorityAuditScenarios.PairwiseRows.Length; i++)
                    foreach (var cap in new[] { 0, 80, 200 })
                    {
                        var input = From(PriorityAuditScenarios.Build(i)); foreach (var employee in input.Employees) employee.IsPriority = false;
                        input.Schedule.MaxHoursPerEmpMonth = cap;
                        await Run($"legacy-matrix-{i:000}-cap-{cap}", input with { PriorityShiftHours = null }, factors: PriorityAuditScenarios.PairwiseRows[i]);
                    }
            if (section == "legacy")
            {
                await using var source = await SqliteReplaySource.OpenAsync(paths.Database, default, "legacy");
                var ids = await source.GraphIdsAsync(null, default);
                for (var index = 0; index < ids.Count; index++)
                {
                    var input = await source.LoadAsync(ids[index], default); foreach (var employee in input.Employees) employee.IsPriority = false;
                    input = input with { PriorityShiftHours = null }; await Run($"legacy-real-{index:000}", input);
                    if (index != 0 && index != ids.Count / 2 && index != ids.Count - 1 && ids[index] != root.GetProperty("rosterSourceGraph").GetInt32()) continue;
                    foreach (var variant in new[] { "zero-minimum", "high-minimum", "higher-staffing", "tight-cap", "restricted", "reduced-roster", "added-worker", "strict-full-days" })
                    {
                        var changed = Copy(input);
                        switch (variant)
                        {
                            case "zero-minimum": foreach (var employee in changed.Employees) employee.MinHoursMonth = 0; break;
                            case "high-minimum": foreach (var employee in changed.Employees) employee.MinHoursMonth = 200; break;
                            case "higher-staffing": changed.Schedule.PeoplePerShift++; break;
                            case "tight-cap": changed.Schedule.MaxHoursPerEmpMonth = 80; break;
                            case "restricted": foreach (var day in changed.Groups.SelectMany(g => g.Members).SelectMany(m => m.Days).Where(d => d.DayOfMonth % 5 == 0)) day.Kind = AvailabilityKind.NONE; break;
                            case "reduced-roster": changed = changed with { Employees = changed.Employees.Take(Math.Max(1, changed.Employees.Count - 2)).ToArray() }; break;
                            case "added-worker": changed = changed with { Employees = changed.Employees.Append(new ScheduleEmployeeModel { EmployeeId = 1_000_000_000, DisplayOrder = int.MaxValue, MinHoursMonth = 120 }).ToArray() }; break;
                            case "strict-full-days": changed.Schedule.MaxFullPerMonth = 1; changed.Schedule.MaxConsecutiveFull = 1; break;
                        }
                        await Run($"legacy-real-{index:000}-{variant}", changed);
                    }
                    await Run($"legacy-real-{index:000}-reversed", Copy(input) with { Employees = input.Employees.Reverse().ToArray(), Groups = input.Groups.Reverse().ToArray() });
                    await Run($"legacy-real-{index:000}-rotated", Permute(input, 1));
                }
                var reconstruction = await source.LoadReconstructionAsync(root.GetProperty("rosterSourceGraph").GetInt32(), root.GetProperty("targetGroup").GetInt32(), root.GetProperty("targetEmployee").GetInt32(), default);
                foreach (var employee in reconstruction.Employees) employee.IsPriority = false;
                await Run("legacy-reconstruction", reconstruction with { PriorityShiftHours = null });
            }
            if (section is "all" or "witnesses")
            {
                foreach (var s in new[] { PriorityAuditScenarios.CalendarTrap(), PriorityAuditScenarios.Handover("09:00 - 13:00"),
                    PriorityAuditScenarios.Handover("17:00 - 21:00"), PriorityAuditScenarios.Handover("ANY"), PriorityAuditScenarios.Handover("ANY", true) })
                {
                    var input = From(s);
                    await Run(s.Id, input);
                    if (control || s.Id == "calendar-trap") continue;
                    await Run(s.Id + "-forced-early", input, new() { [(1, 1)] = (540, 1020) });
                    await Run(s.Id + "-forced-late", input, new() { [(1, 1)] = (780, 1260) });
                }
            }
            if (section is "all" or "matrix")
                for (var index = 0; index < PriorityAuditScenarios.PairwiseRows.Length; index++)
                {
                    var s = PriorityAuditScenarios.Build(index);
                    await Run(s.Id, From(s), factors: s.Factors);
                }
            if (section is "all" or "screenshot")
                foreach (var target in new[] { 0, 40, 80, 120, 160, 168, 170, 180, 200 })
                    foreach (var respect in new[] { true, false })
                        foreach (var weekends in new[] { true, false })
                        {
                            var s = PriorityAuditScenarios.Screenshot(target);
                            await Run($"screenshot-target-{target}-respect-{respect}-weekends-{weekends}", From(s) with
                            {
                                PriorityRespectAvailability = respect,
                                PriorityWeekendsOff = weekends
                            });
                        }
            if (section is "all" or "real" or "real-base" or "benchmark" or "optimization" or "verify-optimization" or "policy-matrix" or "legacy-benchmark" or "cancellation")
            {
                await using var source = await SqliteReplaySource.OpenAsync(paths.Database, default, "app");
                var reproduction = await source.LoadReconstructionAsync(root.GetProperty("rosterSourceGraph").GetInt32(),
                    root.GetProperty("targetGroup").GetInt32(), root.GetProperty("targetEmployee").GetInt32(), default);
                if (section == "cancellation")
                {
                    var measurements = new List<object>();
                    foreach (var priority in new[] { false, true })
                        foreach (var delay in new[] { 1, 25 })
                        {
                            var input = Copy(reproduction);
                            if (!priority) foreach (var employee in input.Employees) employee.IsPriority = false;
                            var original = JsonSerializer.Serialize(input);
                            using var canceled = new CancellationTokenSource(delay);
                            var watch = Stopwatch.StartNew();
                            var observed = false;
                            try { await generate(input, [], false, canceled.Token); }
                            catch (OperationCanceledException) when (canceled.IsCancellationRequested) { observed = true; }
                            watch.Stop();
                            if (!observed || JsonSerializer.Serialize(input) != original) throw new ReplayInputException("cancellation-contract");
                            measurements.Add(new
                            {
                                Engine = priority ? "priority" : "legacy",
                                RequestedDelayMilliseconds = delay,
                                ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
                                InputUnchanged = true,
                                CancellationObserved = true
                            });
                        }
                    await File.WriteAllTextAsync(Path.Combine(paths.Output, control ? "cancellation-control.json" : "cancellation-candidate.json"),
                        JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true }));
                    return 0;
                }
                if (section == "legacy-benchmark")
                {
                    foreach (var employee in reproduction.Employees) employee.IsPriority = false;
                    await Run("legacy-reconstruction", reproduction with { PriorityShiftHours = null });
                    var ids = await source.GraphIdsAsync(null, default);
                    foreach (var index in Enumerable.Range(0, ids.Count))
                    {
                        var saved = await source.LoadAsync(ids[index], default);
                        foreach (var employee in saved.Employees) employee.IsPriority = false;
                        await Run($"legacy-real-{index:000}", saved with { PriorityShiftHours = null });
                    }
                    return hardCases > 0 ? 1 : 0;
                }
                if (section == "policy-matrix")
                {
                    foreach (var hours in new[] { 6, 8, 10, 12 })
                        foreach (var respect in new[] { true, false })
                            foreach (var weekends in new[] { true, false })
                                await Run($"real-hours-{hours}-respect-{respect}-weekends-{weekends}", Copy(reproduction) with
                                { PriorityShiftHours = hours, PriorityRespectAvailability = respect, PriorityWeekendsOff = weekends });
                    return hardCases > 0 ? 1 : 0;
                }
                foreach (var respect in root.TryGetProperty("respectAvailability", out var policy) ? new[] { policy.GetBoolean() } : new[] { true, false })
                {
                    var input = Copy(reproduction) with { PriorityRespectAvailability = respect };
                    var baseline = await Run($"reconstruction-respect-{respect}", input);
                    if (section == "real-base") continue;
                    if (section is "optimization" or "verify-optimization")
                    {
                        if (!control)
                        {
                            var witness = await Run($"known-witness-respect-{respect}", input,
                                new() { [(input.Employees.Single(e => e.IsPriority).EmployeeId, 10)] = (780, 1260) });
                            if (section == "verify-optimization")
                            {
                                double[] Vector(IList<ScheduleSlotModel> slots) => ReplayCase.Create("verify", input, ReplayChecks.Check(input, slots.ToArray()), slots.ToArray(), "app", "audit").ObjectiveVector!;
                                double[] historical = [120, 4260, 17160, 0.5823153000705181, 7.2078685614669435, 177, 11];
                                if (LegacyScheduleReference.Compare(Vector(baseline), historical) > 0) throw new ReplayInputException("production-misses-known-witness");
                            }
                        }
                        continue;
                    }
                    if (section == "benchmark")
                    {
                        foreach (var count in new[] { 6, 12 })
                        {
                            var copy = Copy(input);
                            var ordered = copy.Employees.OrderByDescending(e => e.IsPriority).ThenBy(e => e.EmployeeId).ToArray();
                            for (var i = 0; i < ordered.Length; i++) ordered[i].IsPriority = i < count;
                            await Run($"real-priority-count-{count}-respect-{respect}", copy);
                        }
                        var zero = Copy(input); zero.Employees.Single(e => e.IsPriority).MinHoursMonth = 0;
                        await Run($"real-target-0-respect-{respect}", zero);
                        continue;
                    }
                    if (!control)
                    {
                        var priority = input.Employees.Single(e => e.IsPriority).EmployeeId;
                        var early = baseline.Where(s => s.EmployeeId == priority && s.FromTime == "09:00").OrderBy(s => s.DayOfMonth).ToArray();
                        foreach (var slot in baseline.Where(s => s.EmployeeId == priority).OrderBy(s => s.DayOfMonth))
                        {
                            var late = slot.FromTime == "09:00";
                            await Run($"counterfactual-respect-{respect}-day-{slot.DayOfMonth:00}", input,
                                new() { [(priority, slot.DayOfMonth)] = late ? (780, 1260) : (540, 1020) });
                        }
                        for (var first = 0; first < Math.Min(6, early.Length); first++)
                            for (var second = first + 1; second < Math.Min(6, early.Length); second++)
                                await Run($"paired-respect-{respect}-{first}-{second}", input,
                                    new() { [(priority, early[first].DayOfMonth)] = (780, 1260), [(priority, early[second].DayOfMonth)] = (780, 1260) });
                    }
                    foreach (var count in new[] { 0, 1, 2, 3, 4, 6, 12 })
                    {
                        var copy = Copy(input);
                        var ordered = copy.Employees.OrderByDescending(e => e.IsPriority).ThenBy(e => e.EmployeeId).ToArray();
                        for (var i = 0; i < ordered.Length; i++) ordered[i].IsPriority = i < count;
                        await Run($"real-priority-count-{count}-respect-{respect}", copy);
                    }
                    foreach (var target in new[] { 0, 40, 80, 120, 160, 168, 170, 180, 200 })
                    {
                        var copy = Copy(input); copy.Employees.Single(e => e.IsPriority).MinHoursMonth = target;
                        await Run($"real-target-{target}-respect-{respect}", copy);
                    }
                    await Run($"real-reversed-respect-{respect}", Copy(input) with { Employees = input.Employees.Reverse().ToArray(), Groups = input.Groups.Reverse().ToArray() });
                    foreach (var rotation in new[] { 1, input.Employees.Count - 1 })
                        await Run($"real-id-permutation-{rotation}-respect-{respect}", Permute(input, rotation));
                }
            }
            return hardCases > 0 ? 1 : 0;
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            Console.Error.WriteLine("Audit failed: " + (e is ReplayInputException ? e.Message : e is OperationCanceledException ? "bounded-generation-timeout"
                : e is TypeLoadException or MissingMethodException or MethodAccessException ? "incompatible-audit-control-assembly"
                : e is FileNotFoundException ? "audit-dependency-not-found" : "private-input-or-audit-runtime-error"));
            Console.Error.WriteLine("Audit error category: " + e.GetType().Name);
            return 1;
        }
    }

    internal static ReplayInput Permute(ReplayInput input, int rotation)
    {
        var copy = Copy(input);
        var ids = copy.Employees.Select(e => e.EmployeeId).Distinct().Order().ToArray();
        var map = ids.Select((id, i) => (id, next: ids[(i + rotation) % ids.Length])).ToDictionary(x => x.id, x => x.next);
        foreach (var employee in copy.Employees) employee.EmployeeId = map[employee.EmployeeId];
        foreach (var member in copy.Groups.SelectMany(g => g.Members)) if (map.TryGetValue(member.EmployeeId, out var next)) member.EmployeeId = next;
        foreach (var slot in copy.ExternalSlots.Concat(copy.BaselineSlots)) if (slot.EmployeeId.HasValue && map.TryGetValue(slot.EmployeeId.Value, out var next)) slot.EmployeeId = next;
        return copy;
    }

    private static object[] Profiles(ReplayInput input, IList<ScheduleSlotModel> slots, ReplayMetrics metrics) => input.Employees
        .OrderBy(e => e.EmployeeId).Select((employee, ordinal) =>
        {
            var own = slots.Where(s => s.EmployeeId == employee.EmployeeId).ToArray();
            var templates = new[] { input.Schedule.Shift1Time, input.Schedule.Shift2Time }
                .Where(s => ReplayChecks.Range(s, out _, out _)).Select(s => { ReplayChecks.Range(s, out var from, out var to); return (from, to); }).ToArray();
            var worked = own.Sum(s => { ReplayChecks.Time(s.FromTime, out var from); ReplayChecks.Time(s.ToTime, out var to); return to - from; });
            var capacity = ReplayChecks.Quality(input with { Employees = new[] { employee } }, slots.ToArray(), metrics).OptimisticCapacityMinutes;
            var deficit = Math.Max(0, (employee.MinHoursMonth ?? 0) * 60 - worked);
            var longest = 0; var run = 0; var previous = -2;
            foreach (var day in own.Select(s => s.DayOfMonth).Distinct().Order()) { run = day == previous + 1 ? run + 1 : 1; longest = Math.Max(longest, run); previous = day; }
            return (object)new
            {
                EmployeeOrdinal = ordinal + 1,
                employee.IsPriority,
                MinimumMinutes = (employee.MinHoursMonth ?? 0) * 60,
                WorkedMinutes = worked,
                DeficitMinutes = deficit,
                OptimisticCapacityMinutes = capacity,
                DeficitFractionOfOptimisticCapacity = capacity > 0 ? (double?)deficit / capacity : null,
                DeficitBeyondProvenIndividualLowerBound = Math.Max(0, deficit - Math.Max(0, (employee.MinHoursMonth ?? 0) * 60 - capacity)),
                Early = own.Count(s => ReplayChecks.Time(s.FromTime, out var from) && from == templates.Min(t => t.from)),
                Late = own.Count(s => ReplayChecks.Time(s.ToTime, out var to) && to == templates.Max(t => t.to) &&
                    ReplayChecks.Time(s.FromTime, out var from) && from != templates.Min(t => t.from)),
                FullDays = own.GroupBy(s => s.DayOfMonth).Count(g => g.Count() > 1),
                PartialIntervals = own.Count(s =>
                    TimeSpan.Parse(s.ToTime) - TimeSpan.Parse(s.FromTime) < TimeSpan.FromHours(6)),
                LongestWorkingRun = longest,
                DaysOff = DateTime.DaysInMonth(input.Schedule.Year, input.Schedule.Month) - own.Select(s => s.DayOfMonth).Distinct().Count()
            };
        }).ToArray();
}
