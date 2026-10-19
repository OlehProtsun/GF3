using System.Security.Claims;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BusinessLogicLayer.Security;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApi.Controllers;
using WebApi.Options;
using WebApi.Infrastructure;

namespace GF3.Tests;

public sealed class EmployeePushControllerTests
{
    [Fact]
    public async Task ProductionStaticPipeline_ServesWorkerAsRevalidatedJavaScript()
    {
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../FrontEnd/public"));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production", WebRootPath = webRoot });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = context => StartupConfiguration.ApplyFrontendCacheHeaders(context.Context, context.File.Name) });
        app.MapFallbackToFile("index.html");
        await app.StartAsync(); using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using var response = await client.GetAsync("/sw.js");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType!.MediaType!);
        Assert.True(response.Headers.CacheControl!.NoCache);
        Assert.True(response.Headers.CacheControl.MustRevalidate);
        Assert.False(response.Headers.CacheControl.NoStore);
        Assert.Contains("addEventListener(\"push\"", await response.Content.ReadAsStringAsync());
        await app.StopAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpEndpoints_EnforceAuthenticationRolesAndModelValidation(bool enabled)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders(); builder.Configuration.Sources.Clear();
        var vapid = WebPush.VapidHelper.GenerateVapidKeys();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = database.ConnectionString, ["Jwt:SigningKey"] = "synthetic-employee-push-test-signing-key-only",
            ["Push:Enabled"] = enabled.ToString(), ["Push:VapidPublicKey"] = vapid.PublicKey, ["Push:VapidPrivateKey"] = vapid.PrivateKey, ["Push:VapidSubject"] = "mailto:operator@example.com"
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddWebApiCore(builder.Configuration, readEnvironmentVariable: _ => null);
        builder.Services.RemoveAll<IHostedService>();
        builder.Services.AddControllers().AddApplicationPart(typeof(EmployeePushController).Assembly);
        await using var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await using var db = database.CreateContext(); var now = DateTimeOffset.UtcNow;
            db.ManagerAccounts.Add(new ManagerAccountModel { Username = "push-manager", DisplayName = "Test", PasswordHash = hasher.HashPassword("123456"), CreatedAtUtc = now, UpdatedAtUtc = now, PasswordUpdatedAtUtc = now });
            var employee = TestDataFactory.CreateDalEmployee("Push", "Employee");
            employee.Account = new EmployeeAccountModel { Username = "push-employee", PasswordHash = hasher.HashPassword("123456"), PasswordUpdatedAtUtc = now };
            db.Add(employee); await db.SaveChangesAsync();
        }
        app.UseAuthentication(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/employee-push/config")).StatusCode);
        foreach (var role in new[] { "manager", "employee" })
        {
            client.DefaultRequestHeaders.Authorization = null;
            var login = await client.PostAsJsonAsync("/api/auth/login", new { username = $"push-{role}", password = "123456" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString());
            var response = await client.GetAsync("/api/employee-push/config");
            Assert.Equal(role == "manager" ? HttpStatusCode.Forbidden : HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain(vapid.PrivateKey, await response.Content.ReadAsStringAsync());
        }
        var disabled = await client.PutAsJsonAsync("/api/employee-push/subscriptions", Request());
        Assert.Equal(enabled ? HttpStatusCode.NoContent : HttpStatusCode.ServiceUnavailable, disabled.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/employee-push/subscriptions", new { endpoint = "", keys = new { } })).StatusCode);
        var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/employee-push/subscriptions") { Content = JsonContent.Create(new { endpoint = Request().Endpoint }) };
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(delete)).StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public void VapidOptions_ValidateOnlyWhenEnabled()
    {
        Assert.True(new PushOptions().IsValid());
        Assert.False(new PushOptions { Enabled = true }.IsValid());
        var keys = WebPush.VapidHelper.GenerateVapidKeys();
        Assert.True(new PushOptions { Enabled = true, VapidPublicKey = keys.PublicKey, VapidPrivateKey = keys.PrivateKey, VapidSubject = "mailto:operator@example.com" }.IsValid());
    }

    [Theory]
    [InlineData("bad", "bad")]
    [InlineData("AAAAAAAA", "AAAAAAAA")]
    public async Task Put_RejectsMalformedKeys(string p256dh, string auth)
    {
        await using var database = await SqliteTestDatabase.CreateAsync(); await using var db = database.CreateContext();
        Assert.IsType<BadRequestResult>(await Create(db, 1).Put(new(Request().Endpoint, new(p256dh, auth)), default));
    }

    internal static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    internal static EmployeePushController.SubscriptionRequest Request(string endpoint = "https://fcm.googleapis.com/send/test") =>
        new(endpoint, new(Encode([4, .. new byte[64]]), Encode(new byte[16])));

    [Theory]
    [InlineData("http://fcm.googleapis.com/test")]
    [InlineData("https://localhost/test")]
    [InlineData("https://127.0.0.1/test")]
    [InlineData("https://10.0.0.1/test")]
    [InlineData("https://fcm.googleapis.com.evil.example/test")]
    [InlineData("https://user@fcm.googleapis.com/test")]
    [InlineData("https://fcm.googleapis.com:999/test")]
    public async Task Put_RejectsUnsafeEndpoints(string endpoint)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var controller = Create(db, 1);
        Assert.IsType<BadRequestResult>(await controller.Put(Request(endpoint), default));
    }

    [Fact]
    public async Task Subscription_PreservesSameOwnerAndTransfersAtomically()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var first = TestDataFactory.CreateDalEmployee("First", "Device");
        var second = TestDataFactory.CreateDalEmployee("Second", "Device");
        db.AddRange(first, second);
        await db.SaveChangesAsync();
        var controller = Create(db, first.Id);
        var request = Request();
        Assert.IsType<NoContentResult>(await controller.Put(request, default));
        var subscription = await db.EmployeePushSubscriptions.SingleAsync();
        var created = subscription.CreatedAtUtc;
        db.EmployeePushDeliveries.Add(new() { SubscriptionId = subscription.Id, NotificationId = "swap-public:1", CreatedAtUtc = created, NextAttemptAtUtc = created });
        await db.SaveChangesAsync();
        Assert.IsType<NoContentResult>(await controller.Put(request, default));
        Assert.Equal(created, subscription.CreatedAtUtc);
        Assert.Single(await db.EmployeePushDeliveries.ToListAsync());
        var other = Create(db, second.Id);
        Assert.IsType<NoContentResult>(await other.Delete(new(request.Endpoint), default));
        Assert.Single(await db.EmployeePushSubscriptions.ToListAsync());
        Assert.IsType<NoContentResult>(await other.Put(request, default));
        Assert.Equal(second.Id, subscription.EmployeeId);
        Assert.True(subscription.CreatedAtUtc >= created);
        Assert.Empty(await db.EmployeePushDeliveries.AsNoTracking().ToListAsync());
        Assert.IsType<NoContentResult>(await other.Delete(new(request.Endpoint), default));
        Assert.IsType<NoContentResult>(await other.Delete(new(request.Endpoint), default));
    }

    [Fact]
    public async Task Disabled_RejectsPutAndExposesNoKeys()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var controller = Create(db, 1, false);
        Assert.Equal(503, Assert.IsType<ObjectResult>(await controller.Put(Request(), default)).StatusCode);
        var json = System.Text.Json.JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(controller.GetConfig()).Value);
        Assert.Contains("\"enabled\":false", json);
        Assert.Contains("\"publicKey\":null", json);
        Assert.DoesNotContain("private", json, StringComparison.OrdinalIgnoreCase);
    }

    internal static EmployeePushController Create(DataAccessLayer.Models.DataBaseContext.AppDbContext db, int id, bool enabled = true) =>
        new(db, Options.Create(new PushOptions { Enabled = enabled }), TimeProvider.System)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("employee_id", id.ToString())], "Test")) } }
        };
}
