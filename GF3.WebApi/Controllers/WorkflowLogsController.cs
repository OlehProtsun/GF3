using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.WorkflowLogs;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/workflow-logs")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class WorkflowLogsController(IWorkflowLogService workflowLogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkflowLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WorkflowLogDto>>> GetRecent(
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        var entries = await workflowLogService.GetRecentAsync(limit, cancellationToken).ConfigureAwait(false);
        return Ok(entries.Select(ToDto));
    }

    private static WorkflowLogDto ToDto(DataAccessLayer.Models.WorkflowLogEntryModel entry) => new()
    {
        Id = entry.Id,
        OccurredAtUtc = entry.OccurredAtUtc,
        ActorRole = entry.ActorRole,
        ActorEmployeeId = entry.ActorEmployeeId,
        ActorName = entry.ActorName,
        Action = entry.Action,
    };
}
