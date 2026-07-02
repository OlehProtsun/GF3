using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Auth;
using WebApi.Contracts.AvailabilityGroups.Transfers;
using WebApi.Mappers;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/availability-groups")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class AvailabilityGroupTransfersController(
    AppDbContext db,
    IAvailabilityGroupTransferService transferService,
    IRealtimeNotifier? realtimeNotifier = null,
    IManagerEditLockService? editLockService = null,
    IWorkflowLogService? workflowLogService = null) : ControllerBase
{

    [HttpGet("{targetGroupId:int}/members/{targetMemberId:int}/transfer-sources")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityTransferSourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AvailabilityTransferSourceDto>>> GetSources(
        int targetGroupId,
        int targetMemberId,
        CancellationToken cancellationToken)
    {
        var sources = await transferService
            .GetSourcesAsync(targetGroupId, targetMemberId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(sources.Select(source => source.ToApiDto()));
    }

    [HttpGet("transfer-preview")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityTransferSourceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<AvailabilityTransferSourceDto>>> GetPreviewSources(
        [FromQuery] string employeeIds,
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int? targetGroupId,
        CancellationToken cancellationToken)
    {
        var parsedEmployeeIds = employeeIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var employeeId) ? employeeId : 0)
            .Where(employeeId => employeeId > 0)
            .Distinct()
            .ToList();
        if (parsedEmployeeIds.Count == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "Select at least one employee before loading availability transfer sources.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        var sources = await transferService
            .GetPreviewSourcesAsync(parsedEmployeeIds, year, month, targetGroupId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(sources.Select(source => source.ToApiDto()));
    }

    [HttpGet("{sourceGroupId:int}/transfer-hints")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityTransferHintDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AvailabilityTransferHintDto>>> GetHints(
        int sourceGroupId,
        CancellationToken cancellationToken)
    {
        var hints = await transferService.GetHintsAsync(sourceGroupId, cancellationToken).ConfigureAwait(false);
        return Ok(hints.Select(hint => hint.ToApiDto()));
    }

    [HttpPost("{targetGroupId:int}/members/{targetMemberId:int}/transfer-days")]
    [ProducesResponseType(typeof(AvailabilityTransferResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvailabilityTransferResultDto>> TransferDays(
        int targetGroupId,
        int targetMemberId,
        [FromBody] TransferAvailabilityDaysRequest request,
        CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(targetGroupId, "The target availability") is { } targetConflict)
        {
            return targetConflict;
        }
        if (CreateEditLockConflictResult(request.SourceGroupId, "The source availability") is { } sourceConflict)
        {
            return sourceConflict;
        }

        var result = await transferService.TransferDaysAsync(
            targetGroupId,
            targetMemberId,
            request.SourceGroupId,
            request.DayOfMonths,
            cancellationToken).ConfigureAwait(false);
        var employeeName = await db.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == result.EmployeeId)
            .Select(employee => employee.FirstName + " " + employee.LastName)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? $"Employee #{result.EmployeeId}";

        await LogManagerActionAsync(
            $"Moved {result.DayOfMonths.Count} availability day(s) for {employeeName} from \"{result.SourceGroupName}\" to \"{result.TargetGroupName}\".",
            cancellationToken).ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(result.SourceGroupId, "manager-availability-days-transferred").ConfigureAwait(false);
        await NotifyAvailabilityChangedAsync(result.TargetGroupId, "manager-availability-days-transferred").ConfigureAwait(false);
        return Ok(result.ToApiDto());
    }

    private ActionResult? CreateEditLockConflictResult(int groupId, string resourceLabel)
        => ManagerEditLockHttp.CreateConflictResult(
            this,
            editLockService,
            ManagerEditLockTargets.AvailabilityGroup(groupId),
            resourceLabel);

    private Task NotifyAvailabilityChangedAsync(int groupId, string reason)
        => realtimeNotifier?.NotifyManagerDataChangedAsync(
            ManagerEditResourceTypes.AvailabilityGroup,
            groupId.ToString(),
            reason) ?? Task.CompletedTask;

    private Task LogManagerActionAsync(string action, CancellationToken cancellationToken)
        => workflowLogService?.LogAsync(User, action, cancellationToken) ?? Task.CompletedTask;
}
