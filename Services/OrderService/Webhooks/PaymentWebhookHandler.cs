using MassTransit;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Store.Contracts.Payments.V1;
using Store.Contracts.Payments.Webhooks;
using Store.OrderService.Data;
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

    public TimeSpan Tolerance => TimeSpan.FromSeconds(ToleranceSeconds);
}

/// <summary>A webhook event the order service has acted on, kept so a repeat of it changes nothing.</summary>
public class ProcessedWebhookEvent
{
    public Guid EventId { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    /// <summary>How long a processed event is remembered; a repeat after that fails the signature's time check anyway.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
}

/// <summary>
/// Turns a verified payment webhook into the event the order saga follows: payment accepted,
/// declined or refunded. The event id is recorded in the same transaction as the outbox message,
/// so a webhook delivered twice is acted on once, and one that failed half-way is acted on when
/// the payment service retries it.
/// </summary>
public sealed class PaymentWebhookHandler
{
    private readonly OrderDbContext _context;
    private readonly IPublishEndpoint _publish;
    private readonly TimeProvider _time;
    private readonly ILogger<PaymentWebhookHandler> _logger;

    public PaymentWebhookHandler(OrderDbContext context, IPublishEndpoint publish, TimeProvider time, ILogger<PaymentWebhookHandler> logger)
    {
        _context = context;
        _publish = publish;
        _time = time;
        _logger = logger;
    }

    /// <summary>True when the event was acted on now, false when it had been before.</summary>
    public async Task<bool> HandleAsync(PaymentWebhookEvent webhook, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var now = _time.GetUtcNow().UtcDateTime;
        var recorded = await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProcessedWebhookEvents" ("EventId", "Type", "ReceivedAt") VALUES ({webhook.Id}, {webhook.Type}, {now})
            ON CONFLICT ("EventId") DO NOTHING
            """, cancellationToken);
        if (recorded == 0)
        {
            _logger.LogInformation("Payment webhook {EventId} ({Type}) arrived again; nothing to do", webhook.Id, webhook.Type);
            return false;
        }

        var payment = webhook.Data;
        switch (webhook.Type)
        {
            case PaymentWebhookTypes.Succeeded:
                await _publish.Publish(new PaymentAccepted(payment.PaymentId, payment.OrderId, payment.Amount,
                    payment.CardBrand ?? "card", payment.CardLast4 ?? string.Empty, webhook.Created), cancellationToken);
                break;

            case PaymentWebhookTypes.Failed:
                var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);
                if (order is null)
                {
                    _logger.LogWarning("Payment webhook {EventId} names order {OrderId}, which does not exist", webhook.Id, payment.OrderId);
                    break;
                }

                await _publish.Publish(new PaymentDeclined(payment.PaymentId, order.Id, order.UserId, order.UserEmail, order.CustomerName,
                    payment.FailureReason ?? PaymentDeclineReasons.CardDeclined, order.PaymentDueAt, webhook.Created), cancellationToken);
                break;

            case PaymentWebhookTypes.Refunded:
                await _publish.Publish(new PaymentRefunded(payment.PaymentId, payment.OrderId, payment.Amount, webhook.Created), cancellationToken);
                break;

            default:
                _logger.LogInformation("Payment webhook {EventId} of type {Type} is not one the shop follows", webhook.Id, webhook.Type);
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Payment webhook {EventId} ({Type}) for order {OrderId} processed", webhook.Id, webhook.Type, payment.OrderId);
        return true;
    }
}

/// <summary>
/// Keeps the request body readable after model binding, so the webhook can check its signature
/// against the bytes exactly as they were sent.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BufferedBodyAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context) => context.HttpContext.Request.EnableBuffering();

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
