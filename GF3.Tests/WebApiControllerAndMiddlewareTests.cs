using System.Text.Json;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Administration;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebApi.Controllers;
using WebApi.Contracts.AdminDb;
using WebApi.Contracts.AvailabilityBinds;
using WebApi.Contracts.AvailabilityGroups;
using WebApi.Contracts.AvailabilityGroups.Members;
using WebApi.Contracts.AvailabilityGroups.Slots;
using WebApi.Contracts.Employees;
using WebApi.Infrastructure;
using WebApi.Middleware;
using WebApi.Options;

namespace GF3.Tests;

public sealed class WebApiControllerAndMiddlewareTests
{
    [Fact]
    public async Task ApiExceptionFilter_MapsValidationExceptions_ToBadRequestProblem()
    {
        var httpContext = CreateHttpContext();
        var filter = new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance);
        var context = new ExceptionContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            [])
        {
            Exception = ValidationException.ForField("Name", "Name is required."),
        };

        await filter.OnExceptionAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.True(context.ExceptionHandled);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(["Name is required."], problem.Errors["Name"]);
    }

    [Fact]
    public async Task ApiExceptionFilter_MapsClientCancellation_To499()
    {
        var httpContext = CreateHttpContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        httpContext.RequestAborted = cts.Token;

        var filter = new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance);
        var context = new ExceptionContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            [])
        {
            Exception = new OperationCanceledException(cts.Token),
        };

        await filter.OnExceptionAsync(context);

        Assert.True(context.ExceptionHandled);
        Assert.Equal(499, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ApiExceptionMiddleware_WritesStructuredProblemResponse()
    {
        var httpContext = CreateHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var middleware = new ApiExceptionMiddleware(
            _ => Task.FromException(new KeyNotFoundException("Missing resource.")),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var jsonDocument = await JsonDocument.ParseAsync(httpContext.Response.Body);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);
        Assert.Equal("not_found", jsonDocument.RootElement.GetProperty("type").GetString());
        Assert.Equal("Missing resource.", jsonDocument.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AdminToolsGuardMiddleware_BlocksDisabledAndRemoteRequests()
    {
        var options = Options.Create(new AdminToolsOptions
        {
            Enabled = false,
            AllowRemoteAccess = false,
        });

        var middleware = new AdminToolsGuardMiddleware(
            _ => Task.CompletedTask,
            new OptionsMonitorStub<AdminToolsOptions>(options.Value));

        var disabledContext = CreateHttpContext("/api/admin/db/metadata");
        await middleware.InvokeAsync(disabledContext);
        Assert.Equal(StatusCodes.Status404NotFound, disabledContext.Response.StatusCode);

        var remoteMiddleware = new AdminToolsGuardMiddleware(
            _ => Task.CompletedTask,
            new OptionsMonitorStub<AdminToolsOptions>(new AdminToolsOptions
            {
                Enabled = true,
                AllowRemoteAccess = false,
            }));

        var remoteContext = CreateHttpContext("/api/admin/db/metadata");
        remoteContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.50");
        await remoteMiddleware.InvokeAsync(remoteContext);
        Assert.Equal(StatusCodes.Status403Forbidden, remoteContext.Response.StatusCode);
    }

    [Fact]
    public async Task HealthController_ReturnsOkAndDatabaseConnectivity()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = new HealthController(context);

        var result = await controller.Get(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);

        Assert.Contains("\"status\":\"ok\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"canConnect\":true", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmployeesController_SupportsCreateGetUpdateAndDelete()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var controller = new EmployeesController(
            TestEmployeeFacadeFactory.Create(new EmployeeService(new DataAccessLayer.Repositories.EmployeeRepository(context))));
        SetHttpContext(controller);

        var createResult = await controller.Create(new CreateEmployeeRequest
        {
            FirstName = "Alice",
            LastName = "Brown",
            Email = "alice@example.com",
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var dto = Assert.IsType<WebApi.Contracts.Employees.EmployeeDto>(created.Value);

        var getResult = await controller.GetById(dto.Id, CancellationToken.None);
        var getOk = Assert.IsType<OkObjectResult>(getResult.Result);
        Assert.Equal(dto.Id, Assert.IsType<WebApi.Contracts.Employees.EmployeeDto>(getOk.Value).Id);

        var updateResult = await controller.Update(dto.Id, new UpdateEmployeeRequest
        {
            FirstName = "Alicia",
            LastName = "Brown",
            Email = "alice@example.com",
        }, CancellationToken.None);
        Assert.IsType<NoContentResult>(updateResult);

        var deleteResult = await controller.Delete(dto.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);
    }

    [Fact]
    public async Task EmployeesController_Delete_ReturnsBadRequest_WhenDeleteGuardFails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee("Alice", "Brown");
        var container = TestDataFactory.CreateDalContainer();
        var shop = TestDataFactory.CreateDalShop();
        context.AddRange(employee, container, shop);
        await context.SaveChangesAsync();

        var schedule = TestDataFactory.CreateDalSchedule(container.Id, shop.Id);
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();
        context.ScheduleSlots.Add(TestDataFactory.CreateDalSlot(schedule.Id, 1, 1, employee.Id, "08:00", "12:00"));
        await context.SaveChangesAsync();

        var controller = new EmployeesController(
            TestEmployeeFacadeFactory.Create(new EmployeeService(new DataAccessLayer.Repositories.EmployeeRepository(context))));
        SetHttpContext(controller);

        var result = await controller.Delete(employee.Id, CancellationToken.None);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);

        Assert.Equal(["To delete this employee, first delete all Availability and Schedule entries where this employee is used."], problem.Errors["general"]);
    }

    [Fact]
    public async Task AvailabilityBindsController_SupportsCreateAndDelete()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var controller = new AvailabilityBindsController(
            new BindService(new DataAccessLayer.Repositories.BindRepository(context)));
        SetHttpContext(controller);

        var createResult = await controller.Create(new CreateAvailabilityBindRequest
        {
            Key = "A",
            Value = "+",
            IsActive = true,
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var dto = Assert.IsType<WebApi.Contracts.AvailabilityBinds.AvailabilityBindDto>(created.Value);

        var deleteResult = await controller.Delete(dto.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);
    }

    [Fact]
    public async Task AvailabilityGroupsController_SupportsMembersSlotsAndItems()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var employee = TestDataFactory.CreateDalEmployee();
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var controller = new AvailabilityGroupsController(
            new AvailabilityGroupService(
                new DataAccessLayer.Repositories.AvailabilityGroupRepository(context),
                new DataAccessLayer.Repositories.AvailabilityGroupMemberRepository(context),
                new DataAccessLayer.Repositories.AvailabilityGroupDayRepository(context)));
        SetHttpContext(controller);

        var groupResult = await controller.Create(new CreateAvailabilityGroupRequest
        {
            Name = "April",
            Year = 2026,
            Month = 4,
        }, CancellationToken.None);
        var groupCreated = Assert.IsType<CreatedAtActionResult>(groupResult.Result);
        var group = Assert.IsType<WebApi.Contracts.AvailabilityGroups.AvailabilityGroupDto>(groupCreated.Value);

        var memberResult = await controller.CreateMember(group.Id, new CreateAvailabilityGroupMemberRequest
        {
            EmployeeId = employee.Id,
            DisplayOrder = 1,
        }, CancellationToken.None);
        var memberCreated = Assert.IsType<CreatedAtActionResult>(memberResult.Result);
        var member = Assert.IsType<WebApi.Contracts.AvailabilityGroups.Members.AvailabilityGroupMemberDto>(memberCreated.Value);

        var slotResult = await controller.CreateSlot(group.Id, new CreateAvailabilitySlotRequest
        {
            AvailabilityGroupMemberId = member.Id,
            DayOfMonth = 3,
            Kind = BusinessLogicLayer.Contracts.Enums.AvailabilityKind.ANY,
        }, CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(slotResult.Result);

        var itemsResult = await controller.GetItems(group.Id, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(itemsResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<WebApi.Contracts.AvailabilityGroups.AvailabilityGroupItemDto>>(ok.Value);

        Assert.Single(items);
        Assert.Equal(employee.Id, items.Single().EmployeeId);
    }

    [Fact]
    public async Task AdminDbController_QueryAndExecute_RespectOptions()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var provider = BuildAdminProvider(database);
        await using var scope = provider.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<IAdminDbService>();
        var controller = new AdminDbController(
            service,
            Options.Create(new AdminToolsOptions
            {
                AllowWriteSql = false,
                MaxSqlLength = 500,
                MaxImportBytes = 5000,
            }));

        var queryResult = await controller.Query(new AdminDbSqlRequest { Sql = "SELECT name FROM sqlite_master;" }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(queryResult);

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            controller.Execute(new AdminDbSqlRequest { Sql = "DELETE FROM employees;" }, CancellationToken.None));
    }

    private static DefaultHttpContext CreateHttpContext(string path = "/api/test")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = path;
        return context;
    }

    private static void SetHttpContext(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(),
        };
    }

    private static ServiceProvider BuildAdminProvider(SqliteTestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISqliteDatabaseWorkspace>(_ => new SqliteDatabaseWorkspace(database.DatabasePath));
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var workspace = serviceProvider.GetRequiredService<ISqliteDatabaseWorkspace>();
            options.UseSqlite(workspace.ConnectionString);
        });
        services.AddScoped<ISqliteAdminService, SqliteAdminService>();
        services.AddScoped<ISqliteAdminFacade, SqliteAdminFacade>();
        services.AddScoped<IAdminDbService, AdminDbService>();
        return services.BuildServiceProvider();
    }

    private sealed class OptionsMonitorStub<TOptions> : IOptionsMonitor<TOptions>
    {
        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();

            public void Dispose()
            {
            }
        }

        public OptionsMonitorStub(TOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public TOptions CurrentValue { get; }

        public TOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<TOptions, string?> listener) => EmptyDisposable.Instance;
    }
}
