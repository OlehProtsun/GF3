using System.Globalization;
using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using WebApi.Auth;
using WebApi.Infrastructure;
using WebApi.Services;

namespace WebApi.Middleware;

public sealed class RegulationAcceptanceGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        var idClaim = role == AuthRoles.Manager ? "manager_id" : "employee_id";
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
            context.User.Identity?.IsAuthenticated != true ||
            role is not AuthRoles.Manager and not AuthRoles.Employee ||
            !int.TryParse(context.User.FindFirstValue(idClaim), out var accountId) || accountId <= 0 ||
            IsExempt(context))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var service = context.RequestServices.GetRequiredService<IRegulationService>();
        var pending = await service.ListPendingAsync(
            RegulationSubjectResolver.Resolve(context.User), context.RequestAborted).ConfigureAwait(false);
        if (pending.Count == 0)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var problem = ApiProblemDetailsFactory.CreateProblem(context,
            StatusCodes.Status428PreconditionRequired, "Regulation acceptance required",
            "Accept the currently published required documents to continue.", "regulations_acceptance_required");
        problem.Extensions["code"] = "regulations_acceptance_required";
        problem.Extensions["pendingUrl"] = "/api/regulations/pending";
        context.Response.StatusCode = StatusCodes.Status428PreconditionRequired;
        await context.Response.WriteAsJsonAsync(problem, options: null,
            contentType: "application/problem+json", cancellationToken: context.RequestAborted).ConfigureAwait(false);
    }

    private static bool IsExempt(HttpContext context)
    {
        var request = context.Request;
        if (request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase)) return true;
        if (context.User.IsInRole(AuthRoles.Manager) &&
            context.User.HasClaim(AuthPolicies.SystemManagerClaim, "true") &&
            request.Path.StartsWithSegments("/api/admin/regulations", StringComparison.OrdinalIgnoreCase)) return true;

        var path = request.Path.Value ?? string.Empty;
        if (HttpMethods.IsGet(request.Method) &&
            (path.Equals("/api/health", StringComparison.OrdinalIgnoreCase) ||
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
