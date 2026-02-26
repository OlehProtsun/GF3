using System.Net;
using Microsoft.Extensions.Options;
using WebApi.Options;

namespace WebApi.Middleware;

public sealed class AdminToolsGuardMiddleware
{
    private const string AdminPathPrefix = "/api/admin/db";
    private const string HeaderName = "X-Admin-Token";

    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<AdminToolsOptions> _options;

    public AdminToolsGuardMiddleware(RequestDelegate next, IOptionsMonitor<AdminToolsOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(AdminPathPrefix, StringComparison.OrdinalIgnoreCase))
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

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is null || !IPAddress.IsLoopback(remoteIp))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var token = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(options.Token) || !string.Equals(token, options.Token, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context).ConfigureAwait(false);
    }
}
