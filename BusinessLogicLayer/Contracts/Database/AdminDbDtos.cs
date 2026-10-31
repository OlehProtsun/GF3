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

    /// <summary>
    /// Database workspace details, including available database files and backup folders.
    /// </summary>
    public AdminDbStorageWorkspaceDto StorageWorkspace { get; init; } = new();
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
/// Represents one database file visible to the admin database workspace.
/// </summary>
public sealed class AdminDbFileEntryDto
{
    /// <summary>
    /// File name without directory information.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Fully qualified path to the file.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Source category of the file inside the workspace.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Current file size when available.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Last write timestamp when available.
    /// </summary>
    public DateTime? LastModifiedUtc { get; init; }

    /// <summary>
    /// Indicates whether this file is the currently active database.
    /// </summary>
    public bool IsActive { get; init; }
}

/// <summary>
/// Describes the storage workspace around the active database file.
/// </summary>
public sealed class AdminDbStorageWorkspaceDto
{
    /// <summary>
    /// Root folder relative to which backup/manual-copy directories are resolved.
    /// </summary>
    public string WorkspaceRootPath { get; init; } = string.Empty;

    /// <summary>
    /// Folder that stores automatic hourly backups.
    /// </summary>
    public string AutomaticBackupDirectoryPath { get; init; } = string.Empty;

    /// <summary>
    /// Folder that stores user-triggered manual copies.
    /// </summary>
    public string ManualCopyDirectoryPath { get; init; } = string.Empty;

    /// <summary>
    /// Maximum number of automatic backups retained in the automatic backup folder.
    /// </summary>
    public int AutomaticBackupRetentionLimit { get; init; }

    /// <summary>
    /// All database-like files currently visible in the workspace.
    /// </summary>
    public IReadOnlyList<AdminDbFileEntryDto> AvailableDatabases { get; init; } = Array.Empty<AdminDbFileEntryDto>();

    /// <summary>
    /// Known automatic backups ordered by recency.
    /// </summary>
    public IReadOnlyList<AdminDbFileEntryDto> AutomaticBackups { get; init; } = Array.Empty<AdminDbFileEntryDto>();

    /// <summary>
    /// Known manual copies ordered by recency.
    /// </summary>
    public IReadOnlyList<AdminDbFileEntryDto> ManualCopies { get; init; } = Array.Empty<AdminDbFileEntryDto>();
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
