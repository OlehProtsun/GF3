using static WebApi.ShiftSwaps.ShiftSwapRules;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.Sqlite;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Realtime;
using WebApi.Services;
using WebApi.ShiftSwaps;

namespace WebApi.Controllers;

[ApiController]
[Route("api/employee-shift-swaps")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeShiftSwapsController(
    AppDbContext db, IScheduleEditLockService scheduleEditLockService, IWorkflowLogService workflowLogService,
    IRealtimeNotifier realtimeNotifier, ScheduleVersionCommitService versionCommitService,
    IEmploymentContractPolicyService? contractPolicy = null, ShiftSwapExecutionService? executionService = null) : ControllerBase
{
    private readonly ShiftSwapExecutionService execution = executionService ?? new(db, scheduleEditLockService,
        workflowLogService, realtimeNotifier, versionCommitService, contractPolicy);

    [HttpGet("employees")]
    public async Task<ActionResult<IEnumerable<ShiftSwapEmployeeDto>>> GetEmployees(CancellationToken cancellationToken)
        => Ok(await execution.GetEmployees(GetRequiredEmployeeId(), HttpContext, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShiftSwapDto>>> GetVisible(CancellationToken cancellationToken)
        => Ok(await execution.GetVisible(GetRequiredEmployeeId(), HttpContext, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ShiftSwapDto>> Create([FromBody] CreateEmployeeShiftSwapRequest request, CancellationToken cancellationToken)
    {
        var dto = await execution.Create(GetRequiredEmployeeId(), HttpContext, request, cancellationToken);
        return CreatedAtAction(nameof(GetVisible), new { id = dto.Id }, dto);
    }

    [HttpPost("{id:int}/accept")]
    public async Task<ActionResult<ShiftSwapDto>> Accept(int id, CancellationToken cancellationToken,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AcceptEmployeeShiftSwapRequest? request = null)
        => Ok(await execution.Accept(GetRequiredEmployeeId(), HttpContext, id, cancellationToken, request));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ShiftSwapDto>> Cancel(int id, CancellationToken cancellationToken)
        => Ok(await execution.Cancel(GetRequiredEmployeeId(), HttpContext, id, cancellationToken));

    [HttpGet("return-candidates")]
    public async Task<ActionResult<ShiftSwapReturnCandidatePageDto>> GetReturnCandidates(
        [FromQuery] int sourceScheduleId, [FromQuery] int sourceSlotId,
        [FromQuery] string fromTime, [FromQuery] string toTime, [FromQuery] int targetEmployeeId,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        => Ok(await execution.GetReturnCandidates(GetRequiredEmployeeId(), HttpContext, sourceScheduleId, sourceSlotId,
            fromTime, toTime, targetEmployeeId, cancellationToken, page, pageSize));

    [HttpPost("preview")]
    public async Task<ActionResult<ShiftSwapOfferPreviewDto>> Preview([FromBody] CreateEmployeeShiftSwapRequest request, CancellationToken cancellationToken)
        => Ok(await execution.Preview(GetRequiredEmployeeId(), HttpContext, request, cancellationToken));

    private int GetRequiredEmployeeId()
    {
        var employeeIdValue = User.FindFirstValue("employee_id");
        if (!int.TryParse(employeeIdValue, out var employeeId) || employeeId <= 0)
        {
            throw new BadHttpRequestException("The current employee session is invalid.");
        }

        return employeeId;
    }

}
