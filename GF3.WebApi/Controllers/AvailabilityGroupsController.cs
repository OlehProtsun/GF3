using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts.AvailabilityGroups;
using WebApi.Contracts.AvailabilityGroups.Members;
using WebApi.Contracts.AvailabilityGroups.Slots;
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

    [HttpGet("{groupId:int}/members")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityGroupMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AvailabilityGroupMemberDto>>> GetMembers(int groupId, CancellationToken cancellationToken)
    {
        var members = await availabilityGroupService.GetMembersAsync(groupId, cancellationToken).ConfigureAwait(false);
        return Ok(members.Select(x => x.ToMemberDto()));
    }

    [HttpPost("{groupId:int}/members")]
    [ProducesResponseType(typeof(AvailabilityGroupMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AvailabilityGroupMemberDto>> CreateMember(int groupId, [FromBody] CreateAvailabilityGroupMemberRequest request, CancellationToken cancellationToken)
    {
        var created = await availabilityGroupService.CreateMemberAsync(groupId, request.ToCreateMemberModel(groupId), cancellationToken).ConfigureAwait(false);
        var dto = created.ToMemberDto();
        return CreatedAtAction(nameof(GetMembers), new { groupId }, dto);
    }

    [HttpPut("{groupId:int}/members/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMember(int groupId, int memberId, [FromBody] UpdateAvailabilityGroupMemberRequest request, CancellationToken cancellationToken)
    {
        await availabilityGroupService.UpdateMemberAsync(groupId, memberId, request.ToUpdateMemberModel(groupId, memberId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{groupId:int}/members/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMember(int groupId, int memberId, CancellationToken cancellationToken)
    {
        await availabilityGroupService.DeleteMemberAsync(groupId, memberId, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("{groupId:int}/slots")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilitySlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AvailabilitySlotDto>>> GetSlots(int groupId, CancellationToken cancellationToken)
    {
        var slots = await availabilityGroupService.GetSlotsAsync(groupId, cancellationToken).ConfigureAwait(false);
        return Ok(slots.Select(x => x.ToSlotDto()));
    }

    [HttpPost("{groupId:int}/slots")]
    [ProducesResponseType(typeof(AvailabilitySlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AvailabilitySlotDto>> CreateSlot(int groupId, [FromBody] CreateAvailabilitySlotRequest request, CancellationToken cancellationToken)
    {
        var created = await availabilityGroupService.CreateSlotAsync(groupId, request.ToCreateSlotModel(), cancellationToken).ConfigureAwait(false);
        var dto = created.ToSlotDto();
        return CreatedAtAction(nameof(GetSlots), new { groupId }, dto);
    }

    [HttpPut("{groupId:int}/slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSlot(int groupId, int slotId, [FromBody] UpdateAvailabilitySlotRequest request, CancellationToken cancellationToken)
    {
        await availabilityGroupService.UpdateSlotAsync(groupId, slotId, request.ToUpdateSlotModel(slotId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{groupId:int}/slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSlot(int groupId, int slotId, CancellationToken cancellationToken)
    {
        await availabilityGroupService.DeleteSlotAsync(groupId, slotId, cancellationToken).ConfigureAwait(false);
        return NoContent();
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
