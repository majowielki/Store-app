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
using Store.CartService.Clients;
using Store.CartService.Consumers;
using Store.CartService.Data;
using Store.CartService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStandardApiControllers();

// FluentValidation: validators from DI, request models validated before the action runs
builder.Services.AddValidatorsFromAssemblyContaining<Store.CartService.Validators.AddCartItemRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Database
builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication - key, issuer and audience come from validated JwtOptions
builder.Services.AddJwtAuthentication(builder.Configuration, options =>
{
    // Clock skew this service used before the shared setup (JwtBearer default); to be unified across services later
    options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
});

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// /api/cart/internal/* is for the order service: callers present the shared internal key
builder.Services.AddInternalApiKeyAuthentication(builder.Configuration);

// Addresses of the services this one calls; startup fails when any is missing
builder.Services.AddServiceEndpoints(builder.Configuration, nameof(ServiceEndpointsOptions.ProductService));

// The catalogue, through a typed client with timeouts, retries and a circuit breaker
builder.Services.AddServiceClient<ICatalogClient, CatalogClient>(builder.Configuration, nameof(ServiceEndpointsOptions.ProductService));

// Message bus: the cart is emptied when the order service reports a placed order; cart
// changes reach the audit service as events, through the outbox
builder.Services.AddStoreMessaging<CartDbContext>(builder.Configuration, serviceName: "cart", bus => bus.AddConsumer<OrderPlacedConsumer>());

// Services
builder.Services.AddStoreOptions<CartOptions>(builder.Configuration, CartOptions.SectionName);
builder.Services.AddScoped<ICartService, CartService>();

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddSwaggerWithJwt("Store Cart Service");
builder.Services.AddStandardCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Cart Service V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapStoreHealthChecks();

// Migrations: applied here in Development, by "--migrate" in a deployment; pending ones stop the start
if (await app.PrepareDatabaseAsync<CartDbContext>(args))
{
    return;
}

app.Run();

