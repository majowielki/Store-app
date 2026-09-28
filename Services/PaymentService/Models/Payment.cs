using Store.Contracts.Payments;

namespace Store.PaymentService.Models;

/// <summary>What the payment keeps of a card: its brand and last four digits, never the number.</summary>
public sealed record CardSummary(string Brand, string Last4);

/// <summary>
/// The payment of one order, the way a card provider keeps it: opened by the shop for an amount,
/// confirmed by the customer with a card, possibly after a 3-D Secure check. Only the card's
/// brand and last four digits are ever stored. It moves only through its own methods, each of
/// which checks the status it moves from.
/// </summary>
public class Payment
{
    public Guid Id { get; private set; }

    /// <summary>The order it pays for; one payment per order.</summary>
    public int OrderId { get; private set; }

    /// <summary>The customer; only they may confirm it.</summary>
    public string UserId { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    /// <summary>ISO code, lowercase (<see cref="Currencies"/>).</summary>
    public string Currency { get; private set; } = Currencies.Usd;

    public PaymentStatus Status { get; private set; } = PaymentStatus.RequiresPaymentMethod;

    /// <summary>The key the shop opened it with; the same key never opens a second one.</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>Brand of the card last used: the one that paid, or the one waiting for its 3-D Secure check.</summary>
    public string? CardBrand { get; private set; }

    public string? CardLast4 { get; private set; }

    /// <summary>Why the last card was refused (<c>PaymentDeclineReasons</c>); cleared by a card that goes through.</summary>
    public string? DeclineReason { get; private set; }

    public List<PaymentAttempt> Attempts { get; private set; } = new();

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? SucceededAt { get; private set; }

    public DateTime? RefundedAt { get; private set; }

    /// <summary>The card waiting for its 3-D Secure answer, or the last one used.</summary>
    public CardSummary? Card => CardBrand is null || CardLast4 is null ? null : new CardSummary(CardBrand, CardLast4);

    /// <summary>A payment waiting for its first card.</summary>
    public static Payment Open(CreatePaymentRequest request, string idempotencyKey, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = request.OrderId,
        UserId = request.UserId,
        Amount = request.Amount,
        Currency = request.Currency,
        IdempotencyKey = idempotencyKey,
        CreatedAt = now,
        UpdatedAt = now
    };

    /// <summary>Whether <paramref name="request"/> with <paramref name="idempotencyKey"/> asks for this very payment again.</summary>
    public bool IsOpenedBy(CreatePaymentRequest request, string idempotencyKey)
        => OrderId == request.OrderId && IdempotencyKey == idempotencyKey && UserId == request.UserId
           && Amount == request.Amount && Currency == request.Currency;

    /// <summary>The card went through, straight away or after the 3-D Secure check.</summary>
    public void Succeed(CardSummary card, AttemptOutcome outcome, DateTime now)
    {
        EnsureIn(PaymentStatus.RequiresPaymentMethod, PaymentStatus.RequiresAction);
        Status = PaymentStatus.Succeeded;
        DeclineReason = null;
        SucceededAt = now;
        Record(card, outcome, now);
    }

    /// <summary>The bank wants the customer's 3-D Secure approval before it takes the card.</summary>
    public void RequireAuthentication(CardSummary card, DateTime now)
    {
        EnsureIn(PaymentStatus.RequiresPaymentMethod);
        Status = PaymentStatus.RequiresAction;
        DeclineReason = null;
        Record(card, AttemptOutcome.AuthenticationRequired, now);
    }

    /// <summary>The card is refused; the payment stays open for another one.</summary>
    public void Decline(CardSummary card, AttemptOutcome outcome, string reason, DateTime now)
    {
        EnsureIn(PaymentStatus.RequiresPaymentMethod, PaymentStatus.RequiresAction);
        Status = PaymentStatus.RequiresPaymentMethod;
        DeclineReason = reason;
        Record(card, outcome, now);
    }

    /// <summary>The order was cancelled: a payment still waiting for a card can no longer be made. False when it is past that.</summary>
    public bool CancelIfOpen(DateTime now)
    {
        if (Status is not (PaymentStatus.RequiresPaymentMethod or PaymentStatus.RequiresAction))
        {
            return false;
        }

        Status = PaymentStatus.Cancelled;
        UpdatedAt = now;
        return true;
    }

    /// <summary>The money of a paid payment goes back. False for a payment that took none, or gave it back already.</summary>
    public bool Refund(DateTime now)
    {
        if (Status != PaymentStatus.Succeeded)
        {
            return false;
        }

        Status = PaymentStatus.Refunded;
        RefundedAt = now;
        UpdatedAt = now;
        return true;
    }

    private void Record(CardSummary card, AttemptOutcome outcome, DateTime now)
    {
        CardBrand = card.Brand;
        CardLast4 = card.Last4;
        UpdatedAt = now;
        // No id of its own: a new attempt found through the navigation is inserted only while its key is unset
        Attempts.Add(new PaymentAttempt
        {
            CardBrand = card.Brand,
            CardLast4 = card.Last4,
            Outcome = outcome,
            CreatedAt = now
        });
    }

    private void EnsureIn(params PaymentStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new InvalidOperationException($"Payment {Id} is {Status}; it cannot take a card now.");
        }
    }
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
    /// <summary>Longest error kept of a failed attempt (the column's length).</summary>
    public const int LastErrorMaxLength = 500;

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
