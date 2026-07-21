using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using WebApi.Options;

namespace WebApi.Middleware;

/// <summary>
/// Protects admin-only database endpoints from accidental exposure.
/// By default requests must originate from the local machine, but the launcher can opt into
/// remote access for LAN scenarios by setting a dedicated configuration override.
/// </summary>
public sealed class AdminToolsGuardMiddleware
{
    private const string DatabasePathPrefix = "/api/admin/db";
    private const string NewsPathPrefix = "/api/admin/system-news";
    private const string DeveloperPasswordHeader = "X-GF3-Developer-Password";

    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<AdminToolsOptions> _options;

    public AdminToolsGuardMiddleware(RequestDelegate next, IOptionsMonitor<AdminToolsOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isDatabaseRequest = IsDatabaseRequest(context.Request.Path);
        var isNewsRequest = context.Request.Path.StartsWithSegments(NewsPathPrefix, StringComparison.OrdinalIgnoreCase);
        if (!isDatabaseRequest && !isNewsRequest)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var options = _options.CurrentValue;
        if (isDatabaseRequest && !options.Enabled)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (isDatabaseRequest && !options.AllowRemoteAccess && !IsLocalRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (!HasValidDeveloperPassword(context.Request, options.DeveloperPassword))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Developer access required",
                detail = "The developer password is required or incorrect.",
                status = StatusCodes.Status403Forbidden,
            }).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool IsDatabaseRequest(PathString requestPath)
        => requestPath.StartsWithSegments(DatabasePathPrefix, StringComparison.OrdinalIgnoreCase);

    private static bool HasValidDeveloperPassword(HttpRequest request, string configuredPassword)
    {
        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            return false;
        }

        var suppliedPassword = request.Headers[DeveloperPasswordHeader].ToString();
        if (string.IsNullOrEmpty(suppliedPassword))
        {
            return false;
        }

        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredPassword));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedPassword));
        return CryptographicOperations.FixedTimeEquals(configuredHash, suppliedHash);
    }

    private static bool IsLocalRequest(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        return remoteIp is not null && IPAddress.IsLoopback(remoteIp);
    }
}
