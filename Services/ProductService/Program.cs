using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using StackExchange.Redis;
using Store.ProductService.Data;
using Store.ProductService.Services;
using Store.Shared.Authorization;
using Store.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add standard API controllers
builder.Services.AddStandardApiControllers();

// FluentValidation: validators from DI, request models validated before the action runs
builder.Services.AddValidatorsFromAssemblyContaining<Store.ProductService.Validators.CreateProductRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

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

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// Configure HttpClient for AuditLogClient with proper base address;
// every call carries the shared service key required by POST /api/auditlog/internal
var auditLogServiceUrl = builder.Configuration["Services:AuditLogService"] ?? "http://localhost:5004";
builder.Services.AddInternalApiKeyClient(builder.Configuration);
builder.Services.AddHttpClient<Store.Shared.Services.IAuditLogClient, Store.Shared.Services.AuditLogClient>(client =>
{
    client.BaseAddress = new Uri(auditLogServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<Store.Shared.Authentication.InternalApiKeyMessageHandler>();

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
