using BusinessLogicLayer.Contracts.Database;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// High-level admin operations for inspecting and importing the application database.
/// </summary>
public interface IAdminDbService
{
    /// <summary>
    /// Returns database metadata used by the admin tools dashboard.
    /// </summary>
    Task<AdminDbMetadataDto> GetMetadataAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the current database file hash for integrity/change detection.
    /// </summary>
    Task<string> GetDbHashAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes a SQL query and returns a result payload constrained by the configured maximum SQL length.
    /// </summary>
    Task<AdminDbQueryResultDto> ExecuteQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default);

    /// <summary>
    /// Executes a SQL non-query command and returns the affected-row count.
    /// </summary>
    Task<int> ExecuteNonQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default);

    /// <summary>
    /// Imports a SQL script while enforcing the configured upload-size limit.
    /// </summary>
    Task<AdminDbImportResultDto> ImportSqlAsync(byte[] fileBytes, int maxImportBytes, CancellationToken ct = default);
}
