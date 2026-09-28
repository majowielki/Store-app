using Store.BuildingBlocks.Serialization;
using Store.Contracts.Payments.Webhooks;
using Store.PaymentService.Data;
using Store.PaymentService.Models;
using System.Text.Json;

namespace Store.PaymentService.Webhooks;

/// <summary>
/// Writes a webhook for a change of a payment, next to the change and in its transaction: the
/// event is never lost when the shop is down, and never sent for a change that was rolled back.
/// <see cref="WebhookDispatcher"/> delivers it.
/// </summary>
public sealed class PaymentWebhooks
{
    private readonly PaymentDbContext _context;
    private readonly TimeProvider _time;

    public PaymentWebhooks(PaymentDbContext context, TimeProvider time)
    {
        _context = context;
        _time = time;
    }

    /// <summary>A webhook of <paramref name="type"/> (<see cref="PaymentWebhookTypes"/>) about <paramref name="payment"/> as it is now.</summary>
    public void Enqueue(Payment payment, string type, string? failureReason = null)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var id = Guid.NewGuid();
        var body = new PaymentWebhookEvent(id, type, now, new PaymentWebhookData(
            payment.Id, payment.OrderId, payment.Amount, payment.Currency, payment.CardBrand, payment.CardLast4, failureReason));

        _context.WebhookDeliveries.Add(new WebhookDelivery
        {
            Id = id,
            PaymentId = payment.Id,
            Type = type,
            Payload = JsonSerializer.Serialize(body, StoreJson.CamelCase),
            NextAttemptAt = now,
            CreatedAt = now
        });
    }
}
