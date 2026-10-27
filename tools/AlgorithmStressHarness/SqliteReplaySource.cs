using System.Diagnostics;
using System.Security.Cryptography;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

internal sealed record ReplayInput(ScheduleModel Schedule, IReadOnlyList<ScheduleEmployeeModel> Employees,
    IReadOnlyList<AvailabilityGroupModel> Groups, IReadOnlyList<ScheduleSlotModel> ExternalSlots,
    IReadOnlyList<ScheduleSlotModel> BaselineSlots, int? PriorityShiftHours = null, bool PriorityWeekendsOff = true);

internal sealed class ReplayInputException(string code) : Exception(code);

internal sealed class SqliteReplaySource : IAsyncDisposable
{
    internal const string PinnedCommit = "a9e79fffab1d5d1adc298ef1f32a66cc429d53cf";
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;
    private readonly ContainerService service;
    private readonly TimedGenerator generator;
    private readonly string engine;
    private readonly string variant;
    private readonly DirectoryInfo? snapshotDirectory;

    private SqliteReplaySource(string path, string engine, string variant, DirectoryInfo? snapshotDirectory)
    {
        this.snapshotDirectory = snapshotDirectory;
        this.engine = engine;
        this.variant = variant;
        connection = new(new SqliteConnectionStringBuilder { DataSource = new Uri(path).AbsoluteUri + "?immutable=1", Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        context = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);
        var legacy = new ScheduleGenerator();
        generator = new(engine == "app" ? new PriorityAwareScheduleGenerator(legacy, new(legacy), context) : legacy);
        service = new(new ContainerRepository(context), new ScheduleRepository(context), new SchedulePresetRepository(context),
            new ScheduleSlotRepository(context), new ScheduleEmployeeRepository(context), new ScheduleCellStyleRepository(context),
            new AvailabilityGroupRepository(context), generator);
    }

    internal static async Task<SqliteReplaySource> OpenAsync(string path, CancellationToken ct, string engine = "legacy", string variant = "stored")
    {
        DirectoryInfo? snapshot = null;
        // Immutable SQLite ignores WAL. Recover only a fingerprint-verified private file copy;
        // SQLite never opens the original WAL database or writes its shared-memory read marks.
        if (File.Exists(path + "-wal") && new FileInfo(path + "-wal").Length > 0)
        {
            var before = await FingerprintAsync(path, ct);
            snapshot = Directory.CreateTempSubdirectory("GF3.ReplaySnapshot-");
            try
            {
                var copy = Path.Combine(snapshot.FullName, "snapshot.db");
                foreach (var suffix in new[] { "", "-wal" })
                {
                    ct.ThrowIfCancellationRequested();
                    File.Copy(path + suffix, copy + suffix);
                }
                if (await FingerprintAsync(path, ct) != before) throw new ReplayInputException("source-changed-during-snapshot");
                await using (var recovery = new SqliteConnection(new SqliteConnectionStringBuilder {
                    DataSource = copy, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString()))
                {
                    await recovery.OpenAsync(ct);
                    await using var checkpoint = recovery.CreateCommand();
                    checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
                    await using var reader = await checkpoint.ExecuteReaderAsync(ct);
                    if (!await reader.ReadAsync(ct) || reader.GetInt32(0) != 0) throw new ReplayInputException("private-snapshot-checkpoint-failed");
                }
                path = copy;
            }
            catch { DeleteSnapshot(snapshot); throw; }
        }
        var source = new SqliteReplaySource(path, engine, variant, snapshot);
        try
        {
            await source.connection.OpenAsync(ct);
            await using var queryOnly = source.connection.CreateCommand();
            queryOnly.CommandText = "PRAGMA query_only = ON";
            await queryOnly.ExecuteNonQueryAsync(ct);
            queryOnly.CommandText = "PRAGMA query_only";
            if (Convert.ToInt32(await queryOnly.ExecuteScalarAsync(ct)) != 1) throw new ReplayInputException("read-only-guard-unavailable");
            var expected = source.context.Database.GetMigrations().ToArray();
            var actual = (await source.context.Database.GetAppliedMigrationsAsync(ct)).ToArray();
            if (!expected.SequenceEqual(actual)) throw new ReplayInputException("incompatible-schema-use-a-consistent-backup-from-the-pinned-version");
            foreach (var entity in source.context.Model.GetEntityTypes().Where(e => e.GetTableName() is not null))
            {
                await using var command = source.connection.CreateCommand();
                var table = entity.GetTableName()!;
                var columns = entity.GetProperties().Select(p => p.GetColumnName()).Distinct();
                command.CommandText = $"SELECT {string.Join(",", columns.Select(column => "t." + Quote(column)))} FROM {Quote(table)} AS t LIMIT 0";
                await using var reader = await command.ExecuteReaderAsync(ct);
            }
            return source;
        }
        catch
        {
            await source.DisposeAsync();
            throw;
        }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    internal async Task<List<int>> GraphIdsAsync(int? selected, CancellationToken ct)
    {
        var query = context.Schedules.AsNoTracking();
        if (selected.HasValue) query = query.Where(s => s.Id == selected.Value);
        var ids = await query.OrderBy(s => s.Id).Select(s => s.Id).ToListAsync(ct);
        if (selected.HasValue && ids.Count == 0) throw new ReplayInputException("selected-graph-not-found");
        return ids;
    }

    internal async Task<ReplayInput> LoadAsync(int graphId, CancellationToken ct)
    {
        var owner = await context.Schedules.AsNoTracking().Where(s => s.Id == graphId).Select(s => s.ContainerId).SingleAsync(ct);
        var schedule = (await service.GetGraphByIdAsync(owner, graphId, ct))!;
        var storedEmployees = (await service.GetGraphEmployeesAsync(owner, graphId, ct))!;
        var prioritySelection = storedEmployees.Select(e => e.EmployeeId).Distinct().Order()
            .Take(variant == "first" ? 1 : variant == "first-two" ? 2 : 0).ToHashSet();
        var employees = storedEmployees.Select(e => new ScheduleEmployeeModel { Id = e.Id, ScheduleId = e.ScheduleId,
                EmployeeId = e.EmployeeId, DisplayOrder = e.DisplayOrder, MinHoursMonth = e.MinHoursMonth,
                IsPriority = variant switch { "none" => false, "first" or "first-two" => prioritySelection.Contains(e.EmployeeId),
                    "all" => true, _ => e.IsPriority } }).ToArray();
        if (schedule.Year is < 1900 or > 9999 || schedule.Month is < 1 or > 12 || schedule.PeoplePerShift <= 0
            || !ReplayChecks.Range(schedule.Shift1Time, out _, out _)
            || !string.IsNullOrWhiteSpace(schedule.Shift2Time) && !ReplayChecks.Range(schedule.Shift2Time, out _, out _)
            || employees.Any(e => e.EmployeeId <= 0 || e.MinHoursMonth < 0))
            throw new ReplayInputException("invalid-generation-settings");
        var groups = new List<AvailabilityGroupModel>();
        if (schedule.AvailabilityGroupId is > 0)
        {
            var group = await new AvailabilityGroupRepository(context).GetFullByIdAsync(schedule.AvailabilityGroupId.Value, ct)
                ?? throw new ReplayInputException("missing-selected-availability");
            if (group.Year != schedule.Year || group.Month != schedule.Month) throw new ReplayInputException("availability-calendar-mismatch");
            groups.Add(new()
            {
                Year = group.Year, Month = group.Month,
                Members = group.Members.Select(member => new AvailabilityGroupMemberModel
                {
                    EmployeeId = member.EmployeeId, DisplayOrder = member.DisplayOrder,
                    Days = member.Days.Select(day => new AvailabilityGroupDayModel
                    {
                        DayOfMonth = day.DayOfMonth, Kind = (AvailabilityKind)(int)day.Kind, IntervalStr = day.IntervalStr
                    }).ToList()
                }).ToList()
            });
            if (groups.SelectMany(g => g.Members).SelectMany(m => m.Days).Any(d =>
                d.DayOfMonth < 1 || d.DayOfMonth > DateTime.DaysInMonth(schedule.Year, schedule.Month)
                || !Enum.IsDefined(d.Kind)
                || (d.Kind == AvailabilityKind.INT || !string.IsNullOrWhiteSpace(d.IntervalStr)) && !ReplayChecks.Range(d.IntervalStr, out _, out _)))
                throw new ReplayInputException("invalid-availability-record");
        }
        var others = await new ScheduleRepository(context).GetByMonthWithSlotsAsync(schedule.Year, schedule.Month, graphId, ct);
        var external = others.SelectMany(s => s.Slots).Select(s => new ScheduleSlotModel
        {
            DayOfMonth = s.DayOfMonth, EmployeeId = s.EmployeeId, SlotNo = s.SlotNo,
            FromTime = s.FromTime, ToTime = s.ToTime, Status = (SlotStatus)(int)s.Status
        }).ToArray();
        if (external.Any(s => s.DayOfMonth < 1 || s.DayOfMonth > DateTime.DaysInMonth(schedule.Year, schedule.Month)
            || !Enum.IsDefined(s.Status) || s.Status == SlotStatus.ASSIGNED && !s.EmployeeId.HasValue
            || s.Status == SlotStatus.UNFURNISHED && s.EmployeeId.HasValue
            || !ReplayChecks.Time(s.FromTime, out var from) || !ReplayChecks.Time(s.ToTime, out var to) || to <= from))
            throw new ReplayInputException("invalid-saved-commitment");
        var settings = engine == "app" ? await context.PriorityGenerationSettings.AsNoTracking().SingleOrDefaultAsync(s => s.Id == 1, ct) : null;
        return new(schedule, employees, groups, external, (await service.GetGraphSlotsAsync(owner, graphId, ct))!,
            engine == "app" ? settings?.ShiftHours ?? 8 : null, settings?.WeekendsOff ?? true);
    }

    internal async Task<(IReadOnlyList<ScheduleSlotModel> Slots, double Milliseconds, double PreviewMilliseconds)> PreviewAsync(ReplayInput input, CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        var result = await service.GenerateGraphPreviewAsync(input.Schedule.ContainerId, input.Schedule, input.Employees, null, ct);
        watch.Stop();
        if (result.WrittenSlotsCount != 0 || context.ChangeTracker.HasChanges()) throw new ReplayInputException("preview-write-contract");
        return (result.Slots.ToArray(), generator.Milliseconds, watch.Elapsed.TotalMilliseconds);
    }

    private sealed class TimedGenerator(IScheduleGenerator selected) : IScheduleGenerator
    {
        public double Milliseconds { get; private set; }
        public async Task<IList<ScheduleSlotModel>> GenerateAsync(ScheduleModel schedule, IEnumerable<AvailabilityGroupModel> availabilities,
            IEnumerable<ScheduleEmployeeModel> employees, IProgress<int>? progress = null, CancellationToken ct = default)
        {
            var watch = Stopwatch.StartNew();
            try { return await selected.GenerateAsync(schedule, availabilities, employees, progress, ct); }
            finally { Milliseconds = watch.Elapsed.TotalMilliseconds; }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        await connection.DisposeAsync();
        if (snapshotDirectory is not null) DeleteSnapshot(snapshotDirectory);
    }

    private static void DeleteSnapshot(DirectoryInfo snapshot)
    {
        var temporaryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(snapshot.FullName);
        if (!target.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) || !snapshot.Name.StartsWith("GF3.ReplaySnapshot-", StringComparison.Ordinal)
            || (snapshot.Attributes & FileAttributes.ReparsePoint) != 0) throw new ReplayInputException("unsafe-snapshot-cleanup-refused");
        snapshot.Delete(true);
    }

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.SequenceEqual(new[] { "--help" }))
        {
            Console.WriteLine("Offline read-only replay: --db <absolute-local-SQLite-file> (--all-graphs | --graph <positive-id>) [--repeat <1..10; default 3>] [--engine legacy|app; default legacy] [--variant stored|none|first|first-two|all; default stored; non-stored requires app] --output <existing-absolute-directory-outside-repository-and-source-directory>. Reports contain aggregate pseudonymous cases only. No schema setup or source writes. Existing four positional HTTP arguments remain supported.");
            return 0;
        }
        try
        {
            var options = Parse(args);
            using var canceled = new CancellationTokenSource();
            ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; canceled.Cancel(); };
            Console.CancelKeyPress += handler;
            try
            {
                var total = Stopwatch.StartNew();
                var fingerprint = await FingerprintAsync(options.Database, canceled.Token);
                List<int> ids;
                string quickCheck;
                await using (var source = await OpenAsync(options.Database, canceled.Token, options.Engine, options.Variant))
                {
                    quickCheck = await source.QuickCheckAsync(canceled.Token);
                    ids = await source.GraphIdsAsync(null, canceled.Token);
                    if (options.Graph.HasValue && !ids.Contains(options.Graph.Value)) throw new ReplayInputException("selected-graph-not-found");
                }
                var reports = new List<ReplayCase>();
                foreach (var (id, index) in ids.Select((id, index) => (id, index)))
                {
                    if (options.Graph.HasValue && options.Graph != id) continue;
                    var caseId = $"case-{index + 1:000}";
                    try
                    {
                        ReplayCase? report = null;
                        string[]? baseline = null;
                        for (var repeat = 0; repeat < options.Repeat; repeat++)
                        {
                            canceled.Token.ThrowIfCancellationRequested();
                            await using var source = await OpenAsync(options.Database, canceled.Token, options.Engine, options.Variant);
                            var input = await source.LoadAsync(id, canceled.Token);
                            using var bounded = CancellationTokenSource.CreateLinkedTokenSource(canceled.Token);
                            bounded.CancelAfter(TimeSpan.FromMinutes(2));
                            var generated = await source.PreviewAsync(input, bounded.Token);
                            var check = ReplayChecks.Check(input, generated.Slots);
                            var normalized = ReplayChecks.Normalize(generated.Slots);
                            report ??= ReplayCase.Create(caseId, input, check, generated.Slots.ToArray(), options.Engine, options.Variant);
                            report.DurationsMilliseconds.Add(generated.Milliseconds);
                            report.PreviewMilliseconds.Add(generated.PreviewMilliseconds);
                            report.RepeatStable &= baseline is null || baseline.SequenceEqual(normalized);
                            report.HardViolations = report.HardViolations.Concat(check.Violations).GroupBy(p => p.Key)
                                .ToDictionary(g => g.Key, g => g.Max(p => p.Value));
                            baseline = normalized;
                        }
                        reports.Add(report!);
                    }
                    catch (ReplayInputException exception) { reports.Add(ReplayCase.Skipped(caseId, exception.Message, options.Engine, options.Variant)); }
                    catch (OperationCanceledException) when (!canceled.IsCancellationRequested) { reports.Add(ReplayCase.Skipped(caseId, "generation-time-limit", options.Engine, options.Variant)); }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    { reports.Add(ReplayCase.Skipped(caseId, "generation-runtime-error", options.Engine, options.Variant)); }
                }
                total.Stop();
                await using (var source = await OpenAsync(options.Database, canceled.Token, options.Engine, options.Variant))
                    if (await source.QuickCheckAsync(canceled.Token) != quickCheck) throw new ReplayInputException("source-integrity-changed");
                if (await FingerprintAsync(options.Database, canceled.Token) != fingerprint) throw new ReplayInputException("source-fingerprint-changed");
                await ReplayReportWriter.WriteAsync(options.Output, reports, total.Elapsed.TotalMilliseconds, canceled.Token,
                    options.Engine, options.Variant, fingerprint, quickCheck);
                Console.WriteLine($"Read-only replay finished: cases={reports.Count}, failures={reports.Count(r => r.Failed)}; schema=compatible.");
                return reports.Any(r => r.Failed) ? 1 : 0;
            }
            finally { Console.CancelKeyPress -= handler; }
        }
        catch (ReplayInputException exception) { Console.Error.WriteLine("Replay refused: " + exception.Message); return 2; }
        catch (OperationCanceledException) { Console.Error.WriteLine("Replay canceled; no source writes."); return 130; }
        catch (Exception exception) when (exception is SqliteException or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine("Replay failed: source could not be read with the pinned schema, or output could not be written. No schema repair or fallback was attempted.");
            return 2;
        }
    }

