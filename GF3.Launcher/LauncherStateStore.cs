using System.Diagnostics;
using System.Text.Json;

namespace GF3.Launcher;

internal static class LauncherStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private static string StateDirectoryPath => LauncherPaths.GetApplicationDataRoot();

    private static string StateFilePath => Path.Combine(StateDirectoryPath, "launcher-state.json");

    public static void Save(string baseUrl)
    {
        try
        {
            Directory.CreateDirectory(StateDirectoryPath);

            var state = new LauncherState(baseUrl, DateTimeOffset.UtcNow);
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(StateFilePath, json);
        }
        catch
        {
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(StateFilePath))
            {
                File.Delete(StateFilePath);
            }
        }
        catch
        {
        }
    }

    public static bool TryOpenRunningInstance()
    {
        if (!TryReadBaseUrl(out var baseUrl) || string.IsNullOrWhiteSpace(baseUrl))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(baseUrl)
            {
                UseShellExecute = true,
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadBaseUrl(out string? baseUrl)
    {
        baseUrl = null;

        try
        {
            if (!File.Exists(StateFilePath))
            {
                return false;
            }

            var json = File.ReadAllText(StateFilePath);
            var state = JsonSerializer.Deserialize<LauncherState>(json, JsonOptions);
            if (string.IsNullOrWhiteSpace(state?.BaseUrl))
            {
                return false;
            }

            baseUrl = state.BaseUrl;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record LauncherState(string BaseUrl, DateTimeOffset StartedAtUtc);
}
