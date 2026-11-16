using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.SpaServices.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure;
using WebApi.Middleware;
using WebApi.Realtime;
using WebApi.Services;

var maintenanceMigration = args is ["--gf3-maintenance-migrate"];
var maintenanceHealth = args is ["--gf3-maintenance-health"];
var builder = WebApplication.CreateBuilder(maintenanceMigration || maintenanceHealth ? [] : args);
if (maintenanceMigration || maintenanceHealth)
{
    if (!builder.Environment.IsProduction() ||
        !builder.Configuration.GetValue<bool>("DatabaseStartup:RequireExistingDatabase") ||
        !builder.Configuration.GetValue<bool>("DatabaseStartup:BackupBeforeMigrate") ||
        builder.Configuration.GetConnectionString("Default") != "Data Source=/data/SQLite.db")
    {
        throw new InvalidOperationException("Maintenance requires the protected production SQLite configuration.");
    }
}

if (maintenanceHealth)
{
    // The deployment engine also mounts the volume read-only. This host serves only health
    // and never registers the ordinary API routes or the application's background services.
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = "/data/SQLite.db",
        Mode = SqliteOpenMode.ReadOnly,
        Pooling = false,
    }.ToString();
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
    var healthApp = builder.Build();
    healthApp.MapGet("/api/health", async (AppDbContext db, CancellationToken cancellationToken) =>
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken) &&
            !(await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any();
        return Results.Json(new { status = canConnect ? "ok" : "unavailable", canConnect },
            statusCode: canConnect ? 200 : 503);
    });
    await healthApp.RunAsync();
    return;
}
SchedulePdfFontResolver.ConfigureOnce(builder.Configuration["PdfExport:FontFilePath"]);
if (builder.Environment.IsProduction())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
}
builder.Services.AddWebApiCore(
    builder.Configuration,
    requireExplicitJwtSigningKey: builder.Environment.IsProduction());
TrustedProxyConfiguration.Configure(builder.Services, builder.Configuration, builder.Environment.IsProduction());

var app = builder.Build();

await DatabaseMigrationStartup.ApplyAsync(app);
if (maintenanceMigration)
{
    await app.DisposeAsync();
    return;
}
ConfigureCommonMiddleware(app);
ConfigureFrontendHosting(app);

app.Run();

/// <summary>
/// Configures middleware shared by all environments.
/// API-only middleware is intentionally scoped to API routes so SPA requests keep their own flow.
/// </summary>
static void ConfigureCommonMiddleware(WebApplication app)
{
    if (app.Environment.IsProduction())
    {
        app.UseForwardedHeaders();
    }

    app.UseResponseCompression();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors("FrontendDev");
    }

    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.UseWhen(
        context => StartupConfiguration.IsApiRequest(context.Request.Path),
        apiBranch =>
        {
            apiBranch.UseMiddleware<ApiExceptionMiddleware>();
            apiBranch.UseMiddleware<RegulationAcceptanceGuardMiddleware>();
            apiBranch.UseMiddleware<ManagerWorkspaceModeGuardMiddleware>();
            apiBranch.UseMiddleware<EmployeePresenceMiddleware>();
            apiBranch.UseMiddleware<AdminToolsGuardMiddleware>();
        });

    app.MapControllers();
    app.MapHub<EmployeePresenceHub>(EmployeePresenceHub.RoutePattern, options =>
    {
        options.CloseOnAuthenticationExpiration = true;
    });
}

/// <summary>
/// Configures how the frontend is served.
/// In development we proxy non-API requests to the Vite dev server; in production we serve the
/// built frontend files from <c>wwwroot</c>.
/// </summary>
static void ConfigureFrontendHosting(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.MapWhen(
            context => StartupConfiguration.ShouldUseSpaProxy(context.Request.Path),
            spaApp =>
            {
                spaApp.UseSpa(spa =>
                {
                    spa.Options.SourcePath = @"..\FrontEnd";
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
                });
            });

        return;
    }

    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = context => StartupConfiguration.ApplyFrontendCacheHeaders(context.Context, context.File.Name)
    });
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        OnPrepareResponse = context => StartupConfiguration.ApplyFrontendCacheHeaders(context.Context, context.File.Name)
    });
}
