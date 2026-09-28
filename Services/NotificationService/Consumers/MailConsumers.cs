using MassTransit;
using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;
using Store.NotificationService.Mail;

namespace Store.NotificationService.Consumers;

/// <summary>
/// Turns an event into its e-mail (<see cref="IMailTemplate{TEvent}"/>) and hands it over for
/// delivery. A new e-mail is a template and a one-line consumer below; each consumer keeps its own
/// queue, named after it.
/// </summary>
public abstract class MailConsumer<TEvent>(IMailTemplate<TEvent> template, IMailSender mail) : IConsumer<TEvent>
    where TEvent : class
{
    public Task Consume(ConsumeContext<TEvent> context)
        => mail.SendAsync(template.Render(context.Message), context.CancellationToken);
}

/// <summary>The order confirmation, once the payment has arrived.</summary>
public sealed class OrderPaidConsumer(IMailTemplate<OrderPaid> template, IMailSender mail) : MailConsumer<OrderPaid>(template, mail);

/// <summary>A refused card, with a link back to the payment while the order still waits.</summary>
public sealed class PaymentDeclinedConsumer(IMailTemplate<PaymentDeclined> template, IMailSender mail) : MailConsumer<PaymentDeclined>(template, mail);

/// <summary>The order has left the workshop.</summary>
public sealed class OrderShippedConsumer(IMailTemplate<OrderShipped> template, IMailSender mail) : MailConsumer<OrderShipped>(template, mail);

/// <summary>A sold-out product the visitor waited for can be bought again.</summary>
public sealed class ProductBackInStockConsumer(IMailTemplate<ProductBackInStock> template, IMailSender mail) : MailConsumer<ProductBackInStock>(template, mail);
