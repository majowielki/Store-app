using Store.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Store.Shared.Services;

/// <summary>
/// Client interface for communicating with the Audit Log Service
/// This interface can be implemented by different services to send audit logs
/// </summary>
public interface IAuditLogClient
{
    /// <summary>
    /// Creates an audit log entry via the Audit Log Service
    /// </summary>
    /// <param name="auditLog">The audit log entry to create</param>
    /// <returns>The ID of the created audit log entry</returns>
    Task<long?> CreateAuditLogAsync(AuditLog auditLog);
    
    /// <summary>
    /// Creates an audit log entry locally if the service is unavailable
    /// </summary>
    /// <param name="auditLog">The audit log entry to create locally</param>
    Task CreateLocalAuditLogAsync(AuditLog auditLog);
}

/// <summary>
/// Generic audit log client that can be used by any service
/// </summary>
public class AuditLogClient : IAuditLogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuditLogClient> _logger;

    public AuditLogClient(HttpClient httpClient, ILogger<AuditLogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<long?> CreateAuditLogAsync(AuditLog auditLog)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(auditLog, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            // Use internal endpoint that doesn't require authentication
            var response = await _httpClient.PostAsync("/api/auditlog/internal", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                if (long.TryParse(responseContent, out long auditLogId))
                {
                    return auditLogId;
                }
            }
            else
            {
                _logger.LogWarning("Failed to create audit log via service. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                
                // Fallback to local logging
                await CreateLocalAuditLogAsync(auditLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log via service. Falling back to local logging.");
            // Fallback to local logging
            await CreateLocalAuditLogAsync(auditLog);
        }

        return null;
    }

    public async Task CreateLocalAuditLogAsync(AuditLog auditLog)
    {
        try
        {
            // Log audit information locally as structured log
            _logger.LogInformation("AUDIT_LOG: {@AuditLog}", new
            {
                auditLog.Action,
                auditLog.EntityName,
                auditLog.EntityId,
                auditLog.UserId,
                auditLog.UserEmail,
                auditLog.Timestamp,
                auditLog.IpAddress,
                auditLog.UserAgent,
                auditLog.AdditionalInfo,
                auditLog.OldValues,
                auditLog.NewValues,
                auditLog.Changes
            });
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create local audit log");
        }
    }
}