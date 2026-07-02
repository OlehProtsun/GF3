using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Database;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer.Services;

/// <summary>
/// Provides a guarded "database administration" surface for the internal admin tool.
/// The service intentionally allows only a narrow subset of SQL and keeps all parsing,
/// validation, metadata loading, and import bookkeeping in one place so the API layer
/// can stay thin and policy-free.
/// </summary>
public sealed class AdminDbService : IAdminDbService
{
    private static readonly string[] ReadPrefixes = ["SELECT", "PRAGMA", "WITH"];
    private static readonly string[] WritePrefixes = ["INSERT", "UPDATE", "DELETE"];
    private static readonly string[] BannedTokens = ["DROP", "ALTER", "CREATE", "ATTACH", "DETACH", ".LOAD", "VACUUM", "REINDEX"];

    private readonly AppDbContext _dbContext;
    private readonly ISqliteAdminFacade _sqliteAdminFacade;
    private readonly ISqliteDatabaseWorkspace _databaseWorkspace;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly string? _localApplicationDataRoot;

    public AdminDbService(
        AppDbContext dbContext,
        ISqliteAdminFacade sqliteAdminFacade,
        ISqliteDatabaseWorkspace databaseWorkspace,
        IServiceScopeFactory serviceScopeFactory,
        string? localApplicationDataRoot = null)
    {
        _dbContext = dbContext;
        _sqliteAdminFacade = sqliteAdminFacade;
        _databaseWorkspace = databaseWorkspace;
        _serviceScopeFactory = serviceScopeFactory;
        _localApplicationDataRoot = localApplicationDataRoot;
    }

    public async Task<AdminDbMetadataDto> GetMetadataAsync(CancellationToken ct = default)
    {
        var connection = await GetOpenConnectionAsync(ct).ConfigureAwait(false);
        var databaseInfo = await _sqliteAdminFacade.GetDatabaseInfoAsync(ct).ConfigureAwait(false);
        var sqliteVersion = await ExecuteScalarAsync(connection, "SELECT sqlite_version();", ct).ConfigureAwait(false) ?? string.Empty;
        var objects = await LoadDatabaseObjectsAsync(connection, ct).ConfigureAwait(false);
        var workspaceState = _databaseWorkspace.GetWorkspaceState();

        return new AdminDbMetadataDto
        {
            SqliteVersion = sqliteVersion,
            DatabasePath = databaseInfo.DatabasePath,
            FileSizeBytes = databaseInfo.FileSizeBytes,
            LastModifiedUtc = databaseInfo.LastModifiedUtc == DateTime.MinValue ? null : databaseInfo.LastModifiedUtc,
            UserVersion = databaseInfo.UserVersion,
            Tables = databaseInfo.Tables,
            Objects = objects,
            StorageWorkspace = MapWorkspaceState(workspaceState),
        };
    }

    public async Task<string> GetDbHashAsync(CancellationToken ct = default)
    {
        var path = _sqliteAdminFacade.DatabasePath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            return await _sqliteAdminFacade.ComputeFileHashAsync(path, ct).ConfigureAwait(false);
        }

