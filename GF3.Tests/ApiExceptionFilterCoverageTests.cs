using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.Infrastructure;

namespace GF3.Tests;

public sealed class ApiExceptionFilterCoverageTests
{
    [Theory]
    [MemberData(nameof(ExceptionMappings))]
    public async Task ApiExceptionFilter_MapsExpectedExceptions_ToProblemDetails(
        Exception exception,
        int expectedStatus,
        string expectedType,
        string expectedTitle)
    {
        var context = CreateExceptionContext(exception);
        var filter = new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance);

        await filter.OnExceptionAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.True(context.ExceptionHandled);
        Assert.Equal(expectedStatus, result.StatusCode);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(expectedType, problem.Type);
        Assert.Equal(expectedTitle, problem.Title);
        Assert.Equal("/api/test", problem.Instance);
    }

    public static IEnumerable<object[]> ExceptionMappings()
    {
        yield return
        [
            new KeyNotFoundException("Missing resource."),
            StatusCodes.Status404NotFound,
            "not_found",
            "Not Found",
        ];
        yield return
        [
            new InvalidOperationException("Missing related resource."),
            StatusCodes.Status404NotFound,
            "not_found",
            "Not Found",
        ];
        yield return
        [
            new BadHttpRequestException("Bad employee session."),
            StatusCodes.Status400BadRequest,
            "bad_request",
            "Bad Request",
        ];
        yield return
        [
            new DbUpdateConcurrencyException("Changed."),
            StatusCodes.Status409Conflict,
            "database_conflict",
            "Conflict",
        ];
        yield return
        [
            new DbUpdateException("Rejected."),
            StatusCodes.Status409Conflict,
            "database_conflict",
            "Conflict",
        ];
        yield return
        [
            new Exception("Boom."),
            StatusCodes.Status500InternalServerError,
            "server_error",
            "Server Error",
        ];
    }

    [Fact]
    public async Task ApiExceptionFilter_LeavesExceptionUnhandled_WhenResponseAlreadyStarted()
    {
        var context = CreateExceptionContext(new Exception("Boom."));
        context.HttpContext.Features.Set<IHttpResponseFeature>(new StartedHttpResponseFeature());
        var filter = new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance);

        await filter.OnExceptionAsync(context);

        Assert.Null(context.Result);
        Assert.False(context.ExceptionHandled);
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/test";

        return new ExceptionContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            [])
        {
            Exception = exception,
        };
    }

    private sealed class StartedHttpResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = new MemoryStream();

        public bool HasStarted => true;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }
    }
}
