using System.Security.Claims;
using DataAccessLayer.Models;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Auth;
using WebApi.Controllers;

namespace GF3.Tests;

public sealed class ManagerNotepadControllerTests
{
    [Fact]
    public async Task NotesAndWindowState_ArePersistedAndIsolatedPerManager()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        int firstManagerId;
        int secondManagerId;
        int firstNoteId;

        await using (var seedContext = database.CreateContext())
        {
            var now = DateTimeOffset.UtcNow;
            var first = CreateManager("first.manager", now);
            var second = CreateManager("second.manager", now);
            seedContext.ManagerAccounts.AddRange(first, second);
            await seedContext.SaveChangesAsync();
            firstManagerId = first.Id;
            secondManagerId = second.Id;
        }

        await using (var firstContext = database.CreateContext())
        {
            var controller = CreateController(firstContext, firstManagerId);
            var createdResult = await controller.CreateNote(new SaveManagerNoteRequest
            {
                Title = "Release checklist",
                Content = "Verify the schedule before publishing.",
                Color = "blue",
            }, CancellationToken.None);
            var created = Assert.IsType<ManagerNoteDto>(Assert.IsType<ObjectResult>(createdResult.Result).Value);
            Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(createdResult.Result).StatusCode);
            firstNoteId = created.Id;