    private async Task<string> QuickCheckAsync(CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA quick_check";
        var result = Convert.ToString(await command.ExecuteScalarAsync(ct));
        if (result != "ok") throw new ReplayInputException("source-integrity-check-failed");
        return result;
    }

    private static async Task<string> FingerprintAsync(string path, CancellationToken ct)
    {
        var parts = new List<string>();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            if (!File.Exists(path + suffix)) { parts.Add("absent"); continue; }
            await using var file = new FileStream(path + suffix, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            parts.Add($"{file.Length}:{Convert.ToHexString(await SHA256.HashDataAsync(file, ct))}");
        }
        return string.Join(";", parts);
    }

    private sealed record ReplayOptions(string Database, int? Graph, int Repeat, string Output, string Engine, string Variant);

    private static ReplayOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index++)
        {
            var flag = args[index];
            if (flag == "--all-graphs")
            {
                if (!values.TryAdd(flag, "true")) throw new ReplayInputException("duplicate-flag");
            }
            else if (flag is "--db" or "--graph" or "--repeat" or "--output" or "--engine" or "--variant")
            {
                if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal) || !values.TryAdd(flag, args[index]))
                    throw new ReplayInputException("missing-value-or-duplicate-flag");
            }
            else throw new ReplayInputException("unknown-flag-use-help");
        }
        if (!values.TryGetValue("--db", out var database) || !values.TryGetValue("--output", out var output)
            || values.ContainsKey("--all-graphs") == values.ContainsKey("--graph")) throw new ReplayInputException("db-output-and-exactly-one-graph-selector-required");
        int? graph = null;
        if (values.TryGetValue("--graph", out var selected))
        {
            if (!int.TryParse(selected, out var number) || number <= 0) throw new ReplayInputException("graph-must-be-positive");
            graph = number;
        }
        var repeat = 3;
        if (values.TryGetValue("--repeat", out var repetitions) && (!int.TryParse(repetitions, out repeat) || repeat < 1 || repeat > 10))
            throw new ReplayInputException("repeat-must-be-1-to-10");
        var engine = values.GetValueOrDefault("--engine", "legacy");
        var variant = values.GetValueOrDefault("--variant", "stored");
        if (engine is not ("legacy" or "app") || variant is not ("stored" or "none" or "first" or "first-two" or "all")
            || engine == "legacy" && variant != "stored") throw new ReplayInputException("invalid-engine-or-variant-combination");
        database = LocalPath(database, false);
        output = LocalPath(output, true);
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory(), output })
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
                if ((Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git"))
                    || File.Exists(Path.Combine(directory.FullName, "GF3.sln"))) && Within(output, directory.FullName))
                    throw new ReplayInputException("output-must-be-outside-repository");
        var sourceDirectory = Path.GetDirectoryName(database)!;
        if (Within(output, sourceDirectory) || Within(sourceDirectory, output)) throw new ReplayInputException("output-must-be-separate-from-source-directory");
        return new(database, graph, repeat, output, engine, variant);
    }

    private static string LocalPath(string path, bool directory)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || path.StartsWith("\\\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal) || path.IndexOf(':', 2) >= 0) throw new ReplayInputException("absolute-local-path-required");
        var full = Path.GetFullPath(path);
        if (new DriveInfo(Path.GetPathRoot(full)!).DriveType == DriveType.Network) throw new ReplayInputException("network-path-refused");
        if (directory ? !Directory.Exists(full) : !File.Exists(full)) throw new ReplayInputException("explicit-source-file-or-output-directory-does-not-exist");
        for (FileSystemInfo? item = directory ? new DirectoryInfo(full) : new FileInfo(full); item is not null;
             item = item is FileInfo file ? file.Directory : ((DirectoryInfo)item).Parent)
            if ((item.Attributes & FileAttributes.ReparsePoint) != 0) throw new ReplayInputException("linked-path-refused");
        return full;
    }

    private static bool Within(string path, string parent) => path.Equals(parent, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(Path.TrimEndingDirectorySeparator(parent) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
