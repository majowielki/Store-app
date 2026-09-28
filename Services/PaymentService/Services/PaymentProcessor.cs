using Microsoft.EntityFrameworkCore;
using Npgsql;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Persistence;
using Store.Contracts.Audit;
using Store.Contracts.Payments;
using Store.Contracts.Payments.V1;
using Store.Contracts.Payments.Webhooks;
using Store.PaymentService.Data;
using Store.PaymentService.Models;
using Store.PaymentService.Providers;
using Store.PaymentService.Webhooks;

namespace Store.PaymentService.Services;

/// <summary>
/// The payments of the shop's orders. Every change of a payment locks its row, so a card being
/// confirmed and the order being cancelled at the same moment are applied one after the other;
/// every change the shop must know about writes its webhook in the same transaction. The payment
/// itself decides which moves its status allows (<see cref="Payment"/>).
/// </summary>
public sealed class PaymentProcessor
{
    private readonly PaymentDbContext _context;
    private readonly IPaymentProvider _provider;
    private readonly PaymentWebhooks _webhooks;
    private readonly IAuditTrail _auditTrail;
    private readonly TimeProvider _time;
    private readonly ILogger<PaymentProcessor> _logger;

    public PaymentProcessor(
        PaymentDbContext context,
        IPaymentProvider provider,
        PaymentWebhooks webhooks,
        IAuditTrail auditTrail,
        TimeProvider time,
        ILogger<PaymentProcessor> logger)
    {
        _context = context;
        _provider = provider;
        _webhooks = webhooks;
        _auditTrail = auditTrail;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Opens the payment of an order, or returns the one opened before (Created is false then).
    /// The same order with another customer or amount, or the key used for another order, is a
    /// <see cref="ConflictException"/>.
    /// </summary>
    public async Task<(Payment Payment, bool Created)> OpenAsync(CreatePaymentRequest request, string idempotencyKey)
    {
        var existing = await FindOpenedAsync(request, idempotencyKey);
        if (existing is not null)
        {
            return (SameRequest(existing, request, idempotencyKey), false);
        }

        var payment = Payment.Open(request, idempotencyKey, Now);
        _context.Payments.Add(payment);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The same order opened twice at once: the other request won, answer with its payment
            _context.ChangeTracker.Clear();
            var winner = await FindOpenedAsync(request, idempotencyKey)
                ?? throw new InvalidOperationException($"The payment of order {request.OrderId} was opened by another request and is gone.");
            return (SameRequest(winner, request, idempotencyKey), false);
        }

        _logger.LogInformation("Payment {PaymentId} opened for order {OrderId}", payment.Id, payment.OrderId);
        return (payment, true);
    }

