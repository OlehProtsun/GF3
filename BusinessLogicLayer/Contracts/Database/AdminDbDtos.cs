namespace BusinessLogicLayer.Contracts.Database;

/// <summary>
/// Aggregated metadata returned by the high-level admin database service.
/// </summary>
public sealed class AdminDbMetadataDto
{
    /// <summary>
    /// SQLite engine version reported by the current database connection.
    /// </summary>
    public string SqliteVersion { get; init; } = string.Empty;

    /// <summary>
    /// Physical path of the database file being inspected.
    /// </summary>
    public string DatabasePath { get; init; } = string.Empty;

    /// <summary>
    /// Current file size of the database.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Last write timestamp of the database file when known.
    /// </summary>
    public DateTime? LastModifiedUtc { get; init; }

    /// <summary>
    /// Current SQLite user version pragma value.
    /// </summary>
    public int UserVersion { get; init; }

    /// <summary>
    /// Table names discovered in the database.
    /// </summary>
    public IReadOnlyList<string> Tables { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Broader list of schema objects such as tables, indexes, and views.
    /// </summary>
    public IReadOnlyList<AdminDbObjectDto> Objects { get; init; } = Array.Empty<AdminDbObjectDto>();
}

/// <summary>
/// Describes one database object discovered during admin inspection.
/// </summary>
public sealed class AdminDbObjectDto
{
    /// <summary>
    /// Object type reported by SQLite, for example table, index, or view.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Schema object name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Raw SQL definition when SQLite exposes one.
    /// </summary>
    public string Sql { get; init; } = string.Empty;
}

/// <summary>
/// Normalized result of a read-only SQL query executed through the admin service.
/// </summary>
public sealed class AdminDbQueryResultDto
{
    /// <summary>
    /// Column names returned by the query.
    /// </summary>
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Query result rows normalized into primitive object values.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; } = Array.Empty<IReadOnlyList<object?>>();

    /// <summary>
    /// Total number of rows returned by the query.
    /// </summary>
    public int RowCount { get; init; }
}

/// <summary>
/// Summary of a SQL import operation executed through the admin service.
/// </summary>
public sealed class AdminDbImportResultDto
{
    /// <summary>
    /// Number of SQL statements parsed from the import payload.
    /// </summary>
    public int StatementsExecuted { get; init; }

    /// <summary>
    /// Number of statements actually applied to the database.
    /// </summary>
    public int StatementsApplied { get; init; }

    /// <summary>
    /// Number of statements skipped because their effect already existed.
    /// </summary>
    public int StatementsAlreadyExisted { get; init; }

    /// <summary>
    /// Number of service-managed statements intentionally ignored by the importer.
    /// </summary>
    public int ServiceStatementsSkipped { get; init; }

    /// <summary>
    /// Index of the first failing statement when the import stops on an error.
    /// </summary>
    public int? FailedStatementIndex { get; init; }

    /// <summary>
    /// Human-readable failure description when the import does not complete successfully.
    /// </summary>
    public string? FailureReason { get; init; }
}
