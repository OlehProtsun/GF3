using BusinessLogicLayer;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using WebApi.Auth;
using WebApi.Options;
using WebApi.Realtime;
using WebApi.Services;

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
        var jwtOptions = JwtAuthOptions.FromConfiguration(configuration);

        services.AddApiMvc(jwtOptions);
        services.AddApiDocumentation();
        services.AddFrontendDevelopmentCors();
        services.AddHttpResponseCompression();
        services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(35);
        });
        services.AddSingleton<ManagerEditLockService>();
        services.AddSingleton<IManagerEditLockService>(services => services.GetRequiredService<ManagerEditLockService>());
        services.AddSingleton<IScheduleEditLockService>(services => services.GetRequiredService<ManagerEditLockService>());
        services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
        services.AddScoped<IWorkflowLogService, WorkflowLogService>();
        services.Configure<SmtpEmailOptions>(configuration.GetSection("Smtp"));
        services.ConfigureAdminTools(configuration, readEnvironmentVariable);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(jwtOptions));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();

        var connectionString = StartupConfiguration.ResolveConnectionString(
            configuration.GetConnectionString("Default"),
            localApplicationDataRoot,
            ensureDirectory);

        services.AddBusinessLogicStack(connectionString);
        services.AddHostedService<DatabaseAutoBackupHostedService>();
        return services;
    }

    public static AdminToolsOptions ResolveAdminToolsOptions(IServiceProvider services)
        => services.GetRequiredService<IOptions<AdminToolsOptions>>().Value;

    public static CorsPolicy? ResolveFrontendDevCorsPolicy(IServiceProvider services)
        => services.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(FrontendDevCorsPolicyName);

    public static string ResolveRegisteredDatabasePath(IServiceProvider services)
        => services.GetRequiredService<ISqliteAdminService>().DatabasePath;

    private static IServiceCollection AddApiMvc(this IServiceCollection services, JwtAuthOptions jwtOptions)
    {
        services.AddLogging();
        services.AddScoped<ApiExceptionFilter>();
        services.AddAuthentication(JwtAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>(
                JwtAuthenticationDefaults.SchemeName,
                _ => { });
        services.AddAuthorization();
        services.AddControllers(options =>
        {
            options.Filters.AddService<ApiExceptionFilter>();
            options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build()));
        });
        services.AddProblemDetails();
        return services;
    }

    private static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT access token in the format: Bearer {token}",
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);
        });
        return services;
    }

    private static IServiceCollection AddHttpResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            [
                "application/json",
                "application/problem+json",
                "application/vnd.api+json",
            ]);
        });

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
                    .AllowAnyMethod()
                    .AllowCredentials();
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