            Assert.IsType<NoContentResult>(await controller.SaveState(new SaveManagerNotepadStateRequest
            {
                IsExpanded = true,
                IsPinned = true,
                Height = 680,
            }, CancellationToken.None));
        }

        await using (var secondContext = database.CreateContext())
        {
            var controller = CreateController(secondContext, secondManagerId);
            await controller.CreateNote(new SaveManagerNoteRequest
            {
                Title = "Private manager note",
                Content = "Only the second manager can see this.",
                Color = "rose",
            }, CancellationToken.None);
            var foreignUpdate = await controller.UpdateNote(firstNoteId, new SaveManagerNoteRequest
            {
                Title = "Changed",
                Content = "Changed",
                Color = "slate",
            }, CancellationToken.None);
            Assert.IsType<NotFoundResult>(foreignUpdate.Result);
        }

        await using (var verificationContext = database.CreateContext())
        {
            var controller = CreateController(verificationContext, firstManagerId);
            var result = await controller.GetCurrent(CancellationToken.None);
            var notepad = Assert.IsType<ManagerNotepadDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
            var note = Assert.Single(notepad.Notes);
            Assert.Equal("Release checklist", note.Title);
            Assert.Equal("blue", note.Color);
            Assert.True(notepad.State.IsExpanded);
            Assert.True(notepad.State.IsPinned);
            Assert.Equal(680, notepad.State.Height);
        }
    }

    private static readonly ServiceProvider MvcServices = new ServiceCollection()
        .AddLogging().AddMvcCore().Services.BuildServiceProvider();
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task MigrationPreservesLegacyNotesAndPanelState()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261123140000_AddCommunicationContentBlocks");
        var manager = CreateManager("legacy", DateTimeOffset.UtcNow);
        db.ManagerAccounts.Add(manager);
        await db.SaveChangesAsync();
        var now = DateTimeOffset.UtcNow;
        const string original = "<b>Legacy</b><br>Keep this text";
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO manager_note (id, manager_account_id, title, content, color, created_at_utc, updated_at_utc)
            VALUES (81, {manager.Id}, 'Legacy', {original}, 'blue', {now}, {now})
            """);
        var controller = CreateController(db, manager.Id);
        await controller.SaveState(new SaveManagerNotepadStateRequest { Height = 10_000, IsExpanded = true, IsPinned = true }, Ct);
        await migrator.MigrateAsync();
        var current = Read(await controller.GetCurrent(Ct), 200);
        var note = Assert.Single(current.Notes);
        Assert.Equal(81, note.Id);
        Assert.Equal(original, note.Content);
        Assert.Equal("private", note.Visibility);
        Assert.Equal(1L, note.Revision);
        Assert.False(note.IsPinned);
        Assert.False(note.IsLocked);
        Assert.Null(note.FolderId);
        Assert.Empty(note.TagIds);
        Assert.Empty(note.SharedWithManagerIds);
        Assert.True(current.State.IsExpanded);
        Assert.True(current.State.IsPinned);
        Assert.Equal(900, current.State.Height);
        await migrator.MigrateAsync("20261123140000_AddCommunicationContentBlocks");
        Assert.Equal(original, await db.Database.SqlQueryRaw<string>("SELECT content AS Value FROM manager_note").SingleAsync());
        Assert.Equal(900, await db.Database.SqlQueryRaw<int>("SELECT height AS Value FROM manager_notepad_state").SingleAsync());
    }

    [Fact]
    public async Task SharedReadersCannotManageAndRevocationRemovesReadAccess()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var ids = await SeedManagers(database, 3);
        await using var db = database.CreateContext();
        var owner = CreateController(db, ids[0]);
        var recipient = CreateController(db, ids[1]);
        var other = CreateController(db, ids[2]);
        var (folder, tag) = await SeedNoteOrganization(db, ids[0]);
        var note = Read(await owner.CreateNote(new()
        {
            Title = "Shared", FolderId = folder.Id, TagIds = [tag.Id],
        }, Ct), 201);

        Assert.IsType<NotFoundResult>((await recipient.GetNote(note.Id, Ct)).Result);
        Assert.IsType<NotFoundResult>((await recipient.GetVersions(note.Id, Ct)).Result);
        Assert.IsType<NoContentResult>(await recipient.DeleteNote(note.Id, Ct));
        note = Read(await owner.UpdateMetadata(note.Id, new() { Visibility = "public", ExpectedRevision = note.Revision }, Ct), 200);
        var shared = Read(await other.GetNote(note.Id, Ct), 200);
        Assert.False(shared.CanManage);
        Assert.False(shared.CanEdit);
        Assert.Null(shared.FolderId);
        Assert.Empty(shared.TagIds);
        Assert.Empty(shared.SharedWithManagerIds);
        foreach (var reader in new[] { recipient, other })
        {
            var current = Read(await reader.GetCurrent(Ct), 200);
            Assert.Single(current.Notes);
            Assert.Empty(current.Folders);
            Assert.Empty(current.Tags);
            Assert.Equal(403, Assert.IsType<StatusCodeResult>((await reader.UpdateNote(note.Id, new() { Title = "Forbidden" }, Ct)).Result).StatusCode);
            Assert.Equal(403, Assert.IsType<StatusCodeResult>((await reader.UpdateMetadata(note.Id, new() { ExpectedRevision = note.Revision, IsPinned = true }, Ct)).Result).StatusCode);
            Assert.IsType<NotFoundResult>((await reader.GetVersions(note.Id, Ct)).Result);
            Assert.Equal(403, Assert.IsType<StatusCodeResult>(await reader.DeleteNote(note.Id, Ct)).StatusCode);
        }
        var targets = Read(await owner.GetShareTargets(Ct), 200);
        Assert.Equal(ids.Skip(1), targets.Select(target => target.Id));

        note = Read(await owner.UpdateMetadata(note.Id, new()
        {
            Visibility = "custom", SharedWithManagerIds = [ids[1]], ExpectedRevision = note.Revision,
        }, Ct), 200);
        Assert.Single(Read(await recipient.GetCurrent(Ct), 200).Notes);
        Assert.IsType<NotFoundResult>((await other.GetNote(note.Id, Ct)).Result);
        Assert.Empty(Read(await recipient.GetNote(note.Id, Ct), 200).SharedWithManagerIds);
        note = Read(await owner.UpdateMetadata(note.Id, new() { Visibility = "public", ExpectedRevision = note.Revision }, Ct), 200);
        Assert.Empty(note.SharedWithManagerIds);
        note = Read(await owner.UpdateMetadata(note.Id, new() { Visibility = "custom", ExpectedRevision = note.Revision }, Ct), 200);
        Assert.IsType<NotFoundResult>((await recipient.GetNote(note.Id, Ct)).Result);
        note = Read(await owner.UpdateMetadata(note.Id, new() { SharedWithManagerIds = [ids[1]], ExpectedRevision = note.Revision }, Ct), 200);
        await owner.UpdateMetadata(note.Id, new() { Visibility = "private", ExpectedRevision = note.Revision }, Ct);
        Assert.Empty(await db.ManagerNoteShares.ToListAsync());
        Assert.Empty(Read(await recipient.GetCurrent(Ct), 200).Notes);
    }

    [Fact]
    public async Task LegacyOrganizationFieldsStayOwnerScopedAndSurviveMetadataUpdates()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var ids = await SeedManagers(database, 2);
        await using var db = database.CreateContext();
        var owner = CreateController(db, ids[0]);
        var (folder, tag) = await SeedNoteOrganization(db, ids[0]);
        var (foreignFolder, foreignTag) = await SeedNoteOrganization(db, ids[1]);
        var note = Read(await owner.CreateNote(new() { FolderId = folder.Id, TagIds = [tag.Id] }, Ct), 201);

        var invalid = await owner.UpdateNote(note.Id, new()
        {
            Title = "Do not save", FolderId = foreignFolder.Id, TagIds = [foreignTag.Id], ExpectedRevision = note.Revision,
        }, Ct);
        Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>(invalid.Result).StatusCode);
        Assert.Equal(note.Revision, Read(await owner.GetNote(note.Id, Ct), 200).Revision);
        Assert.Empty(Read(await owner.GetVersions(note.Id, Ct), 200));
        Assert.Equal(1, Assert.Single(Read(await owner.GetCurrent(Ct), 200).Folders).NoteCount);
        Assert.Equal(1, Assert.Single(Read(await owner.GetCurrent(Ct), 200).Tags).NoteCount);
        var updated = Read(await CreateController(db, ids[0]).UpdateMetadata(note.Id, new()
        {
            IsPinned = true, ExpectedRevision = note.Revision,
        }, Ct), 200);
        Assert.Equal(folder.Id, updated.FolderId);
        Assert.Equal(new[] { tag.Id }, updated.TagIds);
        Assert.Empty(Read(await owner.GetVersions(note.Id, Ct), 200));
    }

    private static async Task<(ManagerNoteFolderModel Folder, ManagerNoteTagModel Tag)> SeedNoteOrganization(
        DataAccessLayer.Models.DataBaseContext.AppDbContext db, int managerId)
    {
        var now = DateTimeOffset.UtcNow;
        var folder = new ManagerNoteFolderModel { ManagerAccountId = managerId, Name = "Legacy folder", CreatedAtUtc = now, UpdatedAtUtc = now };
        var tag = new ManagerNoteTagModel { ManagerAccountId = managerId, Name = "Legacy tag", CreatedAtUtc = now };
        db.AddRange(folder, tag);
        await db.SaveChangesAsync(Ct);
        return (folder, tag);
    }

    [Fact]
    public async Task RevisionsLockAndRestoreWithBoundedHistory()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var ids = await SeedManagers(database, 2);
        await using var db = database.CreateContext();
        await using var concurrentDb = database.CreateContext();
        var owner = CreateController(db, ids[0]);
        var concurrent = CreateController(concurrentDb, ids[0]);
        var initial = Read(await owner.CreateNote(new() { Title = "First", Content = "<b>First</b>" }, Ct), 201);
        var updated = Read(await owner.UpdateNote(initial.Id, new()
        {
            Title = "Second", Content = "Changed", ExpectedRevision = initial.Revision,
        }, Ct), 200);
        Assert.Equal(initial.Revision + 1, updated.Revision);
        var original = Assert.Single(Read(await owner.GetVersions(initial.Id, Ct), 200));
        Assert.Equal(initial.Content, original.Content);
        var stale = await concurrent.UpdateNote(initial.Id, new()
        {
            Title = "Stale", ExpectedRevision = initial.Revision, Visibility = "custom", SharedWithManagerIds = [ids[1]],
        }, Ct);
        Assert.IsType<ConflictResult>(stale.Result);
        Assert.Empty(await db.ManagerNoteShares.ToListAsync());
        var unchanged = Read(await owner.UpdateNote(initial.Id, new()
        {
            Title = updated.Title, Content = updated.Content, ExpectedRevision = updated.Revision,
        }, Ct), 200);
        Assert.Equal(updated.Revision, unchanged.Revision);
        var locked = Read(await owner.UpdateMetadata(initial.Id, new()
        {
            IsPinned = true, IsLocked = true, ExpectedRevision = updated.Revision,
        }, Ct), 200);
        Assert.True(locked.IsPinned);
        Assert.False(locked.CanEdit);
        Assert.Equal(updated.Revision + 1, locked.Revision);
        Assert.Single(Read(await owner.GetVersions(initial.Id, Ct), 200));
        Assert.IsType<ConflictResult>((await concurrent.UpdateMetadata(initial.Id, new()
        {
            IsPinned = false, ExpectedRevision = updated.Revision,
        }, Ct)).Result);
        Assert.Equal(423, Assert.IsType<StatusCodeResult>((await owner.UpdateNote(initial.Id, new()
        {
            Title = "Locked", ExpectedRevision = locked.Revision,
        }, Ct)).Result).StatusCode);
        Assert.Equal(423, Assert.IsType<StatusCodeResult>((await owner.RestoreVersion(initial.Id, original.Id, new()
        {
            ExpectedRevision = locked.Revision,
        }, Ct)).Result).StatusCode);
        var unlocked = Read(await owner.UpdateMetadata(initial.Id, new()
        {
            IsLocked = false, ExpectedRevision = locked.Revision,
        }, Ct), 200);
        var restored = Read(await owner.RestoreVersion(initial.Id, original.Id, new() { ExpectedRevision = unlocked.Revision }, Ct), 200);
        Assert.Equal(original.Title, restored.Title);
        Assert.Equal(original.Content, restored.Content);
        Assert.Equal(unlocked.Revision + 1, restored.Revision);
        Assert.Equal(new[] { "Second", "First" }, Read(await owner.GetVersions(initial.Id, Ct), 200).Select(v => v.Title));
        for (var i = 0; i < 52; i++)
        {
            restored = Read(await owner.UpdateNote(initial.Id, new() { Title = $"Edit {i}", ExpectedRevision = restored.Revision }, Ct), 200);
        }
        Assert.Equal(50, await db.ManagerNoteVersions.CountAsync());
        Assert.Equal(50, Read(await owner.GetVersions(initial.Id, Ct), 200).Count);
        Assert.IsType<NoContentResult>(await owner.DeleteNote(initial.Id, Ct));
        Assert.Empty(await db.ManagerNoteVersions.ToListAsync());
    }

    [Fact]
    public async Task ValidationRejectsUnsupportedValuesAndAcceptsChecklistMarkup()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var ids = await SeedManagers(database, 1);
        await using var db = database.CreateContext();
        var invalid = new SaveManagerNoteRequest[]
        {
            new() { Title = new string('a', 161) }, new() { Content = new string('a', 20_001) },
            new() { Color = "purple" }, new() { Visibility = "unknown" },
            new() { Visibility = "custom", SharedWithManagerIds = [999] },
            new() { Visibility = "custom", SharedWithManagerIds = [ids[0]] },
            new() { Content = "<svg><script>bad</script></svg>" },
            new() { Content = "<b onclick=bad>invalid</b>" },
            new() { Content = "<span style=\"color: url(example)\">invalid</span>" },
        };
        foreach (var request in invalid)
        {
            var response = await CreateController(db, ids[0]).CreateNote(request, Ct);
            Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>(response.Result).StatusCode);
        }
        var owner = CreateController(db, ids[0]);
        const string checklist = """<ul data-checklist="true"><li data-checked="false"><font color="#2563eb" size="3">Text</font></li></ul><span style="color: rgb(1, 2, 3); font-size: 16px;">Safe</span>""";
        var saved = Read(await owner.CreateNote(new() { Title = "   ", Content = checklist }, Ct), 201);
        Assert.Equal("Untitled note", saved.Title);
        Assert.Equal(checklist, saved.Content);
        Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>((await owner.UpdateMetadata(saved.Id, new() { IsPinned = true }, Ct)).Result).StatusCode);
    }

    private static T Read<T>(ActionResult<T> response, int status)
    {
        var result = Assert.IsAssignableFrom<ObjectResult>(response.Result);
        Assert.Equal(status, result.StatusCode);
        return Assert.IsAssignableFrom<T>(result.Value);
    }

    private static async Task<int[]> SeedManagers(SqliteTestDatabase database, int count)
    {
        await using var db = database.CreateContext();
        var managers = Enumerable.Range(1, count).Select(index => CreateManager($"manager{index}", DateTimeOffset.UtcNow)).ToArray();
        db.ManagerAccounts.AddRange(managers);
        await db.SaveChangesAsync();
        return managers.Select(manager => manager.Id).ToArray();
    }

    private static ManagerAccountModel CreateManager(string username, DateTimeOffset now) => new()
    {
        Username = username,
        DisplayName = username,
        PasswordHash = "test-hash",
        PasswordUpdatedAtUtc = now,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    private static ManagerNotepadController CreateController(
        DataAccessLayer.Models.DataBaseContext.AppDbContext context,
        int managerId)
    {
        var controller = new ManagerNotepadController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = MvcServices,
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, $"manager-{managerId}"),
                    new Claim(ClaimTypes.Role, AuthRoles.Manager),
                    new Claim("manager_id", managerId.ToString()),
                ],
                JwtAuthenticationDefaults.SchemeName)),
            },
        };
        return controller;
    }
}
