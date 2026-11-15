namespace GF3.Launcher;

internal static class LauncherPaths
{
    private const string ApplicationFolderName = "GF3";
    private const string DefaultDatabaseFileName = "SQLite.db";
    private const string DatabasePathEnvironmentVariable = "GF3_DATABASE_PATH";

    public static string GetApplicationDataRoot()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationFolderName);

    public static string GetLauncherLogsRoot()
        => Path.Combine(GetApplicationDataRoot(), "logs", "launcher");

    public static string ResolveDatabasePath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(DatabasePathEnvironmentVariable);
        var databasePath = string.IsNullOrWhiteSpace(configuredPath)
            ? LauncherDatabaseSelectionStore.ResolveDatabasePath(Path.Combine(GetApplicationDataRoot(), DefaultDatabaseFileName))
            : configuredPath;

        databasePath = Path.GetFullPath(databasePath);
        var directoryPath = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The launcher database path is invalid.");

        Directory.CreateDirectory(directoryPath);
        return databasePath;
    }
}
