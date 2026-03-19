using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using BusinessLogicLayer.Contracts.Database;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogicLayer.Services;

public sealed class AdminDbService : IAdminDbService
{
    private static readonly string[] ReadPrefixes = ["SELECT", "PRAGMA", "WITH"];
    private static readonly string[] BannedTokens = ["DROP", "ALTER", "CREATE", "ATTACH", "DETACH", ".LOAD", "VACUUM", "REINDEX"];
    private static readonly string[] WritePrefixes = ["INSERT", "UPDATE", "DELETE"];

    private readonly AppDbContext _dbContext;
    private readonly ISqliteAdminFacade _sqliteAdminFacade;

    public AdminDbService(AppDbContext dbContext, ISqliteAdminFacade sqliteAdminFacade)
    {
        _dbContext = dbContext;
        _sqliteAdminFacade = sqliteAdminFacade;
    }

    public async Task<AdminDbMetadataDto> GetMetadataAsync(CancellationToken ct = default)
    {
        await using var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);

        var sqliteVersion = await ExecuteScalarAsync(connection, "SELECT sqlite_version();", ct).ConfigureAwait(false) ?? string.Empty;

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

        return new AdminDbMetadataDto
        {
            SqliteVersion = sqliteVersion,
            Objects = items
        };
    }

    public async Task<string> GetDbHashAsync(CancellationToken ct = default)
    {
        var path = _sqliteAdminFacade.DatabasePath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            await using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(stream, ct).ConfigureAwait(false);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        await using var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(group_concat(COALESCE(sql,''), ';'), '') FROM sqlite_master WHERE type IN ('table','index','view') ORDER BY name;";
        var schema = Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(schema));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<AdminDbQueryResultDto> ExecuteQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default)
    {
        ValidateSql(sql, maxSqlLength, allowWrite: false);

        await using var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var rows = new List<IReadOnlyList<object?>>();

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var row = new object?[reader.FieldCount];
            reader.GetValues(row);
            rows.Add(row);
        }

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

        await using var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
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
        if (fileBytes.Length == 0)
        {
            throw new ValidationException("SQL import file is empty.");
        }

        if (fileBytes.Length > maxImportBytes)
        {
            throw new ValidationException($"SQL import exceeds max size of {maxImportBytes} bytes.");
        }

        var script = Encoding.UTF8.GetString(fileBytes);
        var statements = script.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        await using var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        var executed = 0;
        for (var i = 0; i < statements.Length; i++)
        {
            var sql = statements[i];
            try
            {
                ValidateSql(sql, sql.Length, allowWrite: true);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = tx;
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                executed++;
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new AdminDbImportResultDto
                {
                    StatementsExecuted = executed,
                    FailedStatementIndex = i
                };
            }
        }

        await tx.CommitAsync(ct).ConfigureAwait(false);
        return new AdminDbImportResultDto
        {
            StatementsExecuted = executed
        };
    }

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
        if (normalized.Contains("--") || normalized.Contains("/*", StringComparison.Ordinal))
        {
            throw new ValidationException("SQL comments are not allowed.");
        }

        foreach (var banned in BannedTokens)
        {
            if (normalized.Contains(banned, StringComparison.OrdinalIgnoreCase))
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

    private static async Task<string?> ExecuteScalarAsync(DbConnection connection, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return Convert.ToString(value);
    }
}
