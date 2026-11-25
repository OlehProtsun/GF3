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
    bool IsBackupBundleDatabase(string databasePath) => SqliteMediaBackup.IsBundleDatabasePath(databasePath);
    Task<bool> HasReferencedMediaAsync(string databasePath, CancellationToken ct)
        => SqliteMediaBackup.HasReferencedMediaAsync(databasePath, ct);
    Task<string> MaterializeBackupAsync(string databasePath, string workingDirectory, CancellationToken ct)
        => SqliteMediaBackup.MaterializeAsync(Path.GetDirectoryName(Path.GetFullPath(databasePath))!, workingDirectory, ct);
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
    private readonly SemaphoreSlim _snapshotGate = new(1, 1);
    private string _databasePath;

    public SqliteDatabaseWorkspace(string databasePath)
    {
        RejectArchivePath(databasePath);
        _databasePath = NormalizeDatabasePath(databasePath);
        EnsureDatabaseDirectory(_databasePath);
    }

    public string DatabasePath => GetDatabasePath();

    public string ConnectionString => BuildConnectionString(GetDatabasePath());

    public string WorkspaceRootPath => CreateWorkspacePaths(GetDatabasePath()).WorkspaceRootPath;

    public string AutomaticBackupDirectoryPath => CreateWorkspacePaths(GetDatabasePath()).AutomaticBackupDirectoryPath;

    public string ManualCopyDirectoryPath => CreateWorkspacePaths(GetDatabasePath()).ManualCopyDirectoryPath;

    public int AutomaticBackupRetentionLimit => AutomaticBackupRetentionLimitValue;

    public bool IsBackupBundleDatabase(string databasePath) => SqliteMediaBackup.IsBundleDatabasePath(databasePath);

    public Task<string> MaterializeBackupAsync(string databasePath, string workingDirectory, CancellationToken ct)
        => SqliteMediaBackup.MaterializeAsync(Path.GetDirectoryName(Path.GetFullPath(databasePath))!, workingDirectory, ct);

    public string SetDatabasePath(string databasePath)
    {
        RejectArchivePath(databasePath);
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

        await _snapshotGate.WaitAsync(ct).ConfigureAwait(false);
        string bundlePath;
        try
        {
            bundlePath = BuildBundlePath(workspacePaths.DatabasePath, destinationDirectoryPath, copyKind);
            await SqliteMediaBackup.CreateAsync(workspacePaths.DatabasePath, bundlePath, ct).ConfigureAwait(false);
            if (enforceRetention)
            {
                ApplyAutomaticBackupRetention(destinationDirectoryPath, workspacePaths.DatabasePath);
            }
        }
        finally
        {
            _snapshotGate.Release();
        }

        return CreateBundleEntry(bundlePath, workspacePaths, ResolveCategory(bundlePath, workspacePaths));
    }

    private static IEnumerable<SqliteDatabaseFileEntry> EnumerateAvailableDatabases(WorkspacePaths workspacePaths)
    {
        var entries = new Dictionary<string, SqliteDatabaseFileEntry>(StringComparer.OrdinalIgnoreCase);

        AddDirectoryEntries(entries, workspacePaths.WorkspaceRootPath, workspacePaths, "database");
        AddDirectoryEntries(entries, workspacePaths.AutomaticBackupDirectoryPath, workspacePaths, "backup");
        AddDirectoryEntries(entries, workspacePaths.ManualCopyDirectoryPath, workspacePaths, "manualCopy");
        AddDirectoryEntries(entries, Path.Combine(workspacePaths.WorkspaceRootPath, "restored"), workspacePaths, "database");

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

        var files = Directory.EnumerateFiles(directoryPath)
            .Where(IsSupportedDatabaseFile)
            .Select(path => CreateEntry(path, workspacePaths, category))
            .ToArray();
        var bundles = Directory.EnumerateDirectories(directoryPath)
            .SelectMany(path => IsCompletedBundle(path)
                ? [CreateBundleEntry(path, workspacePaths, category)]
                : IsRestoredWorkingDirectory(path) ? [CreateEntry(Path.Combine(path, "SQLite.db"), workspacePaths, category)] : Array.Empty<SqliteDatabaseFileEntry>());
        return files.Concat(bundles)
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

        foreach (var entry in EnumerateDirectoryEntries(directoryPath, workspacePaths, category))
        {
            entries[entry.Path] = entry;
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

    private static SqliteDatabaseFileEntry CreateBundleEntry(string bundlePath, WorkspacePaths workspacePaths, string category)
    {
        var databasePath = Path.Combine(bundlePath, "SQLite.db");
        var file = new FileInfo(databasePath);
        var directory = new DirectoryInfo(bundlePath);
        return new SqliteDatabaseFileEntry
        {
            Name = directory.Name,
            Path = databasePath,
            Category = category,
            FileSizeBytes = file.Exists ? file.Length : 0,
            LastModifiedUtc = directory.Exists ? directory.LastWriteTimeUtc : null,
            IsActive = PathsEqual(databasePath, workspacePaths.DatabasePath),
        };
    }

    private static string BuildBundlePath(string sourceDatabasePath, string destinationDirectoryPath, string copyKind)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceDatabasePath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        return Path.Combine(destinationDirectoryPath, $"{fileNameWithoutExtension}-{copyKind}-{timestamp}-{Guid.NewGuid():N}");
    }

    private static void ApplyAutomaticBackupRetention(string automaticBackupDirectoryPath, string activeDatabasePath)
    {
        if (!Directory.Exists(automaticBackupDirectoryPath))
        {
            return;
        }

        var deletableBackups = Directory.EnumerateFileSystemEntries(automaticBackupDirectoryPath)
            .Where(path => IsSupportedDatabaseFile(path) || (Directory.Exists(path) && IsCompletedBundle(path)))
            .Where(path => !PathsEqual(path, activeDatabasePath) && !PathsEqual(Path.Combine(path, "SQLite.db"), activeDatabasePath))
            .Select(path => new FileSystemInfoWrapper(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.CreationTimeUtc)
            .ToArray();

        foreach (var obsoleteBackup in deletableBackups.Skip(AutomaticBackupRetentionLimitValue))
        {
            if (obsoleteBackup.IsDirectory) Directory.Delete(obsoleteBackup.Path, recursive: true);
            else File.Delete(obsoleteBackup.Path);
        }
    }

    private static string ResolveCategory(string filePath, WorkspacePaths workspacePaths)
    {
        var parentDirectoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;

        if (IsWithin(parentDirectoryPath, workspacePaths.AutomaticBackupDirectoryPath)) return "backup";
        if (IsWithin(parentDirectoryPath, workspacePaths.ManualCopyDirectoryPath)) return "manualCopy";

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

        if (string.Equals(folderName, "restored", StringComparison.OrdinalIgnoreCase))
            return Directory.GetParent(directoryPath)?.FullName ?? directoryPath;
        if (Directory.GetParent(directoryPath) is { } parent &&
            (parent.Name.Equals(AutomaticBackupFolderName, StringComparison.OrdinalIgnoreCase) || parent.Name.Equals(ManualCopyFolderName, StringComparison.OrdinalIgnoreCase) || parent.Name.Equals("restored", StringComparison.OrdinalIgnoreCase)))
            return Directory.GetParent(parent.FullName)?.FullName ?? parent.FullName;

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

    private static void RejectArchivePath(string databasePath)
    {
        if (SqliteMediaBackup.IsBundleDatabasePath(databasePath))
            throw new InvalidOperationException("A backup archive cannot be opened as the active database. Select it through database administration to create a working copy.");
    }

    private static void EnsureDatabaseDirectory(string databasePath)
    {
        var directoryPath = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The database path does not contain a valid directory.");

        Directory.CreateDirectory(directoryPath);
    }

    private static bool IsSupportedDatabaseFile(string filePath)
        => SupportedExtensions.Contains(Path.GetExtension(filePath));

    private static bool IsCompletedBundle(string path)
        => File.Exists(Path.Combine(path, "SQLite.db")) && File.Exists(Path.Combine(path, "backup-manifest.json")) && Directory.Exists(Path.Combine(path, "communication-gifs"));

    private static bool IsRestoredWorkingDirectory(string path)
        => File.Exists(Path.Combine(path, "SQLite.db")) && Directory.Exists(Path.Combine(path, "communication-gifs")) && !File.Exists(Path.Combine(path, "backup-manifest.json"));

    private static bool IsWithin(string path, string parent)
    {
        var relative = Path.GetRelativePath(parent, path);
        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }

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

    private sealed class FileSystemInfoWrapper
    {
        public FileSystemInfoWrapper(string path)
        {
            Path = path;
            IsDirectory = Directory.Exists(path);
            LastWriteTimeUtc = IsDirectory ? Directory.GetLastWriteTimeUtc(path) : File.GetLastWriteTimeUtc(path);
            CreationTimeUtc = IsDirectory ? Directory.GetCreationTimeUtc(path) : File.GetCreationTimeUtc(path);
        }

        public string Path { get; }
        public bool IsDirectory { get; }
        public DateTime LastWriteTimeUtc { get; }
        public DateTime CreationTimeUtc { get; }
    }
}
