using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.WorkflowLogs;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/workflow-logs")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class WorkflowLogsController(
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier? realtimeNotifier = null) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkflowLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WorkflowLogDto>>> GetRecent(
        CancellationToken cancellationToken = default)
    {
        var entries = await workflowLogService.GetRecentAsync(cancellationToken).ConfigureAwait(false);
        return Ok(entries.Select(ToDto));
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(WorkflowLogSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkflowLogSettingsDto>> GetSettings(CancellationToken cancellationToken = default)
    {
        var settings = await workflowLogService.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ToDto(settings));
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(WorkflowLogSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkflowLogSettingsDto>> UpdateSettings(
        UpdateWorkflowLogSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedAudience(request.Audience))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid workflow-log audience",
                Detail = "Audience must be all, managers, or employees.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        var settings = await workflowLogService
            .UpdateSettingsAsync(request.IsEnabled, request.Audience, cancellationToken)
            .ConfigureAwait(false);
        await NotifyChangedAsync("workflow-log-settings-updated").ConfigureAwait(false);
        return Ok(ToDto(settings));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        if (!await workflowLogService.DeleteAsync(id, cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        await NotifyChangedAsync("workflow-log-deleted").ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("bulk-delete")]
    [ProducesResponseType(typeof(WorkflowLogDeleteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkflowLogDeleteResultDto>> BulkDelete(
        DeleteWorkflowLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        int deletedCount;
        if (request.DeleteAll)
        {
            deletedCount = await workflowLogService.DeleteAllAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (!request.FromUtc.HasValue || !request.ToUtc.HasValue || request.ToUtc <= request.FromUtc)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid workflow-log range",
                    Detail = "Choose a valid start and end date before deleting history.",
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            deletedCount = await workflowLogService
                .DeleteRangeAsync(request.FromUtc.Value, request.ToUtc.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        await NotifyChangedAsync("workflow-logs-bulk-deleted").ConfigureAwait(false);
        return Ok(new WorkflowLogDeleteResultDto { DeletedCount = deletedCount });
    }

    private Task NotifyChangedAsync(string reason)
        => realtimeNotifier?.NotifyManagerDataChangedAsync("workflow-logs", null, reason) ?? Task.CompletedTask;

    private static bool IsSupportedAudience(string audience)
        => audience.Trim().ToLowerInvariant() is
            WorkflowLogSettingsModel.AllAudience or
            WorkflowLogSettingsModel.ManagersAudience or
            WorkflowLogSettingsModel.EmployeesAudience;

    private static WorkflowLogDto ToDto(WorkflowLogEntryModel entry) => new()
    {
        Id = entry.Id,
        OccurredAtUtc = entry.OccurredAtUtc,
        ActorRole = entry.ActorRole,
        ActorEmployeeId = entry.ActorEmployeeId,
        ActorName = entry.ActorName,
        Action = entry.Action,
    };

    private static WorkflowLogSettingsDto ToDto(WorkflowLogSettingsModel settings) => new()
    {
        IsEnabled = settings.IsEnabled,
        Audience = settings.Audience,
    };
}
