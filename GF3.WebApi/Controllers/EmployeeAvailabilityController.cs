using System.Security.Claims;
using BusinessLogicLayer.Contracts.Availability;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.AvailabilityGroups.Slots;
using WebApi.Contracts.EmployeeAvailability;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

/// <summary>
/// Employee self-service endpoints for published availability groups.
/// </summary>
[ApiController]
[Route("api/employee-availability")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeAvailabilityController(
    IAvailabilityGroupService availabilityGroupService,
    IWorkflowLogService workflowLogService,
    IManagerEditLockService? editLockService = null,
    IRealtimeNotifier? realtimeNotifier = null,
    IEmploymentContractPolicyService? contractPolicy = null) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeAvailabilityGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmployeeAvailabilityGroupDto>>> GetVisible(CancellationToken cancellationToken)
    {
        var automatic = await IsAutomaticAsync(cancellationToken);
        var nowUtc = DateTimeOffset.UtcNow;
        var groups = await availabilityGroupService
            .GetPublishedForEmployeeAsync(GetRequiredEmployeeId(), nowUtc, cancellationToken)
            .ConfigureAwait(false);

        return Ok(groups.Select(group => ToApiDto(group, automatic)));
    }

    [HttpGet("{groupId:int}")]
    [ProducesResponseType(typeof(EmployeeAvailabilityGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeAvailabilityGroupDto>> GetById(int groupId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var group = await availabilityGroupService
            .GetPublishedForEmployeeByIdAsync(GetRequiredEmployeeId(), groupId, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        return Ok(ToApiDto(group, await IsAutomaticAsync(cancellationToken)));
    }

    [HttpPut("{groupId:int}/slots")]
    [ProducesResponseType(typeof(EmployeeAvailabilityGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeAvailabilityGroupDto>> UpdateSlots(
        int groupId,
        [FromBody] UpdateEmployeeAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetRequiredEmployeeId();
        var nowUtc = DateTimeOffset.UtcNow;
        _ = await availabilityGroupService
            .GetPublishedForEmployeeByIdAsync(employeeId, groupId, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        if (CreateEditLockConflictResult(groupId) is { } conflict)
        {
            return conflict;
        }

        if (await IsAutomaticAsync(cancellationToken))
            return Problem(statusCode: StatusCodes.Status403Forbidden, type: "CONTRACT_AUTOMATIC_AVAILABILITY",
                detail: "Availability is set automatically by your employment contract and cannot be edited.");

        var slots = request.Slots ?? [];
        var updated = await availabilityGroupService
            .SaveEmployeeAvailabilityAsync(
                employeeId,
                groupId,
                slots.Select(ToDayModel).ToList(),
                nowUtc,
                cancellationToken)
            .ConfigureAwait(false);

        await workflowLogService
            .LogAsync(User, $"Updated availability for {updated.Group.Name} ({updated.Group.Month:00}.{updated.Group.Year}).", cancellationToken)
            .ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(groupId, "employee-availability-updated").ConfigureAwait(false);

        return Ok(ToApiDto(updated));
    }

    private int GetRequiredEmployeeId()
    {
        var employeeIdValue = User.FindFirstValue("employee_id");
        if (!int.TryParse(employeeIdValue, out var employeeId) || employeeId <= 0)
        {
            throw new BadHttpRequestException("The current employee session is invalid.");
        }

        return employeeId;
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
            groupId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            reason) ?? Task.CompletedTask;

    private ManagerEditLockState? GetAvailabilityEditLockState(int groupId)
    {
        var state = editLockService?.GetLockState(ManagerEditLockTargets.AvailabilityGroup(groupId));
        return state?.IsLocked == true ? state : null;
    }

    private Task<bool> IsAutomaticAsync(CancellationToken ct)
        => contractPolicy?.IsRuleActiveAsync(GetRequiredEmployeeId(), EmploymentContractRule.AutomaticWeekdayAvailability, ct)
            ?? Task.FromResult(false);

    private EmployeeAvailabilityGroupDto ToApiDto(EmployeeAvailabilityModel model, bool automatic = false)
    {
        var editLockState = GetAvailabilityEditLockState(model.Group.Id);
        var effectiveDays = automatic ? contractPolicy!.BuildAutomaticWeekdayDays(model.Group.Year, model.Group.Month) : null;

        return new EmployeeAvailabilityGroupDto
        {
            Id = model.Group.Id,
            Name = model.Group.Name,
            Year = model.Group.Year,
            Month = model.Group.Month,
            VisibleFromUtc = model.Group.VisibleFromUtc,
            VisibleToUtc = model.Group.VisibleToUtc,
            PublishedAtUtc = model.PublishedAtUtc,
            NotificationId = model.NotificationId,
            NotificationOccurredAtUtc = model.NotificationOccurredAtUtc,
            IsAutomatic = automatic,
            CanSubmit = model.CanSubmit && editLockState is null && !automatic,
            IsEditLocked = editLockState is not null,
            EditLockedBy = editLockState?.LockedBy,
            EmployeeLastModifiedAtUtc = model.Member.EmployeeLastModifiedAtUtc,
            Slots = (effectiveDays ?? model.Days).Select(ToSlotDto).ToList(),
            Members = (model.Group.Members ?? [])
                .Select(member => new EmployeeAvailabilityMemberDto
                {
                    EmployeeId = member.EmployeeId,
                    FirstName = member.Employee?.FirstName ?? string.Empty,
                    LastName = member.Employee?.LastName ?? string.Empty,
                    DisplayOrder = member.DisplayOrder,
                    Slots = (automatic && member.EmployeeId == model.Member.EmployeeId ? effectiveDays!.AsEnumerable() : member.Days ?? [])
                        .OrderBy(day => day.DayOfMonth).Select(ToSlotDto).ToList(),
                })
                .OrderBy(member => member.DisplayOrder)
                .ThenBy(member => member.FirstName)
                .ThenBy(member => member.LastName)
                .ThenBy(member => member.EmployeeId)
                .ToList(),
        };
    }

    private static AvailabilitySlotDto ToSlotDto(AvailabilityGroupDayModel model) => new()
    {
        Id = model.Id,
        AvailabilityGroupMemberId = model.AvailabilityGroupMemberId,
        DayOfMonth = model.DayOfMonth,
        Kind = model.Kind,
        IntervalStr = model.IntervalStr,
    };

    private static AvailabilityGroupDayModel ToDayModel(UpdateEmployeeAvailabilitySlotRequest request) => new()
    {
        DayOfMonth = request.DayOfMonth,
        Kind = request.Kind,
        IntervalStr = request.IntervalStr,
    };
}
