using System.Security.Claims;
using System.Text.RegularExpressions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/manager-notepad")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class ManagerNotepadController(AppDbContext db, IRealtimeNotifier? realtimeNotifier = null) : ControllerBase
{
    private const int DefaultHeight = 520;
    private const int MinHeight = 320;
    private const int MaxHeight = 900;
    private const int MaxTitleLength = 160;
    private const int MaxContentLength = 20_000;
    private const int EditLeaseMilliseconds = 60_000;
    private const int MaxVersions = 50;
    private static readonly HashSet<string> AllowedColors =
        ["yellow", "blue", "green", "rose", "slate"];
    private static readonly HashSet<string> AllowedVisibilities = ["private", "public", "custom"];

    private const string HtmlTags = "b|strong|i|em|u|s|strike|p|div|br|ul|ol|li|span|font|blockquote";
    private const string HtmlColor = @"(?:#[0-9a-f]{3,8}|rgba?\([0-9 .,/%]+\)|[a-z]+)";
    private const string HtmlStyle = "color: *" + HtmlColor +
        @"|font-size: *[0-9]+(?:\.[0-9]+)?(?:px|rem|em|%)|text-align: *(?:left|center|right|justify)|line-height: *(?:1|1\.15|1\.5|1\.75|2)";
    private const string HtmlStyleAttribute = """(?: +style="(?:{{CSS}})(?:; +(?:{{CSS}}))*;? *")?""";
    private static readonly Regex HtmlStart = new(@"<[a-zA-Z!/?]", RegexOptions.NonBacktracking);
    private static readonly Regex SafeHtmlTag = new(
        """\G(?:</(?:{{TAGS}}) *>|<(?:(?:b|strong|i|em|u|s|strike|p|div|br|ol|span|blockquote){{STYLE}}|ul(?: +data-checklist="true")?{{STYLE}}|li(?: +data-checked="(?:true|false)")?{{STYLE}}|font(?: +color="{{COLOR}}")?(?: +size="[1-7]")?{{STYLE}}) */?>)"""
            .Replace("{{TAGS}}", HtmlTags, StringComparison.Ordinal)
            .Replace("{{STYLE}}", HtmlStyleAttribute, StringComparison.Ordinal)
            .Replace("{{CSS}}", HtmlStyle, StringComparison.Ordinal)
            .Replace("{{COLOR}}", HtmlColor, StringComparison.Ordinal),
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    [HttpGet]
    public async Task<ActionResult<ManagerNotepadDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        var notes = await GetReadableNotes(managerId).ToListAsync(cancellationToken).ConfigureAwait(false);
        var state = await db.ManagerNotepadStates.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ManagerAccountId == managerId, cancellationToken)
            .ConfigureAwait(false);
        var folders = await GetFolders(managerId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var tags = await GetTags(managerId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var editors = await LoadEditors(notes, cancellationToken);
        return Ok(new ManagerNotepadDto
        {
            Notes = notes.OrderByDescending(note => note.IsPinned).ThenByDescending(note => note.UpdatedAtUtc)
                .ThenByDescending(note => note.Id).Select(note => ToDto(note, managerId, editors)).ToList(),
            State = ToDto(state),
            Folders = folders,
            Tags = tags,
        });
    }

    [HttpGet("notes/{noteId:int}")]
    public async Task<ActionResult<ManagerNoteDto>> GetNote(int noteId, CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        var note = await FindReadableNote(noteId, managerId, cancellationToken).ConfigureAwait(false);
        return note is null ? NotFound() : Ok(ToDto(note, managerId, await LoadEditors([note], cancellationToken)));
    }

    [HttpPost("notes/{noteId:int}/edit-session")]
    public Task<ActionResult<ManagerNoteDto>> AcquireEditSession(int noteId, CancellationToken cancellationToken)
        => UpdateEditSession(noteId, acquire: true, cancellationToken);

    [HttpPut("notes/{noteId:int}/edit-session")]
    public Task<ActionResult<ManagerNoteDto>> RenewEditSession(int noteId, CancellationToken cancellationToken)
        => UpdateEditSession(noteId, acquire: false, cancellationToken);

    private async Task<ActionResult<ManagerNoteDto>> UpdateEditSession(int noteId, bool acquire, CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        if (EditSessionToken() is not Guid token) return BadRequest();
        // SQLite starts a write transaction before calculating the lease, including when writers contend.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var eligible = db.ManagerNotes.Where(note => note.Id == noteId && note.Visibility == "public" &&
            (!note.IsLocked || note.ManagerAccountId == managerId));
        var own = eligible.Where(note => note.EditSessionId == token && note.EditManagerId == managerId &&
            note.EditExpiresAtMs > now);
        var changed = await own.ExecuteUpdateAsync(setters => setters
            .SetProperty(note => note.EditExpiresAtMs, now + EditLeaseMilliseconds), cancellationToken);
        var started = false;
        if (changed == 0 && acquire)
        {
            changed = await eligible.Where(note => note.EditExpiresAtMs == null || note.EditExpiresAtMs <= now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(note => note.EditSessionId, token)
                    .SetProperty(note => note.EditManagerId, managerId)
                    .SetProperty(note => note.EditStartedAtMs, now)
                    .SetProperty(note => note.EditExpiresAtMs, now + EditLeaseMilliseconds), cancellationToken);
            started = changed != 0;
        }
        var current = await FindReadableNote(noteId, managerId, cancellationToken);
        if (current is null || current.Visibility != "public") return NotFound();
        if (changed == 0) return EditSessionConflict();
        var editors = await LoadEditors([current], cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (started) await NotifyPublicNotesChanged("edit-session-started");
        return Ok(ToDto(current, managerId, editors));
    }

    [HttpDelete("notes/{noteId:int}/edit-session")]
    public async Task<IActionResult> ReleaseEditSession(int noteId, CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        if (EditSessionToken() is not Guid token) return BadRequest();
        var released = await db.ManagerNotes.Where(note => note.Id == noteId && note.EditSessionId == token && note.EditManagerId == managerId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(note => note.EditSessionId, (Guid?)null)
                .SetProperty(note => note.EditManagerId, (int?)null)
                .SetProperty(note => note.EditStartedAtMs, (long?)null)
                .SetProperty(note => note.EditExpiresAtMs, (long?)null), cancellationToken);
        if (released != 0) await NotifyPublicNotesChanged("edit-session-released");
        return NoContent();
    }

    private Task NotifyPublicNotesChanged(string reason)
        => PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier?.NotifyManagerDataChangedAsync(
            "manager-notepad", null, reason) ?? Task.CompletedTask);

    private Guid? EditSessionToken() =>
        Guid.TryParse(HttpContext?.Request.Headers["X-Notepad-Edit-Session"].ToString(), out var token) && token != Guid.Empty
            ? token : null;

    private ObjectResult EditSessionConflict() => Conflict(new ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "The note edit session is unavailable.",
        Extensions = { ["code"] = "notepad_edit_session_conflict" },
    });

    private IQueryable<ManagerNoteModel> WritableNotes(int managerId, bool allowUnleasedPin = false)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var token = EditSessionToken();
        return db.ManagerNotes.Where(note => note.Visibility != "public" ||
            (token != null && note.EditSessionId == token && note.EditManagerId == managerId && note.EditExpiresAtMs > now) ||
            (allowUnleasedPin && (note.EditExpiresAtMs == null || note.EditExpiresAtMs <= now)));
    }

    private async Task<Dictionary<int, string>> LoadEditors(IEnumerable<ManagerNoteModel> notes, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var ids = notes.Where(note => note.Visibility == "public" && note.EditExpiresAtMs > now)
            .Select(note => note.EditManagerId).OfType<int>().Distinct().ToArray();
        if (ids.Length == 0) return [];
        return await db.ManagerAccounts.AsNoTracking().Where(manager => ids.Contains(manager.Id))
            .ToDictionaryAsync(manager => manager.Id, manager => manager.DisplayName, cancellationToken);
    }

    [HttpPost("notes")]
    public async Task<ActionResult<ManagerNoteDto>> CreateNote(
        [FromBody] SaveManagerNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeNote(request, out var title, out var content, out var color))
        {
            return ValidationProblem(ModelState);
        }

        var managerId = GetRequiredManagerId();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var note = new ManagerNoteModel
        {
            ManagerAccountId = managerId,
            LastEditedByManagerId = managerId,
            Title = title,
            Content = content,
            Color = color,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        var metadata = await ValidateMetadata(note, request, cancellationToken).ConfigureAwait(false);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        note.Visibility = metadata.Visibility;
        note.FolderId = metadata.FolderId;
        note.TagLinks = metadata.TagIds.Select(id => new ManagerNoteTagLinkModel { TagId = id }).ToList();
        note.Shares = metadata.RecipientIds.Select(id => new ManagerNoteShareModel { ManagerAccountId = id }).ToList();
        db.ManagerNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        if (note.Visibility == "public") await NotifyPublicNotesChanged("public-notes-changed");
        return StatusCode(StatusCodes.Status201Created, ToDto(note, managerId));
    }

    [HttpPut("notes/{noteId:int}")]
    public Task<ActionResult<ManagerNoteDto>> UpdateNote(
        int noteId,
        [FromBody] SaveManagerNoteRequest request,
        CancellationToken cancellationToken)
        => UpdateOwnedNote(noteId, request, request, cancellationToken);

    [HttpPatch("notes/{noteId:int}/metadata")]
    public Task<ActionResult<ManagerNoteDto>> UpdateMetadata(
        int noteId,
        [FromBody] UpdateManagerNoteMetadataRequest request,
        CancellationToken cancellationToken)
        => UpdateOwnedNote(noteId, request, null, cancellationToken);

    private async Task<ActionResult<ManagerNoteDto>> UpdateOwnedNote(
        int noteId,
        ManagerNoteMetadataInput request,
        SaveManagerNoteRequest? text,
        CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var note = await FindReadableNote(noteId, managerId, cancellationToken);
        if (note is null) return NotFound();
        var owner = note.ManagerAccountId == managerId;
        if (!owner && (note.Visibility != "public" || text is null || request.Visibility is not null ||
            request.FolderId is not null || request.ClearFolder || request.TagIds is not null || request.SharedWithManagerIds is not null))
            return StatusCode(StatusCodes.Status403Forbidden);

        var pinOnly = request is UpdateManagerNoteMetadataRequest { IsPinned: not null, IsLocked: null } &&
            request.Visibility is null && request.FolderId is null && !request.ClearFolder &&
            request.TagIds is null && request.SharedWithManagerIds is null;
        if (!await WritableNotes(managerId, pinOnly).AnyAsync(item => item.Id == noteId, cancellationToken))
            return EditSessionConflict();
        if ((text is null || note.Visibility == "public") && request.ExpectedRevision is null)
            return MissingRevision();

        var expectedRevision = request.ExpectedRevision ?? note.Revision;
        if (note.Revision != expectedRevision)
        {
            return Conflict();
        }

        var title = note.Title;
        var content = note.Content;
        var color = note.Color;
        if (text is not null && !TryNormalizeNote(text, out title, out content, out color, note.Content))
        {
            return ValidationProblem(ModelState);
        }

        var textChanged = (title, content, color) != (note.Title, note.Content, note.Color);
        if (note.IsLocked && textChanged)
        {
            return StatusCode(StatusCodes.Status423Locked);
        }

        var metadata = owner
            ? await ValidateMetadata(note, request, cancellationToken).ConfigureAwait(false)
            : new NoteMetadata(note.Visibility, note.FolderId, note.IsPinned, note.IsLocked, [], []);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var oldTags = note.TagLinks.Select(link => link.TagId).ToHashSet();
        var oldRecipients = note.Shares.Select(share => share.ManagerAccountId).ToHashSet();
        var tagsChanged = owner && !oldTags.SetEquals(metadata.TagIds);
        var recipientsChanged = owner && !oldRecipients.SetEquals(metadata.RecipientIds);
        var metadataChanged = tagsChanged || recipientsChanged ||
            (metadata.Visibility, metadata.FolderId, metadata.IsPinned, metadata.IsLocked) !=
            (note.Visibility, note.FolderId, note.IsPinned, note.IsLocked);

        if (!textChanged && !metadataChanged)
        {
            return Ok(ToDto(note, managerId));
        }

        if (!await AdvanceRevision(note, expectedRevision, title, content, color, metadata, managerId, pinOnly, cancellationToken)
            .ConfigureAwait(false))
        {
            return Conflict();
        }

        if (textChanged)
        {
            AddSnapshot(note);
        }
        if (tagsChanged)
        {
            var removedTags = oldTags.Except(metadata.TagIds).ToArray();
            if (removedTags.Length > 0)
            {
                db.ManagerNoteTagLinks.RemoveRange(await db.ManagerNoteTagLinks
                    .Where(link => link.NoteId == noteId && removedTags.Contains(link.TagId))
                    .ToListAsync(cancellationToken).ConfigureAwait(false));
            }
            db.ManagerNoteTagLinks.AddRange(metadata.TagIds.Except(oldTags)
                .Select(id => new ManagerNoteTagLinkModel { NoteId = noteId, TagId = id }));
        }
        if (recipientsChanged)
        {
            var removedRecipients = oldRecipients.Except(metadata.RecipientIds).ToArray();
            if (removedRecipients.Length > 0)
            {
                db.ManagerNoteShares.RemoveRange(await db.ManagerNoteShares
                    .Where(share => share.NoteId == noteId && removedRecipients.Contains(share.ManagerAccountId))
                    .ToListAsync(cancellationToken).ConfigureAwait(false));
            }
            db.ManagerNoteShares.AddRange(metadata.RecipientIds.Except(oldRecipients)
                .Select(id => new ManagerNoteShareModel { NoteId = noteId, ManagerAccountId = id }));
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (textChanged)
        {
            await PruneVersions(noteId, cancellationToken).ConfigureAwait(false);
        }
        var saved = await FindReadableNote(noteId, managerId, cancellationToken);
        var editors = await LoadEditors([saved!], cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (note.Visibility == "public" || saved!.Visibility == "public")
            await NotifyPublicNotesChanged("public-notes-changed");
        return Ok(ToDto(saved!, managerId, editors));
    }

    [HttpDelete("notes/{noteId:int}")]
    public async Task<IActionResult> DeleteNote(int noteId, CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var wasPublic = await db.ManagerNotes.AnyAsync(note => note.Id == noteId &&
            note.ManagerAccountId == managerId && note.Visibility == "public", cancellationToken);
        var deleted = await WritableNotes(managerId).Where(note => note.Id == noteId && note.ManagerAccountId == managerId)
            .ExecuteDeleteAsync(cancellationToken);
        if (deleted != 0)
        {
            await transaction.CommitAsync(cancellationToken);
            if (wasPublic) await NotifyPublicNotesChanged("public-notes-changed");
            return NoContent();
        }
        var note = await FindReadableNote(noteId, managerId, cancellationToken);
        if (note is null) return NoContent();
        return note.ManagerAccountId == managerId ? EditSessionConflict() : StatusCode(StatusCodes.Status403Forbidden);
    }


    private IQueryable<ManagerNoteModel> ReadableNotes(int managerId)
        => db.ManagerNotes.Where(note => note.ManagerAccountId == managerId || note.Visibility == "public" ||
            (note.Visibility == "custom" && note.Shares.Any(share => share.ManagerAccountId == managerId)));

    private IQueryable<ManagerNoteModel> GetReadableNotes(int managerId)
        => ReadableNotes(managerId).AsNoTracking().AsSingleQuery()
            .Include(note => note.TagLinks.Where(link => db.ManagerNotes.Any(owned =>
                owned.Id == link.NoteId && owned.ManagerAccountId == managerId)))
            .Include(note => note.Shares.Where(share => db.ManagerNotes.Any(owned =>
                owned.Id == share.NoteId && owned.ManagerAccountId == managerId)));

    private Task<ManagerNoteModel?> FindReadableNote(int noteId, int managerId, CancellationToken cancellationToken)
        => GetReadableNotes(managerId).SingleOrDefaultAsync(note => note.Id == noteId, cancellationToken);

    private Task<ManagerNoteModel?> FindOwnedNote(int noteId, int managerId, CancellationToken cancellationToken)
        => db.ManagerNotes.AsNoTracking().AsSingleQuery().Include(note => note.TagLinks).Include(note => note.Shares)
            .SingleOrDefaultAsync(note => note.Id == noteId && note.ManagerAccountId == managerId, cancellationToken);

    private async Task<ActionResult> OwnerAccessFailure(int noteId, int managerId, CancellationToken cancellationToken)
        => await ReadableNotes(managerId).AnyAsync(note => note.Id == noteId, cancellationToken).ConfigureAwait(false)
            ? StatusCode(StatusCodes.Status403Forbidden)
            : NotFound();

    private ActionResult MissingRevision()
    {
        ModelState.AddModelError("ExpectedRevision", "The expected revision is required.");
        return ValidationProblem(ModelState);
    }

    private async Task<NoteMetadata> ValidateMetadata(
        ManagerNoteModel note,
        ManagerNoteMetadataInput request,
        CancellationToken cancellationToken)
    {
        var managerId = note.ManagerAccountId;
        var patch = request as UpdateManagerNoteMetadataRequest;
        var visibility = request.Visibility ?? note.Visibility;
        var folderId = request.ClearFolder ? null : request.FolderId ?? note.FolderId;
        var tagIds = request.TagIds?.ToHashSet() ?? note.TagLinks.Select(link => link.TagId).ToHashSet();
        var requestedRecipients = request.SharedWithManagerIds?.ToHashSet();
        var recipientIds = visibility == "custom"
            ? requestedRecipients ?? note.Shares.Select(share => share.ManagerAccountId).ToHashSet()
            : [];

        if (!AllowedVisibilities.Contains(visibility))
        {
            ModelState.AddModelError("Visibility", "Choose a supported note visibility.");
        }
        if (request.ClearFolder && request.FolderId.HasValue ||
            folderId.HasValue && !await db.ManagerNoteFolders.AnyAsync(folder =>
                folder.Id == folderId && folder.ManagerAccountId == managerId, cancellationToken).ConfigureAwait(false))
        {
            ModelState.AddModelError("FolderId", "Choose one of your folders.");
        }
        if (request.TagIds is not null && (request.TagIds.Count > 20 ||
            tagIds.Count != await db.ManagerNoteTags.CountAsync(tag =>
                tag.ManagerAccountId == managerId && tagIds.Contains(tag.Id), cancellationToken).ConfigureAwait(false)))
        {
            ModelState.AddModelError("TagIds", "Choose up to 20 of your tags.");
        }
        if (request.SharedWithManagerIds is not null && (request.SharedWithManagerIds.Count > 50 ||
            requestedRecipients!.Contains(managerId) ||
            requestedRecipients.Count != await db.ManagerAccounts.CountAsync(account =>
                requestedRecipients.Contains(account.Id), cancellationToken).ConfigureAwait(false)))
        {
            ModelState.AddModelError("SharedWithManagerIds", "Choose up to 50 other manager accounts.");
        }

        return new NoteMetadata(visibility, folderId, patch?.IsPinned ?? note.IsPinned,
            patch?.IsLocked ?? note.IsLocked, tagIds, recipientIds);
    }

    private sealed record NoteMetadata(string Visibility, int? FolderId, bool IsPinned, bool IsLocked,
        HashSet<int> TagIds, HashSet<int> RecipientIds);

    private async Task<bool> AdvanceRevision(ManagerNoteModel note, long expectedRevision, string title,
        string content, string color, NoteMetadata metadata, int managerId, bool allowUnleasedPin, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var changed = await WritableNotes(managerId, allowUnleasedPin).Where(item =>
                item.Id == note.Id && item.ManagerAccountId == note.ManagerAccountId && item.Revision == expectedRevision)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Title, title)
                .SetProperty(item => item.Content, content)
                .SetProperty(item => item.Color, color)
                .SetProperty(item => item.Visibility, metadata.Visibility)
                .SetProperty(item => item.FolderId, metadata.FolderId)
                .SetProperty(item => item.IsPinned, metadata.IsPinned)
                .SetProperty(item => item.IsLocked, metadata.IsLocked)
                .SetProperty(item => item.Revision, item => item.Revision + 1)
                .SetProperty(item => item.LastEditedByManagerId,
                    title != note.Title || content != note.Content || color != note.Color
                        ? managerId : note.LastEditedByManagerId)
                .SetProperty(item => item.EditSessionId, item => metadata.Visibility == "public" ? item.EditSessionId : null)
                .SetProperty(item => item.EditManagerId, item => metadata.Visibility == "public" ? item.EditManagerId : null)
                .SetProperty(item => item.EditStartedAtMs, item => metadata.Visibility == "public" ? item.EditStartedAtMs : null)
                .SetProperty(item => item.EditExpiresAtMs, item => metadata.Visibility == "public" ? item.EditExpiresAtMs : null)
                .SetProperty(item => item.UpdatedAtUtc, now), cancellationToken).ConfigureAwait(false);
        return changed == 1;
    }

    [HttpGet("share-targets")]
    public async Task<ActionResult<IReadOnlyList<ManagerNoteShareTargetDto>>> GetShareTargets(
        CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        return Ok(await db.ManagerAccounts.AsNoTracking().Where(account => account.Id != managerId)
            .OrderBy(account => account.DisplayName).ThenBy(account => account.Id)
            .Select(account => new ManagerNoteShareTargetDto(account.Id, account.DisplayName))
            .ToListAsync(cancellationToken).ConfigureAwait(false));
    }

    private IQueryable<ManagerNoteFolderDto> GetFolders(int managerId)
        => db.ManagerNoteFolders.Where(folder => folder.ManagerAccountId == managerId)
            .OrderBy(folder => folder.Name)
            .Select(folder => new ManagerNoteFolderDto(folder.Id, folder.Name,
                db.ManagerNotes.Count(note => note.ManagerAccountId == managerId && note.FolderId == folder.Id)));

    private IQueryable<ManagerNoteTagDto> GetTags(int managerId)
        => db.ManagerNoteTags.Where(tag => tag.ManagerAccountId == managerId)
            .OrderBy(tag => tag.Name)
            .Select(tag => new ManagerNoteTagDto(tag.Id, tag.Name, db.ManagerNoteTagLinks.Count(link => link.TagId == tag.Id)));

    [HttpGet("notes/{noteId:int}/versions")]
    public async Task<ActionResult<IReadOnlyList<ManagerNoteVersionDto>>> GetVersions(
        int noteId, CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        if (!await db.ManagerNotes.AnyAsync(note => note.Id == noteId && note.ManagerAccountId == managerId,
            cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }
        return Ok(await NoteVersions(noteId).Take(MaxVersions)
            .Select(version => new ManagerNoteVersionDto(version.Id, version.Revision, version.Title, version.Content,
                version.Color, version.SavedAtUtc, version.SavedByManagerId))
            .ToListAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("notes/{noteId:int}/restore/{versionId:long}")]
    public async Task<ActionResult<ManagerNoteDto>> RestoreVersion(
        int noteId, long versionId, [FromBody] RestoreManagerNoteRequest request, CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var note = await FindOwnedNote(noteId, managerId, cancellationToken).ConfigureAwait(false);
        if (note is null)
        {
            return await OwnerAccessFailure(noteId, managerId, cancellationToken).ConfigureAwait(false);
        }
        if (!await WritableNotes(managerId).AnyAsync(item => item.Id == noteId, cancellationToken))
            return EditSessionConflict();
        var version = await NoteVersions(noteId).SingleOrDefaultAsync(item => item.Id == versionId, cancellationToken)
            .ConfigureAwait(false);
        if (version is null)
        {
            return NotFound();
        }
        if (!request.ExpectedRevision.HasValue)
        {
            return MissingRevision();
        }
        if (note.Revision != request.ExpectedRevision.Value)
        {
            return Conflict();
        }
        if (note.IsLocked)
        {
            return StatusCode(StatusCodes.Status423Locked);
        }

        var metadata = await ValidateMetadata(note, new UpdateManagerNoteMetadataRequest(), cancellationToken)
            .ConfigureAwait(false);
        if (!await AdvanceRevision(note, request.ExpectedRevision.Value, version.Title, version.Content,
            version.Color, metadata, managerId, false, cancellationToken).ConfigureAwait(false))
        {
            return Conflict();
        }
        AddSnapshot(note);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PruneVersions(noteId, cancellationToken).ConfigureAwait(false);
        var saved = await FindOwnedNote(noteId, managerId, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        if (note.Visibility == "public") await NotifyPublicNotesChanged("public-notes-changed");
        return Ok(ToDto(saved!, managerId));
    }

    private IQueryable<ManagerNoteVersionModel> NoteVersions(int noteId)
        => db.ManagerNoteVersions.AsNoTracking().Where(version => version.NoteId == noteId)
            .OrderByDescending(version => version.Revision);

    private void AddSnapshot(ManagerNoteModel note)
        => db.ManagerNoteVersions.Add(new ManagerNoteVersionModel
        {
            NoteId = note.Id,
            Revision = note.Revision,
            Title = note.Title,
            Content = note.Content,
            Color = note.Color,
            SavedAtUtc = DateTimeOffset.UtcNow,
            SavedByManagerId = note.LastEditedByManagerId ?? note.ManagerAccountId,
        });

    private Task<int> PruneVersions(int noteId, CancellationToken cancellationToken)
    {
        var expired = NoteVersions(noteId).Skip(MaxVersions).Select(version => version.Id);
        return db.ManagerNoteVersions.Where(version => expired.Contains(version.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    [HttpPut("state")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SaveState(
        [FromBody] SaveManagerNotepadStateRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        var height = Math.Clamp(request.Height, MinHeight, MaxHeight);
        var updatedAtUtc = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO manager_notepad_state (
                manager_account_id,
                is_expanded,
                is_pinned,
                height,
                updated_at_utc
            ) VALUES ({managerId}, {request.IsExpanded}, {request.IsPinned}, {height}, {updatedAtUtc})
            ON CONFLICT(manager_account_id) DO UPDATE SET
                is_expanded = excluded.is_expanded,
                is_pinned = excluded.is_pinned,
                height = excluded.height,
                updated_at_utc = excluded.updated_at_utc
            """,
            cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private bool TryNormalizeNote(
        SaveManagerNoteRequest request,
        out string title,
        out string content,
        out string color,
        string? previousContent = null)
    {
        title = string.IsNullOrWhiteSpace(request.Title) ? "Untitled note" : request.Title.Trim();
        var submittedContent = request.Content ?? string.Empty;
        content = submittedContent;
        color = string.IsNullOrWhiteSpace(request.Color) ? "yellow" : request.Color.Trim().ToLowerInvariant();

        if (title.Length > MaxTitleLength)
        {
            ModelState.AddModelError(nameof(request.Title), $"Title must be at most {MaxTitleLength} characters.");
        }
        if (content.Length > MaxContentLength)
        {
            ModelState.AddModelError(nameof(request.Content), $"Content must be at most {MaxContentLength} characters.");
        }
        if (content != previousContent && content.Length <= MaxContentLength && HtmlStart.Matches(submittedContent).Any(match =>
            !SafeHtmlTag.Match(submittedContent, match.Index).Success))
        {
            ModelState.AddModelError(nameof(request.Content), "Note content contains unsupported HTML.");
        }
        if (!AllowedColors.Contains(color))
        {
            ModelState.AddModelError(nameof(request.Color), "Choose a supported note color.");
        }

        return ModelState.IsValid;
    }

    private int GetRequiredManagerId()
        => int.TryParse(User.FindFirstValue("manager_id"), out var managerId) && managerId > 0
            ? managerId
            : throw new BadHttpRequestException("The current manager session is invalid.");

    private static ManagerNoteDto ToDto(ManagerNoteModel note, int managerId, Dictionary<int, string>? editors = null)
    {
        var owner = note.ManagerAccountId == managerId;
        return new ManagerNoteDto
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            Color = note.Color,
            CreatedAtUtc = note.CreatedAtUtc,
            UpdatedAtUtc = note.UpdatedAtUtc,
            OwnerManagerId = note.ManagerAccountId,
            Visibility = note.Visibility,
            IsPinned = note.IsPinned,
            IsLocked = note.IsLocked,
            FolderId = owner ? note.FolderId : null,
            TagIds = owner ? note.TagLinks.Select(link => link.TagId).Order().ToArray() : [],
            SharedWithManagerIds = owner ? note.Shares.Select(share => share.ManagerAccountId).Order().ToArray() : [],
            Revision = note.Revision,
            CanEdit = (owner || note.Visibility == "public") && !note.IsLocked,
            ActiveEditor = note.Visibility == "public" && note.EditExpiresAtMs is long expires &&
                expires > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() && note.EditManagerId is int editorId &&
                editors?.TryGetValue(editorId, out var editorName) == true
                    ? new ManagerNoteEditorDto(editorName, expires) : null,
            CanManage = owner,
        };
    }

    private static ManagerNotepadStateDto ToDto(ManagerNotepadStateModel? state) => new()
    {
        IsExpanded = state?.IsExpanded ?? false,
        IsPinned = state?.IsPinned ?? false,
        Height = state?.Height ?? DefaultHeight,
    };
}

public sealed class ManagerNotepadDto
{
    public IReadOnlyList<ManagerNoteDto> Notes { get; init; } = [];
    public ManagerNotepadStateDto State { get; init; } = new();
    public IReadOnlyList<ManagerNoteFolderDto> Folders { get; init; } = [];
    public IReadOnlyList<ManagerNoteTagDto> Tags { get; init; } = [];
}

public sealed class ManagerNoteDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Color { get; init; } = "yellow";
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public int OwnerManagerId { get; init; }
    public string Visibility { get; init; } = "private";
    public bool IsPinned { get; init; }
    public bool IsLocked { get; init; }
    public int? FolderId { get; init; }
    public IReadOnlyList<int> TagIds { get; init; } = [];
    public IReadOnlyList<int> SharedWithManagerIds { get; init; } = [];
    public long Revision { get; init; }
    public bool CanEdit { get; init; }
    public bool CanManage { get; init; }
    public ManagerNoteEditorDto? ActiveEditor { get; init; }
}

public sealed record ManagerNoteEditorDto(string DisplayName, long ExpiresAtMs);
public sealed record ManagerNoteFolderDto(int Id, string Name, int NoteCount);
public sealed record ManagerNoteTagDto(int Id, string Name, int NoteCount);
public sealed record ManagerNoteShareTargetDto(int Id, string DisplayName);
public sealed record ManagerNoteVersionDto(long Id, long Revision, string Title, string Content, string Color,
    DateTimeOffset SavedAtUtc, int SavedByManagerId);

public abstract class ManagerNoteMetadataInput
{
    public string? Visibility { get; init; }
    public int? FolderId { get; init; }
    public bool ClearFolder { get; init; }
    public IReadOnlyList<int>? TagIds { get; init; }
    public IReadOnlyList<int>? SharedWithManagerIds { get; init; }
    public long? ExpectedRevision { get; init; }
}

public sealed class UpdateManagerNoteMetadataRequest : ManagerNoteMetadataInput
{
    public bool? IsPinned { get; init; }
    public bool? IsLocked { get; init; }
}

public sealed class SaveManagerNoteRequest : ManagerNoteMetadataInput
{
    public string? Title { get; init; }
    public string? Content { get; init; }
    public string? Color { get; init; }
}

public sealed class RestoreManagerNoteRequest
{
    public long? ExpectedRevision { get; init; }
}

public sealed class ManagerNotepadStateDto
{
    public bool IsExpanded { get; init; }
    public bool IsPinned { get; init; }
    public int Height { get; init; } = 520;
}

public sealed class SaveManagerNotepadStateRequest
{
    public bool IsExpanded { get; init; }
    public bool IsPinned { get; init; }
    public int Height { get; init; } = 520;
}
