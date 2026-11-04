using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Infrastructure;

namespace GF3.Tests;

public sealed class ApiRateLimitTests
{
    [Fact]
    public async Task Partitions_UseVerifiedIdentitiesOrConnectionIp_AndExemptNonApiPaths()
    {
        var services = new ServiceCollection();
        services.AddWebApiCore(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "synthetic-rate-limiter-tests-only-signing-key",
            ["ConnectionStrings:Default"] = "Data Source=:memory:",
        }).Build(), readEnvironmentVariable: _ => null);
        await using var provider = services.BuildServiceProvider();
        var limiter = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value.GlobalLimiter!;
        DefaultHttpContext Context(string ip, string? role = null, int id = 1, string path = "/api/test", bool authenticated = true)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
            context.Request.Headers["X-Forwarded-For"] = "192.0.2.200";
            context.Request.Headers.Authorization = "Bearer unsigned-user-input";
            if (role is not null) context.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Role, role), new Claim(role == AuthRoles.Manager ? "manager_id" : "employee_id", id.ToString()),
            ], authenticated ? "test-verified" : null));
            return context;
        }
        async Task Exhaust(DefaultHttpContext context, int budget)
        {
            for (var i = 0; i < budget; i++)
            {
                using var lease = await limiter.AcquireAsync(context);
                Assert.True(lease.IsAcquired, $"Request {i + 1} of {budget}");
            }
            using var rejected = await limiter.AcquireAsync(context);
            Assert.False(rejected.IsAcquired);
        }
        await Exhaust(Context("192.0.2.1"), 120);
        using (var mapped = await limiter.AcquireAsync(Context("::ffff:192.0.2.1"))) Assert.False(mapped.IsAcquired);
        await Exhaust(Context("192.0.2.2"), 120);
        await Exhaust(Context("192.0.2.1", AuthRoles.Manager), 240);
        using (var moved = await limiter.AcquireAsync(Context("192.0.2.3", AuthRoles.Manager))) Assert.False(moved.IsAcquired);
        await Exhaust(Context("192.0.2.1", AuthRoles.Manager, 2), 240);
        await Exhaust(Context("192.0.2.1", AuthRoles.Employee), 240);
        using (var unsigned = await limiter.AcquireAsync(Context("192.0.2.1", AuthRoles.Manager, 99, authenticated: false))) Assert.False(unsigned.IsAcquired);
        foreach (var path in new[] { "/assets/app.js", "/sw.js", "/health", "/", "/apiary" })
        for (var i = 0; i < 250; i++)
        {
            using var lease = await limiter.AcquireAsync(Context("192.0.2.1", path: path));
            Assert.True(lease.IsAcquired);
        }
    }

    [Fact]
    public async Task Middleware_ReturnsGenericProblemWithRetryGuidance_AndKeepsStaticAndAuthFunctional()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/guard-business")).StatusCode);
        for (var i = 0; i < 119; i++) (await host.Client.GetAsync("/api/rate-probe")).EnsureSuccessStatusCode();
        var response = await host.Client.GetAsync("/api/rate-probe");
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.InRange(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, 60);
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        Assert.Equal(429, json.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("username", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", text, StringComparison.OrdinalIgnoreCase);
        (await host.Client.GetAsync("/rate-probe")).EnsureSuccessStatusCode();
        host.Bearer(await host.SignedToken("pc"));
        (await host.Client.GetAsync("/api/guard-business")).EnsureSuccessStatusCode();
        host.Bearer("unsigned-user-input");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await host.Client.GetAsync("/api/rate-probe")).StatusCode);
    }
}
