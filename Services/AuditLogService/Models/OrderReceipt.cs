namespace Store.AuditLogService.Models;

/// <summary>Business deduplication retained independently of audit retention and broker message IDs.</summary>
public sealed class OrderReceipt
{
    public int OrderId { get; set; }
}
