using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Reflection;
using System.Text;
using BusinessLogicLayer.Contracts.Database;
using BusinessLogicLayer.Options;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebApi.Contracts.Containers.Graphs;
using WebApi.Auth;
using WebApi.Infrastructure;
using WebApi.Mappers;
using WebApi.Middleware;
using BusinessValidationException = BusinessLogicLayer.Common.ValidationException;
using DataAnnotationsValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace GF3.Tests;

public sealed class AdminAndMiddlewareEdgeCoverageTests
{
    [Fact]
    public async Task AdminDbService_ImportSql_RollsBackAndReportsFailure_WhenLaterStatementFails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var provider = BuildAdminProvider(database);
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDbService>();

        var result = await service.ImportSqlAsync(
            Encoding.UTF8.GetBytes(
                """
                INSERT INTO AvailabilityBinds(Key,Value,IsActive) VALUES ('A','+',1);
                INSERT INTO AvailabilityBinds(Key,Value,IsActive) VALUES ('A','-',1);
                """),
            maxImportBytes: 10_000);
        var countResult = await service.ExecuteQueryAsync(
            "SELECT COUNT(*) AS total FROM AvailabilityBinds;",
            maxSqlLength: 1_000);

        Assert.Equal(1, result.StatementsExecuted);
        Assert.Equal(1, result.StatementsApplied);
        Assert.Equal(0, result.StatementsAlreadyExisted);
        Assert.Equal(0, result.ServiceStatementsSkipped);
        Assert.Equal(1, result.FailedStatementIndex);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
        Assert.Equal(0, Convert.ToInt32(countResult.Rows.Single()[0]));
    }

    [Fact]
    public async Task AdminDbService_ImportSql_RejectsEmptyOversizedAndServiceOnlyScripts()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var provider = BuildAdminProvider(database);
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDbService>();

        var emptyException = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.ImportSqlAsync([], maxImportBytes: 100));
        var oversizedException = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.ImportSqlAsync(new byte[101], maxImportBytes: 100));
        var serviceOnlyException = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.ImportSqlAsync(
                Encoding.UTF8.GetBytes(
                    """
                    -- comment
                    /* comment block */
                    BEGIN TRANSACTION;
                    COMMIT;
                    """),
                maxImportBytes: 1_000));

        Assert.Equal("SQL import file is empty.", emptyException.Message);
        Assert.Equal("SQL import exceeds max size of 100 bytes.", oversizedException.Message);
        Assert.Equal("SQL import does not contain executable write statements.", serviceOnlyException.Message);
    }

    [Fact]
    public async Task AdminDbService_ExecuteSqlValidation_AllowsCommentLikeTokensInsideStringLiterals()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var provider = BuildAdminProvider(database);
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDbService>();

        var affectedRows = await service.ExecuteNonQueryAsync(
            "INSERT INTO AvailabilityBinds(Key,Value,IsActive) VALUES ('L','DROP TABLE -- /* still literal */',1);",
            maxSqlLength: 1_000);
        var queryResult = await service.ExecuteQueryAsync(
            "SELECT '--not-a-comment' AS marker, Value FROM AvailabilityBinds WHERE Key = 'L';",
            maxSqlLength: 1_000);

        Assert.Equal(1, affectedRows);
        Assert.Equal("--not-a-comment", Convert.ToString(queryResult.Rows.Single()[0]));
        Assert.Equal("DROP TABLE -- /* still literal */", Convert.ToString(queryResult.Rows.Single()[1]));
    }

    [Fact]
    public async Task AdminDbService_GetDbHash_UsesSchemaFallback_WhenFacadeDatabasePathMissing()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var provider = database.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var facade = new RecordingSqliteAdminFacade
        {
            DatabasePath = " ",
        };
        var service = new AdminDbService(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            facade,
            scope.ServiceProvider.GetRequiredService<ISqliteDatabaseWorkspace>(),
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>());

        var hash = await service.GetDbHashAsync();

        Assert.Matches("^[a-f0-9]{64}$", hash);
        Assert.Equal(0, facade.ComputeFileHashCalls);
    }

    [Fact]
    public async Task AdminDbService_SelectDatabase_RevertsWorkspace_WhenScopedDbContextResolutionFails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var secondDatabasePath = Path.Combine(database.RootPath, "other.db");
        await File.WriteAllBytesAsync(secondDatabasePath, []);

        var workspace = new SqliteDatabaseWorkspace(database.DatabasePath);
        var service = new AdminDbService(
            context,
            new RecordingSqliteAdminFacade { DatabasePath = database.DatabasePath },
            workspace,
            new ThrowingScopeFactory(new InvalidOperationException("Scoped AppDbContext resolution failed.")));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SelectDatabaseAsync(secondDatabasePath));

        Assert.Equal("Scoped AppDbContext resolution failed.", exception.Message);
        Assert.Equal(Path.GetFullPath(database.DatabasePath), workspace.DatabasePath);
    }

    [Fact]
    public async Task AdminDbService_CreateManualCopy_ReturnsMappedWorkspaceFileEntry()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var provider = BuildAdminProvider(database);
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDbService>();

        var manualCopy = await service.CreateManualCopyAsync();

        Assert.Equal("manualCopy", manualCopy.Category);
        Assert.True(manualCopy.IsActive is false);
        Assert.True(File.Exists(manualCopy.Path));
        Assert.Contains("DatabaseCopyManual", manualCopy.Path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiExceptionMiddleware_MapsBusinessValidationException_ToProblemDetails()
    {
        var httpContext = CreateHttpContext("/api/employees");
        var middleware = new ApiExceptionMiddleware(
            _ => Task.FromException(BusinessValidationException.ForField("Name", "Name is required.")),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        var body = await ReadBodyAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);
        Assert.Contains("\"type\":\"validation_error\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"detail\":\"Name is required.\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiExceptionMiddleware_MapsDataAnnotationsValidationException_ToProblemDetails()
    {
        var httpContext = CreateHttpContext("/api/employees");
        var middleware = new ApiExceptionMiddleware(
            _ => Task.FromException(new DataAnnotationsValidationException(
                new ValidationResult("Bad email.", ["Email"]),
                null,
                null)),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        var body = await ReadBodyAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Contains("\"type\":\"validation_error\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"detail\":\"Bad email.\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiExceptionMiddleware_MapsBadRequestException_ToBadRequestProblem()
    {
        var httpContext = CreateHttpContext("/api/admin/db/query");
        var middleware = new ApiExceptionMiddleware(
            _ => Task.FromException(new BadHttpRequestException("Malformed request body.")),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        var body = await ReadBodyAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Contains("\"type\":\"bad_request\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"detail\":\"Malformed request body.\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiExceptionMiddleware_MapsConcurrencyAndUpdateConflicts_ToConflictProblem()
    {
        var concurrencyContext = CreateHttpContext("/api/containers/1");
        var concurrencyMiddleware = new ApiExceptionMiddleware(
            _ => Task.FromException(new DbUpdateConcurrencyException("Concurrent update.")),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await concurrencyMiddleware.InvokeAsync(concurrencyContext);

        var concurrencyBody = await ReadBodyAsync(concurrencyContext);
        Assert.Equal(StatusCodes.Status409Conflict, concurrencyContext.Response.StatusCode);
        Assert.Contains("\"type\":\"database_conflict\"", concurrencyBody, StringComparison.OrdinalIgnoreCase);

        var updateContext = CreateHttpContext("/api/containers/1");
        var updateMiddleware = new ApiExceptionMiddleware(
            _ => Task.FromException(new DbUpdateException("Rejected by database.")),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await updateMiddleware.InvokeAsync(updateContext);

        var updateBody = await ReadBodyAsync(updateContext);
        Assert.Equal(StatusCodes.Status409Conflict, updateContext.Response.StatusCode);
        Assert.Contains("\"type\":\"database_conflict\"", updateBody, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(KeyNotFoundException), StatusCodes.Status404NotFound, "not_found", "Missing entity.")]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status404NotFound, "not_found", "Missing aggregate.")]
    [InlineData(typeof(Exception), StatusCodes.Status500InternalServerError, "server_error", "An unexpected error occurred.")]
    public async Task ApiExceptionMiddleware_MapsNotFoundAndUnhandledExceptions_ToProblemDetails(
        Type exceptionType,
        int expectedStatus,
        string expectedType,
        string expectedDetail)
    {
        var httpContext = CreateHttpContext("/api/containers/7");
        var exception = (Exception)Activator.CreateInstance(exceptionType, expectedDetail)!;
        var middleware = new ApiExceptionMiddleware(
            _ => Task.FromException(exception),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        var body = await ReadBodyAsync(httpContext);

        Assert.Equal(expectedStatus, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);
        Assert.Contains($"\"type\":\"{expectedType}\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"\"detail\":\"{expectedDetail}\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"instance\":\"/api/containers/7\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiProblemDetailsFactory_TreatsCanceledExceptionToken_AsRequestCancellation()
    {
        var httpContext = CreateHttpContext("/api/test");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var exception = new OperationCanceledException(cts.Token);

        var isCancellation = ApiProblemDetailsFactory.IsRequestCancellation(httpContext, exception);

        Assert.True(isCancellation);
    }

    [Fact]
    public async Task ApiExceptionMiddleware_Uses499ForClientCancellation()
    {
        var httpContext = CreateHttpContext("/api/containers/1");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        httpContext.RequestAborted = cts.Token;

        var middleware = new ApiExceptionMiddleware(
            _ => Task.FromException(new OperationCanceledException(cts.Token)),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(499, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task ApiExceptionMiddleware_SkipsWritingProblemBody_WhenRequestWasAlreadyAborted()
    {
        var httpContext = CreateHttpContext("/api/test");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        httpContext.RequestAborted = cts.Token;

        var middleware = new ApiExceptionMiddleware(
            _ => Task.FromException(new InvalidOperationException("Boom.")),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task ApiExceptionMiddleware_DoesNotWriteProblem_WhenResponseAlreadyStarted()
    {
        var httpContext = CreateHttpContext("/api/test");
        httpContext.Features.Set<IHttpResponseFeature>(new StartedHttpResponseFeature
        {
            Body = httpContext.Response.Body,
        });
        var middleware = new ApiExceptionMiddleware(
            _ => Task.FromException(new Exception("Boom.")),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public void ContainerGraphMapper_MapsCreateUpdateAndDtoShapes()
    {
        var createRequest = new CreateGraphRequest
        {
            ShopId = 4,
            Name = "April Graph",
            Year = 2026,
            Month = 4,
            PeoplePerShift = 2,
            Shift1Time = "08:00 - 12:00",
            Shift2Time = "12:00 - 16:00",
            MaxHoursPerEmpMonth = 120,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 2,
            MaxFullPerMonth = 10,
            Note = "Create note",
            AvailabilityGroupId = 7,
        };
        var updateRequest = new UpdateGraphRequest
        {
            ShopId = 5,
            Name = "April Graph Updated",
            Year = 2026,
            Month = 5,
            PeoplePerShift = 3,
            Shift1Time = "09:00 - 13:00",
            Shift2Time = "13:00 - 17:00",
            MaxHoursPerEmpMonth = 140,
            MaxConsecutiveDays = 6,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 12,
            Note = "Updated note",
            AvailabilityGroupId = 9,
        };

        var createdModel = createRequest.ToContainerCreateGraphModel(containerId: 11);
        var updatedModel = updateRequest.ToContainerUpdateGraphModel(containerId: 11, graphId: 33);
        var dto = updatedModel.ToContainerGraphDto();

        Assert.Equal(11, createdModel.ContainerId);
        Assert.Equal(0, createdModel.Id);
        Assert.Equal("Create note", createdModel.Note);
        Assert.Equal(7, createdModel.AvailabilityGroupId);

        Assert.Equal(33, updatedModel.Id);
        Assert.Equal(11, updatedModel.ContainerId);
        Assert.Equal(5, updatedModel.ShopId);
        Assert.Equal("April Graph Updated", updatedModel.Name);

        Assert.Equal(33, dto.Id);
        Assert.Equal(11, dto.ContainerId);
        Assert.Equal(5, dto.ShopId);
        Assert.Equal("Updated note", dto.Note);
        Assert.Equal(9, dto.AvailabilityGroupId);
    }

    [Fact]
    public void ExportTemplatesOptions_HaveExpectedDefaults()
    {
        var options = new ExportTemplatesOptions();

        Assert.Null(options.TemplateDirectory);
        Assert.Equal("ScheduleTemplate.xlsx", options.ScheduleTemplateFile);
        Assert.Equal("ContainerTemplate.xlsx", options.ContainerTemplateFile);
        Assert.True(options.SeedToLocalAppData);
    }

    [Fact]
    public void ExcelTemplateLocator_ReturnsPreferredPath_FromConfiguredTemplateDirectory()
    {
        var targetDirectory = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        var fileName = $"preferred-{Guid.NewGuid():N}.xlsx";
        var preferredPath = Path.Combine(targetDirectory, fileName);
        var previousValue = Environment.GetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR");

        Directory.CreateDirectory(targetDirectory);
        File.WriteAllBytes(preferredPath, [9, 8, 7]);
        Environment.SetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR", null);

        try
        {
            var locator = new ExcelTemplateLocator(Options.Create(new ExportTemplatesOptions
            {
                TemplateDirectory = targetDirectory,
                ScheduleTemplateFile = fileName,
            }));

            var resolvedPath = locator.GetScheduleTemplatePath();

            Assert.Equal(preferredPath, resolvedPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR", previousValue);
            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void ExcelTemplateLocator_ThrowsFileNotFound_WhenPreferredAndFallbackTemplatesAreMissing()
    {
        var targetDirectory = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        var fileName = $"missing-{Guid.NewGuid():N}.xlsx";
        var previousValue = Environment.GetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR");
        Environment.SetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR", targetDirectory);

        try
        {
            var locator = new ExcelTemplateLocator(Options.Create(new ExportTemplatesOptions
            {
                ContainerTemplateFile = fileName,
            }));

            var exception = Assert.Throws<FileNotFoundException>(locator.GetContainerTemplatePath);

            Assert.Equal(Path.Combine(targetDirectory, fileName), exception.FileName);
            Assert.Contains("template not found path", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR", previousValue);
            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void ExcelTemplateLocator_SeedsPreferredCopy_FromFallbackTemplate()
    {
        var targetDirectory = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        var fileName = $"schedule-{Guid.NewGuid():N}.xlsx";
        var fallbackDirectory = Path.Combine(AppContext.BaseDirectory, "Resources", "Excel");
        var fallbackPath = Path.Combine(fallbackDirectory, fileName);
        var previousValue = Environment.GetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR");

        Directory.CreateDirectory(fallbackDirectory);
        File.WriteAllBytes(fallbackPath, [1, 2, 3, 4]);
        Environment.SetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR", targetDirectory);

        try
        {
            var locator = new ExcelTemplateLocator(Options.Create(new ExportTemplatesOptions
            {
                ScheduleTemplateFile = fileName,
                SeedToLocalAppData = true,
            }));

            var resolvedPath = locator.GetScheduleTemplatePath();

            Assert.Equal(Path.Combine(targetDirectory, fileName), resolvedPath);
            Assert.True(File.Exists(resolvedPath));
            Assert.Equal(File.ReadAllBytes(fallbackPath), File.ReadAllBytes(resolvedPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR", previousValue);
            if (File.Exists(fallbackPath))
            {
                File.Delete(fallbackPath);
            }

            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void ExcelTemplateLocator_ReturnsFallbackPath_WhenSeedingIsDisabled()
    {
        var targetDirectory = Path.Combine(Path.GetTempPath(), "GF3.Tests", Guid.NewGuid().ToString("N"));
        var fileName = $"container-{Guid.NewGuid():N}.xlsx";
        var fallbackDirectory = Path.Combine(AppContext.BaseDirectory, "Resources", "ExcelTemplate");
        var fallbackPath = Path.Combine(fallbackDirectory, fileName);
        var previousValue = Environment.GetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR");

        Directory.CreateDirectory(fallbackDirectory);
        File.WriteAllBytes(fallbackPath, [5, 6, 7, 8]);
        Environment.SetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR", targetDirectory);

        try
        {
            var locator = new ExcelTemplateLocator(Options.Create(new ExportTemplatesOptions
            {
                ContainerTemplateFile = fileName,
                SeedToLocalAppData = false,
            }));

            var resolvedPath = locator.GetContainerTemplatePath();

            Assert.Equal(fallbackPath, resolvedPath);
            Assert.False(File.Exists(Path.Combine(targetDirectory, fileName)));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GF3_EXPORT_TEMPLATES_DIR", previousValue);
            if (File.Exists(fallbackPath))
            {
                File.Delete(fallbackPath);
            }

            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DatabaseAutoBackupHostedService_StopsCleanly_WhenCancellationWasRequestedBeforeStart()
    {
        var workspace = new RecordingDatabaseWorkspace();
        var service = new DatabaseAutoBackupHostedService(
            workspace,
            NullLogger<DatabaseAutoBackupHostedService>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var executeAsync = typeof(DatabaseAutoBackupHostedService)
            .GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(executeAsync);
        var task = Assert.IsAssignableFrom<Task>(executeAsync!.Invoke(service, [cts.Token]));
        await task;

        Assert.Equal(0, workspace.AutomaticBackupCalls);
    }

    [Fact]
    public async Task EmployeesShopsAndAvailabilityBindsControllers_ThrowNotFound_ForMissingResources()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employeesController = new WebApi.Controllers.EmployeesController(
            TestEmployeeFacadeFactory.Create(new EmployeeService(new DataAccessLayer.Repositories.EmployeeRepository(context))));
        var shopsController = new WebApi.Controllers.ShopsController(
            new ShopFacade(new ShopService(new DataAccessLayer.Repositories.ShopRepository(context))));
        var bindsController = new WebApi.Controllers.AvailabilityBindsController(
            new BindService(new DataAccessLayer.Repositories.BindRepository(context)));
        SetHttpContext(employeesController);
        SetHttpContext(shopsController);
        SetHttpContext(bindsController);
        bindsController.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "manager-1"),
            new Claim(ClaimTypes.Role, AuthRoles.Manager),
            new Claim("manager_id", "1"),
        ], JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role));

        var employeeException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            employeesController.GetById(999, CancellationToken.None));
        var shopException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            shopsController.Update(999, new WebApi.Contracts.Shops.UpdateShopRequest
            {
                Name = "Missing",
                Address = "Nowhere",
                Description = "Missing",
            }, CancellationToken.None));
        var bindException = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            bindsController.Delete(999, CancellationToken.None));

        Assert.Equal("Employee with id 999 was not found.", employeeException.Message);
        Assert.Equal("Shop with id 999 was not found.", shopException.Message);
        Assert.Equal("Availability bind with id 999 was not found.", bindException.Message);
    }

    [Fact]
    public void AppDbContextFactory_UsesEnvironmentConnectionString_WhenProvided()
    {
        var previousValue = Environment.GetEnvironmentVariable("GF3_CONNECTION_STRING");
        var connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), "GF3.Tests", $"{Guid.NewGuid():N}.db")}";
        Environment.SetEnvironmentVariable("GF3_CONNECTION_STRING", connectionString);

        try
        {
            var factory = new AppDbContextFactory();
            using var context = factory.CreateDbContext([]);

            Assert.Equal(connectionString, context.Database.GetConnectionString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("GF3_CONNECTION_STRING", previousValue);
        }
    }

    [Fact]
    public void AppDbContextFactory_BuildsDefaultConnectionString_FromLocalAppData()
    {
        var previousConnectionString = Environment.GetEnvironmentVariable("GF3_CONNECTION_STRING");
        Environment.SetEnvironmentVariable("GF3_CONNECTION_STRING", null);

        try
        {
            var factory = new AppDbContextFactory();
            using var context = factory.CreateDbContext([]);
            var expectedDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GF3");
            var expectedConnectionString = $"Data Source={Path.Combine(expectedDirectory, "SQLite.db")}";

            Assert.Equal(expectedConnectionString, context.Database.GetConnectionString());
            Assert.True(Directory.Exists(expectedDirectory));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GF3_CONNECTION_STRING", previousConnectionString);
        }
    }

    private static ServiceProvider BuildAdminProvider(SqliteTestDatabase database)
        => database.BuildServiceProvider(services =>
        {
            services.AddScoped<ISqliteAdminService, SqliteAdminService>();
            services.AddScoped<ISqliteAdminFacade, SqliteAdminFacade>();
            services.AddScoped<IAdminDbService, AdminDbService>();
        });

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static void SetHttpContext(Microsoft.AspNetCore.Mvc.ControllerBase controller)
    {
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
    }

    private sealed class RecordingSqliteAdminFacade : ISqliteAdminFacade
    {
        public string DatabasePath { get; init; } = string.Empty;

        public int ComputeFileHashCalls { get; private set; }

        public Task<SqlExecutionResultDto> ExecuteSqlAsync(string sql, CancellationToken ct)
            => throw new NotSupportedException();

        public Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<DatabaseInfoDto> GetDatabaseInfoAsync(CancellationToken ct)
            => throw new NotSupportedException();

        public Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
        {
            ComputeFileHashCalls++;
            return Task.FromResult("unexpected");
        }
    }

    private sealed class StartedHttpResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted => true;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }
    }

    private sealed class RecordingDatabaseWorkspace : ISqliteDatabaseWorkspace
    {
        public string DatabasePath { get; private set; } = Path.Combine(Path.GetTempPath(), "GF3.Tests", "hosted.db");

        public string ConnectionString => $"Data Source={DatabasePath}";

        public string WorkspaceRootPath => Path.GetDirectoryName(DatabasePath) ?? string.Empty;

        public string AutomaticBackupDirectoryPath => Path.Combine(WorkspaceRootPath, "backups");

        public string ManualCopyDirectoryPath => Path.Combine(WorkspaceRootPath, "DatabaseCopyManual");

        public int AutomaticBackupRetentionLimit => 10;

        public int AutomaticBackupCalls { get; private set; }

        public string SetDatabasePath(string databasePath)
        {
            DatabasePath = Path.GetFullPath(databasePath);
            return DatabasePath;
        }

        public SqliteDatabaseWorkspaceState GetWorkspaceState() => new()
        {
            WorkspaceRootPath = WorkspaceRootPath,
            AutomaticBackupDirectoryPath = AutomaticBackupDirectoryPath,
            ManualCopyDirectoryPath = ManualCopyDirectoryPath,
            AutomaticBackupRetentionLimit = AutomaticBackupRetentionLimit,
        };

        public Task<SqliteDatabaseFileEntry> CreateAutomaticBackupAsync(CancellationToken ct)
        {
            AutomaticBackupCalls++;
            return Task.FromResult(new SqliteDatabaseFileEntry
            {
                Name = "backup.db",
                Path = Path.Combine(AutomaticBackupDirectoryPath, "backup.db"),
                Category = "backup",
            });
        }

        public Task<SqliteDatabaseFileEntry> CreateManualCopyAsync(CancellationToken ct)
            => Task.FromResult(new SqliteDatabaseFileEntry
            {
                Name = "manual.db",
                Path = Path.Combine(ManualCopyDirectoryPath, "manual.db"),
                Category = "manualCopy",
            });
    }

    private sealed class ThrowingScopeFactory(Exception exception) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new ThrowingScope(exception);

        private sealed class ThrowingScope(Exception exception) : IServiceScope, IAsyncDisposable
        {
            public IServiceProvider ServiceProvider { get; } = new ThrowingServiceProvider(exception);

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private sealed class ThrowingServiceProvider(Exception exception) : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(AppDbContext))
                {
                    throw exception;
                }

                return null;
            }
        }
    }
}
