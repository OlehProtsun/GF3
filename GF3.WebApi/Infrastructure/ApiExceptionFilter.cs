using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using BusinessLogicValidationException = BusinessLogicLayer.Common.ValidationException;

namespace WebApi.Infrastructure;

/// <summary>
/// MVC-level exception translator for controller actions.
/// Expected domain and validation failures are converted here before they can escape into the
/// ASP.NET Core pipeline, which keeps controller actions small and their HTTP behavior predictable.
/// </summary>
public sealed class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.HttpContext.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        if (TryHandleClientCancellation(context))
        {
            return Task.CompletedTask;
        }

        context.Result = CreateResult(context);
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }

    private bool TryHandleClientCancellation(ExceptionContext context)
    {
        if (context.Exception is not OperationCanceledException operationCanceledException)
        {
            return false;
        }

        if (!ApiProblemDetailsFactory.IsRequestCancellation(context.HttpContext, operationCanceledException))
        {
            return false;
        }

        if (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            context.HttpContext.Response.StatusCode = 499;
        }

        context.ExceptionHandled = true;
        return true;
    }

    private ObjectResult CreateResult(ExceptionContext context)
    {
        return context.Exception switch
        {
            ValidationException validationException => CreateValidationResult(context.HttpContext, validationException),
            BusinessLogicValidationException validationException => CreateValidationResult(context.HttpContext, validationException),
            KeyNotFoundException notFoundException => CreateNotFoundResult(context.HttpContext, notFoundException),
            InvalidOperationException invalidOperationException => CreateNotFoundResult(context.HttpContext, invalidOperationException),
            BadHttpRequestException badHttpRequestException => CreateBadRequestResult(context.HttpContext, badHttpRequestException),
            DbUpdateConcurrencyException dbUpdateConcurrencyException => CreateDatabaseConflictResult(
                context.HttpContext,
                dbUpdateConcurrencyException,
                "The requested change could not be completed because the underlying data changed."),
            DbUpdateException dbUpdateException => CreateDatabaseConflictResult(
                context.HttpContext,
                dbUpdateException,
                "The requested change could not be completed because related data still exists or the database rejected the operation."),
            _ => CreateServerErrorResult(context.HttpContext, context.Exception),
        };
    }

    private ObjectResult CreateValidationResult(HttpContext httpContext, Exception exception)
    {
        _logger.LogWarning(exception, "Validation error for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        return CreateResult(
            StatusCodes.Status400BadRequest,
            ApiProblemDetailsFactory.CreateValidationProblem(
                httpContext,
                ApiProblemDetailsFactory.BuildValidationErrors(exception),
                exception.Message));
    }

    private ObjectResult CreateNotFoundResult(HttpContext httpContext, Exception exception)
    {
        _logger.LogInformation(exception, "Resource not found for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        return CreateResult(
            StatusCodes.Status404NotFound,
            ApiProblemDetailsFactory.CreateProblem(
                httpContext,
                StatusCodes.Status404NotFound,
                "Not Found",
                exception.Message,
                "not_found"));
    }

    private ObjectResult CreateBadRequestResult(HttpContext httpContext, BadHttpRequestException exception)
    {
        _logger.LogWarning(exception, "Bad request for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        return CreateResult(
            StatusCodes.Status400BadRequest,
            ApiProblemDetailsFactory.CreateProblem(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                exception.Message,
                "bad_request"));
    }

    private ObjectResult CreateDatabaseConflictResult(HttpContext httpContext, Exception exception, string detail)
    {
        _logger.LogWarning(exception, "Database conflict for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        return CreateResult(
            StatusCodes.Status409Conflict,
            ApiProblemDetailsFactory.CreateProblem(
                httpContext,
                StatusCodes.Status409Conflict,
                "Conflict",
                detail,
                "database_conflict"));
    }

    private ObjectResult CreateServerErrorResult(HttpContext httpContext, Exception exception)
    {
        _logger.LogError(exception, "Unhandled API error for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        return CreateResult(
            StatusCodes.Status500InternalServerError,
            ApiProblemDetailsFactory.CreateProblem(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "Server Error",
                "An unexpected error occurred.",
                "server_error"));
    }

    private static ObjectResult CreateResult(int statusCode, object payload)
        => new(payload)
        {
            StatusCode = statusCode
        };
}
