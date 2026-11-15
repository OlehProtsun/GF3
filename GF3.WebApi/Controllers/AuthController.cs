using System.Security.Claims;
using System.Globalization;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.Auth;
using WebApi.Services;

namespace WebApi.Controllers;

/// <summary>
/// JWT-based login/session endpoints used by the React frontend.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IAuthService authService,
    IJwtTokenService jwtTokenService,
    IWorkflowLogService workflowLogService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [AuthAttemptLimit("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.AuthenticateAsync(request.Username, request.Password, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Invalid username or password.",
                Status = StatusCodes.Status401Unauthorized,
            });
        }

        session.WorkspaceMode = session.Role == AuthRoles.Manager ? ManagerWorkspaceModes.Choose : null;
        var token = jwtTokenService.CreateAccessToken(session);
        await workflowLogService
            .LogAsync(session.Role, session.DisplayName, session.EmployeeId, "Logged in.", cancellationToken)
            .ConfigureAwait(false);

        return Ok(new LoginResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            Session = ToSessionDto(session),
        });
    }

    [Authorize]
    [HttpGet("session")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<SessionDto> GetSession()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        return Ok(new SessionDto
        {
            WorkspaceMode = User.IsInRole(AuthRoles.Manager) ? User.FindFirstValue(ManagerWorkspaceModes.ClaimType) : null,
            IsSystemManager = User.IsInRole(AuthRoles.Manager) && User.HasClaim(AuthPolicies.SystemManagerClaim, "true"),
            Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            UserName = User.Identity.Name ?? string.Empty,
            DisplayName = User.FindFirstValue("display_name") ?? User.Identity.Name ?? string.Empty,
            ManagerId = TryParseNullableInt(User.FindFirstValue("manager_id")),
            EmployeeId = TryParseNullableInt(User.FindFirstValue("employee_id")),
        });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout() => NoContent();

    [Authorize(Roles = AuthRoles.Manager)]
    [HttpPost("manager-mode")]
    public ActionResult<LoginResponseDto> ChangeManagerWorkspaceMode([FromBody] ChangeManagerWorkspaceModeRequest request)
    {
        if (!ManagerWorkspaceModes.IsSelectable(request.Mode))
        {
            ModelState.AddModelError(nameof(request.Mode), "Select pc or phone.");
            return ValidationProblem(ModelState);
        }

        var managerId = TryParseNullableInt(User.FindFirstValue("manager_id"));
        if (managerId is not > 0 || string.IsNullOrWhiteSpace(User.Identity?.Name) ||
            !long.TryParse(User.FindFirstValue("credential_version"), NumberStyles.None, CultureInfo.InvariantCulture, out var credentialVersion))
        {
            return Forbid();
        }

        var session = new AuthenticatedSessionDto
        {
            Role = AuthRoles.Manager,
            UserName = User.Identity.Name,
            DisplayName = User.FindFirstValue("display_name") ?? User.Identity.Name,
            ManagerId = managerId,
            CredentialVersion = credentialVersion,
            IsSystemManager = User.HasClaim(AuthPolicies.SystemManagerClaim, "true"),
            WorkspaceMode = request.Mode,
        };
        var token = jwtTokenService.CreateAccessToken(session);
        return Ok(new LoginResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            Session = ToSessionDto(session),
        });
    }

    [AllowAnonymous]
    [HttpPost("password/send-code")]
    [AuthAttemptLimit("reset-code")]
    [ProducesResponseType(typeof(PasswordResetCodeDispatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PasswordResetCodeDispatchDto>> SendPasswordResetCode(
        [FromBody] SendPasswordResetCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.SendPasswordResetCodeAsync(request.Username, cancellationToken).ConfigureAwait(false);
        return Ok(new PasswordResetCodeDispatchDto
        {
            DeliveryHint = result.DeliveryHint,
            ExpiresAtUtc = result.ExpiresAtUtc,
        });
    }

    [AllowAnonymous]
    [HttpPost("password/confirm")]
    [AuthAttemptLimit("reset-confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmPasswordReset(
        [FromBody] CompleteForgotPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        await authService
            .ConfirmPasswordResetAsync(request.Username, request.Code, request.NewPassword, cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }

    private static SessionDto ToSessionDto(BusinessLogicLayer.Contracts.Auth.AuthenticatedSessionDto session) => new()
    {
        WorkspaceMode = session.WorkspaceMode,
        IsSystemManager = session.IsSystemManager,
        Role = session.Role,
        UserName = session.UserName,
        DisplayName = session.DisplayName,
        ManagerId = session.ManagerId,
        EmployeeId = session.EmployeeId,
    };

    private static int? TryParseNullableInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;
}
