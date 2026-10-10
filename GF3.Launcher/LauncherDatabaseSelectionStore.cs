using System.Text.Json;

namespace GF3.Launcher;

internal static class LauncherDatabaseSelectionStore
{
    private const string StateFileName = "database-selection.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static string ResolveDatabasePath(string fallbackDatabasePath, string? applicationDataRoot = null)
    {
        if (TryRead(out var selectedDatabasePath, applicationDataRoot))
        {
            return selectedDatabasePath;
        }

        return Path.GetFullPath(fallbackDatabasePath);
    }

    private static bool TryRead(out string databasePath, string? applicationDataRoot)
    {
        databasePath = string.Empty;

        try
        {
            var stateFilePath = Path.Combine(applicationDataRoot ?? LauncherPaths.GetApplicationDataRoot(), StateFileName);
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
