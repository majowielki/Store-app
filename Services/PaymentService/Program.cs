using FluentValidation;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.BuildingBlocks.OpenApi;
using Store.BuildingBlocks.Persistence;
using Store.PaymentService.Consumers;
using Store.PaymentService.Data;
using Store.PaymentService.Providers;
using Store.PaymentService.Services;
using Store.PaymentService.Webhooks;

var builder = WebApplication.CreateBuilder(args);

// Traces, metrics and logs through OTLP (see Store.BuildingBlocks.Observability)
builder.AddStoreObservability("payment");

builder.Services.AddStandardApiControllers();

// FluentValidation validators for the request models; the shared controller setup runs them before the action
builder.Services.AddValidatorsFromAssemblyContaining<PaymentDbContext>();

// Database
builder.Services.AddStoreDbContext<PaymentDbContext>(builder.Configuration);

// JWT Authentication - the customer confirms their own payments
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// POST /api/v1/payments/internal is for the order service: it presents the shared internal key
builder.Services.AddInternalApiKeyAuthentication(builder.Configuration);

// Message bus: a cancelled order cancels its payment, the order saga asks for refunds; payments
// reach the audit service as events, through the outbox
builder.Services.AddStoreMessaging<PaymentDbContext>(builder.Configuration, serviceName: "payment", bus =>
{
    bus.AddConsumer<OrderCancelledConsumer>();
    bus.AddConsumer<PaymentRefundRequestedConsumer>();
});

// The card network (ADR 011): the test cards only
builder.Services.AddSingleton<IPaymentProvider, TestCardProvider>();
builder.Services.AddScoped<PaymentProcessor>();

// Signed webhooks to the shop, written with each change and sent by the dispatcher with retries
builder.Services.AddStoreOptions<PaymentWebhookOptions>(builder.Configuration, PaymentWebhookOptions.SectionName);
builder.Services.AddScoped<PaymentWebhooks>();
builder.Services.AddSingleton<WebhookSchedule>();
builder.Services.AddHttpClient(WebhookDispatcher.HttpClientName, (services, client) =>
    client.Timeout = services.GetRequiredService<IOptions<PaymentWebhookOptions>>().Value.Timeout);
builder.Services.AddSingleton<WebhookDispatcher>();
builder.Services.AddHostedService(services => services.GetRequiredService<WebhookDispatcher>());

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetStoreConnectionString());

builder.Services.AddSwaggerWithJwt("Payment Service API");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1");
    });
}

app.UseStoreProblemDetails();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStoreHealthChecks();

// Migrations: applied here in Development, by "--migrate" in a deployment; pending ones stop the start
if (await app.PrepareDatabaseAsync<PaymentDbContext>(args))
{
    return;
}

app.Run();
