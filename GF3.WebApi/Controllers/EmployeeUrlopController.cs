using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.Urlop;
using WebApi.Services;
using WebApi.Realtime;

namespace WebApi.Controllers;

[ApiController, Route("api/employee-urlop"), Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeUrlopController(IUrlopService service, TimeProvider timeProvider, IWorkflowLogService workflowLogService, IRealtimeNotifier realtimeNotifier) : ControllerBase
{
    private int EmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var id) && id > 0
        ? id : throw new BadHttpRequestException("The current employee session is invalid.");
    private DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(),
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw")).DateTime);

    [HttpGet]
    public async Task<ActionResult<UrlopEmployeeCalendarDto>> Get(CancellationToken ct)
        => Ok((await service.GetEmployeeCalendarAsync(EmployeeId, Today, ct)).ToDto());

    [HttpGet("approved")]
    public async Task<ActionResult<IReadOnlyList<ApprovedUrlopDayDto>>> Approved(int shopId, int year, int month, CancellationToken ct)
        => Ok((await service.GetEmployeeApprovedMonthAsync(EmployeeId, shopId, year, month, ct)).Select(x => x.ToDto()).ToArray());

    [HttpPost("requests")]
    public async Task<ActionResult<UrlopRequestDto>> Submit(SubmitUrlopDto body, CancellationToken ct)
    {
        var result = await service.SubmitAsync(EmployeeId, body.ShopId, body.Year, body.Month, body.Days, Today, ct);
        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService.LogAsync(User, $"Submitted vacation request {result.RequestId}.", ct));
        await PostCommitActions.RunAsync(HttpContext, () => realtimeNotifier.NotifyManagerDataChangedAsync("urlop", null, "urlop-changed"));
        return CreatedAtAction(nameof(Get), result.ToDto());
    }
}
