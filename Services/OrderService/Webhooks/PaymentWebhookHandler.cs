using MassTransit;
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Persistence;
using Store.Contracts.Payments;
using Store.Contracts.Payments.V1;
using Store.Contracts.Payments.Webhooks;
using Store.OrderService.Data;
using Store.OrderService.Models;
using System.Runtime.CompilerServices;

namespace Store.OrderService.Webhooks;

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

        if (!await RecordAsync(webhook, cancellationToken))
        {
            _logger.LogInformation("Payment webhook {EventId} ({Type}) arrived again; nothing to do", webhook.Id, webhook.Type);
            return false;
        }

        var payment = webhook.Data;
        switch (webhook.Type)
        {
            case PaymentWebhookTypes.Succeeded:
                await _publish.Publish(new PaymentAccepted(payment.PaymentId, payment.OrderId, payment.Amount,
                    payment.CardBrand ?? CardBrands.Unknown, payment.CardLast4 ?? string.Empty, webhook.Created), cancellationToken);
                break;

            case PaymentWebhookTypes.Failed:
                await PublishDeclinedAsync(webhook, cancellationToken);
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

    /// <summary>
    /// Records the event as processed; false when it was already. A repeat arriving while the first
    /// delivery is still being handled waits on the key and then finds it taken.
    /// </summary>
    private async Task<bool> RecordAsync(PaymentWebhookEvent webhook, CancellationToken cancellationToken)
    {
        var events = _context.Sql<ProcessedWebhookEvent>();
        var id = events.Column(e => e.EventId);
        var inserted = await _context.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
            $"INSERT INTO {events.Table} ({id}, {events.Column(e => e.Type)}, {events.Column(e => e.ReceivedAt)}) " +
            $"VALUES ({{0}}, {{1}}, {{2}}) ON CONFLICT ({id}) DO NOTHING",
            webhook.Id, webhook.Type, _time.GetUtcNow().UtcDateTime), cancellationToken);
        return inserted == 1;
    }

    /// <summary>A refused card, with what the customer needs to try another one: who they are and until when.</summary>
    private async Task PublishDeclinedAsync(PaymentWebhookEvent webhook, CancellationToken cancellationToken)
    {
        var payment = webhook.Data;
        var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Payment webhook {EventId} names order {OrderId}, which does not exist", webhook.Id, payment.OrderId);
            return;
        }

        await _publish.Publish(new PaymentDeclined(payment.PaymentId, order.Id, order.UserId, order.UserEmail, order.CustomerName,
            payment.FailureReason ?? PaymentDeclineReasons.CardDeclined, order.PaymentDueAt, webhook.Created), cancellationToken);
    }
}
