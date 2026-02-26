using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.Containers;
using WebApi.Contracts.Containers.Graphs;
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

    [HttpGet("{containerId:int}/graphs")]
    [ProducesResponseType(typeof(IEnumerable<ContainerGraphDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ContainerGraphDto>>> GetGraphs(int containerId, CancellationToken cancellationToken)
    {
        var graphs = await containerService.GetGraphsAsync(containerId, cancellationToken).ConfigureAwait(false);
        return Ok(graphs.Select(x => x.ToGraphDto()));
    }

    [HttpPost("{containerId:int}/graphs")]
    [ProducesResponseType(typeof(ContainerGraphDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContainerGraphDto>> CreateGraph(int containerId, [FromBody] CreateContainerGraphRequest request, CancellationToken cancellationToken)
    {
        var created = await containerService.CreateGraphAsync(containerId, request.ToCreateGraphModel(containerId), cancellationToken).ConfigureAwait(false);
        var dto = created.ToGraphDto();
        return CreatedAtAction(nameof(GetGraphs), new { containerId }, dto);
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateGraph(int containerId, int graphId, [FromBody] UpdateContainerGraphRequest request, CancellationToken cancellationToken)
    {
        await containerService.UpdateGraphAsync(containerId, graphId, request.ToUpdateGraphModel(containerId, graphId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{containerId:int}/graphs/{graphId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteGraph(int containerId, int graphId, CancellationToken cancellationToken)
    {
        await containerService.DeleteGraphAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        return NoContent();
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
