using BusinessLogicLayer;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Infrastructure;
using WebApi.Options;
using WebApi.Realtime;
using WebApi.Services;

namespace GF3.Tests;

public sealed class BusinessLogicCoreTests
{
    [Fact]
    public void ValidationException_NormalizesErrors_AndKeepsCaseInsensitiveKeys()
    {
        var exception = new ValidationException(
            "Validation failed.",
            new Dictionary<string, string[]>
            {
                [" FirstName "] = ["Required", " ", ""],
                [""] = ["ignored"],
                ["LastName"] = [],
            });

        Assert.Single(exception.Errors);
        Assert.True(exception.Errors.ContainsKey(" FirstName "));
        Assert.Equal(["Required"], exception.Errors[" firstNAME "]);
    }

    [Fact]
    public void ValidationException_ForField_CreatesSingleFieldError()
    {
        var exception = ValidationException.ForField("Name", "Name is required.");

        Assert.Equal("Name is required.", exception.Message);
        Assert.Single(exception.Errors);
        Assert.Equal(["Name is required."], exception.Errors["name"]);
    }

    [Fact]
    public void DeleteOperationResult_SuccessAndFailure_ReturnExpectedShape()
    {
        var success = DeleteOperationResult.Success();
        var failure = DeleteOperationResult.Failure("Blocked.");

        Assert.True(success.Succeeded);
        Assert.Empty(success.Errors);
        Assert.False(failure.Succeeded);
        Assert.Equal("Blocked.", failure.Message);
        Assert.Equal(["Blocked."], failure.Errors["general"]);
    }

