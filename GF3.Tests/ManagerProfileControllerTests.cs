using System.Security.Claims;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services.Abstractions;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Auth;
using WebApi.Controllers;
using ManagerApi = WebApi.Contracts.ManagerProfile;
using ManagerContracts = BusinessLogicLayer.Contracts.Managers;

namespace GF3.Tests;

public sealed class ManagerProfileControllerTests
{
    [Fact]
    public async Task ManagerProfileController_MapsCurrentUpdateListCreateAndDeleteRoutes()
    {
        var expiresAtUtc = DateTimeOffset.UtcNow.AddHours(1);
        var currentProfile = CreateProfile(3, "chief", "Chief Manager");
        var updatedProfile = CreateProfile(3, "chief-updated", "Chief Updated");
        var createdProfile = CreateProfile(4, "assistant", "Assistant Manager");
        var deletedProfile = CreateProfile(5, "old", "Old Manager");
        var accountService = new RecordingManagerAccountService
        {
            CurrentProfile = currentProfile,
            UpdatedProfile = updatedProfile,
            CreatedProfile = createdProfile,
            DeletedProfile = deletedProfile,
            Profiles = [currentProfile, createdProfile],
        };
        var presenceService = new RecordingManagerPresenceService(onlineManagerIds: [3]);
        var controller = new ManagerProfileController(
            accountService,
            presenceService,
            new StubJwtTokenService("updated-token", expiresAtUtc),
            new NoopWorkflowLogService(),
            new NoopRealtimeNotifier());
        SetManagerUser(controller, managerId: 3, userName: "chief", displayName: "Chief Manager");

        var currentResult = await controller.GetCurrent(CancellationToken.None);
        var updateResult = await controller.UpdateCurrent(new ManagerApi.UpdateManagerProfileRequest
        {
            UserName = "chief-updated",
            DisplayName = "Chief Updated",
            RecoveryEmail = "chief@example.com",
            NewPassword = "654321",
        }, CancellationToken.None);
        var listResult = await controller.ListManagers(CancellationToken.None);
        var createResult = await controller.CreateManager(new ManagerApi.CreateManagerRequest
        {
            UserName = "assistant",
            DisplayName = "Assistant Manager",
            RecoveryEmail = "assistant@example.com",
            Password = "password",
        }, CancellationToken.None);
        var deleteResult = await controller.DeleteManager(5, CancellationToken.None);

        var currentDto = Assert.IsType<ManagerApi.ManagerProfileDto>(
            Assert.IsType<OkObjectResult>(currentResult.Result).Value);
        var updateDto = Assert.IsType<ManagerApi.ManagerProfileUpdateResponseDto>(
            Assert.IsType<OkObjectResult>(updateResult.Result).Value);
        var managerDtos = Assert.IsAssignableFrom<IReadOnlyList<ManagerApi.ManagerProfileDto>>(
            Assert.IsType<OkObjectResult>(listResult.Result).Value);
        var createdDto = Assert.IsType<ManagerApi.ManagerProfileDto>(
            Assert.IsType<OkObjectResult>(createResult.Result).Value);

        Assert.Equal("Chief Manager", currentDto.DisplayName);
        Assert.True(currentDto.IsOnline);
        Assert.Equal((3, "chief"), accountService.LastProfileRequest);
        Assert.Equal((3, "chief"), accountService.LastUpdateIdentity);
        Assert.Equal("chief-updated", accountService.LastUpdateRequest!.UserName);
        Assert.Equal("updated-token", updateDto.AccessToken);
        Assert.Equal("chief-updated", updateDto.Session.UserName);
        Assert.Equal(3, updateDto.Session.ManagerId);
        Assert.Equal([true, false], managerDtos.Select(manager => manager.IsOnline));
        Assert.Equal("assistant", createdDto.UserName);
        Assert.Equal("assistant@example.com", accountService.LastCreateRequest!.RecoveryEmail);
        Assert.IsType<NoContentResult>(deleteResult);
        Assert.Equal((5, 3, "chief"), accountService.LastDeleteRequest);
    }

