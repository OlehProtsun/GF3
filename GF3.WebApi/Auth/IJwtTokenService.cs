using BusinessLogicLayer.Contracts.Auth;

namespace WebApi.Auth;

/// <summary>
/// Issues signed JWT access tokens for authenticated sessions.
/// </summary>
public interface IJwtTokenService
{
    JwtTokenResult CreateAccessToken(AuthenticatedSessionDto session);
}
