namespace WebApi.Services;

internal static class PostCommitActions
{
    // Only use after the primary mutation has been persisted successfully.
    internal static async Task RunAsync(HttpContext context, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            context.RequestServices?.GetService<ILoggerFactory>()?
                .CreateLogger("WebApi.PostCommitActions")
                .LogError(exception, "A post-commit action failed for {Path}. The primary operation was saved. Trace: {TraceId}",
                    context.Request.Path, context.TraceIdentifier);
        }
    }
}
