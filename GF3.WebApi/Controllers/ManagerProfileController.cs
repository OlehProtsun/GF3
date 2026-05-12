using System.Security.Claims;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Contracts.Auth;
using WebApi.Contracts.ManagerProfile;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/manager-profile")]
[Authorize(Roles = AuthRoles.Manager)]
public sealed class ManagerProfileController(
    IManagerAccountService managerAccountService,
    IManagerPresenceService managerPresenceService,
    IJwtTokenService jwtTokenService,
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier? realtimeNotifier = null) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(ManagerProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagerProfileDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var profile = await managerAccountService
            .GetProfileAsync(GetCurrentManagerId(), GetCurrentUserName(), cancellationToken)
            .ConfigureAwait(false);

        return Ok(ToApiDto(profile, managerPresenceService.IsManagerOnline(profile.Id)));
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(ManagerProfileUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ManagerProfileUpdateResponseDto>> UpdateCurrent(
        [FromBody] UpdateManagerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await managerAccountService
            .UpdateProfileAsync(
                GetCurrentManagerId(),
                GetCurrentUserName(),
                new BusinessLogicLayer.Contracts.Managers.UpdateManagerProfileRequest
                {
                    UserName = request.UserName,
                    DisplayName = request.DisplayName,
                    RecoveryEmail = request.RecoveryEmail,
                    NewPassword = request.NewPassword,
                },
                cancellationToken)
            .ConfigureAwait(false);

        var session = CreateSession(profile);
        var token = jwtTokenService.CreateAccessToken(session);
        await workflowLogService
            .LogAsync(AuthRoles.Manager, profile.DisplayName, null, "Updated manager profile.", cancellationToken)
            .ConfigureAwait(false);
        await NotifyManagerProfileChangedAsync(profile.Id, "manager-profile-updated").ConfigureAwait(false);

        return Ok(new ManagerProfileUpdateResponseDto
        {
            Profile = ToApiDto(profile, managerPresenceService.IsManagerOnline(profile.Id)),
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            Session = ToSessionDto(session),
        });
    }

    [HttpGet("managers")]
    [ProducesResponseType(typeof(IReadOnlyList<ManagerProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ManagerProfileDto>>> ListManagers(CancellationToken cancellationToken)
    {
        var profiles = await managerAccountService.ListProfilesAsync(cancellationToken).ConfigureAwait(false);
        var onlineStates = managerPresenceService.GetOnlineStates(profiles.Select(profile => profile.Id));
        return Ok(profiles
            .Select(profile => ToApiDto(profile, onlineStates.TryGetValue(profile.Id, out var isOnline) && isOnline))
            .ToList());
    }

    [HttpPost("managers")]
    [ProducesResponseType(typeof(ManagerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ManagerProfileDto>> CreateManager(
        [FromBody] CreateManagerRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await managerAccountService
            .CreateAsync(
                new BusinessLogicLayer.Contracts.Managers.CreateManagerAccountRequest
                {
                    UserName = request.UserName,
                    DisplayName = request.DisplayName,
                    RecoveryEmail = request.RecoveryEmail,
                    Password = request.Password,
                },
                cancellationToken)
            .ConfigureAwait(false);

        await workflowLogService
            .LogAsync(AuthRoles.Manager, GetCurrentDisplayName(), null, $"Created manager account {profile.DisplayName}.", cancellationToken)
            .ConfigureAwait(false);
        await NotifyManagerProfileChangedAsync(profile.Id, "manager-account-created").ConfigureAwait(false);

        return Ok(ToApiDto(profile));
    }

    [HttpDelete("managers/{managerId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteManager(int managerId, CancellationToken cancellationToken)
    {
        var deletedProfile = await managerAccountService
            .DeleteAsync(managerId, GetCurrentManagerId(), GetCurrentUserName(), cancellationToken)
            .ConfigureAwait(false);

        await workflowLogService
            .LogAsync(AuthRoles.Manager, GetCurrentDisplayName(), null, $"Deleted manager account {deletedProfile.DisplayName}.", cancellationToken)
            .ConfigureAwait(false);
        await NotifyManagerProfileChangedAsync(managerId, "manager-account-deleted").ConfigureAwait(false);

        return NoContent();
    }

    private Task NotifyManagerProfileChangedAsync(int managerId, string reason)
        => realtimeNotifier?.NotifyManagerDataChangedAsync(
            ManagerEditResourceTypes.ManagerProfile,
            managerId.ToString(),
            reason) ?? Task.CompletedTask;

    private int? GetCurrentManagerId()
        => int.TryParse(User.FindFirstValue("manager_id"), out var parsed) ? parsed : null;

    private string? GetCurrentUserName()
        => User.Identity?.Name;

    private string GetCurrentDisplayName()
        => User.FindFirstValue("display_name") ?? User.Identity?.Name ?? "Manager";

    private static AuthenticatedSessionDto CreateSession(BusinessLogicLayer.Contracts.Managers.ManagerProfileDto profile) => new()
    {
        Role = AuthService.ManagerRole,
        UserName = profile.UserName,
        DisplayName = profile.DisplayName,
        ManagerId = profile.Id,
    };

    private static SessionDto ToSessionDto(AuthenticatedSessionDto session) => new()
    {
        Role = session.Role,
        UserName = session.UserName,
        DisplayName = session.DisplayName,
        ManagerId = session.ManagerId,
        EmployeeId = session.EmployeeId,
    };

    private static ManagerProfileDto ToApiDto(BusinessLogicLayer.Contracts.Managers.ManagerProfileDto profile, bool isOnline = false) => new()
    {
        Id = profile.Id,
        UserName = profile.UserName,
        DisplayName = profile.DisplayName,
        RecoveryEmail = profile.RecoveryEmail,
        LastLoginAtUtc = profile.LastLoginAtUtc,
        IsOnline = isOnline,
        CreatedAtUtc = profile.CreatedAtUtc,
    };
}