    private Task<Payment?> FindOpenedAsync(CreatePaymentRequest request, string idempotencyKey)
        => _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == request.OrderId || p.IdempotencyKey == idempotencyKey);

    private static Payment SameRequest(Payment existing, CreatePaymentRequest request, string idempotencyKey)
        => existing.IsOpenedBy(request, idempotencyKey)
            ? existing
            : throw new ConflictException("The order already has a payment for another amount or customer, or the key was used for another order.");

    /// <summary>A payment of the signed-in customer.</summary>
    public async Task<Payment> GetAsync(Guid id, string userId)
        => Owned(await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id), id, userId);

    /// <summary>
    /// Charges <paramref name="card"/>: the payment succeeds, waits for the 3-D Secure check, or
    /// stays open with the reason the card was refused, so the customer can try another one.
    /// </summary>
    public async Task<Payment> ConfirmAsync(Guid id, string userId, CardDetails card)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var payment = Owned(await LockAsync(id), id, userId);
        if (payment.Status != PaymentStatus.RequiresPaymentMethod)
        {
            throw new ConflictException(payment.Status switch
            {
                PaymentStatus.Succeeded or PaymentStatus.Refunded => "This order has been paid already.",
                PaymentStatus.RequiresAction => "Finish the 3-D Secure check of the card first.",
                _ => "The order was cancelled, so it can no longer be paid."
            });
        }

        var summary = CardNumbers.Summarize(card.Number);
        var result = _provider.Charge(card, payment.Amount);
        switch (result)
        {
            case ChargeResult.Approved:
                await SucceedAsync(payment, summary, AttemptOutcome.Succeeded);
                break;
            case ChargeResult.AuthenticationRequired:
                payment.RequireAuthentication(summary, Now);
                break;
            case ChargeResult.InsufficientFunds:
                Decline(payment, summary, AttemptOutcome.InsufficientFunds, PaymentDeclineReasons.InsufficientFunds);
                break;
            case ChargeResult.Declined:
                Decline(payment, summary, AttemptOutcome.Declined, PaymentDeclineReasons.CardDeclined);
                break;
            default:
                throw new InvalidOperationException($"The card network answered {result}, which the payment service does not know.");
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return payment;
    }

    /// <summary>The customer's answer to the 3-D Secure check of the card they confirmed with.</summary>
    public async Task<Payment> AuthenticateAsync(Guid id, string userId, bool approve)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var payment = Owned(await LockAsync(id), id, userId);
        if (payment is not { Status: PaymentStatus.RequiresAction, Card: { } card })
        {
            throw new ConflictException("The payment is not waiting for a 3-D Secure check.");
        }

        if (approve)
        {
            await SucceedAsync(payment, card, AttemptOutcome.AuthenticationApproved);
        }
        else
        {
            Decline(payment, card, AttemptOutcome.AuthenticationRejected, PaymentDeclineReasons.AuthenticationFailed);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return payment;
    }

    /// <summary>
    /// The order was cancelled: a payment still waiting for a card cannot be made any more.
    /// A paid one stays paid - the order saga asks for its refund. Runs in the message's transaction.
    /// </summary>
    public async Task CancelForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var payment = await LockByOrderAsync(orderId, cancellationToken);
        if (payment is not null && payment.CancelIfOpen(Now))
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Payment {PaymentId} cancelled with order {OrderId}", payment.Id, orderId);
        }
    }

    /// <summary>
    /// Returns the money of a paid order and reports it with a webhook; a refunded payment is
    /// left as it is. Runs in the message's transaction.
    /// </summary>
    public async Task RefundAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var payment = await LockByOrderAsync(orderId, cancellationToken);
        if (payment is null || !payment.Refund(Now))
        {
            if (payment?.Status != PaymentStatus.Refunded)
            {
                _logger.LogWarning("Refund asked for order {OrderId}, whose payment is {Status}; nothing to return",
                    orderId, payment?.Status.ToString() ?? "missing");
            }

            return;
        }

        _webhooks.Enqueue(payment, PaymentWebhookTypes.Refunded);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditTrail.RecordAsync(AuditActions.PaymentRefunded, nameof(Payment), payment.Id.ToString(), payment.UserId,
            details: new { payment.OrderId, payment.Amount, payment.Currency }, cancellationToken: cancellationToken);
        _logger.LogInformation("Payment {PaymentId} of order {OrderId} refunded", payment.Id, orderId);
    }

    private async Task SucceedAsync(Payment payment, CardSummary card, AttemptOutcome outcome)
    {
        payment.Succeed(card, outcome, Now);
        _webhooks.Enqueue(payment, PaymentWebhookTypes.Succeeded);
        await _auditTrail.RecordAsync(AuditActions.PaymentSucceeded, nameof(Payment), payment.Id.ToString(), payment.UserId,
            details: new { payment.OrderId, payment.Amount, payment.Currency, cardBrand = card.Brand });
        _logger.LogInformation("Payment {PaymentId} of order {OrderId} succeeded with a {Brand} card", payment.Id, payment.OrderId, card.Brand);
    }

    /// <summary>The card is refused; the payment stays open for another one and the shop hears why.</summary>
    private void Decline(Payment payment, CardSummary card, AttemptOutcome outcome, string reason)
    {
        payment.Decline(card, outcome, reason, Now);
        _webhooks.Enqueue(payment, PaymentWebhookTypes.Failed, reason);
        _logger.LogInformation("A {Brand} card was refused for payment {PaymentId}: {Reason}", card.Brand, payment.Id, reason);
    }

    /// <summary>The payment, locked until the transaction ends.</summary>
    private async Task<Payment?> LockAsync(Guid id)
    {
        await _context.LockForUpdateAsync<Payment, Guid>(p => p.Id, id);
        return await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>The payment of an order, locked until the transaction ends.</summary>
    private async Task<Payment?> LockByOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        await _context.LockForUpdateAsync<Payment, int>(p => p.OrderId, orderId, cancellationToken);
        return await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }

    /// <summary>The payment, if it belongs to <paramref name="userId"/>; another customer's is forbidden.</summary>
    private static Payment Owned(Payment? payment, Guid id, string userId)
    {
        if (payment is null)
        {
            throw new NotFoundException(nameof(Payment), id);
        }

        return payment.UserId == userId ? payment : throw new ForbiddenException("This payment belongs to another customer.");
    }
}
