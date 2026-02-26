using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.Containers;
using WebApi.Contracts.Containers.Graphs;
using WebApi.Contracts.Containers.Graphs.CellStyles;
using WebApi.Contracts.Containers.Graphs.Employees;
using WebApi.Contracts.Containers.Graphs.Slots;
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
    [ProducesResponseType(typeof(IEnumerable<GraphDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<GraphDto>>> GetGraphs(int containerId, CancellationToken cancellationToken)
    {
        var graphs = await containerService.GetGraphsAsync(containerId, cancellationToken).ConfigureAwait(false);
        return Ok(graphs.Select(x => x.ToGraphDto()));
    }

    [HttpGet("{containerId:int}/graphs/{graphId:int}")]
    [ProducesResponseType(typeof(GraphDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GraphDto>> GetGraphById(int containerId, int graphId, CancellationToken cancellationToken)
    {
        var graph = await containerService.GetGraphByIdAsync(containerId, graphId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Graph with id {graphId} was not found.");

        return Ok(graph.ToGraphDto());
    }

    [HttpPost("{containerId:int}/graphs")]
    [ProducesResponseType(typeof(GraphDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GraphDto>> CreateGraph(int containerId, [FromBody] CreateGraphRequest request, CancellationToken cancellationToken)
    {
        var created = await containerService.CreateGraphAsync(containerId, request.ToCreateModel(containerId), cancellationToken).ConfigureAwait(false);
        var dto = created.ToGraphDto();
        return CreatedAtAction(nameof(GetGraphById), new { containerId, graphId = dto.Id }, dto);
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateGraph(int containerId, int graphId, [FromBody] UpdateGraphRequest request, CancellationToken cancellationToken)
    {
        await containerService.UpdateGraphAsync(containerId, graphId, request.ToUpdateModel(containerId, graphId), cancellationToken).ConfigureAwait(false);
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

    [HttpGet("{containerId:int}/graphs/{graphId:int}/slots")]
    [ProducesResponseType(typeof(IEnumerable<GraphSlotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GraphSlotDto>>> GetGraphSlots(int containerId, int graphId, CancellationToken cancellationToken)
    {
        var slots = await containerService.GetGraphSlotsAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        return Ok(slots.Select(x => x.ToGraphSlotDto()));
    }

    [HttpPost("{containerId:int}/graphs/{graphId:int}/slots")]
    [ProducesResponseType(typeof(GraphSlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GraphSlotDto>> CreateGraphSlot(int containerId, int graphId, [FromBody] CreateGraphSlotRequest request, CancellationToken cancellationToken)
    {
        var created = await containerService.CreateGraphSlotAsync(containerId, graphId, request.ToCreateModel(graphId), cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetGraphSlots), new { containerId, graphId }, created.ToGraphSlotDto());
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}/slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateGraphSlot(int containerId, int graphId, int slotId, [FromBody] UpdateGraphSlotRequest request, CancellationToken cancellationToken)
    {
        await containerService.UpdateGraphSlotAsync(containerId, graphId, slotId, request.ToUpdateModel(graphId, slotId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{containerId:int}/graphs/{graphId:int}/slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGraphSlot(int containerId, int graphId, int slotId, CancellationToken cancellationToken)
    {
        await containerService.DeleteGraphSlotAsync(containerId, graphId, slotId, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("{containerId:int}/graphs/{graphId:int}/employees")]
    [ProducesResponseType(typeof(IEnumerable<GraphEmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GraphEmployeeDto>>> GetGraphEmployees(int containerId, int graphId, CancellationToken cancellationToken)
    {
        var employees = await containerService.GetGraphEmployeesAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        return Ok(employees.Select(x => x.ToGraphEmployeeDto()));
    }

    [HttpPost("{containerId:int}/graphs/{graphId:int}/employees")]
    [ProducesResponseType(typeof(GraphEmployeeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<GraphEmployeeDto>> AddGraphEmployee(int containerId, int graphId, [FromBody] AddGraphEmployeeRequest request, CancellationToken cancellationToken)
    {
        var created = await containerService.AddGraphEmployeeAsync(containerId, graphId, request.ToAddModel(graphId), cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetGraphEmployees), new { containerId, graphId }, created.ToGraphEmployeeDto());
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}/employees/{graphEmployeeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateGraphEmployee(int containerId, int graphId, int graphEmployeeId, [FromBody] UpdateGraphEmployeeRequest request, CancellationToken cancellationToken)
    {
        await containerService.UpdateGraphEmployeeAsync(containerId, graphId, graphEmployeeId, request.ToUpdateModel(graphId, graphEmployeeId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{containerId:int}/graphs/{graphId:int}/employees/{graphEmployeeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveGraphEmployee(int containerId, int graphId, int graphEmployeeId, CancellationToken cancellationToken)
    {
        await containerService.RemoveGraphEmployeeAsync(containerId, graphId, graphEmployeeId, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("{containerId:int}/graphs/{graphId:int}/cell-styles")]
    [ProducesResponseType(typeof(IEnumerable<GraphCellStyleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GraphCellStyleDto>>> GetGraphCellStyles(int containerId, int graphId, CancellationToken cancellationToken)
    {
        var styles = await containerService.GetGraphCellStylesAsync(containerId, graphId, cancellationToken).ConfigureAwait(false);
        return Ok(styles.Select(x => x.ToGraphCellStyleDto()));
    }

    [HttpPut("{containerId:int}/graphs/{graphId:int}/cell-styles")]
    [ProducesResponseType(typeof(GraphCellStyleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GraphCellStyleDto>> UpsertGraphCellStyle(int containerId, int graphId, [FromBody] UpsertGraphCellStyleRequest request, CancellationToken cancellationToken)
    {
        var style = await containerService.UpsertGraphCellStyleAsync(containerId, graphId, request.ToUpsertModel(graphId), cancellationToken).ConfigureAwait(false);
        return Ok(style.ToGraphCellStyleDto());
    }

    [HttpDelete("{containerId:int}/graphs/{graphId:int}/cell-styles/{styleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGraphCellStyle(int containerId, int graphId, int styleId, CancellationToken cancellationToken)
    {
        await containerService.DeleteGraphCellStyleAsync(containerId, graphId, styleId, cancellationToken).ConfigureAwait(false);
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
