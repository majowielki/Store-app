using Microsoft.EntityFrameworkCore;
using Store.ProductService.Data;
using Store.ProductService.Services;
using StackExchange.Redis;
using Store.Shared.Middleware;
using Store.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add standard API controllers
builder.Services.AddStandardApiControllers();

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
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

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy =>
        policy.RequireRole("true-admin", "demo-admin"));
    options.AddPolicy("UserAccess", policy =>
        policy.RequireRole("user", "true-admin", "demo-admin"));
});

// Configure HttpClient for AuditLogClient with proper base address
var auditLogServiceUrl = builder.Configuration["Services:AuditLogService"] ?? "http://localhost:5004";
builder.Services.AddHttpClient<Store.Shared.Services.IAuditLogClient, Store.Shared.Services.AuditLogClient>(client =>
{
    client.BaseAddress = new Uri(auditLogServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Business Services
builder.Services.AddScoped<IProductService, Store.ProductService.Services.ProductService>();

// Health Checks
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
builder.Services.AddStandardHealthChecks(connectionString, redisConnectionString);

// Swagger
builder.Services.AddSwaggerWithJwt("Product Service API");

// CORS
builder.Services.AddStandardCors();

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

// Add audit logging and global exception handling
app.UseAuditLogging();
app.UseGlobalExceptionHandling();

// CORS
app.UseCors("DefaultCorsPolicy");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();
app.MapHealthChecks("/health");

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await context.Database.MigrateAsync();
        
        await DatabaseSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during database migration or seeding");
    }
}

app.Run();
