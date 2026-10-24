using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text;
using System.Text.Json;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Security;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Controllers;
using WebApi.Infrastructure;
using WebApi.Middleware;
using WebApi.Options;
using WebApi.Services;

namespace GF3.Tests;

public sealed class SystemManagerAccessTests
{
    [Theory]
    [InlineData("system-test", true)]
    [InlineData("regular-test", false)]
    [InlineData("manager", false)]
    [InlineData("employee-test", false)]
    public async Task LoginAndRestoration_DerivePrivilegeFromPersistedAccount(string username, bool expected)
    {
        await using var fixture = await Fixture.CreateAsync();
        var token = await fixture.LoginAsync(username, expected);
        fixture.Bearer(token);
        using var session = await fixture.GetJsonAsync("/api/auth/session");
        Assert.Equal(expected, session.RootElement.GetProperty("isSystemManager").GetBoolean());
        using var claims = await fixture.GetJsonAsync("/test/claims");
        Assert.Equal(expected ? 1 : 0, claims.RootElement.GetArrayLength());
        if (expected) Assert.Equal("true", claims.RootElement[0].GetString());
    }

    [Theory]
    [InlineData("system-test", true)]
    [InlineData("manager", false)]
    public async Task CurrentSignedTokensWithoutSpecialClaim_UseDatabasePrivilege(string username, bool expected)
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var db = fixture.Database.CreateContext();
        var account = await db.ManagerAccounts.SingleAsync(item => item.Username == username);
        var options = fixture.App.Services.GetRequiredService<IOptions<JwtAuthOptions>>();
        var token = fixture.App.Services.GetRequiredService<IJwtTokenService>().CreateAccessToken(new AuthenticatedSessionDto
        {
            Role = AuthRoles.Manager, UserName = username, DisplayName = "Synthetic manager",
            ManagerId = account.Id, CredentialVersion = account.PasswordUpdatedAtUtc.UtcTicks,
            IsSystemManager = true,
        }).AccessToken;
        Assert.True(JwtTokenCodec.TryReadAccessToken(token, options.Value, TimeSpan.Zero, out var decoded, out _));
        Assert.False(decoded.IsSystemManager);
        fixture.Bearer(token);
        using var session = await fixture.GetJsonAsync("/api/auth/session");
        Assert.Equal(expected, session.RootElement.GetProperty("isSystemManager").GetBoolean());
    }

    [Theory]
    [InlineData("manager")]
    [InlineData("employee-test")]
    public async Task IndependentlySuppliedSignedPrivilegeClaim_IsIgnored(string username)
    {
        await using var fixture = await Fixture.CreateAsync();
        var token = await fixture.LoginAsync(username, false);
        var parts = token.Split('.');
        var payloadSegment = parts[1].Replace('-', '+').Replace('_', '/');
        payloadSegment = payloadSegment.PadRight((payloadSegment.Length + 3) / 4 * 4, '=');
        var payload = JsonNode.Parse(Convert.FromBase64String(payloadSegment))!.AsObject();
        payload[AuthPolicies.SystemManagerClaim] = "true";
        payload["isSystemManager"] = true;
        static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var input = parts[0] + "." + Encode(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        var signingKey = fixture.App.Services.GetRequiredService<IOptions<JwtAuthOptions>>().Value.SigningKey;
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), Encoding.UTF8.GetBytes(input));
        fixture.Bearer(input + "." + Encode(signature));
        using var session = await fixture.GetJsonAsync("/api/auth/session");
        Assert.False(session.RootElement.GetProperty("isSystemManager").GetBoolean());
        using var claims = await fixture.GetJsonAsync("/test/claims");
        Assert.Equal(0, claims.RootElement.GetArrayLength());
        using var response = await fixture.Client.GetAsync("/api/workflow-logs");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("clear")]
    [InlineData("revoke")]
    [InlineData("delete")]
    public async Task DatabaseChange_RemovesRightsOnNextRequest(string change)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Bearer(await fixture.LoginAsync("system-test", true));
        using var before = await fixture.GetJsonAsync("/test/claims");
        Assert.Equal(1, before.RootElement.GetArrayLength());
        await using (var db = fixture.Database.CreateContext())
        {
            var account = await db.ManagerAccounts.SingleAsync(item => item.Username == "system-test");
            if (change == "clear") account.IsSystem = false;
            else if (change == "revoke") account.PasswordUpdatedAtUtc = account.PasswordUpdatedAtUtc.AddSeconds(1);
            else db.ManagerAccounts.Remove(account);
            await db.SaveChangesAsync();
        }
        using var session = await fixture.Client.GetAsync("/api/auth/session");
        if (change == "clear")
        {
            Assert.Equal(HttpStatusCode.OK, session.StatusCode);
            using var json = JsonDocument.Parse(await session.Content.ReadAsStringAsync());
            Assert.False(json.RootElement.GetProperty("isSystemManager").GetBoolean());
            using var claims = await fixture.GetJsonAsync("/test/claims");
            Assert.Equal(0, claims.RootElement.GetArrayLength());
        }
        else Assert.Equal(HttpStatusCode.Unauthorized, session.StatusCode);
        foreach (var path in ReadPaths)
        {
            using var response = await fixture.Client.GetAsync(path);
            Assert.Equal(change == "clear" ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("invalid")]
    [InlineData("regular-test")]
    [InlineData("manager")]
    [InlineData("employee-test")]
    public async Task EveryPrivilegedArea_DeniesReadsAndWritesBeforeMutation(string username)
    {
        await using var fixture = await Fixture.CreateAsync();
        if (username == "invalid") fixture.Bearer("invalid-test-token");
        else if (username != "anonymous") fixture.Bearer(await fixture.LoginAsync(username, false));
        var expected = username is "anonymous" or "invalid" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
        var before = await fixture.StateAsync();
        foreach (var path in ReadPaths)
        {
            using var response = await fixture.Client.GetAsync(path);
            Assert.Equal(expected, response.StatusCode);
        }
        using var logs = await fixture.Client.PutAsJsonAsync("/api/workflow-logs/settings", new { isEnabled = false, audience = "employees" });
        using var execute = await fixture.Client.PostAsJsonAsync("/api/admin/db/execute", new { sql = "UPDATE employee SET first_name = 'Changed'" });
        using var form = RegulationForm();
        using var regulations = await fixture.Client.PostAsync("/api/admin/regulations", form);
        Assert.Equal(expected, logs.StatusCode);
        Assert.Equal(expected, execute.StatusCode);
        Assert.Equal(expected, regulations.StatusCode);
        Assert.Equal(before, await fixture.StateAsync());
    }

    [Fact]
    public async Task SystemManager_ReachesAllAreasAndWritesOnlyDisposableDatabase()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Bearer(await fixture.LoginAsync("system-test", true));
        foreach (var path in ReadPaths)
        {
            using var response = await fixture.Client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        using var logs = await fixture.Client.PutAsJsonAsync("/api/workflow-logs/settings", new { isEnabled = false, audience = "employees" });
        Assert.Equal(HttpStatusCode.OK, logs.StatusCode);
        using var execute = await fixture.Client.PostAsJsonAsync("/api/admin/db/execute", new { sql = "UPDATE employee SET first_name = first_name WHERE id = -1" });
        Assert.Equal(HttpStatusCode.OK, execute.StatusCode);
        using var form = RegulationForm();
        using var document = await fixture.Client.PostAsync("/api/admin/regulations", form);
        Assert.Equal(HttpStatusCode.OK, document.StatusCode);
        using var db = fixture.Database.CreateContext();
        Assert.Equal(1, await db.RegulationDocuments.CountAsync());
        Assert.Equal("Synthetic", (await db.Employees.SingleAsync()).FirstName);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("missing-password")]
    [InlineData("wrong-password")]
    [InlineData("write-disabled")]
    public async Task SystemManager_DoesNotBypassExistingAdminGuards(string guard)
    {
        await using var fixture = await Fixture.CreateAsync(enabled: guard != "disabled", allowWrite: guard != "write-disabled");
        fixture.Bearer(await fixture.LoginAsync("system-test", true));
        if (guard is "missing-password" or "wrong-password")
        {
            fixture.Client.DefaultRequestHeaders.Remove("X-GF3-Developer-Password");
            if (guard == "wrong-password") fixture.Client.DefaultRequestHeaders.Add("X-GF3-Developer-Password", "wrong-synthetic-password");
        }
        var before = await fixture.StateAsync();
        using var response = guard == "write-disabled"
            ? await fixture.Client.PostAsJsonAsync("/api/admin/db/execute", new { sql = "UPDATE employee SET first_name = 'Changed'" })
            : await fixture.Client.GetAsync("/api/admin/db/metadata");
        if (guard == "write-disabled") Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        else Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
        Assert.Equal(before, await fixture.StateAsync());
        if (guard is "missing-password" or "wrong-password")
        {
            using var regulations = await fixture.Client.GetAsync("/api/admin/regulations");
            Assert.Equal(HttpStatusCode.Forbidden, regulations.StatusCode);
        }
    }

    [Fact]
    public async Task OrdinaryManagerNewsAndEmployeeRegulationAcceptance_KeepExistingAccess()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Bearer(await fixture.LoginAsync("manager", false));
        foreach (var path in new[] { "/api/shops", "/api/system-news", "/api/admin/system-news" })
        {
            using var response = await fixture.Client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        fixture.Bearer(await fixture.LoginAsync("system-test", true));
        using var form = RegulationForm();
        using var created = await fixture.Client.PostAsync("/api/admin/regulations", form);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetInt32();
        using var published = await fixture.Client.PostAsync($"/api/admin/regulations/{id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        fixture.Bearer(await fixture.LoginAsync("employee-test", false));
        using var pending = await fixture.GetJsonAsync("/api/regulations/pending");
        Assert.Equal(1, pending.RootElement.GetArrayLength());
        using var accepted = await fixture.Client.PostAsync($"/api/regulations/{id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using var history = await fixture.GetJsonAsync("/api/regulations/history/me");
        Assert.Equal(1, history.RootElement.GetArrayLength());
        using var shop = await fixture.Client.GetAsync("/api/shops");
        Assert.Equal(HttpStatusCode.Forbidden, shop.StatusCode);
    }

    [Fact]
    public async Task PolicyRequiresAuthenticationManagerRoleAndTrustedClaim()
    {
        await using var fixture = await Fixture.CreateAsync();
        var service = fixture.App.Services.GetRequiredService<IAuthorizationService>();
        foreach (var principal in new[]
        {
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, AuthRoles.Manager), new Claim(AuthPolicies.SystemManagerClaim, "true") })),
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, AuthRoles.Employee), new Claim(AuthPolicies.SystemManagerClaim, "true") }, "test")),
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, AuthRoles.Manager) }, "test")),
        }) Assert.False((await service.AuthorizeAsync(principal, null, AuthPolicies.SystemManager)).Succeeded);
    }

    private static readonly string[] ReadPaths = ["/api/workflow-logs", "/api/admin/db/metadata", "/api/admin/regulations"];

    private static MultipartFormDataContent RegulationForm()
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("Synthetic regulation"), "Title" },
            { new StringContent("test-v1"), "Version" },
            { new StringContent("Synthetic acceptance message"), "Message" },
            { new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4 synthetic test content")), "Pdf", "synthetic.pdf" },
        };
        return form;
    }

    private sealed class Fixture(SqliteTestDatabase database, WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public SqliteTestDatabase Database { get; } = database;
        public WebApplication App { get; } = app;
        public HttpClient Client { get; } = client;

        public static async Task<Fixture> CreateAsync(bool enabled = true, bool allowWrite = true)
        {
            var database = await SqliteTestDatabase.CreateAsync();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = database.ConnectionString,
                ["Jwt:SigningKey"] = "synthetic-system-access-test-signing-key-only",
                ["AdminTools:Enabled"] = enabled.ToString(),
                ["AdminTools:AllowWriteSql"] = allowWrite.ToString(),
                ["AdminTools:DeveloperPassword"] = "synthetic-developer-access",
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
                foreach (var name in new[] { "system-test", "regular-test", "manager" })
                    db.ManagerAccounts.Add(new ManagerAccountModel
                    {
                        Username = name, DisplayName = "Synthetic manager", PasswordHash = hasher.HashPassword("123456"),
                        IsSystem = name == "system-test", PasswordUpdatedAtUtc = now, CreatedAtUtc = now, UpdatedAtUtc = now,
                    });
                db.Employees.Add(new EmployeeModel
                {
                    FirstName = "Synthetic", LastName = "Employee",
                    Account = new EmployeeAccountModel { Username = "employee-test", PasswordHash = hasher.HashPassword("123456"), PasswordUpdatedAtUtc = now },
                });
                await db.SaveChangesAsync();
            }
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<ApiExceptionMiddleware>();
            app.UseMiddleware<EmployeePresenceMiddleware>();
            app.UseMiddleware<AdminToolsGuardMiddleware>();
            app.MapControllers();
            app.MapGet("/test/claims", async (HttpContext context) =>
            {
                var result = await context.AuthenticateAsync(JwtAuthenticationDefaults.SchemeName);
                return result.Principal!.FindAll(AuthPolicies.SystemManagerClaim).Select(claim => claim.Value).ToArray();
            }).RequireAuthorization();
            await app.StartAsync();
            var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            client.DefaultRequestHeaders.Add("X-GF3-Developer-Password", "synthetic-developer-access");
            return new Fixture(database, app, client);
        }

        public async Task<string> LoginAsync(string username, bool expected)
        {
            Client.DefaultRequestHeaders.Authorization = null;
            using var response = await Client.PostAsJsonAsync("/api/auth/login", new { username, password = "123456" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(expected, json.RootElement.GetProperty("session").GetProperty("isSystemManager").GetBoolean());
            return json.RootElement.GetProperty("accessToken").GetString()!;
        }

        public void Bearer(string token) => Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        public async Task<JsonDocument> GetJsonAsync(string path)
        {
            using var response = await Client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task<string> StateAsync()
        {
            await using var db = Database.CreateContext();
            return JsonSerializer.Serialize(new
            {
                employees = await db.Employees.OrderBy(item => item.Id).Select(item => item.FirstName).ToArrayAsync(),
                documents = await db.RegulationDocuments.CountAsync(),
                logs = await db.WorkflowLogEntries.CountAsync(),
                settings = await db.WorkflowLogSettings.AsNoTracking().ToArrayAsync(),
            });
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
            await Database.DisposeAsync();
        }
    }
}
