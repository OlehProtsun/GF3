using System.Text.Json;

namespace GF3.Launcher;

internal static class LauncherDatabaseSelectionStore
{
    private const string StateFileName = "database-selection.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static string ResolveDatabasePath(string fallbackDatabasePath)
    {
        if (TryRead(out var selectedDatabasePath))
        {
            return selectedDatabasePath;
        }

        return Path.GetFullPath(fallbackDatabasePath);
    }

    private static bool TryRead(out string databasePath)
    {
        databasePath = string.Empty;

        try
        {
            var stateFilePath = Path.Combine(LauncherPaths.GetApplicationDataRoot(), StateFileName);
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

            var normalizedDatabasePath = Path.GetFullPath(state.DatabasePath.Trim());
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

    private sealed record DatabaseSelectionState(string DatabasePath, DateTimeOffset UpdatedAtUtc);
}
