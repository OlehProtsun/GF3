using System.Security.Cryptography;
using System.Text.Json;
using DataAccessLayer.Administration;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.Infrastructure;

namespace GF3.Tests;

public sealed class DatabaseMigrationStartupTests
{
    private const string AddCommunicationContentBlocks = "20261123140000_AddCommunicationContentBlocks";
    private const string ExpandManagerNotepad = "20261126170000_ExpandManagerNotepad";
    private const string AddManagerNoteEditSessions = "20261126180000_AddManagerNoteEditSessions";
    private static readonly string[] NotepadTables =
        ["manager_note", "manager_note_folder", "manager_note_share", "manager_note_tag", "manager_note_tag_link", "manager_note_version"];

    [Fact]
    public async Task ReviewedReleaseSchemaPrefixes_MatchImmutableContract()
    {
        const string baseline = "20261115135735_RefreshManualShiftOffer";
        string[] prefixes = [
            baseline,
            "20261122150944_AllowDisjointOpenSwapIntervals",
            "20261122151455_TrackEmployeeLaunchModes",
            "20261122160000_AddWorkflowLogLaunchMode",
            "20261122173000_AddShiftRemovalCorrections",
            "20261123120000_AddCommunicationGifAttachment",
            "20261123130000_AddCommunicationPhotoAttachment",
            "20261123140000_AddCommunicationContentBlocks"
        ];
        var root = CreateTempRoot();
        var path = Path.Combine(root, "SQLite.db");
        var contracts = new List<object>();
        try
        {
            await using var db = CreateContext($"Data Source={path}");
            var migrator = db.GetService<IMigrator>();
            foreach (var prefix in prefixes)
            {
                await migrator.MigrateAsync(prefix);
                contracts.Add(await ReadReviewedSchemaPrefixAsync(db, prefix));
            }

            var actual = JsonSerializer.Serialize(contracts, new JsonSerializerOptions { WriteIndented = true, NewLine = "\n" }) + "\n";
            var exportPath = Environment.GetEnvironmentVariable("GF3_EXPORT_SCHEMA_PREFIX_CONTRACTS");
            if (!string.IsNullOrWhiteSpace(exportPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(exportPath))!);
                await File.WriteAllTextAsync(exportPath, actual);
                return;
            }

            var expected = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "reviewed-schema-prefix-contracts.json"));
            Assert.Equal(expected.ReplaceLineEndings("\n").Replace("\\r\\n", "\\n"), actual.Replace("\\r\\n", "\\n"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReviewedReleaseTransition_PreservesRepresentativeRowsAndExportsSyntheticBaseline()
    {
        const string baseline = "20261115135735_RefreshManualShiftOffer";
        string[] expectedMigrations = [
            "20261122150944_AllowDisjointOpenSwapIntervals",
            "20261122151455_TrackEmployeeLaunchModes",
            "20261122160000_AddWorkflowLogLaunchMode",
            "20261122173000_AddShiftRemovalCorrections",
            "20261123120000_AddCommunicationGifAttachment",
            "20261123130000_AddCommunicationPhotoAttachment",
            "20261123140000_AddCommunicationContentBlocks"
        ];
        var root = CreateTempRoot();
        var sourcePath = Path.Combine(root, "SQLite.db");
        var clonePath = Path.Combine(root, "clone.db");
        var workspace = new SqliteDatabaseWorkspace(sourcePath);
        try
        {
            var mediaDirectory = Path.Combine(root, "communication-gifs");
            Directory.CreateDirectory(mediaDirectory);
            File.WriteAllBytes(Path.Combine(mediaDirectory, $"{Guid.NewGuid():N}.gif"),
                Convert.FromBase64String("R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs="));
            int owner;
            int slotId;
            await using (var source = CreateContext(workspace.ConnectionString))
            {
                await source.GetService<IMigrator>().MigrateAsync(baseline);
                var seeded = await EmployeePushNotificationSourceTests.Seed(source);
                owner = seeded.Owner;
                slotId = seeded.Slot.Id;
                source.ShiftSwapRequests.Add(EmployeePushNotificationSourceTests.Offer(owner, seeded.Schedule, seeded.Slot));
                await source.SaveChangesAsync();
                var created = DateTimeOffset.Parse("2026-11-20T09:00:00Z");
                await source.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO employee_account (employee_id, username, password_hash, password_updated_at_utc, session_version)
                    VALUES ({owner}, 'legacy-account', 'synthetic-hash', {created}, 1);
                    """);
                await source.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO workflow_log_entry (occurred_at_utc, actor_role, actor_employee_id, actor_name, action)
                    VALUES ({created}, 'employee', {owner}, 'Synthetic employee', 'Opened schedule');
                    """);
                await source.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO shift_correction_request
                        (schedule_id, employee_id, schedule_slot_id, day_of_month, original_from_time,
                         original_to_time, requested_from_time, requested_to_time, status, created_at_utc)
                    VALUES ({seeded.Schedule.Id}, {owner}, {slotId}, 20, '08:00', '16:00',
                            '08:00', '16:30', 'Pending', {created});
                    INSERT INTO shift_correction_request
                        (id, schedule_id, employee_id, schedule_slot_id, day_of_month, original_from_time,
                         original_to_time, requested_from_time, requested_to_time, status, created_at_utc)
                    VALUES (9001, {seeded.Schedule.Id}, {owner}, {slotId}, 20, '08:00', '16:00',
                            '08:00', '16:30', 'Approved', {created});
                    DELETE FROM shift_correction_request WHERE id = 9001;
                    """);
                await source.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO communication_message (title, body, deadline_at_utc, created_at_utc, created_by_manager_name)
                    VALUES ('Legacy message', 'Legacy text survives the release', {created.AddDays(1)}, {created}, 'Synthetic manager');
                    """);
            }

            var backup = await workspace.CreateAutomaticBackupAsync(default);
            var artifact = Environment.GetEnvironmentVariable("GF3_EXPORT_RELEASE_BASELINE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(artifact))
            {
                var artifactDirectory = Path.GetFullPath(artifact);
                var prefixes = Path.Combine(artifactDirectory, "prefixes");
                Directory.CreateDirectory(prefixes);
                File.Copy(backup.Path, Path.Combine(artifactDirectory, "SQLite.db"), overwrite: true);
                DirectoryCopy(mediaDirectory, Path.Combine(artifactDirectory, "communication-gifs"));
                File.Copy(backup.Path, Path.Combine(prefixes, "prefix-0.db"), overwrite: true);
                var rehearsalPath = Path.Combine(root, "prefix-rehearsal.db");
                File.Copy(backup.Path, rehearsalPath, overwrite: true);
                for (var i = 0; i < expectedMigrations.Length; i++)
                {
                    await using (var rehearsal = CreateContext($"Data Source={rehearsalPath}"))
                        await rehearsal.GetService<IMigrator>().MigrateAsync(expectedMigrations[i]);
                    SqliteConnection.ClearAllPools();
                    File.Copy(rehearsalPath, Path.Combine(prefixes, $"prefix-{i + 1}.db"), overwrite: true);
                }
            }
            Assert.Equal("0", await ReadPragmaAsync(workspace.ConnectionString, "PRAGMA wal_checkpoint(TRUNCATE)"));
            SqliteConnection.ClearAllPools();
            var sourceHash = await HashAsync(sourcePath);
            var backupHash = await HashAsync(backup.Path);
            File.Copy(backup.Path, clonePath);
            var cloneWorkspace = new SqliteDatabaseWorkspace(clonePath);
            await using (var clone = CreateContext(cloneWorkspace.ConnectionString))
            {
                var result = await DatabaseMigrationStartup.ApplyAsync(clone, cloneWorkspace,
                    requireExistingDatabase: true, backupBeforeMigrate: true, NullLogger.Instance);
                Assert.Equal(expectedMigrations.Append(ExpandManagerNotepad).Append(AddManagerNoteEditSessions), result.AppliedMigrations);
                var correction = await clone.ShiftCorrectionRequests.SingleAsync();
                Assert.Equal(ShiftCorrectionOperation.ChangeTimes, correction.Operation);
                Assert.Null(correction.RemovalSnapshotJson);
                Assert.Equal(("08:00", "16:00", "08:00", "16:30"),
                    (correction.OriginalFromTime, correction.OriginalToTime,
                     correction.RequestedFromTime, correction.RequestedToTime));
                Assert.Equal(owner, (await clone.ShiftSwapRequests.SingleAsync()).FromEmployeeId);
                var account = await clone.EmployeeAccounts.SingleAsync();
                Assert.Null(account.LastBrowserLaunchAtUtc);
                Assert.Null(account.LastPwaLaunchAtUtc);
                Assert.Null((await clone.WorkflowLogEntries.SingleAsync()).LaunchMode);
                var message = await clone.Set<CommunicationMessageModel>().SingleAsync();
                Assert.Null(message.GifAttachmentJson);
                Assert.Null(message.PhotoAttachmentJson);
                Assert.Null(message.ContentBlocksJson);
                Assert.Equal("Legacy text survives the release", message.Body);
                Assert.Empty(await clone.Database.GetPendingMigrationsAsync());
                Assert.False(clone.Database.HasPendingModelChanges());
                Assert.Equal("ok", await ReadPragmaAsync(cloneWorkspace.ConnectionString, "PRAGMA integrity_check"));
                Assert.Null(await ReadPragmaAsync(cloneWorkspace.ConnectionString, "PRAGMA foreign_key_check"));
            }
            Assert.Equal(sourceHash, await HashAsync(sourcePath));
            Assert.Equal(backupHash, await HashAsync(backup.Path));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task NotepadReleaseSchemaPrefixes_MatchImmutableContract()
    {
        var root = CreateTempRoot();
        try
        {
            await using var db = CreateContext($"Data Source={Path.Combine(root, "SQLite.db")}");
            var migrator = db.GetService<IMigrator>();
            var contracts = new List<object>();
            foreach (var prefix in new[] { AddCommunicationContentBlocks, ExpandManagerNotepad, AddManagerNoteEditSessions })
            {
                await migrator.MigrateAsync(prefix);
                contracts.Add(await ReadReviewedSchemaPrefixAsync(db, prefix, NotepadTables));
            }

            var actual = JsonSerializer.Serialize(contracts, new JsonSerializerOptions { WriteIndented = true, NewLine = "\n" }) + "\n";
            var exportPath = Environment.GetEnvironmentVariable("GF3_EXPORT_NOTEPAD_SCHEMA_PREFIX_CONTRACTS");
            if (!string.IsNullOrWhiteSpace(exportPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(exportPath))!);
                await File.WriteAllTextAsync(exportPath, actual);
                return;
            }

            var expected = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "reviewed-notepad-schema-prefix-contracts.json"));
            Assert.Equal(expected.ReplaceLineEndings("\n").Replace("\\r\\n", "\\n"), actual.Replace("\\r\\n", "\\n"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task NotepadReleaseTransition_PreservesRepresentativeRowsAndExportsSyntheticBaseline()
    {
        var root = CreateTempRoot();
        var sourcePath = Path.Combine(root, "SQLite.db");
        var workspace = new SqliteDatabaseWorkspace(sourcePath);
        var mediaDirectory = Path.Combine(root, "communication-gifs");
        Directory.CreateDirectory(mediaDirectory);
        var gif = Convert.FromBase64String("R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs=");
        var key = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        var referencedMedia = Path.Combine(mediaDirectory, $"{key:N}.gif");
        var unreferencedMedia = Path.Combine(mediaDirectory, "aabbccddeeff00112233445566778899.gif");
        await File.WriteAllBytesAsync(referencedMedia, gif);
        await File.WriteAllBytesAsync(unreferencedMedia, gif);
        try
        {
            string[][] tableColumns;
            string[] oldRows;
            string[] history;
            string[] oldHistory;
            await using (var source = CreateContext(workspace.ConnectionString))
            {
                await source.GetService<IMigrator>().MigrateAsync(AddCommunicationContentBlocks);
                await SeedNotepadReleaseBaseline(source, key, gif.Length);
                oldHistory = (await source.Database.GetAppliedMigrationsAsync()).ToArray();
                history = oldHistory.Append(ExpandManagerNotepad).Append(AddManagerNoteEditSessions).ToArray();
                await source.Database.OpenConnectionAsync();
                var tables = await QueryRows(source, """
                    SELECT name FROM sqlite_schema WHERE type = 'table'
                    AND (name NOT LIKE 'sqlite_%' OR name = 'sqlite_sequence')
                    AND name NOT IN ('__EFMigrationsHistory', '__EFMigrationsLock') ORDER BY name
                    """);
                var projections = new List<string[]>();
                foreach (var table in tables)
                {
                    var name = (string)table[0]!;
                    var columns = await ReadPragmaAsync(source, $"table_xinfo({QuoteIdentifier(name)})");
                    projections.Add([name, .. columns.Select(column => (string)column[1]!)]);
                }
                tableColumns = projections.ToArray();
                oldRows = await ReadNotepadBaselineRows(source, tableColumns, oldHistory.Length);
                Assert.Equal(9001, await source.Database.SqlQueryRaw<int>("SELECT seq AS Value FROM sqlite_sequence WHERE name = 'manager_note'").SingleAsync());
                await source.Database.CloseConnectionAsync();
            }

            var backup = await workspace.CreateAutomaticBackupAsync(default);
            Assert.Equal("0", await ReadPragmaAsync(workspace.ConnectionString, "PRAGMA wal_checkpoint(TRUNCATE)"));
            SqliteConnection.ClearAllPools();
            var sourceHash = await HashAsync(sourcePath);
            var backupHash = await HashAsync(backup.Path);
            var backedUpMedia = Path.Combine(Path.GetDirectoryName(backup.Path)!, "communication-gifs", Path.GetFileName(referencedMedia));
            Assert.Equal(gif, await File.ReadAllBytesAsync(backedUpMedia));
            var paths = Enumerable.Range(0, 3).Select(prefix => Path.Combine(root, $"prefix-{prefix}.db")).ToArray();
            for (var prefix = 0; prefix < paths.Length; prefix++)
            {
                File.Copy(prefix == 0 ? backup.Path : paths[prefix - 1], paths[prefix]);
                var connectionString = $"Data Source={paths[prefix]}";
                await using (var copy = CreateContext(connectionString))
                {
                    if (prefix != 0)
                        await copy.GetService<IMigrator>().MigrateAsync(history[oldHistory.Length + prefix - 1]);
                    Assert.Equal(history.Take(oldHistory.Length + prefix), await copy.Database.GetAppliedMigrationsAsync());
                    await copy.Database.OpenConnectionAsync();
                    Assert.Equal(oldRows, await ReadNotepadBaselineRows(copy, tableColumns, oldHistory.Length));
                    if (prefix >= 1)
                    {
                        foreach (var table in NotepadTables.Skip(1))
                            Assert.Empty(await QueryRows(copy, $"SELECT 1 FROM {QuoteIdentifier(table)}"));
                        Assert.Empty(await QueryRows(copy, """
                            SELECT id FROM manager_note WHERE is_locked IS NOT 0 OR is_pinned IS NOT 0 OR revision IS NOT 1
                            OR visibility IS NOT 'private' OR last_edited_by_manager_id IS NOT NULL OR folder_id IS NOT NULL
                            """));
                    }
                    if (prefix == 2)
                    {
                        Assert.Empty(await QueryRows(copy, """
                            SELECT id FROM manager_note WHERE edit_session_id IS NOT NULL OR edit_manager_id IS NOT NULL
                            OR edit_started_at_ms IS NOT NULL OR edit_expires_at_ms IS NOT NULL
                            """));
                        Assert.Empty(await copy.Database.GetPendingMigrationsAsync());
                        Assert.False(copy.Database.HasPendingModelChanges());
                    }
                    await copy.Database.CloseConnectionAsync();
                }
                Assert.Equal("ok", await ReadPragmaAsync(connectionString, "PRAGMA integrity_check"));
                Assert.Null(await ReadPragmaAsync(connectionString, "PRAGMA foreign_key_check"));
                Assert.Equal("0", await ReadPragmaAsync(connectionString, "PRAGMA wal_checkpoint(TRUNCATE)"));
                SqliteConnection.ClearAllPools();
            }

            var constraintsPath = Path.Combine(root, "constraints.db");
            File.Copy(paths[1], constraintsPath);
            await using (var constraints = CreateContext($"Data Source={constraintsPath};Foreign Keys=True"))
                await AssertNotepadMigrationConstraints(constraints);

            Assert.Equal(sourceHash, await HashAsync(sourcePath));
            Assert.Equal(backupHash, await HashAsync(backup.Path));
            Assert.Equal(gif, await File.ReadAllBytesAsync(referencedMedia));
            Assert.Equal(gif, await File.ReadAllBytesAsync(unreferencedMedia));
            Assert.Equal(gif, await File.ReadAllBytesAsync(backedUpMedia));
            var artifact = Environment.GetEnvironmentVariable("GF3_EXPORT_RELEASE_BASELINE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(artifact))
            {
                var artifactDirectory = Path.Combine(Path.GetFullPath(artifact), "notepad");
                var prefixes = Path.Combine(artifactDirectory, "prefixes");
                Directory.CreateDirectory(prefixes);
                File.Copy(backup.Path, Path.Combine(artifactDirectory, "SQLite.db"), overwrite: true);
                DirectoryCopy(mediaDirectory, Path.Combine(artifactDirectory, "communication-gifs"));
                foreach (var path in paths)
                    File.Copy(path, Path.Combine(prefixes, Path.GetFileName(path)), overwrite: true);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task SeedNotepadReleaseBaseline(AppDbContext db, Guid mediaKey, int mediaSize)
    {
        var now = new DateTimeOffset(2026, 11, 26, 9, 0, 0, TimeSpan.Zero);
        var managers = new[] { "legacy-owner", "legacy-peer" }.Select(name => new ManagerAccountModel
        {
            Username = name, DisplayName = name, PasswordHash = "synthetic-hash", PasswordUpdatedAtUtc = now,
            CreatedAtUtc = now, UpdatedAtUtc = now,
        }).ToArray();
        db.ManagerAccounts.AddRange(managers);
        var (owner, _, schedule, slot) = await EmployeePushNotificationSourceTests.Seed(db);
        db.ShiftSwapRequests.Add(EmployeePushNotificationSourceTests.Offer(owner, schedule, slot));
        db.EmployeeAccounts.Add(new EmployeeAccountModel
        {
            EmployeeId = owner, Username = "legacy-employee", PasswordHash = "synthetic-hash", PasswordUpdatedAtUtc = now,
        });
        db.CommunicationMessages.Add(new CommunicationMessageModel
        {
            Title = "Legacy communication", Body = "Synthetic body", CreatedByManagerId = managers[0].Id,
            CreatedByManagerName = managers[0].DisplayName, CreatedAtUtc = now, DeadlineAtUtc = now.AddDays(1),
            GifAttachmentJson = JsonSerializer.Serialize(new { Key = mediaKey, Size = mediaSize, FileName = "synthetic.gif" }),
        });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO workflow_log_entry (occurred_at_utc, actor_role, actor_employee_id, actor_name, action)
            VALUES ({now}, 'employee', {owner}, 'Synthetic employee', 'Opened schedule');
            INSERT INTO shift_correction_request
                (schedule_id, employee_id, schedule_slot_id, day_of_month, original_from_time,
                 original_to_time, requested_from_time, requested_to_time, status, created_at_utc)
            VALUES ({schedule.Id}, {owner}, {slot.Id}, 20, '08:00', '16:00', '08:00', '16:30', 'Pending', {now});
            INSERT INTO manager_note (id, manager_account_id, title, content, color, created_at_utc, updated_at_utc) VALUES
                (101, {managers[0].Id}, 'Legacy plain', 'Plain text', 'blue', {now}, {now}),
                (102, {managers[1].Id}, 'Legacy rich', '<b>Rich</b><br>Text', 'green', {now}, {now.AddMinutes(1)}),
                (9001, {managers[0].Id}, 'Sequence sentinel', '', 'yellow', {now}, {now});
            DELETE FROM manager_note WHERE id = 9001;
            INSERT INTO manager_notepad_state (manager_account_id, is_expanded, is_pinned, height, updated_at_utc)
            VALUES ({managers[0].Id}, 1, 1, 777, {now}), ({managers[1].Id}, 0, 1, 390, {now});
            """);
    }

    private static async Task<string[]> ReadNotepadBaselineRows(AppDbContext db, string[][] tableColumns, int oldMigrationCount)
    {
        var snapshots = new List<string>();
        foreach (var projection in tableColumns)
        {
            var columns = string.Join(", ", projection.Skip(1).Select(QuoteIdentifier));
            var rows = await QueryRows(db, $"SELECT {columns} FROM {QuoteIdentifier(projection[0])}");
            snapshots.Add(JsonSerializer.Serialize(rows.Select(row => JsonSerializer.Serialize(row)).Order(StringComparer.Ordinal)));
        }
        var history = await QueryRows(db, $"SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId LIMIT {oldMigrationCount}");
        snapshots.Add(JsonSerializer.Serialize(history));
        return snapshots.ToArray();
    }

    private static async Task<List<object?[]>> QueryRows(AppDbContext db, string sql)
    {
        var rows = new List<object?[]>();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values.Select(value => value is DBNull ? null : value).ToArray());
        }
        return rows;
    }

    private static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    private static async Task AssertNotepadMigrationConstraints(AppDbContext db)
    {
        var firstManager = await db.ManagerAccounts.OrderBy(manager => manager.Id).Select(manager => manager.Id).FirstAsync();
        var secondManager = await db.ManagerAccounts.OrderBy(manager => manager.Id).Select(manager => manager.Id).LastAsync();
        const string now = "2026-11-26 09:00:00+00:00";
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO manager_note_folder (id, manager_account_id, name, created_at_utc, updated_at_utc)
            VALUES (1, {firstManager}, 'Work', {now}, {now}), (2, {secondManager}, 'work', {now}, {now});
            INSERT INTO manager_note_tag (id, manager_account_id, name, created_at_utc)
            VALUES (1, {firstManager}, 'Urgent', {now}), (2, {secondManager}, 'urgent', {now});
            UPDATE manager_note SET folder_id = 1 WHERE id = 101;
            INSERT INTO manager_note_share (note_id, manager_account_id) VALUES (101, {secondManager});
            INSERT INTO manager_note_tag_link (note_id, tag_id) VALUES (101, 1);
            INSERT INTO manager_note_version (note_id, revision, title, content, color, saved_at_utc, saved_by_manager_id)
            VALUES (101, 1, 'Old title', 'Old content', 'blue', {now}, {firstManager});
            """);
        foreach (var duplicate in new FormattableString[]
        {
            $"INSERT INTO manager_note_folder (manager_account_id, name, created_at_utc, updated_at_utc) VALUES ({firstManager}, 'work', {now}, {now})",
            $"INSERT INTO manager_note_tag (manager_account_id, name, created_at_utc) VALUES ({firstManager}, 'urgent', {now})",
            $"INSERT INTO manager_note_share (note_id, manager_account_id) VALUES (101, {secondManager})",
            $"INSERT INTO manager_note_tag_link (note_id, tag_id) VALUES (101, 1)",
            $"INSERT INTO manager_note_version (note_id, revision, title, content, color, saved_at_utc, saved_by_manager_id) VALUES (101, 1, 'Old title', 'Old content', 'blue', {now}, {firstManager})",
        })
        {
            var error = await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync(duplicate));
            Assert.Contains(error.SqliteExtendedErrorCode, new[] { 1555, 2067 });
        }
        await db.Database.ExecuteSqlRawAsync("DELETE FROM manager_note_folder WHERE id = 1");
        Assert.Null(await db.Database.SqlQueryRaw<int?>("SELECT folder_id AS Value FROM manager_note WHERE id = 101").SingleAsync());
        await db.Database.ExecuteSqlRawAsync("DELETE FROM manager_note WHERE id = 101");
        Assert.Equal(0, await db.ManagerNoteTagLinks.CountAsync());
        Assert.Equal(0, await db.ManagerNoteShares.CountAsync());
        Assert.Equal(0, await db.ManagerNoteVersions.CountAsync());
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM manager_note WHERE id = 102").SingleAsync());
    }

    private static async Task<object> ReadReviewedSchemaPrefixAsync(AppDbContext db, string migrationId, string[]? selectedTables = null)
    {
        string[] tables = selectedTables ?? ["communication_message", "employee_account", "shift_correction_request", "shift_swap_request", "workflow_log_entry"];
        await db.Database.OpenConnectionAsync();
        try
        {
        var schemaObjects = new List<object?[]>();
        await using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT type, name, tbl_name, sql FROM sqlite_schema WHERE (" +
                string.Join(" OR ", tables.Select((_, i) => $"name = @table{i} OR tbl_name = @table{i}")) +
                ")" + (selectedTables is null ? "" : " AND sql IS NOT NULL") + " ORDER BY type, name";
            for (var i = 0; i < tables.Length; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@table{i}";
                parameter.Value = tables[i];
                command.Parameters.Add(parameter);
            }
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                schemaObjects.Add([reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)]);
        }

        var tableDetails = new List<object>();
        foreach (var table in tables)
        {
            var columns = await ReadPragmaAsync(db, $"table_xinfo(\"{table}\")");
            var foreignKeys = await ReadPragmaAsync(db, $"foreign_key_list(\"{table}\")");
            var indexes = await ReadPragmaAsync(db, $"index_list(\"{table}\")");
            var indexDetails = new List<object>();
            foreach (var index in indexes)
                indexDetails.Add(new { row = index, columns = await ReadPragmaAsync(db, $"index_xinfo(\"{index[1]}\")") });
            tableDetails.Add(new { table, columns, foreignKeys, indexes = indexDetails });
        }
        return new { migrationId, schemaObjects, tables = tableDetails };
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task<List<object?[]>> ReadPragmaAsync(AppDbContext db, string pragma)
    {
        var rows = new List<object?[]>();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"PRAGMA {pragma}";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < row.Length; i++) row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    [Fact]
    public async Task AdditiveMigrations_OnCopy_PreserveRowsAndLeaveSourceUnmigrated()
    {
        const string prior = "20261101190152_AddDayOffShiftCorrectionRequests";
        const string correction = "20261114225324_AddShiftCorrectionComment";
        const string refresh = "20261115135735_RefreshManualShiftOffer";
        const string disjointSwaps = "20261122150944_AllowDisjointOpenSwapIntervals";
        const string launchModes = "20261122151455_TrackEmployeeLaunchModes";
        const string launchLog = "20261122160000_AddWorkflowLogLaunchMode";
        const string removal = "20261122173000_AddShiftRemovalCorrections";
        const string gif = "20261123120000_AddCommunicationGifAttachment";
        const string photo = "20261123130000_AddCommunicationPhotoAttachment";
        const string contentBlocks = "20261123140000_AddCommunicationContentBlocks";
        var root = CreateTempRoot();
        var sourcePath = Path.Combine(root, "SQLite.db");
        var clonePath = Path.Combine(root, "clone.db");
        try
        {
            var workspace = new SqliteDatabaseWorkspace(sourcePath);
            await using (var source = CreateContext(workspace.ConnectionString))
            {
                await source.GetService<IMigrator>().MigrateAsync(prior);
                var (owner, _, schedule, slot) = await EmployeePushNotificationSourceTests.Seed(source);
                var now = DateTimeOffset.UtcNow;
                await source.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO shift_correction_request
                        (schedule_id, employee_id, schedule_slot_id, day_of_month, original_from_time, original_to_time,
                         requested_from_time, requested_to_time, status, created_at_utc)
                    VALUES ({schedule.Id}, {owner}, {slot.Id}, 1, '08:00', '16:00', '08:00', '16:30', 'Approved', {now});
                    INSERT INTO communication_message (title, body, deadline_at_utc, created_at_utc, created_by_manager_name)
                    VALUES ('Fixture message', 'Legacy text', {now.AddDays(1)}, {now}, 'Fixture manager');
                    """);
            }
            var snapshot = await workspace.CreateAutomaticBackupAsync(default);
            Assert.Equal("0", await ReadPragmaAsync(workspace.ConnectionString, "PRAGMA wal_checkpoint(TRUNCATE)"));
            await using var sourceConnection = new SqliteConnection(workspace.ConnectionString);
            SqliteConnection.ClearPool(sourceConnection);
            var sourceHash = await HashAsync(sourcePath);
            var backupHash = await HashAsync(snapshot.Path);
            File.Copy(snapshot.Path, clonePath);
            var cloneWorkspace = new SqliteDatabaseWorkspace(clonePath);
            await using (var clone = CreateContext(cloneWorkspace.ConnectionString))
            {
                var result = await DatabaseMigrationStartup.ApplyAsync(clone, cloneWorkspace, true, true,
                    NullLogger.Instance);
                Assert.Equal(new[] { correction, refresh, disjointSwaps, launchModes, launchLog, removal, gif, photo, contentBlocks, ExpandManagerNotepad, AddManagerNoteEditSessions },
                    result.AppliedMigrations);
                Assert.Empty(await clone.Database.GetPendingMigrationsAsync());
                Assert.Equal(new[] { "Owner", "Recipient" }, await clone.Employees.OrderBy(employee => employee.Id).Select(employee => employee.FirstName).ToArrayAsync());
                var legacy = await clone.ShiftCorrectionRequests.SingleAsync();
                Assert.Equal(ShiftCorrectionOperation.ChangeTimes, legacy.Operation);
                Assert.Equal(("08:00", "16:00", "08:00", "16:30"),
                    (legacy.OriginalFromTime, legacy.OriginalToTime, legacy.RequestedFromTime, legacy.RequestedToTime));
                Assert.Null(legacy.Comment);
                Assert.Null(legacy.RemovalSnapshotJson);
                Assert.Empty(await clone.ShiftSwapRequests.Select(request => request.RefreshedAtUtc).ToListAsync());
                Assert.Empty(await clone.EmployeeAccounts.Select(account => new { account.LastBrowserLaunchAtUtc, account.LastPwaLaunchAtUtc }).ToListAsync());
                Assert.Empty(await clone.Set<WorkflowLogEntryModel>().Select(entry => entry.LaunchMode).ToListAsync());
                var message = await clone.Set<CommunicationMessageModel>().SingleAsync();
                Assert.Equal("Legacy text", message.Body);
                Assert.Null(message.GifAttachmentJson);
                Assert.Null(message.PhotoAttachmentJson);
                Assert.Null(message.ContentBlocksJson);
                Assert.False(clone.Database.HasPendingModelChanges());
                Assert.Equal("ok", await ReadPragmaAsync(cloneWorkspace.ConnectionString, "PRAGMA integrity_check"));
                Assert.Null(await ReadPragmaAsync(cloneWorkspace.ConnectionString, "PRAGMA foreign_key_check"));
            }
            SqliteConnection.ClearAllPools();
            var verificationConnection = new SqliteConnectionStringBuilder
            {
                DataSource = new Uri(clonePath).AbsoluteUri + "?immutable=1",
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString();
            await using (var verification = CreateContext(verificationConnection))
            {
                Assert.Empty(await verification.Database.GetPendingMigrationsAsync());
                Assert.Equal(2, await verification.Employees.CountAsync());
                verification.Employees.Add(new EmployeeModel { FirstName = "Unexpected", LastName = "Write" });
                var error = await Assert.ThrowsAsync<DbUpdateException>(() => verification.SaveChangesAsync());
                Assert.Equal(8, Assert.IsType<SqliteException>(error.InnerException).SqliteErrorCode);
            }
            var immutableSource = new SqliteConnectionStringBuilder
            {
                DataSource = new Uri(sourcePath).AbsoluteUri + "?immutable=1",
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString();
            await using var unchanged = CreateContext(immutableSource);
            Assert.Equal(prior, (await unchanged.Database.GetAppliedMigrationsAsync()).Last());
            Assert.Equal(2, await unchanged.Employees.CountAsync());
            Assert.Equal("ok", await ReadPragmaAsync(immutableSource, "PRAGMA integrity_check"));
            Assert.Equal(sourceHash, await HashAsync(sourcePath));
            Assert.Equal(backupHash, await HashAsync(snapshot.Path));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }

        static async Task<string?> ReadPragmaAsync(string connectionString, string pragma)
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = pragma;
            return (await command.ExecuteScalarAsync())?.ToString();
        }

    }

    [Fact]
    public async Task RemovalMigration_EnforcesSnapshotAndTimesOnAnUpgradedDatabase()
    {
        await using var database = await GF3.Tests.Infrastructure.SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261122160000_AddWorkflowLogLaunchMode");
        var (owner, _, schedule, slot) = await EmployeePushNotificationSourceTests.Seed(db);
        await migrator.MigrateAsync();

        var removal = Draft("[]", null);
        removal.RemovalSnapshotJson = JsonSerializer.Serialize(new[] { new { slot.Id, slot.FromTime, slot.ToTime } });
        db.Add(removal);
        await db.SaveChangesAsync();

        await Reject(Draft(null, null));
        await Reject(Draft(removal.RemovalSnapshotJson, "09:00"));
        var attached = Draft(removal.RemovalSnapshotJson, null);
        attached.ScheduleSlotId = slot.Id;
        await Reject(attached);
        var changedTimes = Draft(removal.RemovalSnapshotJson, "09:00");
        changedTimes.Operation = ShiftCorrectionOperation.ChangeTimes;
        changedTimes.RequestedToTime = "14:00";
        await Reject(changedTimes);

        var persisted = await db.ShiftCorrectionRequests.SingleAsync();
        Assert.Equal(ShiftCorrectionOperation.RemoveShift, persisted.Operation);
        Assert.Equal(removal.RemovalSnapshotJson, persisted.RemovalSnapshotJson);
        Assert.Null(persisted.RequestedFromTime);
        Assert.Null(persisted.RequestedToTime);

        ShiftCorrectionRequestModel Draft(string? snapshot, string? start) => new()
        {
            ScheduleId = schedule.Id, EmployeeId = owner, DayOfMonth = 1,
            Operation = ShiftCorrectionOperation.RemoveShift, Status = ShiftCorrectionStatus.Approved,
            RequestedFromTime = start, RemovalSnapshotJson = snapshot, CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        async Task Reject(ShiftCorrectionRequestModel request)
        {
            db.Add(request);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(275, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
            db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task DayOffMigration_PreservesHistoryAndEnforcesBothPendingIndexesAndSnapshotChecks()
    {
        await using var database = await GF3.Tests.Infrastructure.SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        await db.GetService<IMigrator>().MigrateAsync("20261031083410_AddUrlop");
        var (employee, _, schedule, slot) = await EmployeePushNotificationSourceTests.Seed(db);
        foreach (var status in new[] { ShiftCorrectionStatus.Pending, ShiftCorrectionStatus.Approved, ShiftCorrectionStatus.Rejected })
        {
            var legacy = Correction(status);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO shift_correction_request
                    (schedule_id, employee_id, schedule_slot_id, day_of_month, original_from_time, original_to_time,
                     requested_from_time, requested_to_time, status, created_at_utc)
                VALUES ({legacy.ScheduleId}, {legacy.EmployeeId}, {legacy.ScheduleSlotId}, {legacy.DayOfMonth},
                        {legacy.OriginalFromTime}, {legacy.OriginalToTime}, {legacy.RequestedFromTime},
                        {legacy.RequestedToTime}, {legacy.Status.ToString()}, {legacy.CreatedAtUtc});
                """);
        }
        db.ChangeTracker.Clear();
        var workspace = new SqliteDatabaseWorkspace(database.DatabasePath);
        var result = await DatabaseMigrationStartup.ApplyAsync(db, workspace, true, true, NullLogger.Instance);
        Assert.Contains("20261101190152_AddDayOffShiftCorrectionRequests", result.AppliedMigrations);
        Assert.NotNull(result.BackupPath); Assert.True(File.Exists(result.BackupPath));
        await using (var backup = CreateContext($"Data Source={result.BackupPath};Mode=ReadOnly"))
        {
            Assert.Equal(3, await backup.ShiftCorrectionRequests.CountAsync());
            Assert.DoesNotContain("20261101190152_AddDayOffShiftCorrectionRequests", await backup.Database.GetAppliedMigrationsAsync());
        }
        var copyPath = Path.Combine(database.RootPath, "older-copy.db");
        File.Copy(result.BackupPath!, copyPath);
        await using (var copy = CreateContext($"Data Source={copyPath}"))
        {
            var copied = await DatabaseMigrationStartup.ApplyAsync(copy, new SqliteDatabaseWorkspace(copyPath), true, true, NullLogger.Instance);
            Assert.NotNull(copied.BackupPath);
            Assert.Equal(3, await copy.ShiftCorrectionRequests.CountAsync());
            Assert.Empty(await copy.Database.GetPendingMigrationsAsync());
        }
        var history = await db.ShiftCorrectionRequests.AsNoTracking().OrderBy(item => item.Id).ToListAsync();
        Assert.Equal(3, history.Count);
        Assert.All(history, item => { Assert.Equal("08:00", item.OriginalFromTime); Assert.Equal("16:00", item.OriginalToTime); Assert.Equal(slot.Id, item.ScheduleSlotId); });
        await db.GetService<IMigrator>().MigrateAsync("20261031083410_AddUrlop");
        Assert.Equal(3, await db.ShiftCorrectionRequests.CountAsync());
        await db.GetService<IMigrator>().MigrateAsync();
        Assert.Equal(history.Select(item => (item.Id, item.Status, item.OriginalFromTime, item.OriginalToTime)),
            (await db.ShiftCorrectionRequests.AsNoTracking().OrderBy(item => item.Id).ToListAsync())
                .Select(item => (item.Id, item.Status, item.OriginalFromTime, item.OriginalToTime)));
        Assert.False(db.Database.HasPendingModelChanges());
        async Task RejectInsert(ShiftCorrectionRequestModel request)
        {
            db.Add(request); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
        }
        await RejectInsert(Correction(ShiftCorrectionStatus.Pending));
        var dayOff = Correction(ShiftCorrectionStatus.Pending);
        dayOff.DayOfMonth = 2; dayOff.ScheduleSlotId = null; dayOff.OriginalFromTime = null; dayOff.OriginalToTime = null;
        db.Add(dayOff); await db.SaveChangesAsync();
        var duplicate = Correction(ShiftCorrectionStatus.Pending);
        duplicate.DayOfMonth = 2; duplicate.ScheduleSlotId = null; duplicate.OriginalFromTime = null; duplicate.OriginalToTime = null;
        await RejectInsert(duplicate);
        foreach (var scenario in new[] { "mixed-from", "mixed-to", "bad-original", "null-slot", "order", "bad-request" })
        {
            var invalid = Correction(ShiftCorrectionStatus.Rejected);
            if (scenario == "mixed-from") invalid.OriginalFromTime = null;
            if (scenario == "mixed-to") invalid.OriginalToTime = null;
            if (scenario == "bad-original") invalid.OriginalFromTime = "25:00";
            if (scenario == "null-slot") invalid.ScheduleSlotId = null;
            if (scenario == "order") invalid.RequestedFromTime = invalid.RequestedToTime;
            if (scenario == "bad-request") invalid.RequestedFromTime = "ab:cd";
            await RejectInsert(invalid);
        }
        Assert.Equal(4, await db.ShiftCorrectionRequests.CountAsync());
        ShiftCorrectionRequestModel Correction(ShiftCorrectionStatus status) => new() {
            ScheduleId = schedule.Id, EmployeeId = employee, ScheduleSlotId = slot.Id, DayOfMonth = 1,
            OriginalFromTime = "08:00", OriginalToTime = "16:00", RequestedFromTime = "08:00", RequestedToTime = "16:30",
            Status = status, CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    [Theory]
    [InlineData(ShiftCorrectionStatus.Pending)]
    [InlineData(ShiftCorrectionStatus.Approved)]
    [InlineData(ShiftCorrectionStatus.Rejected)]
    public async Task DayOffMigration_RollbackRefusesEveryDayOffStatusWithoutLosingHistory(ShiftCorrectionStatus status)
    {
        await using var database = await GF3.Tests.Infrastructure.SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var (employee, _, schedule, slot) = await EmployeePushNotificationSourceTests.Seed(db);
        var request = new ShiftCorrectionRequestModel {
            ScheduleId = schedule.Id, EmployeeId = employee, DayOfMonth = 2,
            ScheduleSlotId = status == ShiftCorrectionStatus.Approved ? slot.Id : null,
            RequestedFromTime = "08:00", RequestedToTime = "16:00", Status = status, CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Add(request); await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<SqliteException>(() => db.GetService<IMigrator>().MigrateAsync("20261031083410_AddUrlop"));
        Assert.Contains("Rollback blocked", error.Message);
        db.ChangeTracker.Clear();
        var preserved = await db.ShiftCorrectionRequests
            .Select(item => new { item.Status, item.OriginalFromTime, item.OriginalToTime })
            .SingleAsync();
        Assert.Equal(status, preserved.Status); Assert.Null(preserved.OriginalFromTime); Assert.Null(preserved.OriginalToTime);
        Assert.Contains("20261101190152_AddDayOffShiftCorrectionRequests", await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task AddUrlop_RollbackAndReapplyPreserveExistingAvailabilityAndSchedule()
    {
        await using var database = await GF3.Tests.Infrastructure.SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var fixture = await UrlopTestData.SeedAsync(db);
        db.ScheduleSlots.Add(GF3.Tests.Infrastructure.TestDataFactory.CreateDalSlot(fixture.Schedule, 3, 1, fixture.Employee, "09:00", "17:00"));
        await db.SaveChangesAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261029193000_ReservePendingClaimOffer");
        Assert.Single(await db.ScheduleSlots.ToListAsync());
        Assert.Single(await db.AvailabilityGroupDays.ToListAsync());
        await migrator.MigrateAsync();
        Assert.Empty(await db.UrlopDays.ToListAsync());
        Assert.Empty(await db.UrlopMonths.ToListAsync());
        Assert.Single(await db.ScheduleSlots.ToListAsync());
        Assert.Single(await db.AvailabilityGroupDays.ToListAsync());
        await UrlopTestData.Service(db).CreateApprovedAsync(fixture.Manager, fixture.Shop, fixture.Employee, 2026, 11, [4], default);
        db.UrlopDays.Add(new() { RequestId = Guid.NewGuid(), ShopId = fixture.Shop, EmployeeId = fixture.Employee,
            Date = new(2026, 11, 4), Status = "pending", CreatedAtUtc = new UrlopTestClock().GetUtcNow() });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.UrlopDays.Add(new() { RequestId = Guid.NewGuid(), ShopId = fixture.Shop, EmployeeId = fixture.Employee,
            Date = new(2026, 11, 4), Status = "rejected", CreatedAtUtc = new UrlopTestClock().GetUtcNow() });
        await db.SaveChangesAsync();
        Assert.Equal(2, await db.UrlopDays.CountAsync());
    }
    private const string PreviousProductionMigration = "20260509101500_AddManagerRecoveryFields";

    [Fact]
    public async Task ApplyAsync_RequireExistingDatabase_RefusesMissingFile()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "SQLite.db");
        var workspace = new SqliteDatabaseWorkspace(databasePath);

        try
        {
            await using var context = CreateContext(workspace.ConnectionString);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                DatabaseMigrationStartup.ApplyAsync(
                    context,
                    workspace,
                    requireExistingDatabase: true,
                    backupBeforeMigrate: true,
                    NullLogger.Instance));

            Assert.Contains("GF3_DATA_VOLUME", exception.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(databasePath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyAsync_ExistingProductionSchema_BacksUpMigratesAndPreservesData()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "SQLite.db");
        var workspace = new SqliteDatabaseWorkspace(databasePath);

        try
        {
            await using (var oldContext = CreateContext(workspace.ConnectionString))
            {
                var migrator = oldContext.GetService<IMigrator>();
                await migrator.MigrateAsync(PreviousProductionMigration);
                await SeedExistingProductionDataAsync(oldContext);
            }

            await using (var migrationContext = CreateContext(workspace.ConnectionString))
            {
                var result = await DatabaseMigrationStartup.ApplyAsync(
                    migrationContext,
                    workspace,
                    requireExistingDatabase: true,
                    backupBeforeMigrate: true,
                    NullLogger.Instance);

                Assert.True(result.DatabaseExisted);
                Assert.Contains("20260720120000_AddEmployeeSessionVersion", result.AppliedMigrations);
                Assert.NotNull(result.BackupPath);
                Assert.True(File.Exists(result.BackupPath));
            }

            await using var verificationContext = CreateContext(workspace.ConnectionString);
            Assert.Empty(await verificationContext.EmployeePushSubscriptions.ToListAsync());
            Assert.Empty(await verificationContext.EmployeePushDeliveries.ToListAsync());
            Assert.Equal(2, await verificationContext.Employees.CountAsync());
            var employees = await verificationContext.Employees.OrderBy(employee => employee.FirstName).ToListAsync();
            Assert.Equal(new[] { "Alice", "Bob" }, employees.Select(employee => employee.FirstName));
            Assert.All(employees, employee =>
            {
                Assert.Equal("Existing", employee.LastName);
                Assert.False(employee.IsDeleted);
            });
            var migratedSchedule = Assert.Single(await verificationContext.Schedules.ToListAsync());
            Assert.True(migratedSchedule.AllowSwap);
            Assert.Single(await verificationContext.ScheduleSlots.ToListAsync());
            Assert.Single(await verificationContext.ShiftSwapRequests.ToListAsync());
            var importedVersion = Assert.Single(await verificationContext.ScheduleVersions.ToListAsync());
            Assert.Equal(1, importedVersion.VersionNumber);
            Assert.Equal("main", importedVersion.BranchName);
            Assert.Equal("Production import", importedVersion.CreatedByManagerName);
            Assert.Contains("Release schedule", importedVersion.SnapshotJson, StringComparison.Ordinal);
            using var importedSnapshot = JsonDocument.Parse(importedVersion.SnapshotJson);
            Assert.Equal(JsonValueKind.String, importedSnapshot.RootElement.GetProperty("publicationStatus").ValueKind);
            Assert.Equal(JsonValueKind.True, importedSnapshot.RootElement.GetProperty("allowSwap").ValueKind);
            Assert.Single(importedSnapshot.RootElement.GetProperty("slots").EnumerateArray());
            Assert.Equal(
                JsonValueKind.String,
                importedSnapshot.RootElement.GetProperty("slots")[0].GetProperty("status").ValueKind);
            Assert.Equal(importedVersion.Id, Assert.Single(await verificationContext.ScheduleVersionStates.ToListAsync()).CurrentVersionId);

            var history = Assert.Single(await verificationContext.ShiftSwapHistories.ToListAsync());
            Assert.Equal("Release schedule", history.ScheduleName);
            Assert.Equal("Alice Existing", history.FromEmployeeName);
            Assert.Equal("Bob Existing", history.AcceptedByEmployeeName);
            Assert.Empty(await verificationContext.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    private static AppDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<string?> ReadPragmaAsync(string connectionString, string pragma)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = pragma;
        return (await command.ExecuteScalarAsync())?.ToString();
    }

    private static async Task SeedExistingProductionDataAsync(AppDbContext context)
    {
        var container = new ContainerModel { Name = "Release container" };
        var shop = new ShopModel { Name = "Release shop", Address = "Production address" };
        context.AddRange(container, shop);
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO employee (first_name, last_name) VALUES ({"Alice"}, {"Existing"});
            INSERT INTO employee (first_name, last_name) VALUES ({"Bob"}, {"Existing"});
            """);
        var aliceId = await context.Database
            .SqlQueryRaw<int>("SELECT id AS Value FROM employee WHERE first_name = 'Alice' AND last_name = 'Existing'")
            .SingleAsync();
        var bobId = await context.Database
            .SqlQueryRaw<int>("SELECT id AS Value FROM employee WHERE first_name = 'Bob' AND last_name = 'Existing'")
            .SingleAsync();

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO schedule
                (container_id, shop_id, name, year, month, publication_status, people_per_shift,
                 shift1_time, shift2_time, max_hours_per_emp_month, max_consecutive_days,
                 max_consecutive_full, max_full_per_month)
            VALUES
                ({container.Id}, {shop.Id}, {"Release schedule"}, {2026}, {7}, {SchedulePublicationStatus.Public.ToString()}, {1},
                 {"09:00 - 17:00"}, {"17:00 - 22:00"}, {200}, {6}, {3}, {10});
            """);
        var scheduleId = checked((int)await context.Database
            .SqlQueryRaw<long>("SELECT last_insert_rowid() AS Value")
            .SingleAsync());

        var slot = new ScheduleSlotModel
        {
            ScheduleId = scheduleId,
            DayOfMonth = 5,
            SlotNo = 1,
            EmployeeId = aliceId,
            Status = SlotStatus.ASSIGNED,
            FromTime = "09:00",
            ToTime = "17:00",
        };
        context.ScheduleSlots.Add(slot);
        await context.SaveChangesAsync();

        // Seed the historical schema without columns introduced by later migrations.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO shift_swap_request
                (schedule_id, schedule_slot_id, from_employee_id, accepted_by_employee_id,
                 visibility, status, created_at_utc, accepted_at_utc)
            VALUES ({scheduleId}, {slot.Id}, {aliceId}, {bobId}, {"Public"}, {"Accepted"},
                {new DateTimeOffset(2026, 6, 25, 8, 0, 0, TimeSpan.Zero)},
                {new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.Zero)});
            """);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static async Task<byte[]> HashAsync(string path)
    {
        await using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return await SHA256.HashDataAsync(file);
    }

    private static void DirectoryCopy(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
    }
}
