using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Store.Shared.Models;
using System.Net;
using System.Text.Json;

namespace Store.Shared.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ApiResponse<object> response;
        
        switch (exception)
        {
            case ArgumentException argEx:
                response = ApiResponse<object>.Error(argEx.Message, HttpStatusCode.BadRequest);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case KeyNotFoundException notFoundEx:
                response = ApiResponse<object>.Error(notFoundEx.Message, HttpStatusCode.NotFound);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case UnauthorizedAccessException unauthorizedEx:
                response = ApiResponse<object>.Error("Access denied", HttpStatusCode.Unauthorized);
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case InvalidOperationException invalidOpEx:
                response = ApiResponse<object>.Error(invalidOpEx.Message, HttpStatusCode.BadRequest);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case TimeoutException timeoutEx:
                response = ApiResponse<object>.Error("Request timeout", HttpStatusCode.RequestTimeout);
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                break;

            default:
                var message = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? exception.Message
                    : "An internal server error occurred";
                response = ApiResponse<object>.Error(message, HttpStatusCode.InternalServerError);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Extension method to register the global exception handling middleware
/// </summary>
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
