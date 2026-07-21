using DataAccessLayer.Administration;
using Microsoft.AspNetCore.Http;
using WebApi.Options;

namespace WebApi.Infrastructure;

/// <summary>
/// Small, testable startup decisions extracted from Program.cs.
/// This keeps environment-sensitive configuration readable and lets tests verify the behavior
/// without having to boot the full web host pipeline every time.
/// </summary>
public static class StartupConfiguration
{
    private const string AdminEnabledVariable = "GF3_ADMIN_ENABLED";
    private const string AdminAllowRemoteVariable = "GF3_ADMIN_ALLOW_REMOTE";
    private const string AdminAllowWriteVariable = "GF3_ADMIN_ALLOW_WRITE";
    private const string AdminDeveloperPasswordVariable = "GF3_ADMIN_DEVELOPER_PASSWORD";
    private const string ApplicationDataFolderName = "GF3";
    private const string DatabaseFileName = "SQLite.db";

    public static void ApplyAdminToolsEnvironmentOverrides(
        AdminToolsOptions options,
        Func<string, string?>? readEnvironmentVariable = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        readEnvironmentVariable ??= Environment.GetEnvironmentVariable;
        ApplyBooleanOverride(AdminEnabledVariable, value => options.Enabled = value, readEnvironmentVariable);
        ApplyBooleanOverride(AdminAllowRemoteVariable, value => options.AllowRemoteAccess = value, readEnvironmentVariable);
        ApplyBooleanOverride(AdminAllowWriteVariable, value => options.AllowWriteSql = value, readEnvironmentVariable);
        options.DeveloperPassword = readEnvironmentVariable(AdminDeveloperPasswordVariable)?.Trim() ?? options.DeveloperPassword;
    }

    public static string ResolveConnectionString(
        string? configuredConnectionString,
        string? localApplicationDataRoot = null,
        Action<string>? ensureDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        var root = Path.Combine(
            localApplicationDataRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationDataFolderName);

        var defaultDatabasePath = Path.Combine(root, DatabaseFileName);
        var databasePath = SqliteDatabaseSelectionStore.ResolveDatabasePath(
            defaultDatabasePath,
            localApplicationDataRoot,
            ensureDirectory);

        return $"Data Source={databasePath}";
    }

    public static bool IsApiRequest(PathString path)
        => path.StartsWithSegments("/api");

    /// <summary>
    /// In development we proxy everything except API, swagger, and health traffic to Vite.
    /// That keeps backend routes explicit while letting the SPA own all browser navigation paths.
    /// </summary>
    public static bool ShouldUseSpaProxy(PathString path)
        => !path.StartsWithSegments("/api")
           && !path.StartsWithSegments("/swagger")
           && !path.StartsWithSegments("/health");

    private static void ApplyBooleanOverride(
        string variableName,
        Action<bool> applyValue,
        Func<string, string?> readEnvironmentVariable)
    {
        var rawValue = readEnvironmentVariable(variableName);
        if (bool.TryParse(rawValue, out var parsed))
        {
            applyValue(parsed);
        }
    }
}
