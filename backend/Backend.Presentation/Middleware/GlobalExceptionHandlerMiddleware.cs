using System.Net;
using System.Text.Json;

namespace Backend.Presentation.Middleware;

/// <summary>
/// Global exception handler middleware that centralizes error handling across the application.
/// Demonstrates the Single Responsibility Principle by separating error handling from business logic.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                exception.Message
            ),
            InvalidOperationException ex when ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) => (
                HttpStatusCode.NotFound,
                ex.Message
            ),
            InvalidOperationException ex => (
                HttpStatusCode.BadRequest,
                ex.Message
            ),
            ArgumentException ex => (
                HttpStatusCode.BadRequest,
                ex.Message
            ),
            OperationCanceledException => (
                HttpStatusCode.OK, // Request cancelled by client, not an error
                "Request was cancelled"
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later."
            )
        };

        // Log based on severity
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else if (statusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Authentication failure: {Message}", exception.Message);
        }
        else if (statusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("Authorization failure: {Message}", exception.Message);
        }
        else if (exception is not OperationCanceledException)
        {
            _logger.LogInformation("Client error ({StatusCode}): {Message}", statusCode, exception.Message);
        }

        // If response has already started (e.g., SSE stream), we can't modify headers
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Cannot handle exception, response has already started");
            return;
        }

        // Only send detailed error messages in development
        var isDevelopment = context.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment();

        var errorResponse = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Details = isDevelopment ? exception.ToString() : null
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }

    /// <summary>
    /// Standard error response format for consistent API responses
    /// </summary>
    private class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
