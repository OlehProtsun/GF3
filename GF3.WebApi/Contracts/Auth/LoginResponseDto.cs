namespace WebApi.Contracts.Auth;

/// <summary>
/// Login response that carries the issued JWT plus the authenticated session payload.
/// </summary>
public sealed class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public SessionDto Session { get; set; } = new();
}
