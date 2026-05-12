using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.AvailabilityBinds;
using WebApi.Mappers;
using WebApi.Realtime;

namespace WebApi.Controllers;

/// <summary>
/// Exposes CRUD endpoints for availability hotkey bindings.
/// These bindings are small reference records, so the controller intentionally stays thin and delegates
/// validation and duplicate checks to the business service.
/// </summary>
[ApiController]
[Route("api/availability-binds")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class AvailabilityBindsController(
    IBindService bindService,
    IRealtimeNotifier? realtimeNotifier = null) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityBindDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AvailabilityBindDto>>> GetAll(CancellationToken cancellationToken)
    {
        var binds = await bindService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(binds.Select(bind => bind.ToApiDto()));
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityBindDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AvailabilityBindDto>>> GetActive(CancellationToken cancellationToken)
    {
        var binds = await bindService.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        return Ok(binds.Select(bind => bind.ToApiDto()));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AvailabilityBindDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AvailabilityBindDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var bind = await GetExistingBindOrThrowAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(bind.ToApiDto());
    }

    [HttpPost]
    [ProducesResponseType(typeof(AvailabilityBindDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AvailabilityBindDto>> Create([FromBody] CreateAvailabilityBindRequest request, CancellationToken cancellationToken)
    {
        var created = await bindService.CreateAsync(request.ToCreateModel(), cancellationToken).ConfigureAwait(false);
        var dto = created.ToApiDto();
        await NotifyBindChangedAsync(dto.Id, "manager-availability-bind-created").ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAvailabilityBindRequest request, CancellationToken cancellationToken)
    {
        _ = await GetExistingBindOrThrowAsync(id, cancellationToken).ConfigureAwait(false);
        await bindService.UpdateAsync(request.ToUpdateModel(id), cancellationToken).ConfigureAwait(false);
        await NotifyBindChangedAsync(id, "manager-availability-bind-updated").ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _ = await GetExistingBindOrThrowAsync(id, cancellationToken).ConfigureAwait(false);
        await bindService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        await NotifyBindChangedAsync(id, "manager-availability-bind-deleted").ConfigureAwait(false);
        return NoContent();
    }

    private Task NotifyBindChangedAsync(int bindId, string reason)
        => realtimeNotifier?.NotifyManagerDataChangedAsync(
            ManagerEditResourceTypes.AvailabilityBind,
            bindId.ToString(),
            reason) ?? Task.CompletedTask;

    private async Task<BindModel> GetExistingBindOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var bind = await bindService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return bind ?? throw new KeyNotFoundException($"Availability bind with id {id} was not found.");
    }
}
