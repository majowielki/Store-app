using FluentValidation;
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
using Store.ReviewService.Clients;
using Store.ReviewService.Consumers;
using Store.ReviewService.Data;
using Store.ReviewService.Services;

var builder = WebApplication.CreateBuilder(args);

// Traces, metrics and logs through OTLP (see Store.BuildingBlocks.Observability)
builder.AddStoreObservability("review");

builder.Services.AddStandardApiControllers();

// FluentValidation validators for the request models; the shared controller setup runs them before the action
builder.Services.AddValidatorsFromAssemblyContaining<ReviewDbContext>();

// Database
builder.Services.AddStoreDbContext<ReviewDbContext>(builder.Configuration);

// JWT Authentication - customers write and report reviews, administrators moderate them
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// The catalogue: the product ids of the seeded reviews, which name their products by slug
builder.Services.AddServiceEndpoints(builder.Configuration, nameof(ServiceEndpointsOptions.ProductService));
builder.Services.AddServiceClient<ICatalogClient, CatalogClient>(builder.Configuration, nameof(ServiceEndpointsOptions.ProductService));

// Message bus: paid orders make purchases that may be reviewed; ratings go to the catalogue and
// moderation to the audit service, through the outbox
builder.Services.AddStoreMessaging<ReviewDbContext>(builder.Configuration, serviceName: "review", bus =>
{
    bus.AddConsumer<OrderPaidConsumer>();
});

builder.Services.AddScoped<ReviewSummaries>();
builder.Services.AddScoped<ReviewBoard>();
builder.Services.AddScoped<ReviewModeration>();

// The seeded reviews learn their product ids; the demo accounts' reviews and reports go after a day
builder.Services.AddSingleton<SeedReviewLinker>();
builder.Services.AddHostedService(services => services.GetRequiredService<SeedReviewLinker>());
builder.Services.AddSingleton<DemoSandboxCleanup>();
builder.Services.AddHostedService(services => services.GetRequiredService<DemoSandboxCleanup>());

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetStoreConnectionString());

builder.Services.AddSwaggerWithJwt("Review Service API");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Review Service API v1");
    });
}

app.UseStoreProblemDetails();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStoreHealthChecks();

// Migrations and the seeded reviews: applied here in Development, by "--migrate" in a deployment;
// pending migrations stop the start
if (await app.PrepareDatabaseAsync<ReviewDbContext>(args,
        seed: services => ReviewSeeder.SeedAsync(services.GetRequiredService<ReviewDbContext>(), services.GetRequiredService<TimeProvider>())))
{
    return;
}

app.Run();
