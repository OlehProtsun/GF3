using WebApi.Contracts.Auth;

namespace WebApi.Contracts.ManagerProfile;

public sealed class ManagerProfileUpdateResponseDto
{
    public ManagerProfileDto Profile { get; set; } = new();

    public string AccessToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public SessionDto Session { get; set; } = new();
}
