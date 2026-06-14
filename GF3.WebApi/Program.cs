using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.SpaServices.Extensions;
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure;
using WebApi.Middleware;
using WebApi.Realtime;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebApiCore(
    builder.Configuration,
    requireExplicitJwtSigningKey: builder.Environment.IsProduction());

var app = builder.Build();

ApplyDatabaseMigrations(app);
ConfigureCommonMiddleware(app);
ConfigureFrontendHosting(app);

app.Run();

/// <summary>
/// Applies pending EF Core migrations during startup.
/// We do this once on boot so the API always operates against the expected schema.
/// </summary>
static void ApplyDatabaseMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

/// <summary>
/// Configures middleware shared by all environments.
/// API-only middleware is intentionally scoped to API routes so SPA requests keep their own flow.
/// </summary>
static void ConfigureCommonMiddleware(WebApplication app)
{
    app.UseResponseCompression();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors("FrontendDev");
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseWhen(
        context => StartupConfiguration.IsApiRequest(context.Request.Path),
        apiBranch =>
        {
            apiBranch.UseMiddleware<ApiExceptionMiddleware>();
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
        OnPrepareResponse = context =>
        {
            if (context.Context.Request.Path.StartsWithSegments("/assets"))
            {
                context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            }
        }
    });
    app.MapFallbackToFile("index.html");
}
