using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.Containers;
using WebApi.Mappers;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContainersController(IContainerService containerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContainerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ContainerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var containers = await containerService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(containers.Select(x => x.ToApiDto()));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ContainerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContainerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var container = await containerService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (container is null)
        {
            throw new KeyNotFoundException($"Container with id {id} was not found.");
        }

        return Ok(container.ToApiDto());
    }

    [HttpPost]
    [ProducesResponseType(typeof(ContainerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContainerDto>> Create([FromBody] CreateContainerRequest request, CancellationToken cancellationToken)
    {
        var created = await containerService.CreateAsync(request.ToCreateModel(), cancellationToken).ConfigureAwait(false);
        var dto = created.ToApiDto();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContainerRequest request, CancellationToken cancellationToken)
    {
        var existing = await containerService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Container with id {id} was not found.");
        }

        await containerService.UpdateAsync(request.ToUpdateModel(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await containerService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Container with id {id} was not found.");
        }

        await containerService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
