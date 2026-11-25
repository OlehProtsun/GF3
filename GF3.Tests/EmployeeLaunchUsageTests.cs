using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Abstractions;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Contracts.EmployeeLaunches;
using WebApi.Controllers;
using WebApi.Infrastructure;
using WebApi.Options;
using WebApi.Realtime;
using WebApi.Services;

namespace GF3.Tests;

public sealed class EmployeeLaunchUsageTests
{
    private const string Endpoint = "/api/employee-launches";
    private static readonly DateTimeOffset InitialTime = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordsOnlySignedEmployee_AndRestrictsUsageToSystemManagers()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var system = Manager("system", true);
        var ordinary = Manager("ordinary", false);
        await using (var db = database.CreateContext())
        {
            db.AddRange(Employee(1, "Adam", hasAccount: true), Employee(2, "Beata", hasAccount: true),
                Employee(3, "Celia"), Employee(4, "Deleted", deleted: true, hasAccount: true), system, ordinary);
            await db.SaveChangesAsync();
            await new WorkflowLogService(db, new NoopRealtimeNotifier()).UpdateSettingsAsync(false, "all");
        }

        var presence = new EmployeePresenceService();
        presence.ConnectEmployee(1, "test-connection", InitialTime);
        var clock = new TestClock();
        var options = Options.Create(new JwtAuthOptions { Issuer = "tests", Audience = "tests", SigningKey = "launch-usage-tests-signing-key-with-enough-entropy" });
        var tokens = new JwtTokenService(options);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDbContext<AppDbContext>(config => config.UseSqlite(database.ConnectionString));
        builder.Services.AddScoped<IManagerAccountRepository, ManagerAccountRepository>();
        builder.Services.AddScoped<IWorkflowLogService, WorkflowLogService>();
        builder.Services.AddScoped<EmployeeLaunchService>();
        builder.Services.AddSingleton<IRealtimeNotifier, FailingNotifier>();
        builder.Services.AddSingleton<IEmployeePresenceService>(presence);
        builder.Services.AddSingleton<TimeProvider>(clock);
        builder.Services.AddSingleton(options);
        builder.Services.AddScoped<ApiExceptionFilter>();
        builder.Services.AddAuthentication(JwtAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>(JwtAuthenticationDefaults.SchemeName, _ => { });
        builder.Services.AddAuthorization(config => config.AddPolicy(AuthPolicies.SystemManager, policy =>
            policy.RequireAuthenticatedUser().RequireRole(AuthRoles.Manager).RequireClaim(AuthPolicies.SystemManagerClaim, "true")));
        builder.Services.AddControllers(config => config.Filters.AddService<ApiExceptionFilter>())
            .AddApplicationPart(typeof(EmployeeLaunchesController).Assembly);
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        void Authenticate(AuthenticatedSessionDto session) => client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.CreateAccessToken(session).AccessToken);
        Task<HttpResponseMessage> Record(string mode) => client.PostAsJsonAsync(Endpoint, new { mode, employeeId = 2 });

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Record("pwa")).StatusCode);
        foreach (var mode in new[] { ManagerWorkspaceModes.Pc, ManagerWorkspaceModes.Phone })
        {
            Authenticate(ManagerSession(ordinary, mode));
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Endpoint)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await Record("pwa")).StatusCode);
        }

        Authenticate(EmployeeSession(null));
        Assert.Equal(HttpStatusCode.BadRequest, (await Record("pwa")).StatusCode);
        foreach (var employeeId in new[] { 3, 4, 99 })
        {
            Authenticate(EmployeeSession(employeeId));
            Assert.Equal(HttpStatusCode.NotFound, (await Record("pwa")).StatusCode);
        }
        Authenticate(EmployeeSession(1));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Endpoint)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Record("PWA")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Record("browser")).StatusCode);
        await using (var db = database.CreateContext())
        {
            Assert.Empty(await db.WorkflowLogEntries.ToListAsync());
            await new WorkflowLogService(db, new NoopRealtimeNotifier()).UpdateSettingsAsync(true, "all");
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER reject_launch_log BEFORE INSERT ON workflow_log_entry
                BEGIN SELECT RAISE(ABORT, 'test logging failure'); END;
                """);
        }
        clock.Now = InitialTime.AddMinutes(1);
        Assert.Equal(HttpStatusCode.NoContent, (await Record("pwa")).StatusCode);

        Authenticate(ManagerSession(system, ManagerWorkspaceModes.Pc));
        Assert.Equal(HttpStatusCode.Forbidden, (await Record("pwa")).StatusCode);
        using var response = await client.GetAsync(Endpoint);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("synthetic-hash", body);
        var employees = JsonSerializer.Deserialize<List<EmployeeLaunchUsageDto>>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal([1, 2, 3], employees.Select(employee => employee.EmployeeId));
        var usage = employees[0];
        Assert.True(usage.IsOnline);
        Assert.Equal(InitialTime, usage.LastBrowserLaunchAtUtc);
        Assert.Equal(clock.Now, usage.LastPwaLaunchAtUtc);
        Assert.Equal(clock.Now, usage.LastLaunchAtUtc);
        Assert.Equal("pwa", usage.LastLaunchMode);
        Assert.Equal(InitialTime.AddDays(-1), usage.LastLoginAtUtc);
        Assert.Null(employees[1].LastLaunchMode);
        Assert.False(employees[2].HasLoginAccount);
    }

    [Fact]
    public async Task MigrationPreservesExistingAccountMetadata()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var employee = Employee(1, "Adam", hasAccount: true);
        db.Add(employee);
        await db.SaveChangesAsync();
        var prior = db.Database.GetMigrations().Single(name => name.EndsWith("_AllowDisjointOpenSwapIntervals"));
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(prior);
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        var account = await db.EmployeeAccounts.SingleAsync();
        Assert.Equal("synthetic-hash", account.PasswordHash);
        Assert.Equal(InitialTime.AddDays(-1), account.LastLoginAtUtc);
        Assert.Equal(InitialTime.AddHours(-1), account.LastSeenAtUtc);
        Assert.Null(account.LastBrowserLaunchAtUtc);
        Assert.Null(account.LastPwaLaunchAtUtc);
    }

    private static EmployeeModel Employee(int id, string name, bool deleted = false, bool hasAccount = false)
        => new() { Id = id, FirstName = name, LastName = "Test", IsDeleted = deleted, Account = hasAccount
            ? new EmployeeAccountModel { Username = name.ToLowerInvariant(), PasswordHash = "synthetic-hash",
                LastLoginAtUtc = InitialTime.AddDays(-1), LastSeenAtUtc = InitialTime.AddHours(-1) } : null };

    private static ManagerAccountModel Manager(string username, bool system) => new()
    { Username = username, DisplayName = username, PasswordHash = "synthetic-hash", PasswordUpdatedAtUtc = InitialTime, IsSystem = system };

    private static AuthenticatedSessionDto EmployeeSession(int? id) => new() { Role = AuthRoles.Employee, UserName = "employee", EmployeeId = id };

    private static AuthenticatedSessionDto ManagerSession(ManagerAccountModel account, string mode) => new()
    { Role = AuthRoles.Manager, UserName = account.Username, ManagerId = account.Id, WorkspaceMode = mode,
        CredentialVersion = account.PasswordUpdatedAtUtc.UtcTicks };

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = InitialTime;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FailingNotifier : IRealtimeNotifier
    {
        public Task NotifyManagerDataChangedAsync(string type, string? id, string reason, int? containerId, int? graphId)
            => throw new InvalidOperationException("Simulated notification failure.");
        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason) => Task.CompletedTask;
        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId) => Task.CompletedTask;
        public Task NotifyWorkflowLogCreatedAsync(WorkflowLogEntryModel entry) => Task.CompletedTask;
        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state) => Task.CompletedTask;
        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state) => Task.CompletedTask;
    }
}
