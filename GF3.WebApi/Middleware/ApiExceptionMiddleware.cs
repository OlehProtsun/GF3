using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure;
using BusinessLogicValidationException = BusinessLogicLayer.Common.ValidationException;

namespace WebApi.Middleware;

/// <summary>
/// Last-chance exception translator for API requests.
/// MVC filters already convert most expected exceptions, but this middleware still protects
/// non-MVC paths and ensures every unhandled backend failure becomes a structured problem response.
/// </summary>
public sealed class ApiExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ApiProblemDetailsFactory.IsRequestCancellation(context, ex))
        {
            HandleClientCancellation(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex, ApiProblemDetailsFactory.BuildValidationErrors(ex)).ConfigureAwait(false);
        }
        catch (BusinessLogicValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex, ApiProblemDetailsFactory.BuildValidationErrors(ex)).ConfigureAwait(false);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleNotFoundExceptionAsync(context, ex).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            await HandleNotFoundExceptionAsync(context, ex).ConfigureAwait(false);
        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogWarning(ex, "Bad request for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(
                context,
                ApiProblemDetailsFactory.CreateProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    ex.Message,
                    "bad_request")).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Database concurrency conflict for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(
                context,
                CreateDatabaseConflictProblem(
                    context,
                    "The requested change could not be completed because the underlying data changed.")).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database update conflict for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(
                context,
                CreateDatabaseConflictProblem(
                    context,
                    "The requested change could not be completed because related data still exists or the database rejected the operation.")).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled API error for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(
                context,
                ApiProblemDetailsFactory.CreateProblem(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Server Error",
                    "An unexpected error occurred.",
                    "server_error")).ConfigureAwait(false);
        }
    }

    private void HandleClientCancellation(HttpContext context)
    {
        if (!context.Response.HasStarted && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
        }
    }

    private async Task HandleValidationExceptionAsync(
        HttpContext context,
        Exception exception,
        IReadOnlyDictionary<string, string[]> errors)
    {
        _logger.LogWarning(exception, "Validation error for {Method} {Path}", context.Request.Method, context.Request.Path);
        await WriteProblemAsync(
            context,
            ApiProblemDetailsFactory.CreateValidationProblem(
                context,
                errors,
                exception.Message)).ConfigureAwait(false);
    }

    private async Task HandleNotFoundExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogInformation(exception, "Resource not found for {Method} {Path}", context.Request.Method, context.Request.Path);
        await WriteProblemAsync(
            context,
            ApiProblemDetailsFactory.CreateProblem(
                context,
                StatusCodes.Status404NotFound,
                "Not Found",
                exception.Message,
                "not_found")).ConfigureAwait(false);
    }

    private static ProblemDetails CreateDatabaseConflictProblem(HttpContext context, string detail)
        => ApiProblemDetailsFactory.CreateProblem(
            context,
            StatusCodes.Status409Conflict,
            "Conflict",
            detail,
            "database_conflict");

    private static Task WriteProblemAsync(HttpContext context, ProblemDetails problem)
    {
        if (context.RequestAborted.IsCancellationRequested || context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
