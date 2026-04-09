using System.Data;

namespace BusinessLogicLayer.Contracts.Database;

/// <summary>
/// Low-level result of an ad-hoc SQLite command.
/// </summary>
public sealed class SqlExecutionResultDto
{
    /// <summary>
    /// Indicates whether the SQL produced a tabular result set.
    /// </summary>
    public bool IsSelect { get; init; }

    /// <summary>
    /// Raw result table returned by a select-like command.
    /// </summary>
    public DataTable? ResultTable { get; init; }

    /// <summary>
    /// Number of rows affected by a non-query command.
    /// </summary>
    public int AffectedRows { get; init; }

    /// <summary>
    /// Human-readable execution message returned by the low-level admin service.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Low-level metadata describing the current SQLite database file and schema.
/// </summary>
public sealed class DatabaseInfoDto
{
    /// <summary>
    /// Physical path of the database file.
    /// </summary>
    public string DatabasePath { get; init; } = string.Empty;

    /// <summary>
    /// Current database file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Last write timestamp of the database file.
    /// </summary>
    public DateTime LastModifiedUtc { get; init; }

    /// <summary>
    /// Current SQLite user version pragma value.
    /// </summary>
    public int UserVersion { get; init; }

    /// <summary>
    /// Table names discovered in the database.
    /// </summary>
    public IReadOnlyList<string> Tables { get; init; } = Array.Empty<string>();
}
