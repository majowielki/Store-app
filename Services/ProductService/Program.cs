using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.BuildingBlocks.OpenApi;
using Store.BuildingBlocks.Persistence;
using Store.ProductService.Data;
using Store.ProductService.Services;

var builder = WebApplication.CreateBuilder(args);

// Traces, metrics and logs through OTLP (see Store.BuildingBlocks.Observability)
builder.AddStoreObservability("catalog");

// Add standard API controllers
builder.Services.AddStandardApiControllers();

// FluentValidation validators for the request models; the shared controller setup runs them before the action
builder.Services.AddValidatorsFromAssemblyContaining<Store.ProductService.Validators.CreateProductRequestValidator>();

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// GET /api/v1/products/{id}/snapshot is for other services: they present the shared internal key
builder.Services.AddInternalApiKeyAuthentication(builder.Configuration);

// Message bus: catalogue changes reach the audit service as events, through the outbox
builder.Services.AddStoreMessaging<ProductDbContext>(builder.Configuration, serviceName: "catalog");

// Business Services
builder.Services.AddScoped<IProductService, Store.ProductService.Services.ProductService>();

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Swagger
builder.Services.AddSwaggerWithJwt("Product Service API");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service API v1");
    });
}

app.UseStoreProblemDetails();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();
app.MapStoreHealthChecks();

// Migrations and the demo catalogue: applied here in Development, by "--migrate" in a deployment;
// pending migrations stop the start
if (await app.PrepareDatabaseAsync<ProductDbContext>(args,
        seed: services => DatabaseSeeder.SeedAsync(services.GetRequiredService<ProductDbContext>())))
{
    return;
}

app.Run();