    [Fact]
    public void AddBusinessLogicLayer_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        services.AddBusinessLogicLayer();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IEmployeeService) &&
            descriptor.ImplementationType == typeof(EmployeeService));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IShopService) &&
            descriptor.ImplementationType == typeof(ShopService));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IContainerService) &&
            descriptor.ImplementationType == typeof(ContainerService));
    }

    [Fact]
    public void AddBusinessLogicStack_WithExplicitConnectionString_RegistersWorkspaceAndAdminService()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"), "stack.db");
        var services = new ServiceCollection();

        services.AddBusinessLogicStack($"Data Source={databasePath}", databasePath);
        using var provider = services.BuildServiceProvider();

        var workspace = provider.GetRequiredService<ISqliteDatabaseWorkspace>();
        var adminService = provider.GetRequiredService<ISqliteAdminService>();

        Assert.Equal(Path.GetFullPath(databasePath), workspace.DatabasePath);
        Assert.Equal(Path.GetFullPath(databasePath), adminService.DatabasePath);
    }

    [Fact]
    public void AddBusinessLogicStack_ParsesDatabasePathFromConnectionString()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "parsed.db");
        var services = new ServiceCollection();

        try
        {
            services.AddBusinessLogicStack($"Mode=ReadWriteCreate;Data Source={databasePath};Cache=Shared");
            using var provider = services.BuildServiceProvider();

            var workspace = provider.GetRequiredService<ISqliteDatabaseWorkspace>();

            Assert.Equal(Path.GetFullPath(databasePath), workspace.DatabasePath);
        }
        finally
        {
            if (Directory.Exists(databaseDirectory))
            {
                Directory.Delete(databaseDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void AddBusinessLogicStack_UsesEnvironmentConnectionString_WhenNoConnectionStringIsSupplied()
    {
        var databaseDirectory = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(databaseDirectory, "env.db");
        var previousValue = Environment.GetEnvironmentVariable("GF3_CONNECTION_STRING");
        Environment.SetEnvironmentVariable("GF3_CONNECTION_STRING", $"Data Source={databasePath}");
        var services = new ServiceCollection();

        try
        {
            services.AddBusinessLogicStack();
            using var provider = services.BuildServiceProvider();

            var workspace = provider.GetRequiredService<ISqliteDatabaseWorkspace>();

            Assert.Equal(Path.GetFullPath(databasePath), workspace.DatabasePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GF3_CONNECTION_STRING", previousValue);
            if (Directory.Exists(databaseDirectory))
            {
                Directory.Delete(databaseDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void StartupConfiguration_ApplyAdminToolsEnvironmentOverrides_UsesBooleanVariables()
    {
        var options = new AdminToolsOptions();
        var variables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["GF3_ADMIN_ENABLED"] = "true",
            ["GF3_ADMIN_ALLOW_REMOTE"] = "true",
            ["GF3_ADMIN_ALLOW_WRITE"] = "false",
        };

        StartupConfiguration.ApplyAdminToolsEnvironmentOverrides(
            options,
            variables.GetValueOrDefault);

        Assert.True(options.Enabled);
        Assert.True(options.AllowRemoteAccess);
        Assert.False(options.AllowWriteSql);
    }

    [Fact]
    public void StartupConfiguration_ResolveConnectionString_UsesConfiguredValueWhenPresent()
    {
        var connectionString = StartupConfiguration.ResolveConnectionString("Data Source=configured.db");

        Assert.Equal("Data Source=configured.db", connectionString);
    }

    [Fact]
    public void StartupConfiguration_ResolveConnectionString_BuildsFallbackPath_AndCreatesDirectory()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        string? ensuredPath = null;

        var connectionString = StartupConfiguration.ResolveConnectionString(
            configuredConnectionString: null,
            localApplicationDataRoot: tempRoot,
            ensureDirectory: path => ensuredPath = path);

        var expectedDatabasePath = Path.Combine(tempRoot, "GF3", "SQLite.db");
        Assert.Equal($"Data Source={expectedDatabasePath}", connectionString);
        Assert.Equal(Path.GetDirectoryName(expectedDatabasePath), ensuredPath);
    }

    [Fact]
    public void StartupConfiguration_RequestHelpers_ReturnExpectedDecisions()
    {
        Assert.True(StartupConfiguration.IsApiRequest(new PathString("/api/employees")));
        Assert.False(StartupConfiguration.IsApiRequest(new PathString("/swagger")));

        Assert.True(StartupConfiguration.ShouldUseSpaProxy(new PathString("/dashboard")));
        Assert.False(StartupConfiguration.ShouldUseSpaProxy(new PathString("/api/health")));
        Assert.False(StartupConfiguration.ShouldUseSpaProxy(new PathString("/swagger/index.html")));
        Assert.False(StartupConfiguration.ShouldUseSpaProxy(new PathString("/health")));
    }

    [Fact]
    public async Task AddWebApiCore_RegistersExpectedOptionsCorsAndDatabasePath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminTools:Enabled"] = "false",
                ["AdminTools:AllowWriteSql"] = "false",
                ["AdminTools:MaxSqlLength"] = "500",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddWebApiCore(
            configuration,
            readEnvironmentVariable: name => name switch
            {
                "GF3_ADMIN_ENABLED" => "true",
                "GF3_ADMIN_ALLOW_REMOTE" => "true",
                _ => null,
            },
            localApplicationDataRoot: tempRoot,
            ensureDirectory: _ => { });

        using var provider = services.BuildServiceProvider();

        var options = WebApiServiceCollectionExtensions.ResolveAdminToolsOptions(provider);
        var corsPolicy = WebApiServiceCollectionExtensions.ResolveFrontendDevCorsPolicy(provider);
        var databasePath = WebApiServiceCollectionExtensions.ResolveRegisteredDatabasePath(provider);
        var jwtOptions = provider.GetRequiredService<IOptions<JwtAuthOptions>>().Value;
        var compressionOptions = provider.GetRequiredService<IOptions<ResponseCompressionOptions>>().Value;
        var authScheme = await provider
            .GetRequiredService<IAuthenticationSchemeProvider>()
            .GetSchemeAsync(JwtAuthenticationDefaults.SchemeName);
        var editLockService = provider.GetRequiredService<IManagerEditLockService>();
        var scheduleEditLockService = provider.GetRequiredService<IScheduleEditLockService>();

        Assert.True(options.Enabled);
        Assert.True(options.AllowRemoteAccess);
        Assert.False(options.AllowWriteSql);
        Assert.Equal(500, options.MaxSqlLength);
        Assert.NotNull(corsPolicy);
        Assert.Contains(corsPolicy!.Origins, origin => origin == "http://localhost:5173");
        Assert.True(corsPolicy.SupportsCredentials);
        Assert.Contains("*", corsPolicy.Headers);
        Assert.Contains("*", corsPolicy.Methods);
        Assert.EndsWith(Path.Combine("GF3", "SQLite.db"), databasePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("GF3.WebApi", jwtOptions.Issuer);
        Assert.Equal("GF3.FrontEnd", jwtOptions.Audience);
        Assert.False(string.IsNullOrWhiteSpace(jwtOptions.SigningKey));
        Assert.NotNull(authScheme);
        Assert.True(compressionOptions.EnableForHttps);
        Assert.Contains("application/problem+json", compressionOptions.MimeTypes);
        Assert.Same(editLockService, scheduleEditLockService);
        Assert.Contains(provider.GetServices<IHostedService>(), service => service is DatabaseAutoBackupHostedService);
        Assert.NotNull(provider.GetRequiredService<IJwtTokenService>());
        Assert.NotNull(provider.GetRequiredService<IEmailSender>());
        Assert.NotNull(provider.GetRequiredService<IWorkflowLogService>());
        Assert.NotNull(provider.GetRequiredService<IRealtimeNotifier>());
    }
}
