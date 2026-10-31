using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApi.Contracts.Auth;

namespace WebApi.Auth;

/// <summary>Bounded, process-local protection for the single-instance API deployment.</summary>
public sealed class AuthAttemptLimiter(TimeProvider clock)
{
    private readonly object gate = new();
    private readonly Dictionary<string, (int Count, DateTimeOffset ResetAt)> attempts = new();
    private const int MaximumKeys = 10_000;

    public bool TryAcquire(string key, int limit, TimeSpan window, out int retryAfterSeconds)
    {
        lock (gate)
        {
            var now = clock.GetUtcNow();
            if (!attempts.TryGetValue(key, out var entry) || entry.ResetAt <= now)
            {
                if (attempts.Count >= MaximumKeys)
                {
                    foreach (var expired in attempts.Where(item => item.Value.ResetAt <= now).Select(item => item.Key).ToArray())
                        attempts.Remove(expired);
                    if (attempts.Count >= MaximumKeys && !attempts.ContainsKey(key))
                    {
                        retryAfterSeconds = 60;
                        return false;
                    }
                }
                entry = (0, now.Add(window));
            }

            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((entry.ResetAt - now).TotalSeconds));
            if (entry.Count >= limit) return false;
            attempts[key] = (entry.Count + 1, entry.ResetAt);
            return true;
        }
    }
}

public sealed class AuthAttemptLimitAttribute : TypeFilterAttribute
{
    public AuthAttemptLimitAttribute(string operation) : base(typeof(AuthAttemptFilter))
        => Arguments = [operation];
}

public sealed class AuthAttemptFilter(AuthAttemptLimiter limiter, string operation) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var username = context.ActionArguments.Values.Select(value => value switch
        {
            LoginRequest login => login.Username,
            SendPasswordResetCodeRequest reset => reset.Username,
            CompleteForgotPasswordResetRequest reset => reset.Username,
            _ => null,
        }).FirstOrDefault(value => value is not null) ?? context.HttpContext.User.Identity?.Name ?? "";
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(username.Trim().ToUpperInvariant())));
        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var accountLimit = operation == "login" ? 10 : operation == "reset-code" ? 3 : 5;
        if (!limiter.TryAcquire($"ip:{operation}:{ip}", 100, TimeSpan.FromMinutes(1), out var retryAfter) ||
            !limiter.TryAcquire($"account:{operation}:{identity}", accountLimit, TimeSpan.FromMinutes(15), out retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many attempts",
                Detail = "Too many attempts. Please wait before trying again.",
            }) { StatusCode = StatusCodes.Status429TooManyRequests };
            return;
        }

        await next().ConfigureAwait(false);
    }
}