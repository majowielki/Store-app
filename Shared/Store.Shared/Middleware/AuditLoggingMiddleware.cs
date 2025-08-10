using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Store.Shared.Models;
using Store.Shared.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Store.Shared.Middleware;

/// <summary>
/// Middleware to automatically log HTTP requests, responses, and errors to audit log
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;
        
        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            var startTime = DateTime.UtcNow;
            
            // Log request
            await LogRequestAsync(context, startTime);

            // Execute the next middleware in the pipeline
            await _next(context);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Log response
            await LogResponseAsync(context, startTime, endTime, duration);

            // Rewind and copy the buffered response back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            
            // Log error
            await LogErrorAsync(context, ex, endTime);
            
            // Re-throw the exception to let the global exception middleware handle it
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context, DateTime timestamp)
    {
        try
        {
            // Only log requests to API endpoints, skip health checks, swagger, etc.
            if (ShouldSkipLogging(context.Request.Path))
                return;

            var auditLog = new AuditLog
            {
                Action = "HTTP_REQUEST",
                EntityName = "HttpRequest",
                EntityId = context.TraceIdentifier,
                UserId = GetUserId(context),
                UserEmail = GetUserEmail(context),
                Timestamp = timestamp,
                IpAddress = GetClientIpAddress(context),
                UserAgent = GetUserAgent(context),
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    Method = context.Request.Method,
                    Path = context.Request.Path.Value,
                    QueryString = context.Request.QueryString.Value,
                    Headers = context.Request.Headers.Where(h => !ShouldSkipHeader(h.Key))
                        .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.AsEnumerable())),
                    ContentType = context.Request.ContentType,
                    ContentLength = context.Request.ContentLength,
                    Protocol = context.Request.Protocol,
                    Scheme = context.Request.Scheme,
                    Host = context.Request.Host.Value
                })
            };

            await CreateAuditLogAsync(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging HTTP request");
        }
    }

    private async Task LogResponseAsync(HttpContext context, DateTime startTime, DateTime endTime, TimeSpan duration)
    {
        try
        {
            // Only log responses to API endpoints, skip health checks, swagger, etc.
            if (ShouldSkipLogging(context.Request.Path))
                return;

            var statusCode = context.Response.StatusCode;
            var action = statusCode >= 400 ? "HTTP_ERROR_RESPONSE" : "HTTP_RESPONSE";

            var auditLog = new AuditLog
            {
                Action = action,
                EntityName = "HttpResponse",
                EntityId = context.TraceIdentifier,
                UserId = GetUserId(context),
                UserEmail = GetUserEmail(context),
                Timestamp = endTime,
                IpAddress = GetClientIpAddress(context),
                UserAgent = GetUserAgent(context),
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    Method = context.Request.Method,
                    Path = context.Request.Path.Value,
                    StatusCode = statusCode,
                    StatusText = GetStatusText(statusCode),
                    DurationMs = duration.TotalMilliseconds,
                    ResponseHeaders = context.Response.Headers.Where(h => !ShouldSkipHeader(h.Key))
                        .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.AsEnumerable())),
                    ContentType = context.Response.ContentType,
                    ContentLength = context.Response.ContentLength,
                    StartTime = startTime,
                    EndTime = endTime
                })
            };

            // Add severity for error responses
            if (statusCode >= 500)
            {
                auditLog.Action = "HTTP_SERVER_ERROR";
            }
            else if (statusCode >= 400)
            {
                auditLog.Action = "HTTP_CLIENT_ERROR";
            }

            await CreateAuditLogAsync(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging HTTP response");
        }
    }

    private async Task LogErrorAsync(HttpContext context, Exception exception, DateTime timestamp)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = "HTTP_UNHANDLED_EXCEPTION",
                EntityName = "HttpException",
                EntityId = context.TraceIdentifier,
                UserId = GetUserId(context),
                UserEmail = GetUserEmail(context),
                Timestamp = timestamp,
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
                    Headers = context.Request.Headers.Where(h => !ShouldSkipHeader(h.Key))
                        .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.AsEnumerable()))
                })
            };

            await CreateAuditLogAsync(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging HTTP exception");
        }
    }

    private async Task CreateAuditLogAsync(AuditLog auditLog)
    {
        try
        {
            // Try to use the audit log client if available (for other services)
            using var scope = _serviceProvider.CreateScope();
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
            // Final fallback - just log the audit action
            _logger.LogWarning(ex, "Failed to create audit log. Action: {Action}, Entity: {Entity}, User: {User}", 
                auditLog.Action, auditLog.EntityName, auditLog.UserEmail ?? auditLog.UserId);
        }
    }

    private static bool ShouldSkipLogging(PathString path)
    {
        var pathValue = path.Value?.ToLower() ?? "";
        
        // Skip health checks, swagger, static files, and metrics endpoints
        return pathValue.Contains("/health") ||
               pathValue.Contains("/swagger") ||
               pathValue.Contains("/metrics") ||
               pathValue.Contains("/_vs/") ||
               pathValue.Contains("/favicon.ico") ||
               pathValue.EndsWith(".js") ||
               pathValue.EndsWith(".css") ||
               pathValue.EndsWith(".png") ||
               pathValue.EndsWith(".jpg") ||
               pathValue.EndsWith(".jpeg") ||
               pathValue.EndsWith(".gif") ||
               pathValue.EndsWith(".ico");
    }

    private static bool ShouldSkipHeader(string headerName)
    {
        var lowerHeaderName = headerName.ToLower();
        
        // Skip sensitive or noisy headers
        return lowerHeaderName == "authorization" ||
               lowerHeaderName == "cookie" ||
               lowerHeaderName == "set-cookie" ||
               lowerHeaderName.Contains("password") ||
               lowerHeaderName.Contains("secret") ||
               lowerHeaderName.Contains("key");
    }

    private static string? GetUserId(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static string? GetUserEmail(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers (useful when behind proxy/load balancer)
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

    private static string GetStatusText(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            201 => "Created",
            204 => "No Content",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => "Unknown"
        };
    }
}