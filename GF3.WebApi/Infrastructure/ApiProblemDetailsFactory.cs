using System.ComponentModel.DataAnnotations;
using BusinessLogicValidationException = BusinessLogicLayer.Common.ValidationException;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Infrastructure;

/// <summary>
/// Central factory for RFC7807 / ProblemDetails payloads returned by the API.
/// Keeping this logic in one place guarantees that exception middleware, filters,
/// and controllers produce a consistent error contract for the frontend.
/// </summary>
public static class ApiProblemDetailsFactory
{
    private const string GeneralErrorKey = "general";
    private const string DefaultValidationDetail = "Validation failed.";

    public static ValidationProblemDetails CreateValidationProblem(
        HttpContext context,
        IReadOnlyDictionary<string, string[]> errors,
        string? fallbackMessage = null)
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]>(errors, StringComparer.OrdinalIgnoreCase))
        {
            Type = "validation_error",
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = GetFirstValidationMessage(errors) ?? fallbackMessage ?? DefaultValidationDetail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        return problem;
    }

    public static ProblemDetails CreateProblem(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string type)
    {
        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        return problem;
    }

    /// <summary>
    /// Converts different validation exception styles into one dictionary shape that the frontend
    /// can reliably render. Business-layer validation keeps field names when available; generic or
    /// framework validation falls back to the shared "general" bucket.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> BuildValidationErrors(Exception ex)
    {
        if (ex is BusinessLogicValidationException businessLogicEx && businessLogicEx.Errors.Count > 0)
        {
            return businessLogicEx.Errors;
        }

        if (ex is ValidationException dataAnnotationsEx && dataAnnotationsEx.ValidationResult is { } result)
        {
            return BuildDataAnnotationsErrors(result, ex.Message);
        }

        return CreateGeneralErrors(ex.Message);
    }

    public static bool IsRequestCancellation(HttpContext context, OperationCanceledException exception)
    {
        if (context.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        return exception.CancellationToken.CanBeCanceled && exception.CancellationToken.IsCancellationRequested;
    }

    private static IReadOnlyDictionary<string, string[]> BuildDataAnnotationsErrors(
        ValidationResult result,
        string fallbackMessage)
    {
        var members = result.MemberNames?.ToArray() ?? [];
        if (members.Length == 0)
        {
            return CreateGeneralErrors(result.ErrorMessage ?? fallbackMessage);
        }

        return members.ToDictionary(
            member => member,
            _ => new[] { result.ErrorMessage ?? fallbackMessage },
            StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string[]> CreateGeneralErrors(string message)
        => new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [GeneralErrorKey] = [message],
        };

    private static string? GetFirstValidationMessage(IReadOnlyDictionary<string, string[]> errors)
        => errors.Values
            .SelectMany(messages => messages)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
}
