namespace WebApi.Contracts.ManagerProfile;

public sealed class UpdateManagerProfileRequest
{
    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? RecoveryEmail { get; set; }

    public string? NewPassword { get; set; }
}
