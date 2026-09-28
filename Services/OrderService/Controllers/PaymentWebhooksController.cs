using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Webhooks;
using Store.Contracts.Payments.Webhooks;
using Store.OrderService.Webhooks;
using System.Text;

namespace Store.OrderService.Controllers;

/// <summary>
/// Where the payment service reports payments. Not routed by the gateway - only the payment
/// service on the internal network reaches it - and trusted only with a valid signature.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/payments")]
[AllowAnonymous]
public class PaymentWebhooksController : ControllerBase
{
    private readonly PaymentWebhookHandler _handler;
    private readonly PaymentWebhookOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<PaymentWebhooksController> _logger;

    public PaymentWebhooksController(
        PaymentWebhookHandler handler,
        IOptions<PaymentWebhookOptions> options,
        TimeProvider time,
        ILogger<PaymentWebhooksController> logger)
    {
        _handler = handler;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    /// <summary>
    /// A payment event, signed in the Store-Signature header (HMAC-SHA256 over "t.body"). A wrong,
    /// missing or stale (more than 5 minutes) signature is 401; an event already processed is
    /// acknowledged again and changes nothing.
    /// </summary>
    [HttpPost]
    [BufferedBody]
    [RequestSizeLimit(64 * 1024)]
    [ProducesResponseType<WebhookReceipt>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WebhookReceipt>> Receive([FromBody] PaymentWebhookEvent webhook, CancellationToken cancellationToken)
    {
        // The signature covers the bytes as they were sent, not the model they were bound to
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var check = WebhookSignature.Verify(Request.Headers[WebhookSignature.HeaderName], body, _options.SigningSecret, _time.GetUtcNow(), _options.Tolerance);
        if (check != WebhookSignatureCheck.Valid)
        {
            _logger.LogWarning("Payment webhook refused: signature {Check}", check);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The webhook signature is missing, stale or wrong.");
        }

        var processed = await _handler.HandleAsync(webhook, cancellationToken);
        return new WebhookReceipt(webhook.Id, Duplicate: !processed);
    }
}

/// <summary>The acknowledgement of a webhook: its id, and whether it had been received before.</summary>
public sealed record WebhookReceipt(Guid EventId, bool Duplicate);
