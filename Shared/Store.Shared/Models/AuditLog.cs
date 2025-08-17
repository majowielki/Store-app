using System.ComponentModel.DataAnnotations;

namespace Store.Shared.Models;

public class AuditLog
{
    public long Id { get; set; }
    
    [Required]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    public string EntityName { get; set; } = string.Empty;
    
    public string? EntityId { get; set; }
    
    public string? UserId { get; set; }
    
    public string? UserEmail { get; set; }
    
    public string? OldValues { get; set; }
    
    public string? NewValues { get; set; }
    
    public string? Changes { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    public string? AdditionalInfo { get; set; }
    
    // New fields for richer audit
    public string? Severity { get; set; }
    
    public string? ServiceName { get; set; }
    
    public string? CorrelationId { get; set; }
    
    public string? HttpMethod { get; set; }
    
    public string? Path { get; set; }
    
    public int? StatusCode { get; set; }
    
    public string? SessionId { get; set; }
    
    public string? StackTrace { get; set; } // New for call stack
}