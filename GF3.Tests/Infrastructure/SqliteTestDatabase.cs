using DataAccessLayer.Administration;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GF3.Tests.Infrastructure;

internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private SqliteTestDatabase(string rootPath)
    {
        RootPath = rootPath;
        DatabasePath = Path.Combine(rootPath, "test.db");
        ConnectionString = $"Data Source={DatabasePath}";
    }

    public string RootPath { get; }

    public string DatabasePath { get; }

    public string ConnectionString { get; }

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);

        var database = new SqliteTestDatabase(rootPath);
        await database.EnsureCreatedAsync().ConfigureAwait(false);
        return database;
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public ServiceProvider BuildServiceProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISqliteDatabaseWorkspace>(_ => new SqliteDatabaseWorkspace(DatabasePath));
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(ConnectionString));

        configureServices?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });
    }

    public async Task EnsureCreatedAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync().ConfigureAwait(false);
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }

    public async Task SeedAsync(Func<AppDbContext, Task> seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        await using var context = CreateContext();
        await seed(context).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
        }

        return ValueTask.CompletedTask;
    }
}