    private static ManagerContracts.ManagerProfileDto CreateProfile(int id, string userName, string displayName)
        => new()
        {
            Id = id,
            UserName = userName,
            DisplayName = displayName,
            RecoveryEmail = $"{userName}@example.com",
            CreatedAtUtc = new DateTimeOffset(2026, 5, 13, 12, 0, 0, TimeSpan.Zero),
        };

    private static void SetManagerUser(
        ControllerBase controller,
        int managerId,
        string userName,
        string displayName)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Role, AuthRoles.Manager),
                    new Claim("manager_id", managerId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new Claim("display_name", displayName),
                ], JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role)),
            },
        };
    }

    private sealed class RecordingManagerAccountService : IManagerAccountService
    {
        public ManagerContracts.ManagerProfileDto CurrentProfile { get; init; } = new();
        public ManagerContracts.ManagerProfileDto UpdatedProfile { get; init; } = new();
        public ManagerContracts.ManagerProfileDto CreatedProfile { get; init; } = new();
        public ManagerContracts.ManagerProfileDto DeletedProfile { get; init; } = new();
        public IReadOnlyList<ManagerContracts.ManagerProfileDto> Profiles { get; init; } = [];
        public (int? ManagerId, string? UserName)? LastProfileRequest { get; private set; }
        public (int? ManagerId, string? UserName)? LastUpdateIdentity { get; private set; }
        public ManagerContracts.UpdateManagerProfileRequest? LastUpdateRequest { get; private set; }
        public ManagerContracts.CreateManagerAccountRequest? LastCreateRequest { get; private set; }
        public (int ManagerId, int? CurrentManagerId, string? CurrentUserName)? LastDeleteRequest { get; private set; }

        public Task<ManagerContracts.ManagerProfileDto> GetProfileAsync(int? managerId, string? userName, CancellationToken ct = default)
        {
            LastProfileRequest = (managerId, userName);
            return Task.FromResult(CurrentProfile);
        }

        public Task<ManagerContracts.ManagerProfileDto> UpdateProfileAsync(
            int? managerId,
            string? userName,
            ManagerContracts.UpdateManagerProfileRequest request,
            CancellationToken ct = default)
        {
            LastUpdateIdentity = (managerId, userName);
            LastUpdateRequest = request;
            return Task.FromResult(UpdatedProfile);
        }

        public Task<IReadOnlyList<ManagerContracts.ManagerProfileDto>> ListProfilesAsync(CancellationToken ct = default)
            => Task.FromResult(Profiles);

        public Task<ManagerContracts.ManagerProfileDto> CreateAsync(ManagerContracts.CreateManagerAccountRequest request, CancellationToken ct = default)
        {
            LastCreateRequest = request;
            return Task.FromResult(CreatedProfile);
        }

        public Task<ManagerContracts.ManagerProfileDto> DeleteAsync(int managerId, int? currentManagerId, string? currentUserName, CancellationToken ct = default)
        {
            LastDeleteRequest = (managerId, currentManagerId, currentUserName);
            return Task.FromResult(DeletedProfile);
        }

        public Task<ManagerContracts.ManagerAccountModel?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ManagerContracts.ManagerAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task MarkLoginSucceededAsync(int managerId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingManagerPresenceService(HashSet<int> onlineManagerIds) : IManagerPresenceService
    {
        public bool IsManagerOnline(int managerId)
            => onlineManagerIds.Contains(managerId);

        public IReadOnlyDictionary<int, bool> GetOnlineStates(IEnumerable<int> managerIds)
            => managerIds.ToDictionary(managerId => managerId, onlineManagerIds.Contains);

        public ManagerPresenceChange ConnectManager(int managerId, string connectionId, DateTimeOffset connectedAtUtc)
            => throw new NotSupportedException();

        public ManagerPresenceChange? DisconnectConnection(string connectionId, DateTimeOffset disconnectedAtUtc)
            => throw new NotSupportedException();
    }

    private sealed class StubJwtTokenService(string accessToken, DateTimeOffset expiresAtUtc) : IJwtTokenService
    {
        public JwtTokenResult CreateAccessToken(AuthenticatedSessionDto session)
            => new()
            {
                AccessToken = accessToken,
                ExpiresAtUtc = expiresAtUtc,
            };
    }
}
