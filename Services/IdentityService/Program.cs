using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Store.IdentityService.Data;
using Store.IdentityService.Models;
using Store.IdentityService.Services;
using Store.Shared.Authorization;
using Store.Shared.Configuration;
using Store.Shared.Extensions;
using Store.Shared.Utility;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// FluentValidation: validators from DI, request models validated before the action runs
builder.Services.AddValidatorsFromAssemblyContaining<Store.IdentityService.Validators.RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// Ensure API returns 401/403 instead of redirecting to /Account/Login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// JWT Authentication - key, issuer and audience come from validated JwtOptions
builder.Services.AddJwtAuthentication(builder.Configuration, options =>
    {
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(2);
        options.TokenValidationParameters.RoleClaimType = System.Security.Claims.ClaimTypes.Role;

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                var rawMsg = context.Exception.Message ?? "invalid token";
                var safeMsg = rawMsg.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
                context.Response.Headers["WWW-Authenticate"] =
                    $"Bearer error=\"invalid_token\", error_description=\"{safeMsg}\"";
                // Never log headers, the raw token or claims; the exception type and
                // IdentityModel's PII-free message are enough to diagnose a rejected token.
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("IdentityAuth");
                logger.LogWarning("JWT authentication failed at identity service: {ErrorType}: {Error}",
                    context.Exception.GetType().Name, context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();

// Addresses of the services this one calls; startup fails when any is missing
builder.Services.AddServiceEndpoints(builder.Configuration,
    nameof(ServiceEndpointsOptions.OrderService),
    nameof(ServiceEndpointsOptions.AuditLogService));

// Audit entries go to AuditLogService (address from Services:AuditLogService, validated at startup)
builder.Services.AddAuditLogClient(builder.Configuration);

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Store Identity Service", Version = "v1" });

    // JWT Bearer token support
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Identity Service V1");
        c.RoutePrefix = "swagger"; // This ensures Swagger UI is available at /swagger
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapStoreHealthChecks();

// Database migration and comprehensive user seeding
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Check if database exists and is accessible
            if (context.Database.CanConnect())
            {
                // Apply migrations
                context.Database.Migrate();
                logger.LogInformation("Database migration completed successfully.");

                // Seed roles
                await SeedRolesAsync(roleManager, logger);
                logger.LogInformation("Role seeding completed successfully.");

                // Seed all users (True Admin, Demo Admin, Demo User)
                await SeedUsersAsync(userManager, builder.Configuration, app.Environment, logger);
                logger.LogInformation("User seeding completed successfully.");
            }
            else
            {
                logger.LogWarning("Database connection failed. Skipping migration and seeding.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database. Continuing without database setup.");
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

// Helper methods for comprehensive seeding
static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
{
    var roles = new[] { Constants.Role_TrueAdmin, Constants.Role_DemoAdmin, Constants.Role_User };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            logger.LogInformation("Created role: {Role}", role);
        }
    }
}

static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration, IHostEnvironment environment, ILogger logger)
{
    // 1. Seed True Admin - Use environment variables or secure configuration
    await SeedTrueAdminAsync(userManager, configuration, environment, logger);

    // 2. Seed Demo Admin
    await SeedDemoAdminAsync(userManager, logger);

    // 3. Seed Demo Store User
    await SeedDemoStoreUserAsync(userManager, logger);
}

static async Task SeedTrueAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration, IHostEnvironment environment, ILogger logger)
{
    // Use environment variables for maximum security
    var adminEmail = Environment.GetEnvironmentVariable("TRUE_ADMIN_EMAIL")
                    ?? configuration["TrueAdmin:Email"]
                    ?? "trueadmin@store.com";

    // Password should come from environment variables or secure key vault
    var adminPassword = Environment.GetEnvironmentVariable("TRUE_ADMIN_PASSWORD")
                       ?? configuration["TrueAdmin:Password"];

    if (await userManager.FindByEmailAsync(adminEmail) != null)
    {
        logger.LogInformation("True Admin already exists: {Email}", adminEmail);
        return;
    }

    // The password is never generated and never logged. Without one the
    // account is not created: production refuses to start, other environments skip the seed.
    if (string.IsNullOrEmpty(adminPassword))
    {
        const string hint = "Set TrueAdmin:Password (TrueAdmin__Password / TRUE_ADMIN_PASSWORD) to create the true admin account.";
        if (environment.IsProduction())
        {
            throw new InvalidOperationException("True admin password is not configured. " + hint);
        }

        logger.LogWarning("True Admin not created: no password configured. {Hint}", hint);
        return;
    }

    var adminUser = new ApplicationUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        EmailConfirmed = true,
        FirstName = "True",
        LastName = "Administrator",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(adminUser, adminPassword);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, Constants.Role_TrueAdmin);
        logger.LogInformation("True Admin created successfully: {Email}", adminEmail);

        // Log admin creation token information
        var adminCreationToken = Environment.GetEnvironmentVariable("ADMIN_CREATION_TOKEN")
                               ?? configuration[Constants.AdminCreationTokenKey];
        if (!string.IsNullOrEmpty(adminCreationToken))
        {
            logger.LogInformation("Admin creation token is configured for additional true admin creation");
        }
        else
        {
            logger.LogWarning("Consider setting ADMIN_CREATION_TOKEN environment variable for secure additional admin creation");
        }
    }
    else
    {
        logger.LogError("Failed to create True Admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}

static async Task SeedDemoAdminAsync(UserManager<ApplicationUser> userManager, ILogger logger)
{
    var demoAdminEmail = Constants.DemoAdminEmail;
    var demoAdminPassword = Constants.DemoAdminPassword;
    const string demoAdminAddress = "123 Demo Street, Demo City, DC 12345"; // Match demo user address

    if (await userManager.FindByEmailAsync(demoAdminEmail) == null)
    {
        var demoAdminUser = new ApplicationUser
        {
            UserName = demoAdminEmail,
            Email = demoAdminEmail,
            EmailConfirmed = true,
            FirstName = "Demo",
            LastName = "Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SimpleAddress = demoAdminAddress // Set address
        };

        var result = await userManager.CreateAsync(demoAdminUser, demoAdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(demoAdminUser, Constants.Role_DemoAdmin);
            logger.LogInformation("Demo Admin created: {Email}", demoAdminEmail);
        }
        else
        {
            logger.LogError("Failed to create Demo Admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        logger.LogInformation("Demo Admin already exists: {Email}", demoAdminEmail);
    }
}

static async Task SeedDemoStoreUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
{
    if (await userManager.FindByEmailAsync(Constants.DemoUserEmail) == null)
    {
        var demoUser = new ApplicationUser
        {
            UserName = Constants.DemoUserEmail,
            Email = Constants.DemoUserEmail,
            EmailConfirmed = true,
            FirstName = "Demo",
            LastName = "User",
            SimpleAddress = "123 Demo Street, Demo City, DC 12345",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(demoUser, Constants.DemoUserPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(demoUser, Constants.Role_User);
            logger.LogInformation("Demo Store User created: {Email} (Use demo login endpoint - no password required)", Constants.DemoUserEmail);
        }
        else
        {
            logger.LogError("Failed to create Demo Store User: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        logger.LogInformation("Demo Store User already exists: {Email}", Constants.DemoUserEmail);
    }
}
