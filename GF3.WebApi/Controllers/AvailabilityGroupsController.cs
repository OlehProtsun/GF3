using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.AvailabilityGroups;
using WebApi.Mappers;

namespace WebApi.Controllers;

[ApiController]
[Route("api/availability-groups")]
public class AvailabilityGroupsController(IAvailabilityGroupService availabilityGroupService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AvailabilityGroupDto>>> GetAll(CancellationToken cancellationToken)
    {
        var groups = await availabilityGroupService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(groups.Select(x => x.ToApiDto()));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AvailabilityGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AvailabilityGroupDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var group = await availabilityGroupService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (group is null)
        {
            throw new KeyNotFoundException($"Availability group with id {id} was not found.");
        }

        return Ok(group.ToApiDto());
    }

    [HttpGet("{id:int}/items")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityGroupItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AvailabilityGroupItemDto>>> GetItems(int id, CancellationToken cancellationToken)
    {
        var group = await availabilityGroupService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (group is null)
        {
            throw new KeyNotFoundException($"Availability group with id {id} was not found.");
        }

        var (_, members, days) = await availabilityGroupService.LoadFullAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(members.ToItemDtos(days));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AvailabilityGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AvailabilityGroupDto>> Create([FromBody] CreateAvailabilityGroupRequest request, CancellationToken cancellationToken)
    {
        var created = await availabilityGroupService.CreateAsync(request.ToCreateModel(), cancellationToken).ConfigureAwait(false);
        var dto = created.ToApiDto();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAvailabilityGroupRequest request, CancellationToken cancellationToken)
    {
        var existing = await availabilityGroupService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Availability group with id {id} was not found.");
        }

        await availabilityGroupService.UpdateAsync(request.ToUpdateModel(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await availabilityGroupService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Availability group with id {id} was not found.");
        }

        await availabilityGroupService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
