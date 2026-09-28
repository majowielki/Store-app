namespace Store.OrderService.Models;

/// <summary>A payment webhook event the order service has acted on, kept so a repeat of it changes nothing.</summary>
public class ProcessedWebhookEvent
{
    public const int TypeMaxLength = 50;

    public Guid EventId { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }
}
