using System.Net;
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
    private const string AdminPathPrefix = "/api/admin/db";

    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<AdminToolsOptions> _options;

    public AdminToolsGuardMiddleware(RequestDelegate next, IOptionsMonitor<AdminToolsOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsAdminToolsRequest(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var options = _options.CurrentValue;
        if (!options.Enabled)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!options.AllowRemoteAccess && !IsLocalRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool IsAdminToolsRequest(PathString requestPath)
        => requestPath.StartsWithSegments(AdminPathPrefix, StringComparison.OrdinalIgnoreCase);

    private static bool IsLocalRequest(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        return remoteIp is not null && IPAddress.IsLoopback(remoteIp);
    }
}
