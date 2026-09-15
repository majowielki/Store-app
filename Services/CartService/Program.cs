using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using StackExchange.Redis;
using Store.CartService.Data;
using Store.CartService.Services;
using Store.Shared.Authorization;
using Store.Shared.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// FluentValidation: validators from DI, request models validated before the action runs (MAJ-17)
builder.Services.AddValidatorsFromAssemblyContaining<Store.CartService.Validators.AddCartItemRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Database
builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis - Make Redis optional to prevent startup failures
try
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
    {
        var connectionString = builder.Configuration.GetConnectionString("Redis")
            ?? builder.Configuration["Redis:ConnectionString"];
        return ConnectionMultiplexer.Connect(connectionString!);
    });
}
catch (Exception ex)
{
    // Log Redis connection failure but don't stop the app
    var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
    logger.LogWarning(ex, "Redis connection failed, continuing without Redis");
}

// JWT Authentication - key, issuer and audience come from validated JwtOptions (SEC-02)
builder.Services.AddJwtAuthentication(builder.Configuration, options =>
{
    // Clock skew this service used before the shared setup (JwtBearer default); unified in SEC-18
    options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
});

// Authorization - shared policies User / Admin / AdminWrite (MAJ-09)
builder.Services.AddStoreAuthorization();

// Configure HttpClient for AuditLogClient with proper base address;
// every call carries the shared service key required by POST /api/auditlog/internal (SEC-04)
var auditLogServiceUrl = builder.Configuration["Services:AuditLogService"] ?? "http://localhost:5004";
builder.Services.AddInternalApiKeyClient(builder.Configuration);
builder.Services.AddHttpClient<Store.Shared.Services.IAuditLogClient, Store.Shared.Services.AuditLogClient>(client =>
{
    client.BaseAddress = new Uri(auditLogServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<Store.Shared.Authentication.InternalApiKeyMessageHandler>();

// HTTP Client
builder.Services.AddHttpClient();

// Services
builder.Services.AddScoped<ICartService, CartService>();

// Health Checks - Make them optional to prevent startup failures
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database", failureStatus: HealthStatus.Degraded)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", failureStatus: HealthStatus.Degraded);

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Store Cart Service", Version = "v1" });

    // JWT Bearer token support
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuditLogging();
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Cart Service V1");
        c.RoutePrefix = "swagger"; // This ensures Swagger UI is available at /swagger
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Database migration - Make this optional to prevent startup failures
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
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
