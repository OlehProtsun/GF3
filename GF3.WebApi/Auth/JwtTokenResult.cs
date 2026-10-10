namespace WebApi.Auth;

/// <summary>
/// One signed JWT access token together with its UTC expiration timestamp.
/// </summary>
public sealed class JwtTokenResult
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
