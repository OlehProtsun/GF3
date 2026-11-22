using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.EmployeeLaunches;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/employee-launches")]
public sealed class EmployeeLaunchesController(EmployeeLaunchService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = AuthRoles.Employee)]
    public async Task<IActionResult> Record(RecordEmployeeLaunchRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) || employeeId <= 0)
            throw new BadHttpRequestException("The current employee session is invalid.");

        await service.RecordAsync(employeeId, request.Mode, ct);
        return NoContent();
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.SystemManager)]
    public async Task<ActionResult<IReadOnlyList<EmployeeLaunchUsageDto>>> GetUsage(CancellationToken ct)
        => Ok(await service.GetUsageAsync(ct));
}
