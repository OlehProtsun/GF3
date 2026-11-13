using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WebApi.Contracts.Auth;

namespace GF3.Tests;

public sealed class ManagerWorkspaceModeApiTests
{
    [Fact]
    public async Task LoginExchangeRefreshAndNewLoginPreserveIdentity()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        await host.Login("manager");
        var session = (await host.Client.GetFromJsonAsync<SessionDto>("/api/auth/session"))!;
        Assert.Equal("choose", session.WorkspaceMode);
        var previous = host.Client.DefaultRequestHeaders.Authorization!.Parameter;
        foreach (var mode in new[] { "phone", "pc", "phone" })
        {
            var result = await host.Exchange(mode);
            Assert.NotEqual(previous, result.AccessToken); previous = result.AccessToken;
            Assert.Equal(session.ManagerId, result.Session.ManagerId); Assert.Equal(session.UserName, result.Session.UserName);
            Assert.Equal(mode, result.Session.WorkspaceMode); Assert.False(result.Session.IsSystemManager);
            Assert.Equal(mode, (await host.Client.GetFromJsonAsync<SessionDto>("/api/auth/session"))!.WorkspaceMode);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync("/api/auth/logout", null)).StatusCode);
        await host.Login("manager");
        Assert.Equal("choose", (await host.Client.GetFromJsonAsync<SessionDto>("/api/auth/session"))!.WorkspaceMode);
    }
    [Theory]
    [InlineData("choose")]
    [InlineData("PHONE")]
    [InlineData("")]
    [InlineData("other")]
    [InlineData(null)]
    public async Task InvalidModesReturn400(string? mode)
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync(); await host.Login("manager");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/auth/manager-mode", new { mode })).StatusCode);
    }
    [Fact]
    public async Task ExchangeRequiresAuthenticatedManagerAndCannotElevateSystemPrivilege()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PostAsJsonAsync("/api/auth/manager-mode", new { mode = "pc" })).StatusCode);
        host.Bearer("invalid");
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PostAsJsonAsync("/api/auth/manager-mode", new { mode = "pc" })).StatusCode);
        await host.Login("employee");
        Assert.Null((await host.Client.GetFromJsonAsync<SessionDto>("/api/auth/session"))!.WorkspaceMode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync("/api/auth/manager-mode", new { mode = "pc" })).StatusCode);
        await host.Login("manager");
        var response = await host.Client.PostAsJsonAsync("/api/auth/manager-mode", new { mode = "pc", isSystemManager = true, managerId = 999, credentialVersion = 0 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<LoginResponseDto>())!;
        Assert.False(result.Session.IsSystemManager); Assert.NotEqual(999, result.Session.ManagerId);
        host.Bearer(result.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/admin/regulations")).StatusCode);
        await using var db = host.Database.CreateContext();
        var manager = await db.ManagerAccounts.FindAsync(result.Session.ManagerId);
        manager!.PasswordUpdatedAtUtc = manager.PasswordUpdatedAtUtc.AddSeconds(1); await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/auth/session")).StatusCode);
    }
}
