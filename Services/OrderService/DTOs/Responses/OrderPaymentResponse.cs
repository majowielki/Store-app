using Store.Contracts.Payments;

namespace Store.OrderService.DTOs.Responses;

/// <summary>
/// The payment of an order, for the payment page: the browser confirms it with a card straight at
/// the payment service (POST /api/v1/payments/{paymentId}/confirm) before <see cref="PaymentDueAt"/>.
/// </summary>
public class OrderPaymentResponse
{
    public int OrderId { get; set; }

    public Guid PaymentId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>ISO code, lowercase.</summary>
    public string Currency { get; set; } = Currencies.Usd;

    /// <summary>Where the payment stands at the payment service.</summary>
    public PaymentStatus Status { get; set; }

    /// <summary>When the order is cancelled if it is still unpaid.</summary>
    public DateTime PaymentDueAt { get; set; }
}