        var connection = await GetOpenConnectionAsync(ct).ConfigureAwait(false);
        var schema = await ExecuteScalarAsync(
            connection,
            "SELECT COALESCE(group_concat(COALESCE(sql,''), ';'), '') FROM sqlite_master WHERE type IN ('table','index','view') ORDER BY name;",
            ct).ConfigureAwait(false) ?? string.Empty;

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(schema));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<AdminDbQueryResultDto> ExecuteQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default)
    {
        ValidateSql(sql, maxSqlLength, allowWrite: false);

        var connection = await GetOpenConnectionAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var rows = await ReadRowsAsync(reader, ct).ConfigureAwait(false);

        return new AdminDbQueryResultDto
        {
            Columns = columns,
            Rows = rows,
            RowCount = rows.Count
        };
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default)
    {
        ValidateSql(sql, maxSqlLength, allowWrite: true);

        var connection = await GetOpenConnectionAsync(ct).ConfigureAwait(false);
        await using var tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = tx;

        var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
        return affected;
    }

    public async Task<AdminDbImportResultDto> ImportSqlAsync(byte[] fileBytes, int maxImportBytes, CancellationToken ct = default)
    {
        ValidateImportFile(fileBytes, maxImportBytes);

        var script = Encoding.UTF8.GetString(fileBytes);
        var (statements, serviceStatementsSkipped) = ParseImportStatements(script);
        if (statements.Length == 0)
        {
            throw new ValidationException("SQL import does not contain executable write statements.");
        }

        var connection = await GetOpenConnectionAsync(ct).ConfigureAwait(false);
        await using var tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        var progress = new ImportProgress();
        for (var i = 0; i < statements.Length; i++)
        {
            var statement = statements[i];

            try
            {
                ValidateSql(statement, statement.Length, allowWrite: true);
                var affected = await ExecuteImportStatementAsync(connection, tx, statement, ct).ConfigureAwait(false);
                progress.RegisterExecution(statement, affected);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return progress.ToFailedResult(serviceStatementsSkipped, i, ex.Message);
            }
        }

        await tx.CommitAsync(ct).ConfigureAwait(false);
        return progress.ToSucceededResult(serviceStatementsSkipped);
    }

    public async Task<AdminDbFileEntryDto> CreateManualCopyAsync(CancellationToken ct = default)
    {
        var fileEntry = await _databaseWorkspace.CreateManualCopyAsync(ct).ConfigureAwait(false);
        return MapFileEntry(fileEntry);
    }

    public async Task<AdminDbFileEntryDto> SelectDatabaseAsync(string databasePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ValidationException("Database path is required.");
        }

        var normalizedPath = Path.GetFullPath(databasePath.Trim());
        if (!File.Exists(normalizedPath))
        {
            throw new ValidationException("Selected database file does not exist.");
        }

        var previousPath = _databaseWorkspace.DatabasePath;
        _databaseWorkspace.SetDatabasePath(normalizedPath);

        try
        {
            await EnsureSelectedDatabaseIsReadyAsync(ct).ConfigureAwait(false);
            SqliteDatabaseSelectionStore.Save(normalizedPath, _localApplicationDataRoot);
        }
        catch
        {
            _databaseWorkspace.SetDatabasePath(previousPath);
            throw;
        }

        var workspaceState = _databaseWorkspace.GetWorkspaceState();
        var selectedFile = workspaceState.AvailableDatabases.FirstOrDefault(entry =>
            string.Equals(entry.Path, normalizedPath, StringComparison.OrdinalIgnoreCase));

        return selectedFile is not null
            ? MapFileEntry(selectedFile)
            : new AdminDbFileEntryDto
            {
                Name = Path.GetFileName(normalizedPath),
                Path = normalizedPath,
                Category = "database",
                FileSizeBytes = new FileInfo(normalizedPath).Length,
                LastModifiedUtc = File.GetLastWriteTimeUtc(normalizedPath),
                IsActive = true,
            };
    }

    private async Task<DbConnection> GetOpenConnectionAsync(CancellationToken ct)
    {
        // The DbContext owns the connection lifetime; this service only ensures it is open
        // before issuing raw commands. We do not dispose it here because EF still owns it.
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
        }

        return connection;
    }

    private async Task EnsureSelectedDatabaseIsReadyAsync(CancellationToken ct)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(ct).ConfigureAwait(false);
    }

    private static void ValidateImportFile(byte[] fileBytes, int maxImportBytes)
    {
        if (fileBytes.Length == 0)
        {
            throw new ValidationException("SQL import file is empty.");
        }

        if (fileBytes.Length > maxImportBytes)
        {
            throw new ValidationException($"SQL import exceeds max size of {maxImportBytes} bytes.");
        }
    }

    private static async Task<List<IReadOnlyList<object?>>> ReadRowsAsync(DbDataReader reader, CancellationToken ct)
    {
        var rows = new List<IReadOnlyList<object?>>();

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var row = new object[reader.FieldCount];
            reader.GetValues(row);
            rows.Add(row.Cast<object?>().ToArray());
        }

        return rows;
    }

    private static async Task<int> ExecuteImportStatementAsync(
        DbConnection connection,
        DbTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task<List<AdminDbObjectDto>> LoadDatabaseObjectsAsync(DbConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT type, name, COALESCE(sql, '') FROM sqlite_master WHERE type IN ('table','index','view') ORDER BY type, name;";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

        var items = new List<AdminDbObjectDto>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            items.Add(new AdminDbObjectDto
            {
                Type = reader.GetString(0),
                Name = reader.GetString(1),
                Sql = reader.GetString(2)
            });
        }

        return items;
    }

    private static (string[] Statements, int ServiceStatementsSkipped) ParseImportStatements(string script)
    {
        var statements = new List<string>();
        var current = new StringBuilder(script.Length);
        var serviceStatementsSkipped = 0;
        var inString = false;

        for (var i = 0; i < script.Length; i++)
        {
            var ch = script[i];

            if (!inString && ch == '-' && i + 1 < script.Length && script[i + 1] == '-')
            {
                serviceStatementsSkipped++;
                i += 2;

                while (i < script.Length && script[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            if (!inString && ch == '/' && i + 1 < script.Length && script[i + 1] == '*')
            {
                serviceStatementsSkipped++;
                i += 2;

                while (i + 1 < script.Length && !(script[i] == '*' && script[i + 1] == '/'))
                {
                    i++;
                }

                i = Math.Min(i + 1, script.Length - 1);
                continue;
            }

            current.Append(ch);

            if (ch == '\'')
            {
                if (inString && i + 1 < script.Length && script[i + 1] == '\'')
                {
                    current.Append(script[i + 1]);
                    i++;
                    continue;
                }

                inString = !inString;
                continue;
            }

            if (!inString && ch == ';')
            {
                FinalizeStatement(current, statements, ref serviceStatementsSkipped);
            }
        }

        FinalizeStatement(current, statements, ref serviceStatementsSkipped);

        return (statements.ToArray(), serviceStatementsSkipped);
    }

    private static void FinalizeStatement(StringBuilder current, List<string> statements, ref int serviceStatementsSkipped)
    {
        var raw = current.ToString().Trim();
        current.Clear();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var statement = raw.TrimEnd(';').Trim();
        if (string.IsNullOrWhiteSpace(statement))
        {
            return;
        }

        var normalized = NormalizeSqlForClassification(statement);
        if (normalized is "BEGIN" or "BEGIN TRANSACTION" or "COMMIT" or "END" or "END TRANSACTION" or "ROLLBACK")
        {
            serviceStatementsSkipped++;
            return;
        }

        statements.Add(statement);
    }

    private static string NormalizeSqlForClassification(string sql)
    {
        var builder = new StringBuilder(sql.Length);
        var previousWasWhitespace = false;

        foreach (var ch in sql)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (previousWasWhitespace)
                {
                    continue;
                }

                builder.Append(' ');
                previousWasWhitespace = true;
                continue;
            }

            builder.Append(char.ToUpperInvariant(ch));
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private static bool IsIdempotentInsert(string sql)
    {
        var normalized = NormalizeSqlForClassification(sql);
        return normalized.StartsWith("INSERT", StringComparison.Ordinal)
            && (normalized.Contains("OR IGNORE", StringComparison.Ordinal) || normalized.Contains("WHERE NOT EXISTS", StringComparison.Ordinal));
    }

    /// <summary>
    /// Validation is intentionally conservative. The admin tool is powerful, but it is still
    /// part of the application surface, so we deny comments, schema-changing commands, and
    /// everything outside the explicitly allowed statement families.
    /// </summary>
    private static void ValidateSql(string sql, int maxSqlLength, bool allowWrite)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ValidationException("SQL is required.");
        }

        if (sql.Length > maxSqlLength)
        {
            throw new ValidationException($"SQL exceeds max length of {maxSqlLength} characters.");
        }

        var normalized = sql.Trim();
        var validationSql = StripSqlStringLiterals(normalized);
        if (validationSql.Contains("--") || validationSql.Contains("/*", StringComparison.Ordinal))
        {
            throw new ValidationException("SQL comments are not allowed.");
        }

        foreach (var banned in BannedTokens)
        {
            if (validationSql.Contains(banned, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"SQL token '{banned}' is not allowed.");
            }
        }

        if (allowWrite)
        {
            if (!WritePrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationException("Only INSERT, UPDATE, DELETE statements are allowed for write operations.");
            }

            return;
        }

        if (!ReadPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ValidationException("Only SELECT, PRAGMA, WITH statements are allowed.");
        }
    }

    private static string StripSqlStringLiterals(string sql)
    {
        var builder = new StringBuilder(sql.Length);
        var inString = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var ch = sql[i];
            if (ch == '\'')
            {
                if (inString && i + 1 < sql.Length && sql[i + 1] == '\'')
                {
                    builder.Append("  ");
                    i++;
                    continue;
                }

                builder.Append(ch);
                inString = !inString;
                continue;
            }

            builder.Append(inString ? ' ' : ch);
        }

        return builder.ToString();
    }

    private static async Task<string?> ExecuteScalarAsync(DbConnection connection, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return Convert.ToString(value);
    }

    private static AdminDbStorageWorkspaceDto MapWorkspaceState(SqliteDatabaseWorkspaceState workspaceState)
        => new()
        {
            WorkspaceRootPath = workspaceState.WorkspaceRootPath,
            AutomaticBackupDirectoryPath = workspaceState.AutomaticBackupDirectoryPath,
            ManualCopyDirectoryPath = workspaceState.ManualCopyDirectoryPath,
            AutomaticBackupRetentionLimit = workspaceState.AutomaticBackupRetentionLimit,
            AvailableDatabases = workspaceState.AvailableDatabases.Select(MapFileEntry).ToArray(),
            AutomaticBackups = workspaceState.AutomaticBackups.Select(MapFileEntry).ToArray(),
            ManualCopies = workspaceState.ManualCopies.Select(MapFileEntry).ToArray(),
        };

    private static AdminDbFileEntryDto MapFileEntry(SqliteDatabaseFileEntry entry)
        => new()
        {
            Name = entry.Name,
            Path = entry.Path,
            Category = entry.Category,
            FileSizeBytes = entry.FileSizeBytes,
            LastModifiedUtc = entry.LastModifiedUtc,
            IsActive = entry.IsActive,
        };

    private sealed class ImportProgress
    {
        public int StatementsExecuted { get; private set; }
        public int StatementsApplied { get; private set; }
        public int StatementsAlreadyExisted { get; private set; }

        public void RegisterExecution(string sql, int affectedRows)
        {
            StatementsExecuted++;

            if (affectedRows > 0)
            {
                StatementsApplied++;
            }
            else if (IsIdempotentInsert(sql))
            {
                StatementsAlreadyExisted++;
            }
        }

        public AdminDbImportResultDto ToSucceededResult(int serviceStatementsSkipped)
            => new()
            {
                StatementsExecuted = StatementsExecuted,
                StatementsApplied = StatementsApplied,
                StatementsAlreadyExisted = StatementsAlreadyExisted,
                ServiceStatementsSkipped = serviceStatementsSkipped
            };

        public AdminDbImportResultDto ToFailedResult(int serviceStatementsSkipped, int failedStatementIndex, string failureReason)
            => new()
            {
                StatementsExecuted = StatementsExecuted,
                StatementsApplied = StatementsApplied,
                StatementsAlreadyExisted = StatementsAlreadyExisted,
                ServiceStatementsSkipped = serviceStatementsSkipped,
                FailedStatementIndex = failedStatementIndex,
                FailureReason = failureReason
            };
    }
}
