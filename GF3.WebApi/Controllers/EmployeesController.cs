using BusinessLogicLayer.Common;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.Employees;
using WebApi.Infrastructure;
using WebApi.Mappers;
using WebApi.Realtime;

namespace WebApi.Controllers;

/// <summary>
/// CRUD endpoints for employees.
/// The controller intentionally stays thin: it validates resource existence, delegates business
/// rules to the facade layer, and translates domain delete failures into HTTP problem responses.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AuthRoles.Manager)]
public class EmployeesController(
    IEmployeeFacade employeeFacade,
    IRealtimeNotifier? realtimeNotifier = null,
    IManagerEditLockService? editLockService = null) : ControllerBase
{
    /// <summary>
    /// Returns all employees as API DTOs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var employees = await employeeFacade.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(employees.Select(employee => employee.ToApiDto()));
    }

    /// <summary>
    /// Returns one employee by id or fails with a not-found problem.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployeeDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var employee = await GetRequiredEmployeeAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(employee.ToApiDto());
    }

    /// <summary>
    /// Creates a new employee and returns its canonical API representation.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var created = await employeeFacade.CreateAsync(request.ToSaveRequest(), cancellationToken).ConfigureAwait(false);
        var dto = created.ToApiDto();
        await NotifyEmployeeChangedAsync(dto.Id, "manager-employee-created").ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Updates an existing employee.
    /// The explicit existence check keeps the HTTP contract predictable: missing employees return 404
    /// instead of relying on deeper infrastructure to infer the failure mode.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(id) is { } conflict)
        {
            return conflict;
        }

        await EnsureEmployeeExistsAsync(id, cancellationToken).ConfigureAwait(false);
        await employeeFacade.UpdateAsync(request.ToSaveRequest(id), cancellationToken).ConfigureAwait(false);
        await NotifyEmployeeChangedAsync(id, "manager-employee-updated").ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Deletes an employee when no dependent data still references it.
    /// Instead of throwing for expected business-rule failures, the controller returns a structured
    /// validation problem so the frontend can show a friendly, stable error state.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (CreateEditLockConflictResult(id) is { } conflict)
        {
            return conflict;
        }

        await EnsureEmployeeExistsAsync(id, cancellationToken).ConfigureAwait(false);

        var result = await employeeFacade.TryDeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return CreateDeleteValidationResult(result);
        }

        await NotifyEmployeeChangedAsync(id, "manager-employee-deleted").ConfigureAwait(false);
        return NoContent();
    }

    private ActionResult? CreateEditLockConflictResult(int employeeId)
        => ManagerEditLockHttp.CreateConflictResult(
            this,
            editLockService,
            ManagerEditLockTargets.Employee(employeeId),
            "This employee");

    private Task NotifyEmployeeChangedAsync(int employeeId, string reason)
        => realtimeNotifier?.NotifyManagerDataChangedAsync(
            ManagerEditResourceTypes.Employee,
            employeeId.ToString(),
            reason) ?? Task.CompletedTask;

    private async Task EnsureEmployeeExistsAsync(int id, CancellationToken cancellationToken)
        => _ = await GetRequiredEmployeeAsync(id, cancellationToken).ConfigureAwait(false);

    private async Task<BusinessLogicLayer.Contracts.Employees.EmployeeDto> GetRequiredEmployeeAsync(int id, CancellationToken cancellationToken)
        => await employeeFacade.GetAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Employee with id {id} was not found.");

    private BadRequestObjectResult CreateDeleteValidationResult(DeleteOperationResult result)
        => BadRequest(ApiProblemDetailsFactory.CreateValidationProblem(
            HttpContext,
            result.Errors,
            result.Message));
}
