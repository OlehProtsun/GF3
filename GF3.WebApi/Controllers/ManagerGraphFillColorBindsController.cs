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
[Route("api/manager-graph-fill-color-binds")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed partial class ManagerGraphFillColorBindsController(AppDbContext db) : ControllerBase
{
    private const int MaxKeyLength = 64;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ManagerGraphFillColorBindDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ManagerGraphFillColorBindDto>>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        var binds = await db.ManagerGraphFillColorBinds
            .AsNoTracking()
            .Where(bind => bind.ManagerAccountId == managerId)
            .OrderBy(bind => bind.Key)
            .Select(bind => new ManagerGraphFillColorBindDto
            {
                Id = bind.Id,
                Key = bind.Key,
                FillColor = bind.FillColor,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(binds);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ManagerGraphFillColorBindDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ManagerGraphFillColorBindDto>> Upsert(
        [FromBody] SaveManagerGraphFillColorBindRequest request,
        CancellationToken cancellationToken)
    {
        var key = request.Key?.Trim() ?? string.Empty;
        var fillColor = NormalizeFillColor(request.FillColor);

        if (key.Length == 0 || key.Length > MaxKeyLength)
        {
            ModelState.AddModelError(nameof(request.Key), $"Key must contain between 1 and {MaxKeyLength} characters.");
        }
        if (fillColor is null)
        {
            ModelState.AddModelError(nameof(request.FillColor), "Fill color must be a six-digit hex color.");
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

        var textColorKeyInUse = await db.ManagerGraphTextColorBinds
            .AsNoTracking()
            .AnyAsync(
                bind => bind.ManagerAccountId == managerId && EF.Functions.Collate(bind.Key, "NOCASE") == key,
                cancellationToken)
            .ConfigureAwait(false);
        if (textColorKeyInUse)
        {
            ModelState.AddModelError(nameof(request.Key), $"Key '{key}' is already used by a text color bind.");
            return ValidationProblem(ModelState);
        }

        var managerBinds = await db.ManagerGraphFillColorBinds
            .Where(bind => bind.ManagerAccountId == managerId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var bindForColor = managerBinds.SingleOrDefault(bind => bind.FillColor == fillColor);
        var bindForKey = managerBinds.SingleOrDefault(bind => string.Equals(bind.Key, key, StringComparison.OrdinalIgnoreCase));

        ManagerGraphFillColorBindModel target;
        if (bindForColor is not null)
        {
            target = bindForColor;
            if (bindForKey is not null && bindForKey.Id != target.Id)
            {
                db.ManagerGraphFillColorBinds.Remove(bindForKey);
            }
            target.Key = key;
        }
        else if (bindForKey is not null)
        {
            target = bindForKey;
            target.FillColor = fillColor!;
        }
        else
        {
            target = new ManagerGraphFillColorBindModel
            {
                ManagerAccountId = managerId,
                Key = key,
                FillColor = fillColor!,
            };
            db.ManagerGraphFillColorBinds.Add(target);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ToDto(target));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var managerId = GetRequiredManagerId();
        await db.ManagerGraphFillColorBinds
            .Where(bind => bind.Id == id && bind.ManagerAccountId == managerId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    private int GetRequiredManagerId()
        => int.TryParse(User.FindFirstValue("manager_id"), out var managerId) && managerId > 0
            ? managerId
            : throw new BadHttpRequestException("The current manager session is invalid.");

    private static string? NormalizeFillColor(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return normalized is not null && FillColorPattern().IsMatch(normalized) ? normalized : null;
    }

    private static ManagerGraphFillColorBindDto ToDto(ManagerGraphFillColorBindModel bind) => new()
    {
        Id = bind.Id,
        Key = bind.Key,
        FillColor = bind.FillColor,
    };

    [GeneratedRegex("^#[0-9A-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex FillColorPattern();
}

public sealed class ManagerGraphFillColorBindDto
{
    public int Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string FillColor { get; init; } = string.Empty;
}

public sealed class SaveManagerGraphFillColorBindRequest
{
    public string? Key { get; init; }
    public string? FillColor { get; init; }
}
