using BusinessLogicLayer.Contracts.Auth;
using Microsoft.Extensions.Options;
using WebApi.Options;

namespace WebApi.Auth;

/// <summary>
/// Creates signed JWT bearer tokens used by the React client.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtAuthOptions _options;

    public JwtTokenService(IOptions<JwtAuthOptions> options)
    {
        _options = options.Value;
    }

    public JwtTokenResult CreateAccessToken(AuthenticatedSessionDto session)
    {
        var issuedAtUtc = DateTimeOffset.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(_options.AccessTokenMinutes);

        return new JwtTokenResult
        {
            AccessToken = JwtTokenCodec.WriteAccessToken(_options, session, issuedAtUtc, expiresAtUtc),
            ExpiresAtUtc = expiresAtUtc,
        };
    }
}
