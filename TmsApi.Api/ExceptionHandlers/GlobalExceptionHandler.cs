using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Common; // M7 Session 4 - Exercise 7: BadRequestException handling

namespace TmsApi.ExceptionHandlers;

/// <summary>
/// Global exception handler that translates exceptions into RFC 7807 ProblemDetails responses.
/// - ValidationException Gтк 400 Bad Request with field-level errors
/// - All other exceptions Gтк 500 Internal Server Error with trace ID (no stack leak)
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title, detail, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more fields are invalid. See errors for details.",
                (IDictionary<string, string[]>?)ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            // M7 Session 4 - Exercise 7: map BadRequestException (unknown ?fields=) to HTTP 400
            BadRequestException bre => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                bre.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                $"An unexpected error occurred. Trace ID: {httpContext.TraceIdentifier}",
                null)
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception (trace={TraceId})", httpContext.TraceIdentifier);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (errors is not null) problem.Extensions["errors"] = errors;

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, ct);

        return true;
    }
}
