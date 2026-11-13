using Microsoft.Data.Sqlite;

namespace DataAccessLayer.Administration;

/// <summary>
/// Lightweight description of a SQLite file that belongs to the current database workspace.
/// </summary>
public sealed class SqliteDatabaseFileEntry
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime? LastModifiedUtc { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Snapshot of the current database workspace layout and the known database-like files in it.
/// </summary>
public sealed class SqliteDatabaseWorkspaceState
{
    public string WorkspaceRootPath { get; init; } = string.Empty;
    public string AutomaticBackupDirectoryPath { get; init; } = string.Empty;
    public string ManualCopyDirectoryPath { get; init; } = string.Empty;
    public int AutomaticBackupRetentionLimit { get; init; }
    public IReadOnlyList<SqliteDatabaseFileEntry> AvailableDatabases { get; init; } = Array.Empty<SqliteDatabaseFileEntry>();
    public IReadOnlyList<SqliteDatabaseFileEntry> AutomaticBackups { get; init; } = Array.Empty<SqliteDatabaseFileEntry>();
    public IReadOnlyList<SqliteDatabaseFileEntry> ManualCopies { get; init; } = Array.Empty<SqliteDatabaseFileEntry>();
}

/// <summary>
/// Stores the active SQLite database path for the running application instance and provides
/// backup/manual-copy helpers that operate relative to that database workspace.
/// </summary>
public interface ISqliteDatabaseWorkspace
{
    string DatabasePath { get; }
    string ConnectionString { get; }
    string WorkspaceRootPath { get; }
    string AutomaticBackupDirectoryPath { get; }
    string ManualCopyDirectoryPath { get; }
    int AutomaticBackupRetentionLimit { get; }
    string SetDatabasePath(string databasePath);
    SqliteDatabaseWorkspaceState GetWorkspaceState();
    Task<SqliteDatabaseFileEntry> CreateAutomaticBackupAsync(CancellationToken ct);
    Task<SqliteDatabaseFileEntry> CreateManualCopyAsync(CancellationToken ct);
}

/// <summary>
/// Central runtime holder for the currently active SQLite file and the backup folders around it.
/// </summary>
public sealed class SqliteDatabaseWorkspace : ISqliteDatabaseWorkspace
{
    private const string AutomaticBackupFolderName = "backups";
    private const string ManualCopyFolderName = "DatabaseCopyManual";
    private const int AutomaticBackupRetentionLimitValue = 10;
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".db",
        ".sqlite",
        ".sqlite3",
    };

    private readonly object _gate = new();
    private string _databasePath;

    public SqliteDatabaseWorkspace(string databasePath)
    {
        _databasePath = NormalizeDatabasePath(databasePath);
        EnsureDatabaseDirectory(_databasePath);
    }

    public string DatabasePath => GetDatabasePath();

    public string ConnectionString => BuildConnectionString(GetDatabasePath());

    public string WorkspaceRootPath => CreateWorkspacePaths(GetDatabasePath()).WorkspaceRootPath;

    public string AutomaticBackupDirectoryPath => CreateWorkspacePaths(GetDatabasePath()).AutomaticBackupDirectoryPath;

    public string ManualCopyDirectoryPath => CreateWorkspacePaths(GetDatabasePath()).ManualCopyDirectoryPath;

    public int AutomaticBackupRetentionLimit => AutomaticBackupRetentionLimitValue;

    public string SetDatabasePath(string databasePath)
    {
        var normalizedPath = NormalizeDatabasePath(databasePath);
        EnsureDatabaseDirectory(normalizedPath);

        lock (_gate)
        {
            _databasePath = normalizedPath;
        }

        return normalizedPath;
    }

    public SqliteDatabaseWorkspaceState GetWorkspaceState()
    {
        var workspacePaths = CreateWorkspacePaths(GetDatabasePath());

        return new SqliteDatabaseWorkspaceState
        {
            WorkspaceRootPath = workspacePaths.WorkspaceRootPath,
            AutomaticBackupDirectoryPath = workspacePaths.AutomaticBackupDirectoryPath,
            ManualCopyDirectoryPath = workspacePaths.ManualCopyDirectoryPath,
            AutomaticBackupRetentionLimit = AutomaticBackupRetentionLimitValue,
            AvailableDatabases = EnumerateAvailableDatabases(workspacePaths).ToArray(),
            AutomaticBackups = EnumerateDirectoryEntries(workspacePaths.AutomaticBackupDirectoryPath, workspacePaths, "backup").ToArray(),
            ManualCopies = EnumerateDirectoryEntries(workspacePaths.ManualCopyDirectoryPath, workspacePaths, "manualCopy").ToArray(),
        };
    }

    public Task<SqliteDatabaseFileEntry> CreateAutomaticBackupAsync(CancellationToken ct)
    {
        var workspacePaths = CreateWorkspacePaths(GetDatabasePath());
        return CreateCopyAsync(workspacePaths, workspacePaths.AutomaticBackupDirectoryPath, "auto", enforceRetention: true, ct);
    }

    public Task<SqliteDatabaseFileEntry> CreateManualCopyAsync(CancellationToken ct)
    {
        var workspacePaths = CreateWorkspacePaths(GetDatabasePath());
        return CreateCopyAsync(workspacePaths, workspacePaths.ManualCopyDirectoryPath, "manual", enforceRetention: false, ct);
    }

    private string GetDatabasePath()
    {
        lock (_gate)
        {
            return _databasePath;
        }
    }

    private async Task<SqliteDatabaseFileEntry> CreateCopyAsync(
        WorkspacePaths workspacePaths,
        string destinationDirectoryPath,
        string copyKind,
        bool enforceRetention,
        CancellationToken ct)
    {
        if (!File.Exists(workspacePaths.DatabasePath))
        {
            throw new FileNotFoundException("The active database file does not exist.", workspacePaths.DatabasePath);
        }

        Directory.CreateDirectory(destinationDirectoryPath);

        var destinationPath = BuildCopyPath(workspacePaths.DatabasePath, destinationDirectoryPath, copyKind);
        await BackupDatabaseAsync(workspacePaths.DatabasePath, destinationPath, ct).ConfigureAwait(false);

        if (enforceRetention)
        {
            ApplyAutomaticBackupRetention(destinationDirectoryPath, workspacePaths.DatabasePath);
        }

        return CreateEntry(destinationPath, workspacePaths, ResolveCategory(destinationPath, workspacePaths));
    }

    private static IEnumerable<SqliteDatabaseFileEntry> EnumerateAvailableDatabases(WorkspacePaths workspacePaths)
    {
        var entries = new Dictionary<string, SqliteDatabaseFileEntry>(StringComparer.OrdinalIgnoreCase);

        AddDirectoryEntries(entries, workspacePaths.WorkspaceRootPath, workspacePaths, "database");
        AddDirectoryEntries(entries, workspacePaths.AutomaticBackupDirectoryPath, workspacePaths, "backup");
        AddDirectoryEntries(entries, workspacePaths.ManualCopyDirectoryPath, workspacePaths, "manualCopy");

        return entries.Values
            .OrderByDescending(entry => entry.IsActive)
            .ThenByDescending(entry => entry.LastModifiedUtc ?? DateTime.MinValue)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<SqliteDatabaseFileEntry> EnumerateDirectoryEntries(
        string directoryPath,
        WorkspacePaths workspacePaths,
        string category)
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(directoryPath)
            .Where(IsSupportedDatabaseFile)
            .Select(path => CreateEntry(path, workspacePaths, category))
            .OrderByDescending(entry => entry.LastModifiedUtc ?? DateTime.MinValue)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddDirectoryEntries(
        IDictionary<string, SqliteDatabaseFileEntry> entries,
        string directoryPath,
        WorkspacePaths workspacePaths,
        string category)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(directoryPath).Where(IsSupportedDatabaseFile))
        {
            entries[filePath] = CreateEntry(filePath, workspacePaths, category);
        }
    }

    private static SqliteDatabaseFileEntry CreateEntry(string filePath, WorkspacePaths workspacePaths, string category)
    {
        var fileInfo = new FileInfo(filePath);

        return new SqliteDatabaseFileEntry
        {
            Name = Path.GetFileName(filePath),
            Path = filePath,
            Category = category,
            FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            LastModifiedUtc = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : null,
            IsActive = PathsEqual(filePath, workspacePaths.DatabasePath),
        };
    }

    private static string BuildCopyPath(string sourceDatabasePath, string destinationDirectoryPath, string copyKind)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceDatabasePath);
        var extension = Path.GetExtension(sourceDatabasePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".db";
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
        return Path.Combine(destinationDirectoryPath, $"{fileNameWithoutExtension}-{copyKind}-{timestamp}{extension}");
    }

    private static async Task BackupDatabaseAsync(string sourceDatabasePath, string destinationDatabasePath, CancellationToken ct)
    {
        await using var sourceConnection = new SqliteConnection(BuildConnectionString(sourceDatabasePath));
        await sourceConnection.OpenAsync(ct).ConfigureAwait(false);

        await using var destinationConnection = new SqliteConnection(BuildConnectionString(destinationDatabasePath));
        await destinationConnection.OpenAsync(ct).ConfigureAwait(false);

        sourceConnection.BackupDatabase(destinationConnection);
    }

    private static void ApplyAutomaticBackupRetention(string automaticBackupDirectoryPath, string activeDatabasePath)
    {
        if (!Directory.Exists(automaticBackupDirectoryPath))
        {
            return;
        }

        var deletableBackups = Directory.EnumerateFiles(automaticBackupDirectoryPath)
            .Where(IsSupportedDatabaseFile)
            .Where(path => !PathsEqual(path, activeDatabasePath))
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.CreationTimeUtc)
            .ToArray();

        foreach (var obsoleteBackup in deletableBackups.Skip(AutomaticBackupRetentionLimitValue))
        {
            obsoleteBackup.Delete();
        }
    }

    private static string ResolveCategory(string filePath, WorkspacePaths workspacePaths)
    {
        var parentDirectoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;

        if (PathsEqual(parentDirectoryPath, workspacePaths.AutomaticBackupDirectoryPath))
        {
            return "backup";
        }

        if (PathsEqual(parentDirectoryPath, workspacePaths.ManualCopyDirectoryPath))
        {
            return "manualCopy";
        }

        return "database";
    }

    private static WorkspacePaths CreateWorkspacePaths(string databasePath)
    {
        var workspaceRootPath = ResolveWorkspaceRoot(databasePath);
        return new WorkspacePaths(
            databasePath,
            workspaceRootPath,
            Path.Combine(workspaceRootPath, AutomaticBackupFolderName),
            Path.Combine(workspaceRootPath, ManualCopyFolderName));
    }

    private static string ResolveWorkspaceRoot(string databasePath)
    {
        var directoryPath = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The database path does not contain a valid directory.");

        var folderName = Path.GetFileName(directoryPath);
        if (string.Equals(folderName, AutomaticBackupFolderName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(folderName, ManualCopyFolderName, StringComparison.OrdinalIgnoreCase))
        {
            return Directory.GetParent(directoryPath)?.FullName ?? directoryPath;
        }

        return directoryPath;
    }

    private static string NormalizeDatabasePath(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new InvalidOperationException("The database path is required.");
        }

        return Path.GetFullPath(databasePath.Trim());
    }

    private static void EnsureDatabaseDirectory(string databasePath)
    {
        var directoryPath = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The database path does not contain a valid directory.");

        Directory.CreateDirectory(directoryPath);
    }

    private static bool IsSupportedDatabaseFile(string filePath)
        => SupportedExtensions.Contains(Path.GetExtension(filePath));

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildConnectionString(string databasePath)
        => new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
        }.ToString();

    private readonly record struct WorkspacePaths(
        string DatabasePath,
        string WorkspaceRootPath,
        string AutomaticBackupDirectoryPath,
        string ManualCopyDirectoryPath);
}
