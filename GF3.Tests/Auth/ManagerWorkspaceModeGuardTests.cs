using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Middleware;

namespace GF3.Tests;

public sealed class ManagerWorkspaceModeGuardTests
{
    [Fact]
    public async Task PhoneReadsPrivateManagerRecordsAndCannotWrite()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(); await host.Login("manager"); await host.Exchange("phone");
        foreach (var path in new[] { "/api/containers", "/api/containers/1/graphs", "/api/containers/1/graphs/1", "/api/availability-groups", "/api/employees" })
            Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(path)).StatusCode);
        var before = await State(host);
        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
        {
            var response = await host.Client.SendAsync(new HttpRequestMessage(method, "/api/guard-write"));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("manager_phone_read_only", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        foreach (var (method, path, body) in new[] { (HttpMethod.Post, "/api/employees", new { firstName = "Changed", lastName = "User" }), (HttpMethod.Put, "/api/employees/1", new { firstName = "Changed", lastName = "User" }), (HttpMethod.Delete, "/api/employees/1", new { firstName = "Changed", lastName = "User" }) })
        {
            var response = await host.Client.SendAsync(new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        Assert.Equal(before, await State(host));
        await host.Exchange("pc");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync("/api/guard-write", null)).StatusCode);
        var employeeResponse = await host.Client.PostAsJsonAsync("/api/employees", new { firstName = "New", lastName = "User" });
        Assert.Equal(HttpStatusCode.Created, employeeResponse.StatusCode);
        host.Bearer(await host.SignedToken(null));
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync("/api/guard-write", null)).StatusCode);
        await host.Login("employee");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/employee-schedules")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync("/api/guard-write", null)).StatusCode);
    }
    [Theory]
    [InlineData("choose")]
    [InlineData("phone")]
    public async Task RequiredLegalAcceptanceKeepsBlockingAndAllowedWrites(string mode)
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(); await host.Login("manager");
        if (mode == "phone") await host.Exchange(mode);
        var id = await host.Publish("1.0");
        Assert.Equal((HttpStatusCode)428, (await host.Client.GetAsync("/api/containers")).StatusCode);
        foreach (var path in new[] { "/api/regulations/pending", "/api/regulations/history/me", $"/api/regulations/{id}/pdf", "/api/auth/session" })
            Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync($"/api/regulations/{id}/accept", null)).StatusCode);
        var expected = mode == "choose" ? HttpStatusCode.Forbidden : HttpStatusCode.OK;
        Assert.Equal(expected, (await host.Client.GetAsync("/api/containers")).StatusCode);
        await host.Exchange("pc");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/containers")).StatusCode);
    }
    [Theory]
    [InlineData("phone", "GET", "/api/employees", true)]
    [InlineData("phone", "HEAD", "/api/employees", true)]
    [InlineData("phone", "OPTIONS", "/api/employees", true)]
    [InlineData("phone", "POST", "/api/auth/manager-mode", true)]
    [InlineData("choose", "POST", "/API/AUTH/MANAGER-MODE", true)]
    [InlineData("choose", "GET", "/api/auth/session", true)]
    [InlineData("choose", "GET", "/api/regulations/pending", true)]
    [InlineData("choose", "GET", "/api/regulations/history/me", true)]
    [InlineData("choose", "GET", "/api/regulations/1/pdf", true)]
    [InlineData("choose", "POST", "/api/regulations/1/accept", true)]
    [InlineData("phone", "POST", "/api/regulations/1/accept", true)]
    [InlineData("choose", "POST", "/api/auth/logout", true)]
    [InlineData("choose", "GET", "/api/containers", false)]
    [InlineData("choose", "POST", "/api/containers", false)]
    [InlineData("choose", "HEAD", "/api/auth/session", false)]
    [InlineData("phone", "POST", "/api/auth/login", false)]
    [InlineData("phone", "POST", "/api/auth/manager-mode/evil", false)]
    [InlineData("phone", "DELETE", "/api/regulations/1/accept", false)]
    [InlineData("phone", "POST", "/api/regulations/0/accept", false)]
    [InlineData("phone", "POST", "/api/regulations/-1/accept", false)]
    [InlineData("phone", "POST", "/api/regulations/1/accept/evil", false)]
    [InlineData("choose", "GET", "/api/regulations/2147483648/pdf", false)]
    [InlineData("pc", "DELETE", "/api/employees/1", true)]
    [InlineData(null, "DELETE", "/api/employees/1", true)]
    public async Task ExactMethodAndPathBoundaries(string? mode, string method, string path, bool allowed)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "manager") };
        if (mode is not null) claims.Add(new(ManagerWorkspaceModes.ClaimType, mode));
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "tests")) };
        context.Request.Path = path; context.Request.Method = method; context.Response.Body = new MemoryStream();
        var reached = false;
        await new ManagerWorkspaceModeGuardMiddleware(_ => { reached = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.Equal(allowed, reached);
        if (!allowed)
        {
            Assert.Equal(403, context.Response.StatusCode); Assert.Equal("application/problem+json", context.Response.ContentType);
            context.Response.Body.Position = 0; var json = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.Equal(mode == "phone" ? "manager_phone_read_only" : "manager_workspace_mode_required", json.RootElement.GetProperty("code").GetString());
        }
    }
    private static async Task<string> State(ManagerWorkspaceTestHost host)
    {
        await using var db = host.Database.CreateContext();
        return JsonSerializer.Serialize(await db.Employees.AsNoTracking().OrderBy(e => e.Id).Select(e => new { e.Id, e.FirstName, e.LastName }).ToArrayAsync());
    }
}
