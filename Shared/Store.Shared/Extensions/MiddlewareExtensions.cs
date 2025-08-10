using Microsoft.AspNetCore.Builder;
using Store.Shared.Middleware;

namespace Store.Shared.Extensions;

/// <summary>
/// Middleware configuration extensions
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds global exception handling middleware
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder</returns>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds audit logging middleware (should not be used in AuditLogService itself)
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder</returns>
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<AuditLoggingMiddleware>();
        return app;
    }
}