using System.Security.Claims;
using DataAccessLayer.Repositories.Abstractions;
using WebApi.Realtime;
using System.Text.Encodings.Web;
using BusinessLogicLayer.Services.Abstractions;
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
    private readonly IEmployeeAccountService? _employeeAccountService;
    private readonly IManagerAccountRepository? _managerAccounts;

    public JwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<JwtAuthOptions> jwtOptions,
        IEmployeeAccountService? employeeAccountService = null,
        IManagerAccountRepository? managerAccounts = null)
        : base(options, logger, encoder)
    {
        _jwtOptions = jwtOptions.Value;
        _employeeAccountService = employeeAccountService;
        _managerAccounts = managerAccounts;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = TryResolveAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        if (!JwtTokenCodec.TryReadAccessToken(token, _jwtOptions, TimeSpan.FromMinutes(1), out var session, out var error))
        {
            return AuthenticateResult.Fail(error ?? "Invalid bearer token.");
        }

        if (session.EmployeeId is > 0 && _employeeAccountService is not null)
        {
            var account = await _employeeAccountService
                .GetByEmployeeIdAsync(session.EmployeeId.Value, Context.RequestAborted)
                .ConfigureAwait(false);
            if (account is null || account.SessionVersion != (session.SessionVersion ?? 0))
            {
                return AuthenticateResult.Fail("Employee session was revoked.");
            }
        }

        if (session.Role == AuthRoles.Manager)
        {
            var account = session.ManagerId is > 0 && _managerAccounts is not null
                ? await _managerAccounts.GetByIdAsync(session.ManagerId.Value, Context.RequestAborted).ConfigureAwait(false)
                : null;
            if (account is null || session.CredentialVersion != account.PasswordUpdatedAtUtc.UtcTicks)
            {
                return AuthenticateResult.Fail("Manager session was revoked.");
            }
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
        return AuthenticateResult.Success(ticket);
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

        if (Request.Path.StartsWithSegments(EmployeePresenceHub.RoutePattern) &&
            Request.Query.TryGetValue("access_token", out var queryTokenValues))
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
