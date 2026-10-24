using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BusinessLogicLayer.Contracts.Regulations;
using BusinessLogicLayer.Security;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
using BusinessLogicLayer.Contracts.Auth;
using Microsoft.Extensions.Options;
using WebApi.Options;
using WebApi.Contracts.Auth;
using WebApi.Realtime;

namespace GF3.Tests;

internal sealed class ManagerWorkspaceTestHost(SqliteTestDatabase database, WebApplication app, HttpClient client) : IAsyncDisposable
{
    public HttpClient Client { get; } = client;
    public SqliteTestDatabase Database => database;
    public WebApplication App => app;
    public static async Task<ManagerWorkspaceTestHost> CreateAsync()
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
                db.ManagerAccounts.Add(new ManagerAccountModel
                {
                    Username = name,
                    DisplayName = "Synthetic",
                    PasswordHash = hasher.HashPassword("123456"),
                    IsSystem = name == "system",
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    PasswordUpdatedAtUtc = now
                });
            db.Employees.Add(new EmployeeModel
            {
                FirstName = "Synthetic",
                LastName = "Employee",
                Account = new EmployeeAccountModel { Username = "employee", PasswordHash = hasher.HashPassword("123456"), PasswordUpdatedAtUtc = now }
            });
            db.Containers.Add(TestDataFactory.CreateDalContainer("Synthetic container"));
            db.Shops.Add(TestDataFactory.CreateDalShop());
            db.AvailabilityGroups.Add(new AvailabilityGroupModel { Name = "Synthetic dispo", Year = 2026, Month = 4 });
            await db.SaveChangesAsync();
            db.Schedules.Add(TestDataFactory.CreateDalSchedule(1, 1));
            await db.SaveChangesAsync();
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWhen(c => c.Request.Path.StartsWithSegments("/api"), branch =>
        {
            branch.UseMiddleware<ApiExceptionMiddleware>();
            branch.UseMiddleware<RegulationAcceptanceGuardMiddleware>();
            branch.UseMiddleware<ManagerWorkspaceModeGuardMiddleware>();
            branch.UseMiddleware<EmployeePresenceMiddleware>();
            branch.UseMiddleware<AdminToolsGuardMiddleware>();
        });
        app.MapControllers();
        app.MapHub<EmployeePresenceHub>(EmployeePresenceHub.RoutePattern);
        app.MapMethods("/api/guard-write", ["POST", "PUT", "PATCH", "DELETE"], () => "written").RequireAuthorization();
        app.MapGet("/api/guard-business", () => "ok").RequireAuthorization();
        await app.StartAsync();
        return new ManagerWorkspaceTestHost(database, app, new HttpClient { BaseAddress = new Uri(app.Urls.Single()) });
    }
    public async Task Login(string name)
    {
        Client.DefaultRequestHeaders.Authorization = null;
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { username = name, password = "123456" });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.GetProperty("accessToken").GetString());
    }
    public void Bearer(string? token) => Client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
    public async Task<LoginResponseDto> Exchange(string mode)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/manager-mode", new { mode });
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<LoginResponseDto>())!;
        Bearer(result.AccessToken);
        return result;
    }
    public async Task<string> SignedToken(string? mode, string role = AuthRoles.Manager)
    {
        await using var db = database.CreateContext();
        var manager = await db.ManagerAccounts.FirstAsync(item => item.Username == "manager");
        var options = app.Services.GetRequiredService<IOptions<JwtAuthOptions>>().Value;
        return JwtTokenCodec.WriteAccessToken(options, new AuthenticatedSessionDto { Role = role, UserName = role == AuthRoles.Manager ? "manager" : "employee", ManagerId = role == AuthRoles.Manager ? manager.Id : null, EmployeeId = role == AuthRoles.Employee ? 1 : null, SessionVersion = 0, CredentialVersion = manager.PasswordUpdatedAtUtc.UtcTicks, WorkspaceMode = mode }, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(10));
    }
    public Task<int> Publish(string version) => CreateDocument(version, true);
    public async Task<int> CreateDocument(string version, bool publish)
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegulationService>();
        var doc = await service.CreateAsync(new SaveRegulationDocumentRequest { Title = "Synthetic rules", Version = version, Message = "Test", PdfFileName = "rules.pdf", PdfContent = Encoding.ASCII.GetBytes("%PDF-1.7 synthetic") }, null, "synthetic");
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