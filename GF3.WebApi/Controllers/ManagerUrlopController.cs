using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.Urlop;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController, Route("api/manager-urlop"), Authorize(Roles = AuthRoles.Manager)]
public sealed class ManagerUrlopController(IUrlopService service, IWorkflowLogService workflowLogService, IRealtimeNotifier realtimeNotifier) : ControllerBase
{
    private int ManagerId => int.TryParse(User.FindFirstValue("manager_id"), out var id) && id > 0
        ? id : throw new BadHttpRequestException("The current manager session is invalid.");

    [HttpGet]
    public async Task<ActionResult<UrlopManagerMonthDto>> Get(int shopId, int year, int month, CancellationToken ct)
        => Ok((await service.GetManagerMonthAsync(shopId, year, month, ct)).ToDto());

    [HttpGet("approved")]
    public async Task<ActionResult<IReadOnlyList<ApprovedUrlopDayDto>>> Approved(DateOnly from, DateOnly to, int? shopId, CancellationToken ct)
        => Ok((await service.GetApprovedDaysAsync(from, to, shopId, ct)).Select(x => x.ToDto()).ToArray());

    [HttpPut("month")]
    public async Task<ActionResult<UrlopMonthDto>> SetMonth(SetUrlopMonthDto body, CancellationToken ct)
    {
        var result = await service.SetMonthOpenAsync(body.ShopId, body.Year, body.Month, body.IsOpen, ct);
        await ChangedAsync($"{(body.IsOpen ? "Opened" : "Closed")} vacation submissions for shop {body.ShopId}, {body.Year}-{body.Month:00}.", ct);
        return Ok(result.ToDto());
    }

    [HttpPost("requests")]
    public async Task<ActionResult<UrlopRequestDto>> Create(CreateUrlopDto body, CancellationToken ct)
    {
        var result = await service.CreateApprovedAsync(ManagerId, body.ShopId, body.EmployeeId, body.Year, body.Month, body.Days, ct);
        await ChangedAsync($"Created approved vacation request {result.RequestId}.", ct);
        return CreatedAtAction(nameof(Get), new { shopId = body.ShopId, year = body.Year, month = body.Month }, result.ToDto());
    }

    [HttpPut("requests/{requestId:guid}")]
    public async Task<ActionResult<UrlopRequestDto>> Replace(Guid requestId, ReplaceUrlopDaysDto body, CancellationToken ct)
    {
        var result = await service.ReplaceDaysAsync(ManagerId, requestId, body.Days, ct);
        await ChangedAsync($"Edited vacation request {requestId}.", ct);
        return Ok(result.ToDto());
    }

    [HttpPost("requests/{requestId:guid}/approve")]
    public async Task<ActionResult<UrlopRequestDto>> Approve(Guid requestId, CancellationToken ct)
    {
        var result = await service.ApproveAsync(ManagerId, requestId, ct);
        await ChangedAsync($"Approved vacation request {requestId}.", ct);
        return Ok(result.ToDto());
    }

    [HttpPost("requests/{requestId:guid}/reject")]
    public async Task<ActionResult<UrlopRequestDto>> Reject(Guid requestId, CancellationToken ct)
    {
        var result = await service.RejectAsync(ManagerId, requestId, ct);
        await ChangedAsync($"Rejected vacation request {requestId}.", ct);
        return Ok(result.ToDto());
    }

    [HttpDelete("requests/{requestId:guid}")]
    public async Task<IActionResult> Delete(Guid requestId, CancellationToken ct)
    {
        await service.DeleteAsync(ManagerId, requestId, ct);
        await ChangedAsync($"Deleted vacation request {requestId}.", ct);
        return NoContent();
    }

    private async Task ChangedAsync(string action, CancellationToken ct)
    {
        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService.LogAsync(User, action, ct));
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier.NotifyManagerDataChangedAsync("urlop", null, "urlop-changed"));
    }
}
