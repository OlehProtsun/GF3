using BusinessLogicLayer;
using DataAccessLayer.Administration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebApi.Options;

namespace WebApi.Infrastructure;

/// <summary>
/// Composition root helpers for the API project.
/// The extension method keeps Program.cs intentionally small and exposes a few resolver helpers
/// that are useful in integration/unit tests.
/// </summary>
public static class WebApiServiceCollectionExtensions
{
    private const string FrontendDevCorsPolicyName = "FrontendDev";
    private static readonly string[] FrontendDevOrigins = ["http://localhost:5173", "https://localhost:5173"];

    public static IServiceCollection AddWebApiCore(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<string, string?>? readEnvironmentVariable = null,
        string? localApplicationDataRoot = null,
        Action<string>? ensureDirectory = null)
    {
        services.AddApiMvc();
        services.AddApiDocumentation();
        services.AddFrontendDevelopmentCors();
        services.ConfigureAdminTools(configuration, readEnvironmentVariable);

        var connectionString = StartupConfiguration.ResolveConnectionString(
            configuration.GetConnectionString("Default"),
            localApplicationDataRoot,
            ensureDirectory);

        services.AddBusinessLogicStack(connectionString);
        return services;
    }

    public static AdminToolsOptions ResolveAdminToolsOptions(IServiceProvider services)
        => services.GetRequiredService<IOptions<AdminToolsOptions>>().Value;

    public static CorsPolicy? ResolveFrontendDevCorsPolicy(IServiceProvider services)
        => services.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(FrontendDevCorsPolicyName);

    public static string ResolveRegisteredDatabasePath(IServiceProvider services)
        => services.GetRequiredService<ISqliteAdminService>().DatabasePath;

    private static IServiceCollection AddApiMvc(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddScoped<ApiExceptionFilter>();
        services.AddControllers(options =>
        {
            options.Filters.AddService<ApiExceptionFilter>();
        });
        services.AddProblemDetails();
        return services;
    }

    private static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    private static IServiceCollection AddFrontendDevelopmentCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(FrontendDevCorsPolicyName, policy =>
            {
                policy.WithOrigins(FrontendDevOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static IServiceCollection ConfigureAdminTools(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<string, string?>? readEnvironmentVariable)
    {
        services.Configure<AdminToolsOptions>(configuration.GetSection("AdminTools"));
        services.PostConfigure<AdminToolsOptions>(options =>
            StartupConfiguration.ApplyAdminToolsEnvironmentOverrides(options, readEnvironmentVariable));

        return services;
    }
}
