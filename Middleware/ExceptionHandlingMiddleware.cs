using System.Net;
using System.Text.Json;

namespace SmartHomeIoT.Api.Middleware;

/// <summary>
/// Requirement I-03: errors must come back with the correct HTTP status code and a JSON error
/// object with a description. This is the last-resort catch-all for unhandled exceptions;
/// expected error cases (404, 409, 422, ...) are returned directly from the controllers.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var payload = new
            {
                statusCode = context.Response.StatusCode,
                message = "An unexpected error occurred.",
                detail = ex.Message,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
