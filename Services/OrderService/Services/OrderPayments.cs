using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.Contracts.Payments;
using Store.OrderService.Clients;
using Store.OrderService.Data;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;

namespace Store.OrderService.Services;

/// <summary>
/// The payment of a customer's order, opened at the payment service. How the payment ends is not
/// asked for here: it arrives as a signed webhook (<c>PaymentWebhookHandler</c>) and moves the
/// order through its saga.
/// </summary>
public interface IOrderPayments
{
    /// <summary>
    /// Opens (or finds) the payment of a customer's order waiting for it. An order still reserving
    /// its stock, paid, cancelled or past its deadline is a <see cref="ConflictException"/>.
    /// </summary>
    Task<OrderPaymentResponse> StartAsync(int orderId, string userId);
}

public sealed class OrderPayments : IOrderPayments
{
    private readonly OrderDbContext _context;
    private readonly IPaymentClient _payments;
    private readonly TimeProvider _time;

    public OrderPayments(OrderDbContext context, IPaymentClient payments, TimeProvider time)
    {
        _context = context;
        _payments = payments;
        _time = time;
    }

    /// <summary>
    /// Opens the payment with the order's own idempotency key (<see cref="CreatePaymentRequest.IdempotencyKeyFor"/>),
    /// so every call for the same order - a second click, a reload of the page - gets the same
    /// payment. The browser then pays it at the payment service.
    /// </summary>
    public async Task<OrderPaymentResponse> StartAsync(int orderId, string userId)
    {
        var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException(nameof(Order), orderId);
        if (order.UserId != userId)
        {
            throw new ForbiddenException("This order belongs to another customer");
        }

        var due = EnsurePayable(order, _time.GetUtcNow().UtcDateTime);
        var payment = await _payments.OpenAsync(
            new CreatePaymentRequest(order.Id, order.UserId, order.Total, Currencies.Usd),
            CreatePaymentRequest.IdempotencyKeyFor(order.Id));
        if (order.PaymentId != payment.Id)
        {
            await _context.Orders.Where(o => o.Id == order.Id && o.PaymentId == null)
                .ExecuteUpdateAsync(o => o.SetProperty(x => x.PaymentId, payment.Id));
        }

        return new OrderPaymentResponse
        {
            OrderId = order.Id,
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status,
            PaymentDueAt = due
        };
    }

    /// <summary>The deadline of an order that may be paid now; why it may not, as a conflict, otherwise.</summary>
    private static DateTime EnsurePayable(Order order, DateTime now)
    {
        var refusal = order.Status switch
        {
            OrderStatus.Placed => "We are still reserving the pieces of this order - try again in a moment.",
            OrderStatus.Paid or OrderStatus.Shipped => "This order has been paid already.",
            OrderStatus.Cancelled or OrderStatus.Refunded => "This order was cancelled, so it can no longer be paid.",
            _ when order.PaymentDueAt is not { } due || due <= now => "The time to pay this order has run out.",
            _ => null
        };

        return refusal is null ? order.PaymentDueAt!.Value : throw new ConflictException(refusal);
    }
}
