using System.Globalization;
using System.Security.Claims;
using WebApi.Auth;
using WebApi.Infrastructure;

namespace WebApi.Middleware;

public sealed class ManagerWorkspaceModeGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var mode = context.User.FindFirstValue(ManagerWorkspaceModes.ClaimType) ?? ManagerWorkspaceModes.Pc;
        if (!request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
            context.User.Identity?.IsAuthenticated != true || !context.User.IsInRole(AuthRoles.Manager) ||
            mode == ManagerWorkspaceModes.Pc || IsExempt(request) ||
            (mode == ManagerWorkspaceModes.Phone && (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var code = mode == ManagerWorkspaceModes.Phone ? "manager_phone_read_only" : "manager_workspace_mode_required";
        var problem = ApiProblemDetailsFactory.CreateProblem(context, StatusCodes.Status403Forbidden,
            "Manager workspace restricted", mode == ManagerWorkspaceModes.Phone ? "Phone workspace is read only." : "Choose a manager workspace to continue.", code);
        problem.Extensions["code"] = code;
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json",
            cancellationToken: context.RequestAborted).ConfigureAwait(false);
    }

    private static bool IsExempt(HttpRequest request)
    {
        var path = request.Path.Value ?? string.Empty;
        if (HttpMethods.IsPost(request.Method) &&
            (path.Equals("/api/auth/manager-mode", StringComparison.OrdinalIgnoreCase) ||
             path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase))) return true;
        if (HttpMethods.IsGet(request.Method) &&
            (path.Equals("/api/auth/session", StringComparison.OrdinalIgnoreCase) ||
             path.Equals("/api/regulations/pending", StringComparison.OrdinalIgnoreCase) ||
             path.Equals("/api/regulations/history/me", StringComparison.OrdinalIgnoreCase))) return true;

        var segments = path.Split('/');
        return segments.Length == 5 && segments[0].Length == 0 &&
            segments[1].Equals("api", StringComparison.OrdinalIgnoreCase) &&
            segments[2].Equals("regulations", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(segments[3], NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 &&
            ((HttpMethods.IsGet(request.Method) && segments[4].Equals("pdf", StringComparison.OrdinalIgnoreCase)) ||
             (HttpMethods.IsPost(request.Method) && segments[4].Equals("accept", StringComparison.OrdinalIgnoreCase)));
    }
}
