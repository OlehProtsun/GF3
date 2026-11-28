using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WebApi.Contracts.Auth;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class ManagerWorkspaceModeApiTests
{
    [Fact]
    public async Task NotepadUsesManagerRoutesAndPhoneCanSaveNotesWithoutWritingPanelState()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        const string root = "/api/manager-notepad";
        var state = new { isExpanded = true, isPinned = true, height = 999 };
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync(root)).StatusCode);
        await host.Login("employee");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync(root)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync($"{root}/notes", new { title = "Denied" })).StatusCode);
        await host.Login("manager");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync(root)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync($"{root}/notes", new { title = "Denied" })).StatusCode);

        await host.Exchange("pc");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync($"{root}/share-targets")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"{root}/state", state)).StatusCode);
        using var created = await host.Client.PostAsJsonAsync($"{root}/notes", new { title = "PC note", content = "Original", visibility = "public" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var note = Assert.IsType<ManagerNoteDto>(await created.Content.ReadFromJsonAsync<ManagerNoteDto>());
        var path = $"{root}/notes/{note.Id}";

        await host.Exchange("phone");
        using var phoneCreated = await host.Client.PostAsJsonAsync($"{root}/notes", new { title = "Phone note" });
        Assert.Equal(HttpStatusCode.Created, phoneCreated.StatusCode);
        var privateNote = Assert.IsType<ManagerNoteDto>(await phoneCreated.Content.ReadFromJsonAsync<ManagerNoteDto>());
        Assert.Equal("private", privateNote.Visibility);
        Assert.Equal(1, privateNote.Revision);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync($"{root}/state", new { height = 320 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync(path)).StatusCode);

        using var denied = await host.Client.PutAsJsonAsync(path, new { content = "Unleased", expectedRevision = note.Revision });
        Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        Assert.Equal("notepad_edit_session_conflict", (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        host.Client.DefaultRequestHeaders.Add("X-Notepad-Edit-Session", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync($"{path}/edit-session", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsync($"{path}/edit-session", null)).StatusCode);
        using var saved = await host.Client.PutAsJsonAsync(path, new { title = note.Title, content = "Saved on phone", expectedRevision = note.Revision });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var updated = Assert.IsType<ManagerNoteDto>(await saved.Content.ReadFromJsonAsync<ManagerNoteDto>());
        Assert.Equal("Saved on phone", updated.Content);
        Assert.Equal(note.Revision + 1, updated.Revision);
        using var pinned = await host.Client.PatchAsJsonAsync($"{path}/metadata", new { isPinned = true, expectedRevision = updated.Revision });
        Assert.Equal(HttpStatusCode.OK, pinned.StatusCode);
        var pinnedNote = Assert.IsType<ManagerNoteDto>(await pinned.Content.ReadFromJsonAsync<ManagerNoteDto>());
        var version = Assert.Single((await host.Client.GetFromJsonAsync<ManagerNoteVersionDto[]>($"{path}/versions"))!);
        using var restored = await host.Client.PostAsJsonAsync($"{path}/restore/{version.Id}", new { expectedRevision = pinnedNote.Revision });
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal("Original", (await restored.Content.ReadFromJsonAsync<ManagerNoteDto>())!.Content);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.DeleteAsync($"{path}/edit-session")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.DeleteAsync($"{root}/notes/{privateNote.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync($"{root}/notes/{privateNote.Id}")).StatusCode);

        var panel = (await host.Client.GetFromJsonAsync<ManagerNotepadDto>(root))!.State;
        Assert.True(panel.IsExpanded);
        Assert.True(panel.IsPinned);
        Assert.Equal(900, panel.Height);
    }

    [Fact]
    public async Task PhoneCanNegotiatePresenceWithoutGainingBusinessWriteAccess()
    {
        await using var host = await ManagerWorkspaceTestHost.CreateAsync();
        const string negotiate = "/api/realtime/presence/negotiate?negotiateVersion=1";
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PostAsync(negotiate, null)).StatusCode);
        await host.Login("manager");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsync(negotiate, null)).StatusCode);
        await host.Exchange("phone");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsync(negotiate, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsync("/api/guard-write", null)).StatusCode);
    }

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
