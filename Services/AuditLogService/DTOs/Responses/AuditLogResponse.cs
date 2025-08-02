namespace Store.AuditLogService.DTOs.Responses;

public class AuditLogResponse
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Changes { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalInfo { get; set; }
}