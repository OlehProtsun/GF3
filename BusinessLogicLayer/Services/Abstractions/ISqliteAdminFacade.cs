using BusinessLogicLayer.Contracts.Database;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Thin facade over low-level SQLite admin operations exposed to higher layers.
/// </summary>
public interface ISqliteAdminFacade
{
    /// <summary>
    /// Returns the physical path of the currently used database file.
    /// </summary>
    string DatabasePath { get; }

    /// <summary>
    /// Executes an ad-hoc SQL command and returns a DTO-oriented result.
    /// </summary>
    Task<SqlExecutionResultDto> ExecuteSqlAsync(string sql, CancellationToken ct);

    /// <summary>
    /// Imports a SQL script into the current database.
    /// </summary>
    Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct);

    /// <summary>
    /// Returns database metadata in DTO form.
    /// </summary>
    Task<DatabaseInfoDto> GetDatabaseInfoAsync(CancellationToken ct);

    /// <summary>
    /// Computes a file hash for the supplied path.
    /// </summary>
    Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct);
}
