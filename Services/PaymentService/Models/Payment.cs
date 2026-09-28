namespace Store.PaymentService.Models;

/// <summary>
/// The payment of one order, the way a card provider keeps it: opened by the shop for an amount,
/// confirmed by the customer with a card, possibly after a 3-D Secure check. Only the card's
/// brand and last four digits are ever stored.
/// </summary>
public class Payment
{
    public Guid Id { get; set; }

    /// <summary>The order it pays for; one payment per order.</summary>
    public int OrderId { get; set; }

    /// <summary>The customer; only they may confirm it.</summary>
    public string UserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    /// <summary>ISO code, lowercase ("usd").</summary>
    public string Currency { get; set; } = "usd";

    public PaymentStatus Status { get; set; } = PaymentStatus.RequiresPaymentMethod;

    /// <summary>The key the shop opened it with; the same key never opens a second one.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Brand of the card last used: the one that paid, or the one waiting for its 3-D Secure check.</summary>
    public string? CardBrand { get; set; }

    public string? CardLast4 { get; set; }

    /// <summary>Why the last card was refused (<c>PaymentDeclineReasons</c>); cleared by a card that goes through.</summary>
    public string? DeclineReason { get; set; }

    public List<PaymentAttempt> Attempts { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? SucceededAt { get; set; }

    public DateTime? RefundedAt { get; set; }
}

/// <summary>Stored by name; the API writes them in camelCase.</summary>
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

/// <summary>One card tried on a payment, and what came of it.</summary>
public class PaymentAttempt
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public string CardBrand { get; set; } = string.Empty;

    public string CardLast4 { get; set; } = string.Empty;

    public AttemptOutcome Outcome { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Stored by name.</summary>
public enum AttemptOutcome
{
    Succeeded,
    Declined,
    InsufficientFunds,
    /// <summary>The card asked for 3-D Secure; the outcome is recorded again once the customer answers.</summary>
    AuthenticationRequired,
    AuthenticationApproved,
    AuthenticationRejected
}

/// <summary>
/// One webhook to the shop: the event body, signed when it is sent, and how its delivery goes.
/// It is written in the same transaction as the change it reports, so a change is never left
/// unreported; the dispatcher sends it and retries on a schedule until the shop answers 2xx.
/// </summary>
public class WebhookDelivery
{
    /// <summary>The event id, the same in every retry, so the shop can tell a repeat.</summary>
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public string Type { get; set; } = string.Empty;

    /// <summary>The JSON body, exactly as it is signed and sent.</summary>
    public string Payload { get; set; } = string.Empty;

    public int Attempts { get; set; }

    public DateTime NextAttemptAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    /// <summary>Set when the last attempt failed and no more are made.</summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>The status code or error of the last failed attempt.</summary>
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; }
}
