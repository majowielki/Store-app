using MassTransit;
using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;
using Store.NotificationService.Mail;

namespace Store.NotificationService.Consumers;

/// <summary>The order confirmation, once the payment has arrived.</summary>
public sealed class OrderPaidConsumer(MailTemplates templates, IMailSender mail) : IConsumer<OrderPaid>
{
    public Task Consume(ConsumeContext<OrderPaid> context)
        => mail.SendAsync(templates.OrderPaid(context.Message), context.CancellationToken);
}

/// <summary>A refused card, with a link back to the payment while the order still waits.</summary>
public sealed class PaymentDeclinedConsumer(MailTemplates templates, IMailSender mail) : IConsumer<PaymentDeclined>
{
    public Task Consume(ConsumeContext<PaymentDeclined> context)
        => mail.SendAsync(templates.PaymentDeclined(context.Message), context.CancellationToken);
}

/// <summary>The order has left the workshop.</summary>
public sealed class OrderShippedConsumer(MailTemplates templates, IMailSender mail) : IConsumer<OrderShipped>
{
    public Task Consume(ConsumeContext<OrderShipped> context)
        => mail.SendAsync(templates.OrderShipped(context.Message), context.CancellationToken);
}

/// <summary>A sold-out product the visitor waited for can be bought again.</summary>
public sealed class ProductBackInStockConsumer(MailTemplates templates, IMailSender mail) : IConsumer<ProductBackInStock>
{
    public Task Consume(ConsumeContext<ProductBackInStock> context)
        => mail.SendAsync(templates.ProductBackInStock(context.Message), context.CancellationToken);
}
