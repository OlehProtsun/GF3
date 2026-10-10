using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Security;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Abstractions;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Options;
using WebApi.Realtime;

namespace GF3.Tests;

public sealed class ReviewSecurityTests
{
    [Fact]
    public async Task PasswordReset_ReturnsSuccessAfterLogFailure_AndConsumesCodeOnce()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var employee = new EmployeeModel { FirstName = "Reset", LastName = "Worker" };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        var accounts = CreateEmployeeService(db);
        await accounts.UpsertForEmployeeAsync(employee.Id, "reset.worker", "111111");
        var challenge = await accounts.CreatePasswordResetChallengeAsync(employee.Id);
        var service = new EmployeeProfileService(new EmployeeService(new EmployeeRepository(db)), accounts,
            System.Reflection.DispatchProxy.Create<IEmailSender, ShiftCorrectionControllerTests.FailingPostCommitProxy>());
        var controller = new WebApi.Controllers.EmployeeProfileController(service,
            System.Reflection.DispatchProxy.Create<WebApi.Services.IWorkflowLogService, ShiftCorrectionControllerTests.FailingPostCommitProxy>());
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                    new System.Security.Claims.Claim("employee_id", employee.Id.ToString()),
                ], "test")),
            },
        };
        var request = new WebApi.Contracts.EmployeeProfile.CompletePasswordResetRequest { Code = challenge.Code, NewPassword = "222222" };
        Assert.IsType<Microsoft.AspNetCore.Mvc.NoContentResult>(await controller.ConfirmPasswordReset(request, CancellationToken.None));
        var updated = await accounts.GetByEmployeeIdAsync(employee.Id);
        Assert.True(new PasswordHasher().VerifyPassword("222222", updated!.PasswordHash));
        Assert.False(new PasswordHasher().VerifyPassword("111111", updated.PasswordHash));
        await Assert.ThrowsAsync<BusinessLogicLayer.Common.ValidationException>(() => controller.ConfirmPasswordReset(request, CancellationToken.None));
    }

    [Theory]
    [InlineData("login", 10)]
    [InlineData("reset-code", 3)]
    [InlineData("reset-confirm", 5)]
    public async Task AttemptFilter_BlocksAccountAcrossAddresses(string operation, int budget)
    {
        var limiter = new AuthAttemptLimiter(TimeProvider.System);
        var filter = new AuthAttemptFilter(limiter, operation);
        var calls = 0;
        for (var i = 0; i <= budget; i++)
        {
            var http = new DefaultHttpContext();
            http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse($"192.0.2.{i + 1}");
            var action = new Microsoft.AspNetCore.Mvc.ActionContext(http, new(), new(), new());
            var context = new Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext(action, [],
                new Dictionary<string, object?> { ["request"] = new WebApi.Contracts.Auth.LoginRequest { Username = i % 2 == 0 ? " Worker " : "worker", Password = "000000" } }, new object());
            await filter.OnActionExecutionAsync(context, () =>
            {
                calls++;
                return Task.FromResult(new Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext(action, [], new object()));
            });
            if (i == budget)
            {
                Assert.Equal(429, Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(context.Result).StatusCode);
                Assert.True(int.Parse(http.Response.Headers.RetryAfter.ToString()) > 0);
            }
        }
        Assert.Equal(budget, calls);
    }
    private static readonly JwtAuthOptions JwtOptions = new()
    {
        SigningKey = "review-security-tests-signing-key-2026",
        Issuer = "tests", Audience = "tests",
    };

    [Fact]
    public async Task ManagerTokens_RequireCurrentCredentialsAndExistingAccount()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var account = new ManagerAccountModel
        {
            Username = "review.manager", DisplayName = "Review manager", PasswordHash = "unused",
            PasswordUpdatedAtUtc = DateTimeOffset.UtcNow, CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.ManagerAccounts.Add(account);
        await db.SaveChangesAsync();
        var session = new AuthenticatedSessionDto
        {
            UserName = account.Username, Role = AuthRoles.Manager, ManagerId = account.Id,
            CredentialVersion = account.PasswordUpdatedAtUtc.UtcTicks,
        };
        var token = new JwtTokenService(Options.Create(JwtOptions)).CreateAccessToken(session).AccessToken;
        var authenticated = await Authenticate(db, token);
        Assert.True(authenticated.Succeeded);
        Assert.True(JwtTokenCodec.TryReadAccessToken(token, JwtOptions, TimeSpan.Zero, out _, out _, out var expiration));
        Assert.Equal(expiration, authenticated.Properties!.ExpiresUtc);
        account.PasswordUpdatedAtUtc = account.PasswordUpdatedAtUtc.AddTicks(1);
        await db.SaveChangesAsync();
        Assert.False((await Authenticate(db, token)).Succeeded);
        session.CredentialVersion = account.PasswordUpdatedAtUtc.UtcTicks;
        token = new JwtTokenService(Options.Create(JwtOptions)).CreateAccessToken(session).AccessToken;
        Assert.True((await Authenticate(db, token)).Succeeded);
        db.ManagerAccounts.Remove(account);
        await db.SaveChangesAsync();
        Assert.False((await Authenticate(db, token)).Succeeded);
    }

    [Fact]
    public async Task PasswordChangeAndRecovery_InvalidateEmployeeTokens()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var employee = new EmployeeModel { FirstName = "Review", LastName = "Worker" };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        var service = CreateEmployeeService(db);
        var account = await service.UpsertForEmployeeAsync(employee.Id, "review.worker", "111111");
        string Token(int version) => new JwtTokenService(Options.Create(JwtOptions)).CreateAccessToken(new AuthenticatedSessionDto
        {
            UserName = "review.worker", Role = AuthRoles.Employee, EmployeeId = employee.Id, SessionVersion = version,
        }).AccessToken;
        var token = Token(account!.SessionVersion);
        Assert.True((await Authenticate(db, token)).Succeeded);
        account = await service.UpsertForEmployeeAsync(employee.Id, "review.worker", "222222");
        Assert.False((await Authenticate(db, token)).Succeeded);
        token = Token(account!.SessionVersion);
        var challenge = await service.CreatePasswordResetChallengeAsync(employee.Id);
        await service.CompletePasswordResetAsync(employee.Id, challenge.Code, "333333");
        Assert.False((await Authenticate(db, token)).Succeeded);
        var updated = await service.GetByEmployeeIdAsync(employee.Id);
        Assert.True((await Authenticate(db, Token(updated!.SessionVersion))).Succeeded);
    }

    [Theory]
    [InlineData("/api/employees", false)]
    [InlineData("/api/realtime/presence", true)]
    [InlineData("/api/realtime/presence/negotiate", true)]
    [InlineData("/api/realtime/presence-other", false)]
    public async Task QueryToken_IsLimitedToRealtimePath(string path, bool expected)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var employee = new EmployeeModel { FirstName = "Query", LastName = "Token" };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        await CreateEmployeeService(db).UpsertForEmployeeAsync(employee.Id, "query.worker", "111111");
        var token = new JwtTokenService(Options.Create(JwtOptions)).CreateAccessToken(new AuthenticatedSessionDto
        {
            UserName = "query.worker", Role = AuthRoles.Employee, EmployeeId = employee.Id, SessionVersion = 0,
        }).AccessToken;
        Assert.Equal(expected, (await Authenticate(db, token, path)).Succeeded);
    }

    [Fact]
    public void AttemptBudget_IsAtomicAndRecoversAfterWindow()
    {
        var clock = new TestClock();
        var limiter = new AuthAttemptLimiter(clock);
        var accepted = 0;
        Parallel.For(0, 30, attempt =>
        {
            if (limiter.TryAcquire("account:reset:user", 5, TimeSpan.FromMinutes(15), out _))
                Interlocked.Increment(ref accepted);
        });
        Assert.Equal(5, accepted);
        Assert.True(limiter.TryAcquire("account:reset:other", 5, TimeSpan.FromMinutes(15), out _));
        clock.Now += TimeSpan.FromMinutes(15);
        Assert.True(limiter.TryAcquire("account:reset:user", 5, TimeSpan.FromMinutes(15), out _));
    }

    private static EmployeeAccountService CreateEmployeeService(AppDbContext db)
        => new(new EmployeeAccountRepository(db), new ManagerAccountRepository(db), new PasswordHasher());

    private static async Task<AuthenticateResult> Authenticate(AppDbContext db, string token, string? queryPath = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(JwtOptions));
        services.AddSingleton<IManagerAccountRepository>(new ManagerAccountRepository(db));
        services.AddSingleton<IEmployeeAccountService>(CreateEmployeeService(db));
        services.AddAuthentication(JwtAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>(JwtAuthenticationDefaults.SchemeName, _ => { });
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        if (queryPath is null) context.Request.Headers.Authorization = $"Bearer {token}";
        else
        {
            context.Request.Path = queryPath;
            context.Request.QueryString = QueryString.Create("access_token", token);
        }
        return await context.AuthenticateAsync(JwtAuthenticationDefaults.SchemeName);
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
