using BusinessLogicLayer.Services.Abstractions;
using BusinessLogicLayer.Contracts.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.AvailabilityGroups;
using WebApi.Contracts.AvailabilityGroups.Members;
using WebApi.Contracts.AvailabilityGroups.Slots;
using WebApi.Mappers;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/availability-groups")]
[Authorize(Roles = AuthRoles.Manager)]
/// <summary>
/// HTTP surface for the availability-group aggregate.
/// The controller deliberately stays thin: it validates route-level existence where needed,
/// delegates business rules to the service layer, and only maps contracts to API DTOs.
/// </summary>
public class AvailabilityGroupsController(
    IAvailabilityGroupService availabilityGroupService,
    IRealtimeNotifier? realtimeNotifier = null,
    IManagerEditLockService? editLockService = null,
    IWorkflowLogService? workflowLogService = null) : ControllerBase
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
        var group = await RequireGroupAsync(id, cancellationToken).ConfigureAwait(false);

        return Ok(group.ToApiDto());
    }

    [HttpGet("{id:int}/items")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityGroupItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<AvailabilityGroupItemDto>>> GetItems(int id, CancellationToken cancellationToken)
    {
        _ = await RequireGroupAsync(id, cancellationToken).ConfigureAwait(false);

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
        if (CreateEditLockConflictResult(groupId) is { } conflict)
        {
            return conflict;
        }

        var created = await availabilityGroupService.CreateMemberAsync(groupId, request.ToCreateMemberModel(groupId), cancellationToken).ConfigureAwait(false);
        var dto = created.ToMemberDto();
        await NotifyAvailabilityChangedAsync(groupId, "manager-availability-member-created").ConfigureAwait(false);
        return CreatedAtAction(nameof(GetMembers), new { groupId }, dto);
    }

    [HttpPut("{groupId:int}/members/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMember(int groupId, int memberId, [FromBody] UpdateAvailabilityGroupMemberRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(groupId) is { } conflict)
        {
            return conflict;
        }

        await availabilityGroupService.UpdateMemberAsync(groupId, memberId, request.ToUpdateMemberModel(groupId, memberId), cancellationToken).ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(groupId, "manager-availability-member-updated").ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{groupId:int}/members/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMember(int groupId, int memberId, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(groupId) is { } conflict)
        {
            return conflict;
        }

        await availabilityGroupService.DeleteMemberAsync(groupId, memberId, cancellationToken).ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(groupId, "manager-availability-member-deleted").ConfigureAwait(false);
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
        if (CreateEditLockConflictResult(groupId) is { } conflict)
        {
            return conflict;
        }

        var created = await availabilityGroupService.CreateSlotAsync(groupId, request.ToCreateSlotModel(), cancellationToken).ConfigureAwait(false);
        var dto = created.ToSlotDto();
        await NotifyAvailabilityChangedAsync(groupId, "manager-availability-slot-created").ConfigureAwait(false);
        return CreatedAtAction(nameof(GetSlots), new { groupId }, dto);
    }

    [HttpPut("{groupId:int}/slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSlot(int groupId, int slotId, [FromBody] UpdateAvailabilitySlotRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(groupId) is { } conflict)
        {
            return conflict;
        }

        await availabilityGroupService.UpdateSlotAsync(groupId, slotId, request.ToUpdateSlotModel(slotId), cancellationToken).ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(groupId, "manager-availability-slot-updated").ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{groupId:int}/slots/{slotId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSlot(int groupId, int slotId, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(groupId) is { } conflict)
        {
            return conflict;
        }

        await availabilityGroupService.DeleteSlotAsync(groupId, slotId, cancellationToken).ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(groupId, "manager-availability-slot-deleted").ConfigureAwait(false);
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
        var isPublished = created.PublicationStatus == AvailabilityPublicationStatus.Public;
        await LogManagerActionAsync(
            $"{(isPublished ? "Published" : "Created")} {DescribeAvailability(created)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(
            dto.Id,
            isPublished ? "manager-availability-published" : "manager-availability-created").ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAvailabilityGroupRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(id) is { } conflict)
        {
            return conflict;
        }

        var existing = await RequireGroupAsync(id, cancellationToken).ConfigureAwait(false);
        var updatedModel = request.ToUpdateModel(id);
        var becamePublic =
            existing.PublicationStatus != AvailabilityPublicationStatus.Public &&
            updatedModel.PublicationStatus == AvailabilityPublicationStatus.Public;

        await availabilityGroupService.UpdateAsync(updatedModel, cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync(
            $"{(becamePublic ? "Published" : "Updated")} {DescribeAvailability(updatedModel)}.",
            cancellationToken).ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(
            id,
            becamePublic ? "manager-availability-published" : "manager-availability-updated").ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(id) is { } conflict)
        {
            return conflict;
        }

        var existing = await RequireGroupAsync(id, cancellationToken).ConfigureAwait(false);

        await availabilityGroupService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        await LogManagerActionAsync($"Deleted {DescribeAvailability(existing)}.", cancellationToken).ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(id, "manager-availability-deleted").ConfigureAwait(false);
        return NoContent();
    }

    private ActionResult? CreateEditLockConflictResult(int groupId)
        => ManagerEditLockHttp.CreateConflictResult(
            this,
            editLockService,
            ManagerEditLockTargets.AvailabilityGroup(groupId),
            "This availability group");

    private Task NotifyAvailabilityChangedAsync(int groupId, string reason)
        => realtimeNotifier?.NotifyManagerDataChangedAsync(
            ManagerEditResourceTypes.AvailabilityGroup,
            groupId.ToString(),
            reason) ?? Task.CompletedTask;

    private Task LogManagerActionAsync(string action, CancellationToken cancellationToken)
        => workflowLogService?.LogAsync(User, action, cancellationToken) ?? Task.CompletedTask;

    private static string DescribeAvailability(BusinessLogicLayer.Contracts.Models.AvailabilityGroupModel group)
    {
        var period = new DateTime(group.Year, group.Month, 1)
            .ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        var window = group.VisibleFromUtc.HasValue && group.VisibleToUtc.HasValue
            ? $", visible {FormatDateTime(group.VisibleFromUtc.Value)} to {FormatDateTime(group.VisibleToUtc.Value)}"
            : string.Empty;
        return $"availability \"{group.Name}\" for {period}{window}";
    }

    private static string FormatDateTime(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture);

    private async Task<BusinessLogicLayer.Contracts.Models.AvailabilityGroupModel> RequireGroupAsync(int groupId, CancellationToken cancellationToken)
    {
        var group = await availabilityGroupService.GetAsync(groupId, cancellationToken).ConfigureAwait(false);
        if (group is null)
        {
            throw new KeyNotFoundException($"Availability group with id {groupId} was not found.");
        }

        return group;
    }
}
