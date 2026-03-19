using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Administration
{
    public sealed class SqlExecutionResult
    {
        public bool IsSelect { get; init; }
        public DataTable? ResultTable { get; init; }
        public int AffectedRows { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public sealed class DatabaseInfo
    {
        public string DatabasePath { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; }
        public DateTime LastModifiedUtc { get; init; }
        public int UserVersion { get; init; }
        public IReadOnlyList<string> Tables { get; init; } = Array.Empty<string>();
    }

    public interface ISqliteAdminService
    {
        string DatabasePath { get; }
        Task<SqlExecutionResult> ExecuteSqlAsync(string sql, CancellationToken ct);
        Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct);
        Task<DatabaseInfo> GetDatabaseInfoAsync(CancellationToken ct);
        Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct);
    }

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

        public async Task<SqlExecutionResult> ExecuteSqlAsync(string sql, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new InvalidOperationException("SQL command is empty.");

            var normalized = sql.TrimStart();
            var isSelectLike =
                normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            if (isSelectLike)
            {
                await using var reader = await command.ExecuteReaderAsync(ct);
                var table = new DataTable();
                table.Load(reader);

                return new SqlExecutionResult
                {
                    IsSelect = true,
                    ResultTable = table,
                    Message = $"Query completed. Rows: {table.Rows.Count}."
                };
            }

            var rows = await command.ExecuteNonQueryAsync(ct);
            return new SqlExecutionResult
            {
                IsSelect = false,
                AffectedRows = rows,
                Message = $"Command executed successfully. Affected rows: {rows}."
            };
        }

        public async Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(sqlScript))
                throw new InvalidOperationException("Import script is empty.");

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();
            command.CommandText = sqlScript;
            await command.ExecuteNonQueryAsync(ct);
        }

        public async Task<DatabaseInfo> GetDatabaseInfoAsync(CancellationToken ct)
        {
            var dbPath = _databasePath;
            var fi = new FileInfo(dbPath);

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            var tables = new List<string>();
            await using (var tableCommand = connection.CreateCommand())
            {
                tableCommand.CommandText = @"
SELECT name
FROM sqlite_master
WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
ORDER BY name;";

                await using var tableReader = await tableCommand.ExecuteReaderAsync(ct);
                while (await tableReader.ReadAsync(ct))
                {
                    tables.Add(tableReader.GetString(0));
                }
            }

            var userVersion = 0;
            await using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA user_version;";
                var result = await pragma.ExecuteScalarAsync(ct);
                if (result != null && result != DBNull.Value)
                {
                    userVersion = Convert.ToInt32(result, CultureInfo.InvariantCulture);
                }
            }

            return new DatabaseInfo
            {
                DatabasePath = dbPath,
                FileSizeBytes = fi.Exists ? fi.Length : 0,
                LastModifiedUtc = fi.Exists ? fi.LastWriteTimeUtc : DateTime.MinValue,
                UserVersion = userVersion,
                Tables = tables
            };
        }

        public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
        {
            await using var stream = File.OpenRead(filePath);
            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(stream, ct);
            var builder = new StringBuilder(hash.Length * 2);

            foreach (var b in hash)
                builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));

            return builder.ToString();
        }
    }
}
