using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace DataAccessLayer.Administration;

/// <summary>
/// Result returned after executing an ad-hoc SQL command through the admin tools.
/// </summary>
public sealed class SqlExecutionResult
{
    public bool IsSelect { get; init; }
    public DataTable? ResultTable { get; init; }
    public int AffectedRows { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Lightweight database metadata shown in the admin tools.
/// </summary>
public sealed class DatabaseInfo
{
    public string DatabasePath { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime LastModifiedUtc { get; init; }
    public int UserVersion { get; init; }
    public IReadOnlyList<string> Tables { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Admin-oriented helper for executing SQL against the SQLite database and retrieving metadata.
/// </summary>
public interface ISqliteAdminService
{
    string DatabasePath { get; }
    Task<SqlExecutionResult> ExecuteSqlAsync(string sql, CancellationToken ct);
    Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct);
    Task<DatabaseInfo> GetDatabaseInfoAsync(CancellationToken ct);
    Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct);
}

/// <summary>
/// SQLite-specific implementation of the admin helper.
/// The service is intentionally low-level and deterministic because it is used by diagnostics,
/// import/export tooling, and tests that need predictable database-side behavior.
/// </summary>
public sealed class SqliteAdminService : ISqliteAdminService
{
    private readonly string _connectionString;
    private readonly string _databasePath;

    public SqliteAdminService(string connectionString, string databasePath)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
    }

    public string DatabasePath => _databasePath;

    /// <summary>
    /// Executes a single SQL command.
    /// Statements that behave like queries return a populated <see cref="DataTable"/>; all other
    /// statements return the affected row count.
    /// </summary>
    public async Task<SqlExecutionResult> ExecuteSqlAsync(string sql, CancellationToken ct)
    {
        ValidateNonEmptySql(sql, "SQL command is empty.");

        await using var connection = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var command = CreateCommand(connection, sql);

        if (IsSelectLikeStatement(sql))
        {
            return await ExecuteQueryAsync(command, ct).ConfigureAwait(false);
        }

        return await ExecuteNonQueryAsync(command, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a multi-statement SQL script used for import/restore scenarios.
    /// </summary>
    public async Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct)
    {
        ValidateNonEmptySql(sqlScript, "Import script is empty.");

        await using var connection = await OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var command = CreateCommand(connection, sqlScript);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns file-level and schema-level metadata about the current SQLite database.
    /// </summary>
    public async Task<DatabaseInfo> GetDatabaseInfoAsync(CancellationToken ct)
    {
        var fileInfo = new FileInfo(_databasePath);

        await using var connection = await OpenConnectionAsync(ct).ConfigureAwait(false);
        var tables = await GetUserTableNamesAsync(connection, ct).ConfigureAwait(false);
        var userVersion = await GetUserVersionAsync(connection, ct).ConfigureAwait(false);

        return new DatabaseInfo
        {
            DatabasePath = _databasePath,
            FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            LastModifiedUtc = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue,
            UserVersion = userVersion,
            Tables = tables,
        };
    }

    /// <summary>
    /// Computes the SHA-256 hash of an arbitrary file while allowing concurrent read/write access.
    /// This is useful for change detection around import/export scenarios.
    /// </summary>
    public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
    {
        await using var stream = new FileStream(
            filePath,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            });

        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        var builder = new StringBuilder(hash.Length * 2);

        foreach (var hashByte in hash)
        {
            builder.Append(hashByte.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }

    private static SqliteCommand CreateCommand(SqliteConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private static bool IsSelectLikeStatement(string sql)
    {
        var normalized = sql.TrimStart();
        return normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<SqlExecutionResult> ExecuteQueryAsync(SqliteCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var table = new DataTable();
        table.Load(reader);

        return new SqlExecutionResult
        {
            IsSelect = true,
            ResultTable = table,
            Message = $"Query completed. Rows: {table.Rows.Count}.",
        };
    }

    private static async Task<SqlExecutionResult> ExecuteNonQueryAsync(SqliteCommand command, CancellationToken ct)
    {
        var affectedRows = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return new SqlExecutionResult
        {
            IsSelect = false,
            AffectedRows = affectedRows,
            Message = $"Command executed successfully. Affected rows: {affectedRows}.",
        };
    }

    private static async Task<IReadOnlyList<string>> GetUserTableNamesAsync(SqliteConnection connection, CancellationToken ct)
    {
        var tables = new List<string>();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name;
            """;

        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task<int> GetUserVersionAsync(SqliteConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";

        var result = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (result is null or DBNull)
        {
            return 0;
        }

        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static void ValidateNonEmptySql(string sql, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException(errorMessage);
        }
    }
}
