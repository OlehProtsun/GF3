using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.ShiftSwaps;

namespace WebApi.Controllers;

[ApiController, Route("api/employee-shift-swap-approvals"), Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeContractShiftApprovalsController(ContractShiftApprovalService service) : ControllerBase
{
    private int EmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var id) && id > 0
        ? id : throw new BadHttpRequestException("The current employee session is invalid.");
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContractShiftApprovalDto>>> Get(CancellationToken ct)
        => Ok(await service.GetMyRequestsAsync(EmployeeId, ct));
    [HttpPost("claims")]
    public async Task<ActionResult<ContractShiftApprovalDto>> Claim(CreateContractClaimApprovalRequest request, CancellationToken ct)
    {
        var dto = await service.CreateClaimRequestAsync(EmployeeId, request, HttpContext, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }
    [HttpPost("giveaway-exceptions")]
    public async Task<ActionResult<ContractShiftApprovalDto>> GiveException(CreateEmployeeShiftSwapRequest request, CancellationToken ct)
    {
        var dto = await service.CreateGiveExceptionRequestAsync(EmployeeId, request, HttpContext, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ContractShiftApprovalDto>> Cancel(int id, CancellationToken ct)
        => Ok(await service.CancelAsync(EmployeeId, id, HttpContext, ct));
}
