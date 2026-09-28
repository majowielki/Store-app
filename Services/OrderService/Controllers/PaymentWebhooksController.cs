using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Contracts.Payments.Webhooks;
using Store.OrderService.Webhooks;

namespace Store.OrderService.Controllers;

/// <summary>
/// Where the payment service reports payments. Not routed by the gateway - only the payment
/// service on the internal network reaches it - and trusted only with a valid signature, which a
/// filter checks before the body is bound.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/payments")]
[AllowAnonymous]
public class PaymentWebhooksController : ControllerBase
{
    /// <summary>A payment event is a few hundred bytes; anything much larger is not one.</summary>
    private const int MaxBodyBytes = 64 * 1024;

    private readonly PaymentWebhookHandler _handler;

    public PaymentWebhooksController(PaymentWebhookHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// A payment event, signed in the Store-Signature header (HMAC-SHA256 over "t.body"). A wrong,
    /// missing or stale (older than PaymentWebhooks:ToleranceSeconds) signature is 401; an event
    /// already processed is acknowledged again and changes nothing.
    /// </summary>
    [HttpPost]
    [ServiceFilter<PaymentWebhookSignatureFilter>]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<WebhookReceipt>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<WebhookReceipt> Receive([FromBody] PaymentWebhookEvent webhook, CancellationToken cancellationToken)
    {
        var processed = await _handler.HandleAsync(webhook, cancellationToken);
        return new WebhookReceipt(webhook.Id, Duplicate: !processed);
    }
}

/// <summary>The acknowledgement of a webhook: its id, and whether it had been received before.</summary>
public sealed record WebhookReceipt(Guid EventId, bool Duplicate);
