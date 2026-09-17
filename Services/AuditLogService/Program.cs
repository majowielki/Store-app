using Microsoft.EntityFrameworkCore;
using Store.AuditLogService.Consumers;
using Store.AuditLogService.Data;
using Store.AuditLogService.Services;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.BuildingBlocks.OpenApi;
using Store.BuildingBlocks.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Traces, metrics and logs through OTLP (see Store.BuildingBlocks.Observability)
builder.AddStoreObservability("audit");

builder.Services.AddStandardApiControllers();

// Database
builder.Services.AddDbContext<AuditLogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication - key, issuer, audience and the validation rules come from the shared setup
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// Services
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Message bus: every audit entry arrives as an event from the service that performed the action
builder.Services.AddStoreMessaging<AuditLogDbContext>(builder.Configuration, serviceName: "audit", bus =>
{
    bus.AddConsumer<AuditEventConsumer>();
    bus.AddConsumer<OrderPlacedConsumer>();
});

// Entries older than AuditRetention:RetentionDays are deleted once a day
builder.Services.AddStoreOptions<AuditRetentionOptions>(builder.Configuration, AuditRetentionOptions.SectionName);
builder.Services.AddHostedService<AuditRetentionService>();

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddSwaggerWithJwt("Store AuditLog Service");
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseStoreProblemDetails();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store AuditLog Service V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapStoreHealthChecks();

// Migrations: applied here in Development, by "--migrate" in a deployment; pending ones stop the start
if (await app.PrepareDatabaseAsync<AuditLogDbContext>(args))
{
    return;
}

app.Run();

