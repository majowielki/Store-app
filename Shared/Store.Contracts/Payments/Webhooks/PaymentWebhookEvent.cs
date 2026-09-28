namespace Store.Contracts.Payments.Webhooks;

/// <summary>
/// What the payment service posts to the order service's webhook when a payment changes, the way
/// a card provider reports to a shop: JSON in camelCase, signed in the <c>Store-Signature</c>
/// header (<c>WebhookSignature</c>). An event may arrive more than once and out of order; its
/// <paramref name="Id"/> tells a repeat from a new one.
/// </summary>
/// <param name="Id">Unique per event; the same when a delivery is retried</param>
/// <param name="Type">One of <see cref="PaymentWebhookTypes"/></param>
/// <param name="Created">When the event happened (UTC)</param>
/// <param name="Data">The payment it is about</param>
public sealed record PaymentWebhookEvent(Guid Id, string Type, DateTime Created, PaymentWebhookData Data);

/// <summary>The payment an event is about.</summary>
/// <param name="PaymentId">Id of the payment</param>
/// <param name="OrderId">The order it pays for</param>
/// <param name="Amount">Amount taken, refused or returned</param>
/// <param name="Currency">ISO code, lowercase</param>
/// <param name="CardBrand">Brand of the card (visa, mastercard), when a card was used</param>
/// <param name="CardLast4">Last four digits of the card; the full number is never sent</param>
/// <param name="FailureReason">For a failed payment: one of <see cref="V1.PaymentDeclineReasons"/></param>
public sealed record PaymentWebhookData(
    Guid PaymentId,
    int OrderId,
    decimal Amount,
    string Currency,
    string? CardBrand,
    string? CardLast4,
    string? FailureReason);

/// <summary>The kinds of payment events.</summary>
public static class PaymentWebhookTypes
{
    /// <summary>The longest type a service stores.</summary>
    public const int MaxLength = 50;

    public const string Succeeded = "payment.succeeded";
    public const string Failed = "payment.failed";
    public const string Refunded = "payment.refunded";
}
