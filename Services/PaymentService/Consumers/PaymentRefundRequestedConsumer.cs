using MassTransit;
using Store.Contracts.Payments.V1;
using Store.PaymentService.Services;

namespace Store.PaymentService.Consumers;

/// <summary>The order saga wants the money of an order back: the payment is refunded and the shop told by webhook.</summary>
public sealed class PaymentRefundRequestedConsumer : IConsumer<PaymentRefundRequested>
{
    private readonly PaymentProcessor _payments;

    public PaymentRefundRequestedConsumer(PaymentProcessor payments)
    {
        _payments = payments;
    }

    public Task Consume(ConsumeContext<PaymentRefundRequested> context)
        => _payments.RefundAsync(context.Message.OrderId, context.CancellationToken);
}
