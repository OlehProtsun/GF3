using System.Text.Json;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed partial class ManagerNotepadControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentAcquisitionsGiveExactlyOneTabTheLease(bool sameManager)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var ids = await SeedManagers(database, 2);
        await using var firstDb = database.CreateContext();
        await using var secondDb = database.CreateContext();
        var first = CreateController(firstDb, ids[0]);
        var secondId = sameManager ? ids[0] : ids[1];
        var second = CreateController(secondDb, secondId);
        var note = Read(await first.CreateNote(new() { Visibility = "public", Content = "Original" }, Ct), 201);
        var firstToken = Guid.NewGuid();
        var secondToken = Guid.NewGuid();
        SetSession(first, firstToken);
        SetSession(second, secondToken);

        var results = await Race(() => first.AcquireEditSession(note.Id, Ct), () => second.AcquireEditSession(note.Id, Ct));

        var winningIndex = Array.FindIndex(results, item => item.Result is OkObjectResult);
        Assert.InRange(winningIndex, 0, 1);
        var winner = Read(results[winningIndex], 200);
        AssertSessionConflict(results[1 - winningIndex].Result);
        var editor = Assert.IsType<ManagerNoteEditorDto>(winner.ActiveEditor);
        var winningId = winningIndex == 0 ? ids[0] : secondId;
        var winningToken = winningIndex == 0 ? firstToken : secondToken;
        Assert.Equal($"manager{(winningId == ids[0] ? 1 : 2)}", editor.DisplayName);
        var json = JsonSerializer.Serialize(winner);
        Assert.DoesNotContain(firstToken.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(secondToken.ToString(), json, StringComparison.OrdinalIgnoreCase);

        var stored = await ReadStoredNote(database, note.Id);
        Assert.Equal(winningToken, stored.EditSessionId);
        Assert.Equal(winningId, stored.EditManagerId);
        Assert.Equal(stored.EditStartedAtMs + 60_000, stored.EditExpiresAtMs);
        Assert.Equal(stored.EditExpiresAtMs, editor.ExpiresAtMs);
        Assert.Equal(note.Revision, stored.Revision);
        Assert.Equal(note.Content, stored.Content);
        Assert.Empty(await firstDb.ManagerNoteVersions.AsNoTracking().ToListAsync(Ct));
    }

    [Fact]
    public async Task LeaseRenewalExpiryAndReleaseStayBoundToManagerTabAndNote()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var ids = await SeedManagers(database, 2);
        await using var ownerDb = database.CreateContext();
        await using var otherDb = database.CreateContext();
        var owner = CreateController(ownerDb, ids[0]);
        var other = CreateController(otherDb, ids[1]);
        var note = Read(await owner.CreateNote(new() { Visibility = "public", Content = "Shared" }, Ct), 201);
        var second = Read(await owner.CreateNote(new() { Visibility = "public" }, Ct), 201);
        var privateNote = Read(await owner.CreateNote(new(), Ct), 201);
        Assert.IsType<BadRequestResult>((await owner.AcquireEditSession(note.Id, Ct)).Result);
        owner.Request.Headers["X-Notepad-Edit-Session"] = "invalid";
        Assert.IsType<BadRequestResult>((await owner.RenewEditSession(note.Id, Ct)).Result);
        Assert.IsType<BadRequestResult>(await owner.ReleaseEditSession(note.Id, Ct));
        var oldToken = Guid.NewGuid();
        SetSession(owner, oldToken);
        SetSession(other, oldToken);
        Assert.IsType<NotFoundResult>((await owner.AcquireEditSession(privateNote.Id, Ct)).Result);
        Assert.IsType<NotFoundResult>((await other.AcquireEditSession(privateNote.Id, Ct)).Result);
        Assert.IsType<NotFoundResult>((await other.RenewEditSession(privateNote.Id, Ct)).Result);

        Read(await owner.AcquireEditSession(note.Id, Ct), 200);
        var shortenedExpiry = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 20_000;
        await ownerDb.ManagerNotes.Where(row => row.Id == note.Id).ExecuteUpdateAsync(update =>
            update.SetProperty(row => row.EditExpiresAtMs, shortenedExpiry), Ct);
        var initial = await ReadStoredNote(database, note.Id);
        AssertSessionConflict((await other.RenewEditSession(note.Id, Ct)).Result);
        Assert.IsType<NoContentResult>(await other.ReleaseEditSession(note.Id, Ct));
        AssertSessionConflict((await owner.RenewEditSession(second.Id, Ct)).Result);
        AssertSessionConflict((await owner.UpdateNote(second.Id, new() { Content = "Wrong note", ExpectedRevision = second.Revision }, Ct)).Result);
        Assert.Equal(JsonSerializer.Serialize(initial), JsonSerializer.Serialize(await ReadStoredNote(database, note.Id)));

        var renewed = Read(await owner.RenewEditSession(note.Id, Ct), 200);
        var stored = await ReadStoredNote(database, note.Id);
        Assert.True(renewed.ActiveEditor!.ExpiresAtMs > shortenedExpiry);
        Assert.Equal(initial.EditStartedAtMs, stored.EditStartedAtMs);
        Assert.Equal(note.Revision, stored.Revision);
        Assert.Equal(note.UpdatedAtUtc, stored.UpdatedAtUtc);
        await ownerDb.ManagerNotes.Where(row => row.Id == note.Id).ExecuteUpdateAsync(update =>
            update.SetProperty(row => row.EditExpiresAtMs, 0L), Ct);
        AssertSessionConflict((await owner.RenewEditSession(note.Id, Ct)).Result);
        AssertSessionConflict((await owner.UpdateNote(note.Id, new() { Content = "Expired", ExpectedRevision = note.Revision }, Ct)).Result);
        Assert.Equal(0L, (await ReadStoredNote(database, note.Id)).EditExpiresAtMs);

        var newToken = Guid.NewGuid();
        SetSession(other, newToken);
        Read(await other.AcquireEditSession(note.Id, Ct), 200);
        Assert.IsType<NoContentResult>(await owner.ReleaseEditSession(note.Id, Ct));
        SetSession(other, oldToken);
        Assert.IsType<NoContentResult>(await other.ReleaseEditSession(note.Id, Ct));
        stored = await ReadStoredNote(database, note.Id);
        Assert.Equal(newToken, stored.EditSessionId);
        Assert.Equal(ids[1], stored.EditManagerId);
        Assert.Equal(note.Revision, stored.Revision);
        Assert.Equal(note.Content, stored.Content);
        Assert.Equal(second.Content, (await ReadStoredNote(database, second.Id)).Content);
        Assert.Empty(await ownerDb.ManagerNoteVersions.AsNoTracking().ToListAsync(Ct));
        SetSession(other, newToken);
        Assert.IsType<NoContentResult>(await other.ReleaseEditSession(note.Id, Ct));
        Assert.Null((await ReadStoredNote(database, note.Id)).EditSessionId);
    }

    [Fact]
    public async Task ConcurrentPublicSavesWithOneRevisionOnlyCommitOneSnapshot()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var managerId = Assert.Single(await SeedManagers(database, 1));
        await using var firstDb = database.CreateContext();
        await using var secondDb = database.CreateContext();
        var first = CreateController(firstDb, managerId);
        var second = CreateController(secondDb, managerId);
        var note = Read(await first.CreateNote(new() { Visibility = "public", Content = "Original" }, Ct), 201);
        var token = Guid.NewGuid();
        SetSession(first, token);
        SetSession(second, token);
        Read(await first.AcquireEditSession(note.Id, Ct), 200);

        var results = await Race(
            () => first.UpdateNote(note.Id, new() { Title = note.Title, Content = "First", ExpectedRevision = note.Revision }, Ct),
            () => second.UpdateNote(note.Id, new() { Title = note.Title, Content = "Second", ExpectedRevision = note.Revision }, Ct));

        Assert.Single(results, item => item.Result is ConflictResult);
        var winner = Read(Assert.Single(results, item => item.Result is OkObjectResult), 200);
        Assert.Equal(note.Revision + 1, winner.Revision);
        Assert.IsType<ConflictResult>((await first.UpdateNote(note.Id, new() { Content = "Stale", ExpectedRevision = note.Revision }, Ct)).Result);
        var unchanged = Read(await first.UpdateNote(note.Id, new()
        {
            Title = winner.Title, Content = winner.Content, Color = winner.Color, ExpectedRevision = winner.Revision,
        }, Ct), 200);
        Assert.Equal(winner.Revision, unchanged.Revision);
        Assert.Equal(winner.UpdatedAtUtc, unchanged.UpdatedAtUtc);
        await using var verify = database.CreateContext();
        var stored = await verify.ManagerNotes.AsNoTracking().SingleAsync(row => row.Id == note.Id, Ct);
        Assert.Equal(winner.Content, stored.Content);
        Assert.Equal(winner.Revision, stored.Revision);
        var snapshot = Assert.Single(await verify.ManagerNoteVersions.AsNoTracking().ToListAsync(Ct));
        Assert.Equal(note.Content, snapshot.Content);
        Assert.Equal(note.Revision, snapshot.Revision);
    }

    [Fact]
    public async Task PublicManagementRequiresCurrentOwnerLeaseExceptIdlePin()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var ids = await SeedManagers(database, 2);
        await using var ownerDb = database.CreateContext();
        await using var otherDb = database.CreateContext();
        var owner = CreateController(ownerDb, ids[0]);
        var other = CreateController(otherDb, ids[1]);
        var (folder, tag) = await SeedNoteOrganization(ownerDb, ids[0]);
        var note = Read(await owner.CreateNote(new()
        {
            Visibility = "public", Content = "Original", FolderId = folder.Id, TagIds = [tag.Id],
        }, Ct), 201);
        note = Read(await owner.UpdateMetadata(note.Id, new() { IsPinned = true, ExpectedRevision = note.Revision }, Ct), 200);
        Assert.True(note.IsPinned);
        AssertSessionConflict(await owner.DeleteNote(note.Id, Ct));
        SetSession(other, Guid.NewGuid());
        Read(await other.AcquireEditSession(note.Id, Ct), 200);
        AssertSessionConflict((await owner.UpdateMetadata(note.Id, new() { IsPinned = false, ExpectedRevision = note.Revision }, Ct)).Result);
        Assert.Equal(403, Assert.IsType<StatusCodeResult>((await other.UpdateMetadata(note.Id, new()
        {
            IsLocked = true, ExpectedRevision = note.Revision,
        }, Ct)).Result).StatusCode);
        Assert.IsType<NoContentResult>(await other.ReleaseEditSession(note.Id, Ct));
        SetSession(owner, Guid.NewGuid());
        Read(await owner.AcquireEditSession(note.Id, Ct), 200);
        note = Read(await owner.UpdateNote(note.Id, new() { Content = "Updated", ExpectedRevision = note.Revision }, Ct), 200);
        var snapshot = Assert.Single(Read(await owner.GetVersions(note.Id, Ct), 200));

        var missingPut = CreateController(ownerDb, ids[0]);
        var missingPatch = CreateController(ownerDb, ids[0]);
        var missingRestore = CreateController(ownerDb, ids[0]);
        foreach (var controller in new[] { missingPut, missingPatch, missingRestore })
            controller.Request.Headers["X-Notepad-Edit-Session"] = owner.Request.Headers["X-Notepad-Edit-Session"];
        Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>((await missingPut.UpdateNote(note.Id, new(), Ct)).Result).StatusCode);
        Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>((await missingPatch.UpdateMetadata(note.Id, new(), Ct)).Result).StatusCode);
        Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>((await missingRestore.RestoreVersion(note.Id, snapshot.Id, new(), Ct)).Result).StatusCode);

        var privateNote = Read(await owner.CreateNote(new() { Title = "Keep", Visibility = "custom", TagIds = [tag.Id], SharedWithManagerIds = [ids[1]] }, Ct), 201);
        privateNote = Read(await owner.UpdateNote(privateNote.Id, new() { Title = "Keep updated", ExpectedRevision = privateNote.Revision }, Ct), 200);
        var foreignVersion = Assert.Single(Read(await owner.GetVersions(privateNote.Id, Ct), 200));
        Assert.IsType<NotFoundResult>((await owner.RestoreVersion(note.Id, foreignVersion.Id, new() { ExpectedRevision = note.Revision }, Ct)).Result);
        var wrongOwnerTab = CreateController(ownerDb, ids[0]);
        SetSession(wrongOwnerTab, Guid.NewGuid());
        AssertSessionConflict(await wrongOwnerTab.DeleteNote(note.Id, Ct));
        AssertSessionConflict((await wrongOwnerTab.RestoreVersion(note.Id, snapshot.Id, new() { ExpectedRevision = note.Revision }, Ct)).Result);
        Assert.Equal(note.Revision, (await ReadStoredNote(database, note.Id)).Revision);
        Assert.Single(Read(await owner.GetVersions(note.Id, Ct), 200));

        note = Read(await owner.UpdateMetadata(note.Id, new() { IsLocked = true, ExpectedRevision = note.Revision }, Ct), 200);
        Assert.IsType<NoContentResult>(await owner.ReleaseEditSession(note.Id, Ct));
        AssertSessionConflict((await other.AcquireEditSession(note.Id, Ct)).Result);
        Read(await owner.AcquireEditSession(note.Id, Ct), 200);
        Assert.Equal(423, Assert.IsType<StatusCodeResult>((await owner.UpdateNote(note.Id, new() { Content = "Locked", ExpectedRevision = note.Revision }, Ct)).Result).StatusCode);
        Assert.Equal(423, Assert.IsType<StatusCodeResult>((await owner.RestoreVersion(note.Id, snapshot.Id, new() { ExpectedRevision = note.Revision }, Ct)).Result).StatusCode);
        note = Read(await owner.UpdateMetadata(note.Id, new() { IsLocked = false, ExpectedRevision = note.Revision }, Ct), 200);
        var restored = Read(await owner.RestoreVersion(note.Id, snapshot.Id, new() { ExpectedRevision = note.Revision }, Ct), 200);
        Assert.Equal(note.Revision + 1, restored.Revision);
        Assert.Equal(snapshot.Content, restored.Content);
        Assert.Equal(new[] { note.Content, snapshot.Content }, Read(await owner.GetVersions(note.Id, Ct), 200).Select(version => version.Content));
        Assert.IsType<NoContentResult>(await owner.DeleteNote(note.Id, Ct));
        Assert.IsType<NoContentResult>(await other.DeleteNote(note.Id, Ct));

        await using var verify = database.CreateContext();
        Assert.Equal(privateNote.Id, Assert.Single(await verify.ManagerNotes.AsNoTracking().ToListAsync(Ct)).Id);
        Assert.Equal(privateNote.Id, Assert.Single(await verify.ManagerNoteVersions.AsNoTracking().ToListAsync(Ct)).NoteId);
        Assert.Equal(privateNote.Id, Assert.Single(await verify.ManagerNoteTagLinks.AsNoTracking().ToListAsync(Ct)).NoteId);
        Assert.Equal(privateNote.Id, Assert.Single(await verify.ManagerNoteShares.AsNoTracking().ToListAsync(Ct)).NoteId);
        Assert.Equal(folder.Id, Assert.Single(await verify.ManagerNoteFolders.AsNoTracking().ToListAsync(Ct)).Id);
    }

    private static async Task<T[]> Race<T>(Func<Task<T>> first, Func<Task<T>> second)
    {
        using var ready = new Barrier(2);
        Task<T> Start(Func<Task<T>> action) => Task.Run(async () =>
        {
            Assert.True(ready.SignalAndWait(TimeSpan.FromSeconds(10)));
            return await action();
        });
        return await Task.WhenAll(Start(first), Start(second));
    }

    private static async Task<ManagerNoteModel> ReadStoredNote(SqliteTestDatabase database, int id)
    {
        await using var verify = database.CreateContext();
        return await verify.ManagerNotes.AsNoTracking().SingleAsync(note => note.Id == id, Ct);
    }

    private static void SetSession(ManagerNotepadController controller, Guid token)
        => controller.Request.Headers["X-Notepad-Edit-Session"] = token.ToString();

    private static void AssertSessionConflict(IActionResult? result)
    {
        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(409, response.StatusCode);
        Assert.Equal("notepad_edit_session_conflict", Assert.IsType<ProblemDetails>(response.Value).Extensions["code"]);
    }
}
