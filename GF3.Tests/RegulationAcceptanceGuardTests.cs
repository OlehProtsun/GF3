using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BusinessLogicLayer.Contracts.Regulations;
using BusinessLogicLayer.Security;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Controllers;
using WebApi.Infrastructure;
using WebApi.Middleware;

namespace GF3.Tests;

public sealed class RegulationAcceptanceGuardTests
{
    [Theory]
    [InlineData("manager")]
    [InlineData("employee")]
    public async Task PublishedVersions_BlockUntilAccepted_AndKeepImmutableEvidence(string role)
    {
        await using var host = await Fixture.CreateAsync();
        await host.Login(role);
        var businessPaths = role == "employee"
            ? new[] { "/api/employee-schedules", "/api/employee-shift-swaps" }
            : new[] { "/api/containers/1/graphs", "/api/containers/1/export/sql" };
        foreach (var path in businessPaths)
            Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/guard-business")).StatusCode);
        var id = await host.Publish("1.0");
        var blocked = await host.Client.GetAsync("/api/guard-business");
        Assert.Equal((HttpStatusCode)428, blocked.StatusCode);
        Assert.Equal("application/problem+json", blocked.Content.Headers.ContentType?.MediaType);
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("regulations_acceptance_required", body.GetProperty("code").GetString());
        Assert.Equal("/api/regulations/pending", body.GetProperty("pendingUrl").GetString());
        Assert.Equal("Accept the currently published required documents to continue.", body.GetProperty("detail").GetString());
        Assert.DoesNotContain("synthetic", await blocked.Content.ReadAsStringAsync());
        foreach (var path in businessPaths)
            Assert.Equal((HttpStatusCode)428, (await host.Client.GetAsync(path)).StatusCode);
        foreach (var path in new[] { "/api/regulations/pending", "/api/regulations/history/me", $"/api/regulations/{id}/pdf", "/api/auth/session", "/api/health" })
            Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(path)).StatusCode);
        var accepted = await host.Client.PostAsync($"/api/regulations/{id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var repeated = await host.Client.PostAsync($"/api/regulations/{id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(await accepted.Content.ReadAsStringAsync(), await repeated.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/guard-business")).StatusCode);
        foreach (var path in businessPaths)
            Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(path)).StatusCode);
        await host.Publish("2.0");
        Assert.Equal((HttpStatusCode)428, (await host.Client.GetAsync("/api/guard-business")).StatusCode);
        var history = await host.Client.GetFromJsonAsync<JsonElement>("/api/regulations/history/me");
        Assert.Single(history.EnumerateArray());
    }

    [Fact]
    public async Task AnonymousAndPolicyFailures_PreserveAuthenticationAndAuthorization()
    {
        await using var host = await Fixture.CreateAsync();
        await host.Publish("1.0");
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/guard-business")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/health")).StatusCode);
        await host.Login("manager");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/admin/regulations")).StatusCode);
        await host.Login("system");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/admin/regulations")).StatusCode);
        host.Client.DefaultRequestHeaders.Add("X-GF3-Developer-Password", "synthetic-developer");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/admin/regulations")).StatusCode);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Synthetic rules"), "Title");
        form.Add(new StringContent("2.0"), "Version");
        form.Add(new StringContent("Read synthetic PDF"), "Message");
        form.Add(new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.7 synthetic")), "Pdf", "rules.pdf");
        var created = await host.Client.PostAsync("/api/admin/regulations", form);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var doc = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = doc.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync($"/api/admin/regulations/{id}/publish", null)).StatusCode);
        Assert.Equal((HttpStatusCode)428, (await host.Client.GetAsync("/api/guard-business")).StatusCode);
    }

    [Fact]
    public async Task UnpublishedAndMissingDocumentsCannotBeAccepted()
    {
        await using var host = await Fixture.CreateAsync();
        await host.Login("employee");
        var draft = await host.CreateDocument("draft", false);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync($"/api/regulations/{draft}/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync($"/api/regulations/{draft}/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.PostAsync("/api/regulations/2147483647/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/guard-business")).StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/regulations/1/accept")]
    [InlineData("POST", "/api/regulations/pending")]
    [InlineData("GET", "/api/regulations/0/pdf")]
    [InlineData("GET", "/api/regulations/1/pdf/extra")]
    [InlineData("GET", "/api/authentic/business")]
    [InlineData("GET", "/api/admin/regulations")]
    public async Task NonRecoveryPathsAreNotExempt(string method, string path)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var service = new BusinessLogicLayer.Services.RegulationService(new DataAccessLayer.Repositories.RegulationRepository(db));
        var doc = await service.CreateAsync(Request("1.0"), null, "synthetic");
        await service.PublishAsync(doc.Id);
        using var provider = new ServiceCollection().AddSingleton<IRegulationService>(service).BuildServiceProvider();
        var context = Context(provider, "manager", path, method);
        var reached = false;
        await new RegulationAcceptanceGuardMiddleware(_ => { reached = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.False(reached);
        Assert.Equal(428, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/API/REGULATIONS/1/PDF")]
    [InlineData("POST", "/api/regulations/1/accept")]
    [InlineData("GET", "/api/auth/session")]
    [InlineData("GET", "/legal/index.html")]
    public async Task ExemptPathsDoNotResolveDatabase(string method, string path)
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var context = Context(provider, "employee", path, method);
        var reached = false;
        await new RegulationAcceptanceGuardMiddleware(_ => { reached = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.True(reached);
    }

    [Theory]
    [InlineData("manager", "0")]
    [InlineData("employee", "invalid")]
    [InlineData("unknown", "1")]
    public async Task InvalidSubjectLeavesDecisionToExistingAuthorization(string role, string id)
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var context = Context(provider, role, "/api/guard-business", "GET", id);
        var reached = false;
        await new RegulationAcceptanceGuardMiddleware(_ => { reached = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.True(reached);
    }

    [Fact]
    public async Task DatabaseFailureUsesExceptionMiddlewareAndNeverExecutesBusinessEndpoint()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={Path.Combine(database.RootPath, "missing", "unavailable.db")}").Options);
        var service = new BusinessLogicLayer.Services.RegulationService(new DataAccessLayer.Repositories.RegulationRepository(db));
        using var provider = new ServiceCollection().AddSingleton<IRegulationService>(service).BuildServiceProvider();
        var context = Context(provider, "employee", "/api/guard-business", "GET");
        var reached = false;
        var guard = new RegulationAcceptanceGuardMiddleware(_ => { reached = true; return Task.CompletedTask; });
        await new ApiExceptionMiddleware(guard.InvokeAsync, Microsoft.Extensions.Logging.Abstractions.NullLogger<ApiExceptionMiddleware>.Instance).InvokeAsync(context);
        Assert.False(reached);
        Assert.Equal(500, context.Response.StatusCode);
    }

    private static DefaultHttpContext Context(IServiceProvider provider, string role, string path, string method, string id = "1")
    {
        var context = new DefaultHttpContext { RequestServices = provider };
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Role, role), new Claim(role == "manager" ? "manager_id" : "employee_id", id),
            new Claim(ClaimTypes.Name, "synthetic"), new Claim("display_name", "Synthetic")], "test"));
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static SaveRegulationDocumentRequest Request(string version) => new()
    {
        Title = "Synthetic rules", Version = version, Message = "Synthetic test only",
        PdfFileName = "rules.pdf", PdfContent = Encoding.ASCII.GetBytes("%PDF-1.7 synthetic"),
    };

    private sealed class Fixture(SqliteTestDatabase database, WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;
        public static async Task<Fixture> CreateAsync()
        {
            var database = await SqliteTestDatabase.CreateAsync();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = database.ConnectionString,
                ["Jwt:SigningKey"] = "synthetic-acceptance-test-signing-key-only",
                ["AdminTools:DeveloperPassword"] = "synthetic-developer",
            });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddWebApiCore(builder.Configuration, readEnvironmentVariable: _ => null);
            builder.Services.RemoveAll<IHostedService>();
            builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                await using var db = database.CreateContext();
                var now = DateTimeOffset.UtcNow;
                foreach (var name in new[] { "manager", "system" })
                    db.ManagerAccounts.Add(new ManagerAccountModel { Username = name, DisplayName = "Synthetic",
                        PasswordHash = hasher.HashPassword("123456"), IsSystem = name == "system",
                        CreatedAtUtc = now, UpdatedAtUtc = now, PasswordUpdatedAtUtc = now });
                db.Employees.Add(new EmployeeModel { FirstName = "Synthetic", LastName = "Employee",
                    Account = new EmployeeAccountModel { Username = "employee", PasswordHash = hasher.HashPassword("123456"), PasswordUpdatedAtUtc = now } });
                db.Containers.Add(TestDataFactory.CreateDalContainer("Synthetic container"));
                await db.SaveChangesAsync();
            }
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseWhen(c => c.Request.Path.StartsWithSegments("/api"), branch =>
            {
                branch.UseMiddleware<ApiExceptionMiddleware>();
                branch.UseMiddleware<RegulationAcceptanceGuardMiddleware>();
                branch.UseMiddleware<EmployeePresenceMiddleware>();
                branch.UseMiddleware<AdminToolsGuardMiddleware>();
            });
            app.MapControllers();
            app.MapGet("/api/guard-business", () => "ok").RequireAuthorization();
            await app.StartAsync();
            return new Fixture(database, app, new HttpClient { BaseAddress = new Uri(app.Urls.Single()) });
        }
        public async Task Login(string name)
        {
            Client.DefaultRequestHeaders.Authorization = null;
            var response = await Client.PostAsJsonAsync("/api/auth/login", new { username = name, password = "123456" });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.GetProperty("accessToken").GetString());
        }
        public Task<int> Publish(string version) => CreateDocument(version, true);
        public async Task<int> CreateDocument(string version, bool publish)
        {
            using var scope = app.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRegulationService>();
            var doc = await service.CreateAsync(Request(version), null, "synthetic");
            if (publish) await service.PublishAsync(doc.Id);
            return doc.Id;
        }
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
            await database.DisposeAsync();
        }
    }
}
