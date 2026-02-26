using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BusinessLogicLayer.Common;
using Microsoft.AspNetCore.Mvc;

namespace GF3.WebApi.Middleware;

public sealed class ApiExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;

    public ApiExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            await WriteValidationAsync(context, ex).ConfigureAwait(false);
        }
        catch (BusinessLogicLayer.Common.ValidationException ex)
        {
            await WriteValidationAsync(context, ex).ConfigureAwait(false);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message, "not_found").ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message, "not_found").ConfigureAwait(false);
        }
        catch (Exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Server Error", "An unexpected error occurred.", "server_error").ConfigureAwait(false);
        }
    }

    private static Task WriteValidationAsync(HttpContext context, Exception ex)
    {
        var errors = BuildValidationErrors(ex);

        var payload = new
        {
            type = "validation_error",
            title = "Validation failed",
            status = StatusCodes.Status400BadRequest,
            errors
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail, string type)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions)).ConfigureAwait(false);
    }

    private static object BuildValidationErrors(Exception ex)
    {
        if (ex is ValidationException dataAnnotationsEx && dataAnnotationsEx.ValidationResult is { } result)
        {
            var members = result.MemberNames?.ToArray() ?? [];
            if (members.Length == 0)
            {
                return new[] { result.ErrorMessage ?? ex.Message };
            }

            return members.ToDictionary(x => x, _ => new[] { result.ErrorMessage ?? ex.Message });
        }

        return new[] { ex.Message };
    }
}
