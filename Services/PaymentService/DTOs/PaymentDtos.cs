using Store.PaymentService.Models;

namespace Store.PaymentService.DTOs;

/// <summary>
/// Body of POST /api/v1/payments/{id}/confirm: the card, typed by the customer. Only the test
/// cards are taken (see <c>TestCards</c>); rules: <c>ConfirmPaymentRequestValidator</c>.
/// </summary>
public class ConfirmPaymentRequest
{
    /// <summary>Digits, spaces and dashes allowed.</summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>1-12.</summary>
    public int ExpMonth { get; set; }

    /// <summary>Four digits.</summary>
    public int ExpYear { get; set; }

    public string Cvc { get; set; } = string.Empty;

    // Never let the card reach a log through a ToString
    public override string ToString() => nameof(ConfirmPaymentRequest);
}

/// <summary>Body of POST /api/v1/payments/{id}/authenticate: the customer's answer in the 3-D Secure window.</summary>
public class AuthenticatePaymentRequest
{
    public bool Approve { get; set; }
}

/// <summary>A payment as its customer sees it.</summary>
public class PaymentResponse
{
    public Guid Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>ISO code, lowercase.</summary>
    public string Currency { get; set; } = "usd";

    /// <summary>requiresPaymentMethod (a card is needed, again after a refusal), requiresAction (3-D Secure), succeeded, cancelled, refunded.</summary>
    public PaymentStatus Status { get; set; }

    /// <summary>Brand of the card that paid, or of the one waiting for its 3-D Secure check, or of the one refused.</summary>
    public string? CardBrand { get; set; }

    public string? CardLast4 { get; set; }

    /// <summary>Why the last card was refused: card-declined, insufficient-funds or authentication-failed.</summary>
    public string? DeclineReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public static PaymentResponse From(Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = payment.Status,
        CardBrand = payment.CardBrand,
        CardLast4 = payment.CardLast4,
        DeclineReason = payment.DeclineReason,
        CreatedAt = payment.CreatedAt
    };
}
