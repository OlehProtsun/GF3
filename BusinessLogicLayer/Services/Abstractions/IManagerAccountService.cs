using BusinessLogicLayer.Contracts.Managers;
using BusinessLogicLayer.Contracts.Employees;

namespace BusinessLogicLayer.Services.Abstractions;

/// <summary>
/// Business operations for manager login accounts.
/// </summary>
public interface IManagerAccountService
{
    Task<ManagerAccountModel?> AuthenticateAsync(string username, string password, CancellationToken ct = default);

    Task<ManagerAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<ManagerProfileDto> GetProfileAsync(int? managerId, string? userName, CancellationToken ct = default);

    Task<IReadOnlyList<ManagerProfileDto>> ListProfilesAsync(CancellationToken ct = default);

    Task<ManagerProfileDto> CreateAsync(CreateManagerAccountRequest request, CancellationToken ct = default);

    Task<ManagerProfileDto> UpdateProfileAsync(int? managerId, string? userName, UpdateManagerProfileRequest request, CancellationToken ct = default);

    Task<ManagerProfileDto> DeleteAsync(int managerId, int? currentManagerId, string? currentUserName, CancellationToken ct = default);

    Task MarkLoginSucceededAsync(int managerId, CancellationToken ct = default);

    Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default);

    Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default);
}
