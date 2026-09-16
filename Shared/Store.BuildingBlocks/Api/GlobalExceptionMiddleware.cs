using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Store.BuildingBlocks.Serialization;
using System.Net;
using System.Text.Json;

namespace Store.BuildingBlocks.Api;

/// <summary>
/// Turns an unhandled exception into a JSON error response with the right status code. The
/// exception goes to the application log only - it is a technical event, not a business one,
/// so it does not end up in the audit database.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started, the error response cannot be written");
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            NotImplementedException => (int)HttpStatusCode.NotImplemented,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var response = new
        {
            statusCode = context.Response.StatusCode,
            message = GetUserFriendlyMessage(context.Response.StatusCode),
            // Exception details help only in development; never expose them in production
            details = _environment.IsDevelopment() ? exception.ToString() : null,
            timestamp = DateTime.UtcNow,
            traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, StoreJson.CamelCase));
    }

    private static string GetUserFriendlyMessage(int statusCode) => statusCode switch
    {
        400 => "Invalid request parameters",
        401 => "Unauthorized access",
        404 => "Resource not found",
        408 => "Request timeout",
        501 => "Feature not implemented",
        _ => "An internal server error occurred"
    };
}

public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>Adds the store-wide exception handler; register it first in the pipeline.</summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
