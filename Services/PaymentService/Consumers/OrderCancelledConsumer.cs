using MassTransit;
using Store.Contracts.Orders.V1;
using Store.PaymentService.Services;

namespace Store.PaymentService.Consumers;

/// <summary>A cancelled order can no longer be paid: its open payment is cancelled with it.</summary>
public sealed class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly PaymentProcessor _payments;

    public OrderCancelledConsumer(PaymentProcessor payments)
    {
        _payments = payments;
    }

    public Task Consume(ConsumeContext<OrderCancelled> context)
        => _payments.CancelForOrderAsync(context.Message.OrderId, context.CancellationToken);
}
