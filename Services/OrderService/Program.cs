using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Http;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.OpenApi;
using Store.BuildingBlocks.Persistence;
using Store.OrderService.Clients;
using Store.OrderService.Data;
using Store.OrderService.Models;
using Store.OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStandardApiControllers();

// FluentValidation: validators from DI, request models validated before the action runs
builder.Services.AddValidatorsFromAssemblyContaining<Store.OrderService.Validators.CreateOrderFromCartRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Database - the model and the migrations must agree; a drift is an error, not a warning to silence
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication - key, issuer and audience come from validated JwtOptions;
// the shared setup already adds the Token-Expired header on expired tokens
builder.Services.AddJwtAuthentication(builder.Configuration, options =>
{
    options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(2);
});

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// Addresses of the services this one calls; startup fails when any is missing
builder.Services.AddServiceEndpoints(builder.Configuration,
    nameof(ServiceEndpointsOptions.ProductService),
    nameof(ServiceEndpointsOptions.CartService));

// Other services, through typed clients with timeouts, retries and a circuit breaker
builder.Services.AddServiceClient<ICartClient, CartClient>(builder.Configuration, nameof(ServiceEndpointsOptions.CartService));
builder.Services.AddServiceClient<ICatalogClient, CatalogClient>(builder.Configuration, nameof(ServiceEndpointsOptions.ProductService));

// Message bus: OrderPlaced leaves through the outbox in the orders database; the audit service consumes it
builder.Services.AddStoreMessaging<OrderDbContext>(builder.Configuration, serviceName: "order");

// Services
builder.Services.AddStoreOptions<PricingOptions>(builder.Configuration, PricingOptions.SectionName);
builder.Services.AddScoped<IOrderService, Store.OrderService.Services.OrderService>();
builder.Services.AddHostedService<IdempotencyKeyCleanupService>();

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddSwaggerWithJwt("Store Order Service");
builder.Services.AddStandardCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Order Service V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapStoreHealthChecks();

// Migrations: applied here in Development, by "--migrate" in a deployment; pending ones stop the start
if (await app.PrepareDatabaseAsync<OrderDbContext>(args))
{
    return;
}

app.Run();

