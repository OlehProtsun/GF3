using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.EmployeeProfile;
using WebApi.Services;

namespace WebApi.Controllers;

/// <summary>
/// Employee self-service profile endpoints used by the mobile workspace.
/// </summary>
[ApiController]
[Route("api/employee-profile")]
[Authorize(Roles = AuthRoles.Employee)]
public sealed class EmployeeProfileController(
    IEmployeeProfileService employeeProfileService,
    IWorkflowLogService workflowLogService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeProfileDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var profile = await employeeProfileService.GetAsync(GetRequiredEmployeeId(), cancellationToken).ConfigureAwait(false);
        return Ok(ToApiDto(profile));
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmployeeProfileDto>> UpdateCurrent(
        [FromBody] UpdateEmployeeProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await employeeProfileService
            .UpdateContactAsync(GetRequiredEmployeeId(), request.RecoveryEmail, request.Phone, cancellationToken)
            .ConfigureAwait(false);

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService
            .LogAsync(User, "Updated profile contact information.", cancellationToken)).ConfigureAwait(false);

        return Ok(ToApiDto(profile));
    }

    [HttpPost("me/password/send-code")]
    [AuthAttemptLimit("reset-code")]
    [ProducesResponseType(typeof(PasswordResetCodeDispatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PasswordResetCodeDispatchDto>> SendPasswordResetCode(CancellationToken cancellationToken)
    {
        var result = await employeeProfileService.SendPasswordResetCodeAsync(GetRequiredEmployeeId(), cancellationToken).ConfigureAwait(false);
        return Ok(new PasswordResetCodeDispatchDto
        {
            DeliveryHint = result.DeliveryHint,
            ExpiresAtUtc = result.ExpiresAtUtc,
        });
    }

    [HttpPost("me/password/confirm")]
    [AuthAttemptLimit("reset-confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmPasswordReset(
        [FromBody] CompletePasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        await employeeProfileService
            .ConfirmPasswordResetAsync(GetRequiredEmployeeId(), request.Code, request.NewPassword, cancellationToken)
            .ConfigureAwait(false);

        await PostCommitActions.RunAsync(HttpContext, () => workflowLogService
            .LogAsync(User, "Changed account password.", cancellationToken)).ConfigureAwait(false);

        return NoContent();
    }

    private int GetRequiredEmployeeId()
    {
        var employeeIdValue = User.FindFirstValue("employee_id");
        if (!int.TryParse(employeeIdValue, out var employeeId) || employeeId <= 0)
        {
            throw new BadHttpRequestException("The current employee session is invalid.");
        }

        return employeeId;
    }

    private static EmployeeProfileDto ToApiDto(BusinessLogicLayer.Contracts.Employees.EmployeeProfileDto profile) => new()
    {
        EmployeeId = profile.EmployeeId,
        Username = profile.Username,
        DisplayName = profile.DisplayName,
        RecoveryEmail = profile.RecoveryEmail,
        Phone = profile.Phone,
    };
}
