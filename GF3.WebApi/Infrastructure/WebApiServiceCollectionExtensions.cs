using BusinessLogicLayer;
using BusinessLogicLayer.Options;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
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
        Action<string>? ensureDirectory = null,
        bool requireExplicitJwtSigningKey = false)
    {
        var jwtOptions = JwtAuthOptions.FromConfiguration(configuration, requireExplicitJwtSigningKey);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IScheduleWorksheetPdfRenderer, ScheduleWorksheetPdfRenderer>();
        services.AddOptions<PushOptions>().Bind(configuration.GetSection("Push"))
            .Validate(options => options.IsValid(), "Push configuration is invalid; check VAPID settings.").ValidateOnStart();
        services.AddSingleton<AuthAttemptLimiter>();
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (!context.Request.Path.StartsWithSegments("/api"))
                    return RateLimitPartition.GetNoLimiter("non-api");

                string? identity = null;
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    if (context.User.IsInRole(AuthRoles.Manager) &&
                        int.TryParse(context.User.FindFirstValue("manager_id"), out var managerId) && managerId > 0)
                        identity = $"manager:{managerId}";
                    else if (context.User.IsInRole(AuthRoles.Employee) &&
                        int.TryParse(context.User.FindFirstValue("employee_id"), out var employeeId) && employeeId > 0)
                        identity = $"employee:{employeeId}";
                }
                var ip = context.Connection.RemoteIpAddress;
                if (ip?.IsIPv4MappedToIPv6 == true) ip = ip.MapToIPv4();
                var key = identity is null ? $"anonymous:{ip?.ToString() ?? "unknown"}" : $"authenticated:{identity}";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = identity is null ? 120 : 240,
                    Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true,
                });
            });
            options.OnRejected = async (context, cancellationToken) =>
            {
                var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)) : 60;
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                response.Headers.CacheControl = "no-store";
                await response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = 429, Title = "Too many requests",
                    Detail = "Too many requests. Please try again later.",
                }, options: (System.Text.Json.JsonSerializerOptions?)null,
                    contentType: "application/problem+json", cancellationToken: cancellationToken);
            };
        });
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
        services.AddScoped<ScheduleVersionCommitService>();
        services.AddScoped<WebApi.ShiftSwaps.ShiftSwapExecutionService>();
        services.AddScoped<WebApi.ShiftSwaps.ContractShiftApprovalService>();
        services.AddScoped<IEmployeePushNotificationSource, EmployeePushNotificationSource>();
        services.AddHttpClient<IEmployeeWebPushSender, EmployeeWebPushSender>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddHostedService<EmployeeWebPushHostedService>();
        services.AddScoped<IScheduleLastUpdateService, ScheduleLastUpdateService>();
        services.Configure<SmtpEmailOptions>(configuration.GetSection("Smtp"));
        services.Configure<ExportTemplatesOptions>(configuration.GetSection("ExportTemplates"));
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
        services.AddHostedService<EmployeeNotificationCleanupHostedService>();
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
        services.AddAuthorization(options =>
            options.AddPolicy(AuthPolicies.SystemManager, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AuthRoles.Manager)
                .RequireClaim(AuthPolicies.SystemManagerClaim, "true")));
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
