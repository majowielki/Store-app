using System.ComponentModel.DataAnnotations;

namespace Store.OrderService.Webhooks;

/// <summary>How the payment webhooks are checked. Section "PaymentWebhooks".</summary>
public sealed class PaymentWebhookOptions
{
    public const string SectionName = "PaymentWebhooks";

    /// <summary>Shared with the payment service, min. 32 characters: PAYMENT_WEBHOOK_SECRET in compose.</summary>
    [Required, MinLength(32)]
    public string SigningSecret { get; init; } = string.Empty;

    /// <summary>How far the signature's timestamp may be from now; an older request is taken for a replay.</summary>
    [Range(30, 3600)]
    public int ToleranceSeconds { get; init; } = 300;

    /// <summary>How long a processed event is remembered, so a repeat of it changes nothing; longer than the tolerance, after which a repeat fails the signature's time check anyway.</summary>
    [Range(1, 90)]
    public int ProcessedEventRetentionDays { get; init; } = 7;

    public TimeSpan Tolerance => TimeSpan.FromSeconds(ToleranceSeconds);

    public TimeSpan ProcessedEventRetention => TimeSpan.FromDays(ProcessedEventRetentionDays);
}
