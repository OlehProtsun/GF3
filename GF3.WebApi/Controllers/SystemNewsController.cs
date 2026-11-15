using System.Security.Claims;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;

namespace WebApi.Controllers;

[ApiController]
[Route("api/system-news")]
[Authorize(Roles = AuthRoles.Manager + "," + AuthRoles.Employee)]
public sealed class SystemNewsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SystemNewsDto>>> GetVisible(CancellationToken cancellationToken)
    {
        var subject = GetSubject();
        var audience = subject.Role == AuthRoles.Manager ? "managers" : "employees";
        var readIds = await db.SystemNewsReads.AsNoTracking()
            .Where(read => read.AccountRole == subject.Role && read.AccountId == subject.Id)
            .Select(read => read.MessageId)
            .ToHashSetAsync(cancellationToken).ConfigureAwait(false);
        var messages = await db.SystemNewsMessages.AsNoTracking()
            .Where(message => message.Audience == "all" || message.Audience == audience)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(messages.OrderByDescending(message => message.CreatedAtUtc)
            .Select(message => SystemNewsMapping.ToDto(message, readIds.Contains(message.Id))).ToList());
    }

    [HttpPost("{messageId:int}/read")]
    public async Task<IActionResult> MarkRead(int messageId, CancellationToken cancellationToken)
    {
        var subject = GetSubject();
        var audience = subject.Role == AuthRoles.Manager ? "managers" : "employees";
        var isVisible = await db.SystemNewsMessages.AsNoTracking()
            .AnyAsync(message => message.Id == messageId && (message.Audience == "all" || message.Audience == audience), cancellationToken)
            .ConfigureAwait(false);
        if (!isVisible) return NotFound();
        await SaveReadAsync(messageId, subject, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var subject = GetSubject();
        var audience = subject.Role == AuthRoles.Manager ? "managers" : "employees";
        var messageIds = await db.SystemNewsMessages.AsNoTracking()
            .Where(message => message.Audience == "all" || message.Audience == audience)
            .Select(message => message.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        foreach (var messageId in messageIds)
            await SaveReadAsync(messageId, subject, now, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private Task SaveReadAsync(int messageId, (string Role, int Id) subject, DateTimeOffset now, CancellationToken cancellationToken)
        => db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO system_news_read (message_id, account_role, account_id, read_at_utc)
            VALUES ({messageId}, {subject.Role}, {subject.Id}, {now})
            ON CONFLICT(message_id, account_role, account_id) DO UPDATE SET read_at_utc = excluded.read_at_utc
            """, cancellationToken);

    private (string Role, int Id) GetSubject()
    {
        var isManager = User.IsInRole(AuthRoles.Manager);
        var role = isManager ? AuthRoles.Manager : AuthRoles.Employee;
        var claimName = isManager ? "manager_id" : "employee_id";
        return int.TryParse(User.FindFirstValue(claimName), out var id) && id > 0
            ? (role, id)
            : throw new BadHttpRequestException("The current account session is invalid.");
    }
}

[ApiController]
[Route("api/admin/system-news")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class AdminSystemNewsController(AppDbContext db) : ControllerBase
{
    private static readonly HashSet<string> Audiences = ["all", "managers", "employees"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SystemNewsDto>>> GetAll(CancellationToken cancellationToken)
        => Ok((await db.SystemNewsMessages.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false))
            .OrderByDescending(message => message.CreatedAtUtc)
            .Select(message => SystemNewsMapping.ToDto(message, false)).ToList());

    [HttpPost]
    public async Task<ActionResult<SystemNewsDto>> Create(SaveSystemNewsRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalize(request, out var values)) return ValidationProblem(ModelState);
        var now = DateTimeOffset.UtcNow;
        var message = new SystemNewsMessageModel
        {
            Title = values.Title, Body = values.Body, Audience = values.Audience,
            ImageUrl = values.ImageUrl, VideoUrl = values.VideoUrl, CreatedAtUtc = now, UpdatedAtUtc = now,
        };
        db.SystemNewsMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return StatusCode(StatusCodes.Status201Created, SystemNewsMapping.ToDto(message, false));
    }

    [HttpPut("{messageId:int}")]
    public async Task<ActionResult<SystemNewsDto>> Update(int messageId, SaveSystemNewsRequest request, CancellationToken cancellationToken)
    {
        if (!TryNormalize(request, out var values)) return ValidationProblem(ModelState);
        var message = await db.SystemNewsMessages.SingleOrDefaultAsync(item => item.Id == messageId, cancellationToken).ConfigureAwait(false);
        if (message is null) return NotFound();
        message.Title = values.Title;
        message.Body = values.Body;
        message.Audience = values.Audience;
        message.ImageUrl = values.ImageUrl;
        message.VideoUrl = values.VideoUrl;
        message.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(SystemNewsMapping.ToDto(message, false));
    }

    [HttpDelete("{messageId:int}")]
    public async Task<IActionResult> Delete(int messageId, CancellationToken cancellationToken)
    {
        await db.SystemNewsMessages.Where(message => message.Id == messageId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private bool TryNormalize(SaveSystemNewsRequest request, out (string Title, string Body, string Audience, string? ImageUrl, string? VideoUrl) values)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        var body = request.Body?.Trim() ?? string.Empty;
        var audience = request.Audience?.Trim().ToLowerInvariant() ?? "all";
        var imageUrl = NullIfBlank(request.ImageUrl);
        var videoUrl = NullIfBlank(request.VideoUrl);
        if (title.Length is < 1 or > 160) ModelState.AddModelError(nameof(request.Title), "Title must contain 1 to 160 characters.");
        if (body.Length is < 1 or > 10_000) ModelState.AddModelError(nameof(request.Body), "Text must contain 1 to 10,000 characters.");
        if (!Audiences.Contains(audience)) ModelState.AddModelError(nameof(request.Audience), "Choose all, managers, or employees.");
        if (imageUrl?.Length > 2_800_000 || imageUrl is not null && !IsSafeImageUrl(imageUrl))
            ModelState.AddModelError(nameof(request.ImageUrl), "Use an HTTP image URL or an uploaded image up to 2 MB.");
        if (videoUrl?.Length > 500 || videoUrl is not null && !IsYoutubeUrl(videoUrl))
            ModelState.AddModelError(nameof(request.VideoUrl), "Use a valid YouTube link.");
        values = (title, body, audience, imageUrl, videoUrl);
        return ModelState.IsValid;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsSafeImageUrl(string value)
        => value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
           Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http";
    private static bool IsYoutubeUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" &&
           (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase));
}

internal static class SystemNewsMapping
{
    internal static SystemNewsDto ToDto(SystemNewsMessageModel message, bool isRead) => new()
    {
        Id = message.Id, Title = message.Title, Body = message.Body, Audience = message.Audience,
        ImageUrl = message.ImageUrl, VideoUrl = message.VideoUrl, IsRead = isRead,
        CreatedAtUtc = message.CreatedAtUtc, UpdatedAtUtc = message.UpdatedAtUtc,
    };
}

public sealed class SystemNewsDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Audience { get; init; } = "all";
    public string? ImageUrl { get; init; }
    public string? VideoUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}

public sealed class SaveSystemNewsRequest
{
    public string? Title { get; init; }
    public string? Body { get; init; }
    public string? Audience { get; init; }
    public string? ImageUrl { get; init; }
    public string? VideoUrl { get; init; }
}
