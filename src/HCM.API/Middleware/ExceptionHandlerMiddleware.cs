using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace HCM.API.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions in the request pipeline
/// and returns RFC 7807 ProblemDetails responses with appropriate HTTP status codes.
/// </summary>
public class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger = logger;

    /// <summary>
    /// Invokes the middleware. Wraps the next middleware in a try-catch to handle all exceptions.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Pass control to the next middleware in the pipeline
            await _next(context);
        }
        catch (Exception exception)
        {
            // Catch any unhandled exception and convert it to a standard error response
            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// Converts exceptions to standardized ProblemDetails responses.
    /// Maps different exception types to appropriate HTTP status codes.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Determine HTTP status code and error details based on exception type
        var (statusCode, title, detail) = exception switch
        {
            // Validation errors (FluentValidation) → 400 Bad Request
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation Failed",
                string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))
            ),

            // Authentication/authorization failures → 401 Unauthorized
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                exception.Message
            ),

            // Resource not found → 404 Not Found
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "Not Found",
                exception.Message
            ),

            // All other unhandled exceptions → 500 Internal Server Error
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                "Please try again later."
            )
        };

        // Log 500 errors with full exception details for investigation
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {ExceptionType} - {Message}", exception.GetType().Name, exception.Message);

        // Build RFC 7807 ProblemDetails response
        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        // Set response status code and content type
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        // Serialize and send the error response
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
