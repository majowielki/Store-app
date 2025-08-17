using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Store.OrderService.Data;
using Store.OrderService.Services;
using System.Text.Json.Serialization;
using Store.Shared.Middleware;
using Store.Shared.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Store.Shared.MessageBus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Database
builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    // Allow applying existing migrations even if there are pending model changes (e.g., new properties not yet migrated)
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "your-very-long-secret-key-here-at-least-32-characters";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "Store.API",
            ValidAudience = jwtSettings["Audience"] ?? "Store.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                return Task.CompletedTask;
            }
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy =>
        policy.RequireRole(Store.Shared.Utility.Constants.Role_TrueAdmin, Store.Shared.Utility.Constants.Role_DemoAdmin));

    options.AddPolicy("UserAccess", policy =>
        policy.RequireRole(Store.Shared.Utility.Constants.Role_User, Store.Shared.Utility.Constants.Role_DemoAdmin, Store.Shared.Utility.Constants.Role_TrueAdmin));
});

// Configure HttpClient for AuditLogClient with proper base address
var auditLogServiceUrl = builder.Configuration["Services:AuditLogService"] ?? "http://localhost:5004";
builder.Services.AddHttpClient<Store.Shared.Services.IAuditLogClient, Store.Shared.Services.AuditLogClient>(client =>
{
    client.BaseAddress = new Uri(auditLogServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// HTTP Client
builder.Services.AddHttpClient();
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
builder.Services.AddScoped<IOrderService, Store.OrderService.Services.OrderService>();

// Health Checks - Make them optional to prevent startup failures
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database", failureStatus: HealthStatus.Degraded);

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Store Order Service", Version = "v1" });
    
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Order Service V1");
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
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            // Check if database exists and is accessible
            if (context.Database.CanConnect())
            {
                context.Database.Migrate();
                logger.LogInformation("Database migration completed successfully.");

                // Cleanup cross-service FK if it exists: OrderItem -> Product and Product table
                try
                {
                    var cleanupSql = """
DO $$
BEGIN
    -- Drop FK constraint if present
    IF EXISTS (
        SELECT 1
        FROM information_schema.table_constraints tc
        WHERE tc.constraint_name = 'FK_OrderItem_Product_ProductId'
          AND tc.table_name = 'OrderItem'
    ) THEN
        ALTER TABLE "OrderItem" DROP CONSTRAINT "FK_OrderItem_Product_ProductId";
    END IF;

    -- Drop Product table if present (OrderService should not own it)
    IF EXISTS (
        SELECT 1 FROM information_schema.tables t
        WHERE t.table_name = 'Product'
    ) THEN
        DROP TABLE "Product";
    END IF;
END $$;
""";
                    context.Database.ExecuteSqlRaw(cleanupSql);
                    logger.LogInformation("OrderService DB cleanup completed (removed Product FK/table if existed).");
                }
                catch (Exception exCleanup)
                {
                    logger.LogWarning(exCleanup, "OrderService DB cleanup step failed (safe to ignore if already clean).");
                }
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
