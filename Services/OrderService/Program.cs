using FluentValidation;
using MassTransit;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Http;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.BuildingBlocks.OpenApi;
using Store.BuildingBlocks.Persistence;
using Store.OrderService.Clients;
using Store.OrderService.Data;
using Store.OrderService.Models;
using Store.OrderService.Saga;
using Store.OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// Traces, metrics and logs through OTLP (see Store.BuildingBlocks.Observability)
builder.AddStoreObservability("order");

builder.Services.AddStandardApiControllers();

// FluentValidation validators for the request models; the shared controller setup runs them before the action
builder.Services.AddValidatorsFromAssemblyContaining<Store.OrderService.Validators.CreateOrderFromCartRequestValidator>();

// Database - the model and the migrations must agree; a drift is an error, not a warning to silence
builder.Services.AddStoreDbContext<OrderDbContext>(builder.Configuration);

// JWT Authentication - key, issuer, audience and the validation rules come from the shared setup
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// Addresses of the services this one calls; startup fails when any is missing
builder.Services.AddServiceEndpoints(builder.Configuration,
    nameof(ServiceEndpointsOptions.ProductService),
    nameof(ServiceEndpointsOptions.CartService));

// Other services, through typed clients with timeouts, retries and a circuit breaker
builder.Services.AddServiceClient<ICartClient, CartClient>(builder.Configuration, nameof(ServiceEndpointsOptions.CartService));
builder.Services.AddServiceClient<ICatalogClient, CatalogClient>(builder.Configuration, nameof(ServiceEndpointsOptions.ProductService));

// Message bus: the order events leave through the outbox in the orders database. The order saga
// (ADR 013) keeps its rows next to the orders and is locked per order while it handles a message.
builder.Services.AddStoreMessaging<OrderDbContext>(builder.Configuration, serviceName: "order", bus =>
{
    bus.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(repository =>
        {
            repository.ExistingDbContext<OrderDbContext>();
            repository.UsePostgres();
            repository.ConcurrencyMode = ConcurrencyMode.Pessimistic;
        });
});
builder.Services.AddStoreOptions<OrderSagaOptions>(builder.Configuration, OrderSagaOptions.SectionName);
builder.Services.AddScoped<OrderStatusWriter>();
builder.Services.AddScoped<OrderSagaActions>();
builder.Services.AddSingleton<PaymentDeadlineService>();
builder.Services.AddHostedService(services => services.GetRequiredService<PaymentDeadlineService>());

// Services
builder.Services.AddStoreOptions<PricingOptions>(builder.Configuration, PricingOptions.SectionName);
builder.Services.AddStoreOptions<DeliveryOptions>(builder.Configuration, DeliveryOptions.SectionName);
builder.Services.AddSingleton<DeliveryEstimator>();
builder.Services.AddScoped<IOrderService, Store.OrderService.Services.OrderService>();
builder.Services.AddScoped<DiscountCodeService>();
builder.Services.AddHostedService<IdempotencyKeyCleanupService>();

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetStoreConnectionString());

builder.Services.AddSwaggerWithJwt("Store Order Service");
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseStoreProblemDetails();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Order Service V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapStoreHealthChecks();

// Migrations and the demo discount codes: applied here in Development, by "--migrate" in a deployment;
// pending migrations stop the start
if (await app.PrepareDatabaseAsync<OrderDbContext>(args,
        seed: services => DiscountCodeSeeder.SeedAsync(services.GetRequiredService<OrderDbContext>(), services.GetRequiredService<TimeProvider>())))
{
    return;
}

app.Run();

