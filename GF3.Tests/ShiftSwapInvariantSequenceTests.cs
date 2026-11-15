using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ShiftSwaps;
using static GF3.Tests.Infrastructure.ShiftSwapScenarioFactory;
using System.Data.Common;
using System.Diagnostics;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit.Abstractions;

namespace GF3.Tests;

public sealed class ShiftSwapInvariantSequenceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ListQueryCount_DoesNotGrowWithOpenOfferCount()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var setup = database.CreateContext();
        var f = await SeedAsync(setup, new());
        var cells = System.Text.Json.JsonSerializer.Serialize(Enumerable.Range(3, 20).ToDictionary(day => day.ToString(), _ => "09:00-17:00"));
        (await setup.Schedules.SingleAsync(s => s.Id == f.SourceSchedule)).Note = "[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\"," + cells + "]]}]]";
        await setup.SaveChangesAsync();
        var counter = new BenchmarkCommands();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(counter).Options);
        async Task Publish(int day)
        {
            await ManagerController(db).CreateManualOffer(f.Container, f.SourceSchedule,
                new() { ManualColumnId = 7, DayOfMonth = day, FromTime = "09:00", ToTime = "17:00" }, default);
        }
        await Publish(3);
        counter.Count = 0;
        var one = await EmployeeController(db, f.Target).GetVisible(default);
        var initialQueries = counter.Count;
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>(one.Result).Value));
        for (var day = 4; day <= 22; day++) await Publish(day);
        var before = await SnapshotAsync(db);
        counter.Count = 0;
        var many = await EmployeeController(db, f.Target).GetVisible(default);
        var finalQueries = counter.Count;
        Assert.Equal(20, Assert.IsAssignableFrom<IEnumerable<ShiftSwapDto>>(Assert.IsType<OkObjectResult>(many.Result).Value).Count());
        Assert.Equal(initialQueries, finalQueries);
        Assert.Equal(before, await SnapshotAsync(db));
        output.WriteLine($"Visible offers 1 -> 20; SQL statements {initialQueries} -> {finalQueries}; persisted tables unchanged.");
    }

    [Theory]
    [InlineData(10)][InlineData(50)]
    public async Task SwapRegressionBenchmark_RecordsEquivalentSqliteFixtures(int employeeCount)
    {
        var samples = new Dictionary<string, List<(double Ms, int Queries)>>();
        for (var repeat = 0; repeat < 6; repeat++)
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            await using var setup = database.CreateContext();
            var f = await SeedAsync(setup, new());
            for (var index = 4; index <= employeeCount; index++)
            {
                var employee = TestDataFactory.CreateDalEmployee($"Bench{index}", "Worker", email: $"bench{index}@example.invalid");
                setup.Add(employee); await setup.SaveChangesAsync();
                setup.ScheduleEmployees.Add(new() { ScheduleId = f.SourceSchedule, EmployeeId = employee.Id });
                for (var day = 1; day <= 31; day++)
                    setup.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(f.SourceSchedule, day, index, employee.Id, "09:00", "17:00"));
            }
            (await setup.Schedules.SingleAsync(s => s.Id == f.SourceSchedule)).Note =
                "[[GF3_GRAPH_META:{\"m\":[[7,\"Manual\",{\"3\":\"09:00-17:00\",\"4\":\"09:00-17:00\"}]]}]]";
            setup.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(f.SourceSchedule, 5, 1, f.Owner, "09:00", "17:00"));
            await setup.SaveChangesAsync();
            var counter = new BenchmarkCommands();
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(database.ConnectionString).AddInterceptors(counter).Options);
            async Task Measure(string name, Func<Task> run)
            {
                counter.Count = 0; var watch = Stopwatch.StartNew();
                await run(); watch.Stop();
                if (repeat == 0) return;
                if (!samples.TryGetValue(name, out var values)) samples[name] = values = [];
                values.Add((watch.Elapsed.TotalMilliseconds, counter.Count));
            }
            await Measure("manual-post", async () => { await ManagerController(db).CreateManualOffer(f.Container, f.SourceSchedule,
                new() { ManualColumnId = 7, DayOfMonth = 3, FromTime = "09:00", ToTime = "17:00" }, default); });
            var full = await db.ShiftSwapRequests.SingleAsync();
            await Measure("employee-list", async () => { await EmployeeController(db, f.Target).GetVisible(default); });
            await Measure("graph-log", async () => { await ManagerController(db).GetGraphLog(f.Container, f.SourceSchedule, default); });
            await Measure("manual-full-accept", async () => { await EmployeeController(db, f.Target).Accept(full.Id, default); });
            await ManagerController(db).CreateManualOffer(f.Container, f.SourceSchedule,
                new() { ManualColumnId = 7, DayOfMonth = 4, FromTime = "09:00", ToTime = "17:00", AllowPartialAcceptance = true }, default);
            var partial = await db.ShiftSwapRequests.SingleAsync(r => r.Status == ShiftSwapStatus.Open);
            await Measure("manual-partial-accept", async () => { await EmployeeController(db, f.Target).Accept(partial.Id, default, new() { FromTime = "11:00", ToTime = "15:00" }); });
            var exchange = await EmployeeController(db, f.Owner).Create(Request(f, new(Kind: SwapKind.Exchange)), default);
            var offer = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>(exchange.Result).Value);
            await Measure("exchange-accept", async () => { await EmployeeController(db, f.Target).Accept(offer.Id, default); });
            await AssertNoOverlaps(db);
        }
        foreach (var (name, values) in samples)
        {
            var sorted = values.Select(v => v.Ms).Order().ToArray();
            output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = name, employees = employeeCount,
                days = 31, warmup = 1, repeats = 5, medianMs = sorted[2], maxMs = sorted[^1], queries = values.Select(v => v.Queries).ToArray() }));
        }
    }

    private sealed class BenchmarkCommands : DbCommandInterceptor
    {
        public int Count;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Count++; return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { Count++; return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        { Count++; return ValueTask.FromResult(result); }
    }

    [Theory]
    [InlineData(1729)]
    [InlineData(104729)]
    [InlineData(8675309)]
    public async Task Forty_controller_operations_preserve_persisted_invariants(int seed)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var setup = database.CreateContext())
        {
            var f = await SeedAsync(setup, new());
            for (var day = 3; day <= 28; day++)
                setup.Add(TestDataFactory.CreateDalSlot(day % 2 == 0 ? f.SourceSchedule : f.ReturnSchedule, day, 1, day % 3 + 1, "09:00", "17:00"));
            // All participants are discoverable in the same published container.
            foreach (var schedule in new[] { f.SourceSchedule, f.ReturnSchedule })
                foreach (var employee in new[] { 1, 2, 3 })
                    if (!await setup.ScheduleEmployees.AnyAsync(e => e.ScheduleId == schedule && e.EmployeeId == employee))
                        setup.Add(new ScheduleEmployeeModel { ScheduleId = schedule, EmployeeId = employee });
            await setup.SaveChangesAsync();
        }
        var random = new Random(seed);
        int latest = 0, latestOwner = 0, latestTarget = 0, lastAccepted = 0;
        for (var operation = 0; operation < 40; operation++)
        {
            try
            {
                await using var db = database.CreateContext();
                await AssertInvariants(db); // Reject an invalid fixture before attributing anything to the action.
                var before = await SnapshotAsync(db);
                var minutesBefore = (await ReadAssignmentIntervalsAsync(db)).Sum(s => s.Minutes);
                var historyBefore = await db.ShiftSwapHistories.CountAsync();
                var versionsBefore = await db.ScheduleVersions.CountAsync();
                var operationKind = operation % 10;
                var expectedVersionDelta = 0;
                if (operationKind is 2 or 5)
                {
                    var accepting = await db.ShiftSwapRequests.AsNoTracking().SingleAsync(r => r.Id == latest);
                    foreach (var schedule in new[] { (int?)accepting.ScheduleId, accepting.ReturnScheduleId }.Where(id => id.HasValue).Distinct())
                        expectedVersionDelta += await db.ScheduleVersions.AnyAsync(v => v.ScheduleId == schedule!.Value) ? 1 : 2;
                }
                var reserved = (await db.ShiftSwapRequests.Where(r => r.Status == ShiftSwapStatus.Open).ToListAsync())
                    .SelectMany(r => new[] { r.ScheduleSlotId, r.ReturnScheduleSlotId }).ToHashSet();
                var available = (await db.ScheduleSlots.AsNoTracking().Where(s => s.EmployeeId != null && s.FromTime == "09:00" && s.ToTime == "17:00").ToListAsync())
                    .Where(s => !reserved.Contains(s.Id)).OrderBy(s => s.Id).ToArray();
                var source = available[random.Next(available.Length)];
                var actor = source.EmployeeId!.Value;
                var target = actor % 3 + 1;
                var request = new CreateEmployeeShiftSwapRequest { ScheduleId = source.ScheduleId, ScheduleSlotId = source.Id,
                    FromTime = random.Next(2) == 0 ? "09:00" : "09:17", ToTime = "13:43", TargetEmployeeId = random.Next(2) == 0 ? target : null };
                Exception? error = null;
                var expectedRejection = operationKind is 8 or 9;
                switch (operationKind)
                {
                    case 0: case 3: case 6:
                        if (operationKind == 3)
                        {
                            var returns = available.Where(s => s.EmployeeId != actor && s.DayOfMonth != source.DayOfMonth).ToArray();
                            var back = returns[random.Next(returns.Length)];
                            target = back.EmployeeId!.Value;
                            request.RequestKind = "exchange"; request.TargetEmployeeId = target;
                            request.ReturnScheduleId = back.ScheduleId; request.ReturnScheduleSlotId = back.Id;
                            request.ReturnFromTime = "10:00"; request.ReturnToTime = "16:00";
                        }
                        var created = Assert.IsType<ShiftSwapDto>(Assert.IsType<CreatedAtActionResult>((await EmployeeController(db, actor).Create(request, default)).Result).Value);
                        latest = created.Id; latestOwner = actor; latestTarget = target;
                        break;
                    case 1:
                        var preview = Assert.IsType<ShiftSwapOfferPreviewDto>(Assert.IsType<OkObjectResult>((await EmployeeController(db, actor).Preview(request, default)).Result).Value);
                        Assert.True(preview.IsValid);
                        break;
                    case 2: case 5:
                        await EmployeeController(db, latestTarget).Accept(latest, default); lastAccepted = latest;
                        break;
                    case 4:
                        Assert.IsType<OkObjectResult>((await EmployeeController(db, actor).GetReturnCandidates(source.ScheduleId, source.Id, "09:00", "13:00", target, default)).Result);
                        break;
                    case 7:
                        await EmployeeController(db, latestOwner).Cancel(latest, default);
                        break;
                    case 8:
                        request.ToTime = "24:00";
                        error = await Record.ExceptionAsync(() => EmployeeController(db, actor).Create(request, default));
                        break;
                    case 9:
                        error = await Record.ExceptionAsync(() => EmployeeController(db, latestTarget).Accept(lastAccepted, default));
                        break;
                }
                await using var fresh = database.CreateContext();
                if (expectedRejection) {
                    Assert.IsType<ValidationException>(error);
                    Assert.Equal(before, await SnapshotAsync(fresh));
                } else {
                    await AssertInvariants(fresh);
                    Assert.Equal(minutesBefore, (await ReadAssignmentIntervalsAsync(fresh)).Sum(s => s.Minutes));
                    var accepting = operationKind is 2 or 5;
                    Assert.Equal(historyBefore + (accepting ? 1 : 0), await fresh.ShiftSwapHistories.CountAsync());
                    if (!accepting) Assert.Equal(versionsBefore, await fresh.ScheduleVersions.CountAsync());
                    else Assert.Equal(expectedVersionDelta, await fresh.ScheduleVersions.CountAsync() - versionsBefore);
                    if (operationKind is 1 or 4) Assert.Equal(before, await SnapshotAsync(fresh));
                }
            }
            catch (Exception exception)
            {
                throw new Xunit.Sdk.XunitException($"Seed={seed}, operation={operation}: {exception}");
            }
        }
        await using var final = database.CreateContext();
        Assert.Equal(8, await final.ShiftSwapHistories.CountAsync());
        Assert.Equal(4, await final.ShiftSwapRequests.CountAsync(r => r.Status == ShiftSwapStatus.Cancelled));
    }
}
