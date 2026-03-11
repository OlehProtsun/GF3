using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.AvailabilityBinds;
using WebApi.Mappers;

namespace WebApi.Controllers;

[ApiController]
[Route("api/availability-binds")]
public sealed class AvailabilityBindsController(IBindService bindService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityBindDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AvailabilityBindDto>>> GetAll(CancellationToken cancellationToken)
    {
        var binds = await bindService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(binds.Select(x => x.ToApiDto()));
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityBindDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AvailabilityBindDto>>> GetActive(CancellationToken cancellationToken)
    {
        var binds = await bindService.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        return Ok(binds.Select(x => x.ToApiDto()));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AvailabilityBindDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AvailabilityBindDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var bind = await bindService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (bind is null)
        {
            throw new KeyNotFoundException($"Availability bind with id {id} was not found.");
        }

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
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAvailabilityBindRequest request, CancellationToken cancellationToken)
    {
        var existing = await bindService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Availability bind with id {id} was not found.");
        }

        await bindService.UpdateAsync(request.ToUpdateModel(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await bindService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Availability bind with id {id} was not found.");
        }

        await bindService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
