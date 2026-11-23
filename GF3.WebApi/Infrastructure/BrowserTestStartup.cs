using DataAccessLayer.Administration;

namespace WebApi.Infrastructure;

internal sealed class BrowserTestStartup(string databasePath, string instance)
{
    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static BrowserTestStartup? Configure(WebApplicationBuilder builder, bool requested)
    {
        var directoryPath = Environment.GetEnvironmentVariable("GF3_E2E_DIRECTORY");
        var instance = Environment.GetEnvironmentVariable("GF3_E2E_INSTANCE");
        if (!requested && !builder.Environment.IsEnvironment("Testing") && directoryPath is null && instance is null)
            return null;

        if (!requested || !builder.Environment.IsEnvironment("Testing") || directoryPath is null ||
            !Guid.TryParseExact(instance, "N", out _) || !Path.IsPathFullyQualified(directoryPath) ||
            Environment.GetEnvironmentVariable("GF3_CONNECTION_STRING") is not null ||
            builder.Configuration.GetSection("Kestrel:Endpoints").Exists())
            throw new InvalidOperationException("Browser tests require the dedicated Testing startup and ConnectionStrings:Default.");

        var directory = new DirectoryInfo(directoryPath);
        var tempRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if (!directory.Exists || (directory.Attributes & FileAttributes.ReparsePoint) != 0 ||
            !string.Equals(directory.Parent?.FullName, tempRoot, PathComparison) ||
            !directory.Name.StartsWith("gf3-browser-", StringComparison.Ordinal) ||
            directory.EnumerateFileSystemInfos().Any())
            throw new InvalidOperationException("Browser tests require an empty GF3 directory directly under the OS temporary directory.");

        var databasePath = Path.Combine(directory.FullName, "fixture.sqlite");
        if (builder.Configuration.GetConnectionString("Default") != $"Data Source={databasePath}")
            throw new InvalidOperationException("Browser tests require ConnectionStrings:Default to point to the new temporary SQLite file.");

        builder.WebHost.UseUrls("http://127.0.0.1:0");
        return new BrowserTestStartup(databasePath, instance!);
    }

    public void VerifyDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var workspace = scope.ServiceProvider.GetRequiredService<ISqliteDatabaseWorkspace>();
        if (!string.Equals(workspace.DatabasePath, databasePath, PathComparison) || File.Exists(databasePath))
            throw new InvalidOperationException("The registered browser-test SQLite workspace is unexpected.");
        app.Logger.LogInformation("Isolated browser-test SQLite path {DatabasePath}", databasePath);
    }

    public void MapReadiness(WebApplication app)
    {
        if (new FileInfo(databasePath) is not { Exists: true, Length: > 0 })
            throw new InvalidOperationException("Browser-test migrations did not create the expected SQLite file.");

        app.MapGet("/api/e2e-isolation", (HttpContext context) =>
            string.Equals(context.Request.Headers["X-GF3-E2E-Instance"], instance, StringComparison.Ordinal)
                ? Results.Ok(new { databasePath })
                : Results.NotFound());
    }
}
