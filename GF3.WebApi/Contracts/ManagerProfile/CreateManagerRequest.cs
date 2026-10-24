namespace WebApi.Contracts.ManagerProfile;

public sealed class CreateManagerRequest
{
    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? RecoveryEmail { get; set; }

    public string Password { get; set; } = string.Empty;
}
