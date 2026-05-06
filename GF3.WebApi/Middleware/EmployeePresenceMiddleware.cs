using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using WebApi.Auth;

namespace WebApi.Middleware;

/// <summary>
/// Best-effort employee activity tracker used for online/offline presence in manager views.
/// Presence updates must never block or fail the main API request.
/// </summary>
public sealed class EmployeePresenceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EmployeePresenceMiddleware> _logger;

    public EmployeePresenceMiddleware(RequestDelegate next, ILogger<EmployeePresenceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IEmployeeAccountService employeeAccountService)
    {
        await _next(context).ConfigureAwait(false);

        if (!TryGetTrackedEmployeeId(context.User, out var employeeId))
        {
            return;
        }

        try
        {
            await employeeAccountService.TouchLastSeenAsync(employeeId, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Employee presence update skipped for employee {EmployeeId}.", employeeId);
        }
    }

    private static bool TryGetTrackedEmployeeId(ClaimsPrincipal user, out int employeeId)
    {
        employeeId = 0;

        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (!string.Equals(user.FindFirstValue(ClaimTypes.Role), AuthRoles.Employee, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(user.FindFirstValue("employee_id"), out employeeId) && employeeId > 0;
    }
}
