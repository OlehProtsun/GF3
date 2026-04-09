using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataAccessLayer.Models.DataBaseContext;

/// <summary>
/// Design-time factory used by EF Core tooling.
/// The factory mirrors the runtime SQLite configuration closely enough for migrations while still
/// allowing developers to override the connection string from the environment when needed.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "GF3_CONNECTION_STRING";
    private const string DefaultDatabaseFolderName = "GF3";
    private const string DefaultDatabaseFileName = "SQLite.db";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        var environmentConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            return environmentConnectionString;
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DefaultDatabaseFolderName);

        Directory.CreateDirectory(root);

        var databasePath = Path.Combine(root, DefaultDatabaseFileName);
        return $"Data Source={databasePath}";
    }
}
