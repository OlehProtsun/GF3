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
    private readonly Dictionary<string, (int Count, DateTimeOffset ResetAt)> loginFailures = new();
    private const int MaximumKeys = 10_000;

    internal bool IsLoginLocked(string hashedAccountKey, out int retryAfterSeconds)
    {
        lock (gate)
        {
            var now = clock.GetUtcNow();
            if (loginFailures.TryGetValue(hashedAccountKey, out var entry) && entry.ResetAt > now)
            {
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((entry.ResetAt - now).TotalSeconds));
                return entry.Count >= 5;
            }
            loginFailures.Remove(hashedAccountKey);
            if (loginFailures.Count >= MaximumKeys)
            {
                foreach (var expired in loginFailures.Where(item => item.Value.ResetAt <= now).Select(item => item.Key).ToArray())
                    loginFailures.Remove(expired);
                if (loginFailures.Count >= MaximumKeys)
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((loginFailures.Min(item => item.Value.ResetAt) - now).TotalSeconds));
                    return true;
                }
            }
            // Reserve capacity before credential verification so parallel new accounts fail closed.
            loginFailures[hashedAccountKey] = (0, now.AddMinutes(15));
            retryAfterSeconds = 0;
            return false;
        }
    }

    internal void RecordLoginFailure(string hashedAccountKey)
    {
        lock (gate)
        {
            var now = clock.GetUtcNow();
            if (!loginFailures.TryGetValue(hashedAccountKey, out var entry) || entry.ResetAt <= now || entry.Count == 0)
            {
                if (!loginFailures.ContainsKey(hashedAccountKey) && loginFailures.Count >= MaximumKeys)
                {
                    foreach (var expired in loginFailures.Where(item => item.Value.ResetAt <= now).Select(item => item.Key).ToArray())
                        loginFailures.Remove(expired);
                    if (loginFailures.Count >= MaximumKeys) return;
                }
                entry = (0, now.AddMinutes(15));
            }
            loginFailures[hashedAccountKey] = (Math.Min(5, entry.Count + 1), entry.ResetAt);
        }
    }

    internal void ClearLoginFailures(string hashedAccountKey)
    {
        lock (gate) loginFailures.Remove(hashedAccountKey);
    }

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
            !limiter.TryAcquire($"account:{operation}:{identity}", accountLimit, TimeSpan.FromMinutes(15), out retryAfter) ||
            (operation == "login" && limiter.IsLoginLocked(identity, out retryAfter)))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many attempts",
                Detail = "Too many attempts. Please wait before trying again.",
            }) { StatusCode = StatusCodes.Status429TooManyRequests };
            return;
        }

        var executed = await next().ConfigureAwait(false);
        if (operation == "login" && context.Controller is WebApi.Controllers.AuthController &&
            executed.Exception is null && !executed.Canceled)
        {
            if (executed.Result is UnauthorizedObjectResult) limiter.RecordLoginFailure(identity);
            else if (executed.Result is OkObjectResult) limiter.ClearLoginFailures(identity);
        }
    }
}
