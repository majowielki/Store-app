using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;
using Store.NotificationService.Consumers;
using Store.NotificationService.Mail;
using Store.NotificationService.Mail.Templates;

var builder = WebApplication.CreateBuilder(args);

// Traces, metrics and logs through OTLP (see Store.BuildingBlocks.Observability)
builder.AddStoreObservability("notification");

// The e-mails: to Mailpit over SMTP in development, only to the log elsewhere (the shop is a demo)
builder.Services.AddStoreOptions<MailOptions>(builder.Configuration, MailOptions.SectionName);
builder.Services.AddSingleton<ShopLinks>();
builder.Services.AddSingleton<IMailTemplate<OrderPaid>, OrderPaidMail>();
builder.Services.AddSingleton<IMailTemplate<PaymentDeclined>, PaymentDeclinedMail>();
builder.Services.AddSingleton<IMailTemplate<OrderShipped>, OrderShippedMail>();
builder.Services.AddSingleton<IMailTemplate<ProductBackInStock>, BackInStockMail>();
builder.Services.AddSingleton<IMailSender>(services =>
    services.GetRequiredService<IOptions<MailOptions>>().Value.Delivery == MailDelivery.Smtp
        ? ActivatorUtilities.CreateInstance<SmtpMailSender>(services)
        : ActivatorUtilities.CreateInstance<LogMailSender>(services));

// Message bus: the service only listens and keeps no database, so no outbox - the in-memory inbox
// drops a message redelivered to the same instance
builder.Services.AddStoreMessagingWithoutOutbox(builder.Configuration, serviceName: "notification", bus =>
{
    bus.AddConsumer<OrderPaidConsumer>();
    bus.AddConsumer<PaymentDeclinedConsumer>();
    bus.AddConsumer<OrderShippedConsumer>();
    bus.AddConsumer<ProductBackInStockConsumer>();
});

// Health checks: /health/live, /health/ready, /health (details, the broker included)
builder.Services.AddStoreHealthChecks();

var app = builder.Build();

app.MapStoreHealthChecks();

app.Run();
