using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using WebApi.Options;

namespace WebApi.Auth;

/// <summary>
/// Lightweight bearer-token authentication handler that validates JWTs without external packages.
/// </summary>
public sealed class JwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly JwtAuthOptions _jwtOptions;

    public JwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<JwtAuthOptions> jwtOptions)
        : base(options, logger, encoder)
    {
        _jwtOptions = jwtOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = TryResolveAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!JwtTokenCodec.TryReadAccessToken(token, _jwtOptions, TimeSpan.FromMinutes(1), out var session, out var error))
        {
            return Task.FromResult(AuthenticateResult.Fail(error ?? "Invalid bearer token."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, session.UserName),
            new(ClaimTypes.Role, session.Role),
            new("display_name", session.DisplayName),
        };

        if (session.EmployeeId.HasValue)
        {
            claims.Add(new Claim("employee_id", session.EmployeeId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (session.ManagerId.HasValue)
        {
            claims.Add(new Claim("manager_id", session.ManagerId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        var identity = new ClaimsIdentity(claims, JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, JwtAuthenticationDefaults.SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string? TryResolveAccessToken()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
        {
            var authorizationHeader = authorizationHeaderValues.ToString();
            const string bearerPrefix = "Bearer ";
            if (authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var token = authorizationHeader[bearerPrefix.Length..].Trim();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    return token;
                }
            }
        }

        if (Request.Query.TryGetValue("access_token", out var queryTokenValues))
        {
            var queryToken = queryTokenValues.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(queryToken))
            {
                return queryToken;
            }
        }

        return null;
    }
}
