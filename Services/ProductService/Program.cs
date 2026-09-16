using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.OpenApi;
using Store.ProductService.Data;
using Store.ProductService.Services;
using Store.Shared.Extensions;
using Store.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add standard API controllers
builder.Services.AddStandardApiControllers();

// FluentValidation: validators from DI, request models validated before the action runs
builder.Services.AddValidatorsFromAssemblyContaining<Store.ProductService.Validators.CreateProductRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// GET /api/products/{id}/snapshot is for other services: they present the shared internal key
builder.Services.AddInternalApiKeyAuthentication(builder.Configuration);

// Addresses of the services this one calls; startup fails when any is missing
builder.Services.AddServiceEndpoints(builder.Configuration, nameof(ServiceEndpointsOptions.AuditLogService));

// Audit entries go to AuditLogService (address from Services:AuditLogService, validated at startup)
builder.Services.AddAuditLogClient(builder.Configuration);

// Business Services
builder.Services.AddScoped<IProductService, Store.ProductService.Services.ProductService>();

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetConnectionString("DefaultConnection")!);

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
app.MapStoreHealthChecks();

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
