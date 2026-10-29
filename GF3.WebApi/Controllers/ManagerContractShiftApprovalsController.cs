using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Realtime;
using WebApi.ShiftSwaps;

namespace WebApi.Controllers;

[ApiController, Route("api/containers/{containerId:int}/graphs/{graphId:int}/shift-swap-approvals"), Authorize(Roles = AuthRoles.Manager)]
public sealed class ManagerContractShiftApprovalsController(ContractShiftApprovalService service, IManagerEditLockService locks) : ControllerBase
{
    private int ManagerId => int.TryParse(User.FindFirstValue("manager_id"), out var id) && id > 0
        ? id : throw new BadHttpRequestException("The current manager session is invalid.");
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContractShiftApprovalDto>>> Get(int containerId, int graphId, CancellationToken ct)
        => Ok(await service.GetGraphRequestsAsync(containerId, graphId, ct));
    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<ContractShiftApprovalDto>> Approve(int containerId, int graphId, int id, CancellationToken ct)
    {
        if (ManagerEditLockHttp.CreateConflictResult(this, locks, ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict) return conflict;
        return Ok(await service.ApproveAsync(ManagerId, containerId, graphId, id, HttpContext, ct));
    }
    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<ContractShiftApprovalDto>> Reject(int containerId, int graphId, int id, CancellationToken ct)
    {
        if (ManagerEditLockHttp.CreateConflictResult(this, locks, ManagerEditLockTargets.Schedule(containerId, graphId), "This schedule") is { } conflict) return conflict;
        return Ok(await service.RejectAsync(ManagerId, containerId, graphId, id, HttpContext, ct));
    }
}
