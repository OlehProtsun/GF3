using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;

namespace WebApi.Controllers;

[ApiController]
[Route("api/manager-notepad")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class ManagerNotepadController(AppDbContext db) : ControllerBase
{
    private const int DefaultHeight = 520;
    private const int MinHeight = 320;
    private const int MaxHeight = 900;
    private const int MaxTitleLength = 160;
    private const int MaxContentLength = 20_000;
    private static readonly HashSet<string> AllowedColors =
        ["yellow", "blue", "green", "rose", "slate"];

    [HttpGet]
    [ProducesResponseType(typeof(ManagerNotepadDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagerNotepadDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        var notes = await db.ManagerNotes
            .FromSqlInterpolated(
                $"""
                SELECT id, manager_account_id, title, content, color, created_at_utc, updated_at_utc
                FROM manager_note
                WHERE manager_account_id = {managerId}
                ORDER BY updated_at_utc DESC
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var state = await db.ManagerNotepadStates
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ManagerAccountId == managerId, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new ManagerNotepadDto
        {
            Notes = notes.Select(ToDto).ToList(),
            State = ToDto(state),
        });
    }

    [HttpPost("notes")]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ManagerNoteDto>> CreateNote(
        [FromBody] SaveManagerNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeNote(request, out var title, out var content, out var color))
        {
            return ValidationProblem(ModelState);
        }

        var now = DateTimeOffset.UtcNow;
        var note = new ManagerNoteModel
        {
            ManagerAccountId = GetRequiredManagerId(),
            Title = title,
            Content = content,
            Color = color,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.ManagerNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return StatusCode(StatusCodes.Status201Created, ToDto(note));
    }

    [HttpPut("notes/{noteId:int}")]
    [ProducesResponseType(typeof(ManagerNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagerNoteDto>> UpdateNote(
        int noteId,
        [FromBody] SaveManagerNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeNote(request, out var title, out var content, out var color))
        {
            return ValidationProblem(ModelState);
        }

        var managerId = GetRequiredManagerId();
        var note = await db.ManagerNotes
            .SingleOrDefaultAsync(item => item.Id == noteId && item.ManagerAccountId == managerId, cancellationToken)
            .ConfigureAwait(false);
        if (note is null)
        {
            return NotFound();
        }

        note.Title = title;
        note.Content = content;
        note.Color = color;
        note.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ToDto(note));
    }

    [HttpDelete("notes/{noteId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteNote(int noteId, CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        await db.ManagerNotes
            .Where(note => note.Id == noteId && note.ManagerAccountId == managerId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
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
        out string color)
    {
        title = string.IsNullOrWhiteSpace(request.Title) ? "Untitled note" : request.Title.Trim();
        content = request.Content ?? string.Empty;
        color = string.IsNullOrWhiteSpace(request.Color) ? "yellow" : request.Color.Trim().ToLowerInvariant();

        if (title.Length > MaxTitleLength)
        {
            ModelState.AddModelError(nameof(request.Title), $"Title must be at most {MaxTitleLength} characters.");
        }
        if (content.Length > MaxContentLength)
        {
            ModelState.AddModelError(nameof(request.Content), $"Content must be at most {MaxContentLength} characters.");
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

    private static ManagerNoteDto ToDto(ManagerNoteModel note) => new()
    {
        Id = note.Id,
        Title = note.Title,
        Content = note.Content,
        Color = note.Color,
        CreatedAtUtc = note.CreatedAtUtc,
        UpdatedAtUtc = note.UpdatedAtUtc,
    };

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
}

public sealed class ManagerNoteDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Color { get; init; } = "yellow";
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}

public sealed class ManagerNotepadStateDto
{
    public bool IsExpanded { get; init; }
    public bool IsPinned { get; init; }
    public int Height { get; init; } = 520;
}

public sealed class SaveManagerNoteRequest
{
    public string? Title { get; init; }
    public string? Content { get; init; }
    public string? Color { get; init; }
}

public sealed class SaveManagerNotepadStateRequest
{
    public bool IsExpanded { get; init; }
    public bool IsPinned { get; init; }
    public int Height { get; init; } = 520;
}
