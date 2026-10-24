using System.Security.Claims;
using System.Text.RegularExpressions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;

namespace WebApi.Controllers;

[ApiController]
[Route("api/manager-graph-text-color-binds")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed partial class ManagerGraphTextColorBindsController(AppDbContext db) : ControllerBase
{
    private const int MaxKeyLength = 64;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ManagerGraphTextColorBindDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ManagerGraphTextColorBindDto>>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        var binds = await db.ManagerGraphTextColorBinds
            .AsNoTracking()
            .Where(bind => bind.ManagerAccountId == managerId)
            .OrderBy(bind => bind.Key)
            .Select(bind => new ManagerGraphTextColorBindDto
            {
                Id = bind.Id,
                Key = bind.Key,
                TextColor = bind.TextColor,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(binds);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ManagerGraphTextColorBindDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ManagerGraphTextColorBindDto>> Upsert(
        [FromBody] SaveManagerGraphTextColorBindRequest request,
        CancellationToken cancellationToken)
    {
        var key = request.Key?.Trim() ?? string.Empty;
        var textColor = NormalizeTextColor(request.TextColor);

        if (key.Length == 0 || key.Length > MaxKeyLength)
        {
            ModelState.AddModelError(nameof(request.Key), $"Key must contain between 1 and {MaxKeyLength} characters.");
        }
        if (textColor is null)
        {
            ModelState.AddModelError(nameof(request.TextColor), "Text color must be a six-digit hex color.");
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var managerId = GetRequiredManagerId();
        var valueKeyInUse = await db.AvailabilityBinds
            .AsNoTracking()
            .AnyAsync(
                bind => bind.ManagerAccountId == managerId && EF.Functions.Collate(bind.Key, "NOCASE") == key,
                cancellationToken)
            .ConfigureAwait(false);
        if (valueKeyInUse)
        {
            ModelState.AddModelError(nameof(request.Key), $"Key '{key}' is already used by a value bind.");
            return ValidationProblem(ModelState);
        }

        var fillColorKeyInUse = await db.ManagerGraphFillColorBinds
            .AsNoTracking()
            .AnyAsync(
                bind => bind.ManagerAccountId == managerId && EF.Functions.Collate(bind.Key, "NOCASE") == key,
                cancellationToken)
            .ConfigureAwait(false);
        if (fillColorKeyInUse)
        {
            ModelState.AddModelError(nameof(request.Key), $"Key '{key}' is already used by a fill color bind.");
            return ValidationProblem(ModelState);
        }

        var managerBinds = await db.ManagerGraphTextColorBinds
            .Where(bind => bind.ManagerAccountId == managerId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var bindForColor = managerBinds.SingleOrDefault(bind => bind.TextColor == textColor);
        var bindForKey = managerBinds.SingleOrDefault(bind => string.Equals(bind.Key, key, StringComparison.OrdinalIgnoreCase));

        ManagerGraphTextColorBindModel target;
        if (bindForColor is not null)
        {
            target = bindForColor;
            if (bindForKey is not null && bindForKey.Id != target.Id)
            {
                db.ManagerGraphTextColorBinds.Remove(bindForKey);
            }
            target.Key = key;
        }
        else if (bindForKey is not null)
        {
            target = bindForKey;
            target.TextColor = textColor!;
        }
        else
        {
            target = new ManagerGraphTextColorBindModel
            {
                ManagerAccountId = managerId,
                Key = key,
                TextColor = textColor!,
            };
            db.ManagerGraphTextColorBinds.Add(target);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ToDto(target));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        await db.ManagerGraphTextColorBinds
            .Where(bind => bind.Id == id && bind.ManagerAccountId == managerId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    private int GetRequiredManagerId()
        => int.TryParse(User.FindFirstValue("manager_id"), out var managerId) && managerId > 0
            ? managerId
            : throw new BadHttpRequestException("The current manager session is invalid.");

    private static string? NormalizeTextColor(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return normalized is not null && TextColorPattern().IsMatch(normalized) ? normalized : null;
    }

    private static ManagerGraphTextColorBindDto ToDto(ManagerGraphTextColorBindModel bind) => new()
    {
        Id = bind.Id,
        Key = bind.Key,
        TextColor = bind.TextColor,
    };

    [GeneratedRegex("^#[0-9A-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex TextColorPattern();
}

public sealed class ManagerGraphTextColorBindDto
{
    public int Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string TextColor { get; init; } = string.Empty;
}

public sealed class SaveManagerGraphTextColorBindRequest
{
    public string? Key { get; init; }
    public string? TextColor { get; init; }
}
