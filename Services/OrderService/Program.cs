using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Http;
using Store.BuildingBlocks.OpenApi;
using Store.OrderService.Clients;
using Store.OrderService.Data;
using Store.OrderService.Models;
using Store.OrderService.Services;
using Store.Shared.Extensions;
using Store.Shared.MessageBus;
using Store.Shared.Middleware;

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
    nameof(ServiceEndpointsOptions.IdentityService),
    nameof(ServiceEndpointsOptions.ProductService),
    nameof(ServiceEndpointsOptions.CartService),
    nameof(ServiceEndpointsOptions.AuditLogService));

// Audit entries go to AuditLogService (address from Services:AuditLogService, validated at startup)
builder.Services.AddAuditLogClient(builder.Configuration);

// Other services, through typed clients with timeouts, retries and a circuit breaker
builder.Services.AddServiceClient<ICartClient, CartClient>(builder.Configuration, nameof(ServiceEndpointsOptions.CartService));
builder.Services.AddServiceClient<ICatalogClient, CatalogClient>(builder.Configuration, nameof(ServiceEndpointsOptions.ProductService));
builder.Services.AddServiceClient<IIdentityClient, IdentityClient>(builder.Configuration, nameof(ServiceEndpointsOptions.IdentityService));
builder.Services.AddHttpContextAccessor();

// Message Bus - Make it optional to prevent startup failures
try
{
    builder.Services.AddRabbitMQ(builder.Configuration);
    builder.Services.AddMessageBusSubscriptions();
}
catch (Exception ex)
{
    var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
    logger.LogWarning(ex, "Message bus setup failed, continuing without message bus");
}

// Services
builder.Services.AddStoreOptions<PricingOptions>(builder.Configuration, PricingOptions.SectionName);
builder.Services.AddScoped<IOrderService, Store.OrderService.Services.OrderService>();

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddSwaggerWithJwt("Store Order Service");
builder.Services.AddStandardCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuditLogging();
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

// Database migration - Make this optional to prevent startup failures
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Check if database exists and is accessible
            if (context.Database.CanConnect())
            {
                context.Database.Migrate();
                logger.LogInformation("Database migration completed successfully.");
            }
            else
            {
                logger.LogWarning("Database connection failed. Skipping migration.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database. Continuing without database setup.");
        }
    }
}
catch (Exception ex)
{
    // Log the error but don't stop the application
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to initialize database. Application will continue without database setup.");
}

app.Run();
