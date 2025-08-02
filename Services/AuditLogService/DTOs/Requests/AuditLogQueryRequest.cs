using System.ComponentModel.DataAnnotations;

namespace Store.AuditLogService.DTOs.Requests;

public class AuditLogQueryRequest
{
    public int Page { get; set; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; set; } = 50;
    
    public string? EntityName { get; set; }
    
    public string? EntityId { get; set; }
    
    public string? UserId { get; set; }
    
    public string? Action { get; set; }
    
    public DateTime? FromDate { get; set; }
    
    public DateTime? ToDate { get; set; }
    
    public string? SortBy { get; set; } = "Timestamp";
    
    public string? SortOrder { get; set; } = "desc";
}