using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Store.Shared.Models;
using Store.Shared.Services;
using System.Security.Claims;

namespace Store.Shared.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _serviceProvider = serviceProvider;
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
            
            // Log exception to audit service
            await LogExceptionToAuditAsync(context, ex);
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task LogExceptionToAuditAsync(HttpContext context, Exception exception)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = "GLOBAL_EXCEPTION",
                EntityName = "Exception",
                EntityId = context.TraceIdentifier,
                UserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserEmail = context.User?.FindFirst(ClaimTypes.Email)?.Value,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetClientIpAddress(context),
                UserAgent = GetUserAgent(context),
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    Method = context.Request.Method,
                    Path = context.Request.Path.Value,
                    QueryString = context.Request.QueryString.Value,
                    ExceptionType = exception.GetType().Name,
                    ExceptionMessage = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message,
                    RequestHeaders = context.Request.Headers
                        .Where(h => !ShouldSkipHeader(h.Key))
                        .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.AsEnumerable()))
                })
            };

            await CreateAuditLogAsync(auditLog);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(auditEx, "Failed to log exception to audit service");
        }
    }

    private async Task CreateAuditLogAsync(AuditLog auditLog)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            
            // Try to use the audit log client if available (for other services)
            var auditLogClient = scope.ServiceProvider.GetService<IAuditLogClient>();
            if (auditLogClient != null)
            {
                await auditLogClient.CreateAuditLogAsync(auditLog);
                return;
            }

            // Fallback to structured logging
            _logger.LogInformation("AUDIT_LOG: {@AuditLog}", auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create audit log for exception");
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ErrorResponse();

        switch (exception)
        {
            case ArgumentNullException:
            case ArgumentException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid request parameters";
                response.Details = _environment.IsDevelopment() ? exception.Message : null;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access";
                response.Details = _environment.IsDevelopment() ? exception.Message : null;
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Resource not found";
                response.Details = _environment.IsDevelopment() ? exception.Message : null;
                break;

            case InvalidOperationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid operation";
                response.Details = _environment.IsDevelopment() ? exception.Message : null;
                break;

            case TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Message = "Request timeout";
                response.Details = _environment.IsDevelopment() ? exception.Message : null;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An internal server error occurred";
                response.Details = _environment.IsDevelopment() ? exception.ToString() : null;
                break;
        }

        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static string? GetUserAgent(HttpContext context)
    {
        return context.Request.Headers.UserAgent.ToString();
    }

    private static bool ShouldSkipHeader(string headerName)
    {
        var lowerHeaderName = headerName.ToLower();
        return lowerHeaderName == "authorization" ||
               lowerHeaderName == "cookie" ||
               lowerHeaderName == "set-cookie" ||
               lowerHeaderName.Contains("password") ||
               lowerHeaderName.Contains("secret") ||
               lowerHeaderName.Contains("key");
    }

    private class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string TraceId { get; set; } = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}
