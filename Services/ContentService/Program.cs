using FluentValidation;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.BuildingBlocks.OpenApi;
using Store.BuildingBlocks.Persistence;
using Store.BuildingBlocks.Pictures;
using Store.BuildingBlocks.Shop;
using Store.ContentService.Data;
using Store.ContentService.Services;

var builder = WebApplication.CreateBuilder(args);

// Traces, metrics and logs through OTLP (see Store.BuildingBlocks.Observability)
builder.AddStoreObservability("content");

builder.Services.AddStandardApiControllers();

// FluentValidation validators for the request models; the shared controller setup runs them before the action
builder.Services.AddValidatorsFromAssemblyContaining<ContentDbContext>();

// Database
builder.Services.AddStoreDbContext<ContentDbContext>(builder.Configuration);

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// Message bus: content changes reach the audit service as events, through the outbox
builder.Services.AddStoreMessaging<ContentDbContext>(builder.Configuration, serviceName: "content");

// Reading and editing each kind of content (makers, collections, articles, lookbooks)
builder.Services.AddScoped(typeof(ContentStore<>));

// The editorial pages in the sitemap, at their addresses in the shop
builder.Services.AddShopLinks(builder.Configuration);
// Where the demo data points its pictures
builder.Services.AddPictureLinks(builder.Configuration);
builder.Services.AddScoped<ContentSitemap>();

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetStoreConnectionString());

builder.Services.AddSwaggerWithJwt("Content Service API");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Content Service API v1");
    });
}

app.UseStoreProblemDetails();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStoreHealthChecks();

// Migrations and the demo content: applied here in Development, by "--migrate" in a deployment;
// pending migrations stop the start
if (await app.PrepareDatabaseAsync<ContentDbContext>(args,
        seed: services => ContentSeeder.SeedAsync(services.GetRequiredService<ContentDbContext>(), services.GetRequiredService<PictureLinks>(), services.GetRequiredService<TimeProvider>())))
{
    return;
}

app.Run();
