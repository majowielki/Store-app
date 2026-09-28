using System.Globalization;

namespace Store.Contracts.Payments;

/// <summary>
/// Body of <c>POST /api/v1/payments/internal</c>, with which the order service opens the payment
/// of an order (an internal call, with the key "order-{id}" - <c>IdempotencyKeyFor</c> - in the
/// Idempotency-Key header). One payment per order: asking again returns the payment made the first time.
/// </summary>
/// <param name="OrderId">The order to be paid</param>
/// <param name="UserId">The customer; only they may pay it</param>
/// <param name="Amount">What the order costs</param>
/// <param name="Currency">ISO code, lowercase ("usd")</param>
public sealed record CreatePaymentRequest(int OrderId, string UserId, decimal Amount, string Currency)
{
    /// <summary>The key the payment of an order is opened with: the same for every call about the order, so it has one payment.</summary>
    public static string IdempotencyKeyFor(int orderId) => "order-" + orderId.ToString(CultureInfo.InvariantCulture);
}

/// <summary>What the payment service tells the order service about a payment.</summary>
/// <param name="Id">Id of the payment; the browser confirms it with a card at the payment service</param>
/// <param name="OrderId">The order it pays for</param>
/// <param name="Amount">Amount to take</param>
/// <param name="Currency">ISO code, lowercase</param>
/// <param name="Status">Where the payment stands</param>
/// <param name="CreatedAt">When it was opened (UTC)</param>
public sealed record PaymentSnapshot(Guid Id, int OrderId, decimal Amount, string Currency, PaymentStatus Status, DateTime CreatedAt);

/// <summary>Where a payment stands; stored by name, written in camelCase by the APIs.</summary>
public enum PaymentStatus
{
    /// <summary>Waiting for a card, or for another one after a refusal.</summary>
    RequiresPaymentMethod,

    /// <summary>The card needs the customer's 3-D Secure approval.</summary>
    RequiresAction,

    Succeeded,

    /// <summary>The order was cancelled before it was paid; no card is taken any more.</summary>
    Cancelled,

    Refunded
}

/// <summary>The currencies the shop charges in, as ISO codes in lower case.</summary>
public static class Currencies
{
    /// <summary>US dollars: the one currency of the shop (the UI formats every amount as dollars).</summary>
    public const string Usd = "usd";

    /// <summary>ISO 4217 codes are three letters.</summary>
    public const int CodeLength = 3;
}

/// <summary>Card brands as the payment service names them in its answers and webhooks.</summary>
public static class CardBrands
{
    /// <summary>The longest brand name a service stores.</summary>
    public const int MaxLength = 20;

    public const string Visa = "visa";
    public const string Mastercard = "mastercard";
    public const string Amex = "amex";

    /// <summary>A card of a brand the service does not tell apart, or one it was not told about.</summary>
    public const string Unknown = "card";
}

/// <summary>The last digits of a card, all the payments ever carry of its number.</summary>
public static class CardLast4
{
    public const int Length = 4;
}
