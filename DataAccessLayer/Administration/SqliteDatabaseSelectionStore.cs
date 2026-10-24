using System.Text.Json;

namespace DataAccessLayer.Administration;

/// <summary>
/// Persists the last selected SQLite database path so the desktop app can restore it on the next launch.
/// </summary>
public static class SqliteDatabaseSelectionStore
{
    private const string ApplicationFolderName = "GF3";
    private const string StateFileName = "database-selection.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static string ResolveDatabasePath(
        string fallbackDatabasePath,
        string? localApplicationDataRoot = null,
        Action<string>? ensureDirectory = null)
    {
        if (TryRead(out var selectedDatabasePath, localApplicationDataRoot))
        {
            return selectedDatabasePath;
        }

        var normalizedFallbackPath = NormalizeDatabasePath(fallbackDatabasePath);
        ensureDirectory ??= path => _ = Directory.CreateDirectory(path);
        ensureDirectory(GetDirectoryPath(normalizedFallbackPath));
        return normalizedFallbackPath;
    }

    public static void Save(
        string databasePath,
        string? localApplicationDataRoot = null,
        Action<string>? ensureDirectory = null)
    {
        var normalizedDatabasePath = NormalizeDatabasePath(databasePath);
        var stateDirectoryPath = GetStateDirectoryPath(localApplicationDataRoot);

        ensureDirectory ??= path => _ = Directory.CreateDirectory(path);
        ensureDirectory(stateDirectoryPath);

        var state = new DatabaseSelectionState(normalizedDatabasePath, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(GetStateFilePath(localApplicationDataRoot), json);
    }

    public static bool TryRead(out string databasePath, string? localApplicationDataRoot = null)
    {
        databasePath = string.Empty;

        try
        {
            var stateFilePath = GetStateFilePath(localApplicationDataRoot);
            if (!File.Exists(stateFilePath))
            {
                return false;
            }

            var json = File.ReadAllText(stateFilePath);
            var state = JsonSerializer.Deserialize<DatabaseSelectionState>(json, JsonOptions);
            if (string.IsNullOrWhiteSpace(state?.DatabasePath))
            {
                return false;
            }

            var normalizedDatabasePath = NormalizeDatabasePath(state.DatabasePath);
            if (!File.Exists(normalizedDatabasePath))
            {
                return false;
            }

            databasePath = normalizedDatabasePath;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetStateDirectoryPath(string? localApplicationDataRoot)
        => Path.Combine(
            localApplicationDataRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationFolderName);

    private static string GetStateFilePath(string? localApplicationDataRoot)
        => Path.Combine(GetStateDirectoryPath(localApplicationDataRoot), StateFileName);

    private static string NormalizeDatabasePath(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new InvalidOperationException("The database path is required.");
        }

        return Path.GetFullPath(databasePath.Trim());
    }

    private static string GetDirectoryPath(string databasePath)
        => Path.GetDirectoryName(databasePath)
           ?? throw new InvalidOperationException("The database path does not contain a valid directory.");

    private sealed record DatabaseSelectionState(string DatabasePath, DateTimeOffset UpdatedAtUtc);
}
