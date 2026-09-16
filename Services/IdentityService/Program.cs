using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.OpenApi;
using Store.BuildingBlocks.Persistence;
using Store.Contracts.Authorization;
using Store.IdentityService.Consumers;
using Store.IdentityService.Data;
using Store.IdentityService.Models;
using Store.IdentityService.Seeding;
using Store.IdentityService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStandardApiControllers();

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

// Message bus: a placed order may carry a delivery address to store in the profile; profile changes reach the audit service as events
builder.Services.AddStoreMessaging<IdentityDbContext>(builder.Configuration, serviceName: "identity", bus => bus.AddConsumer<OrderPlacedConsumer>());

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddSwaggerWithJwt("Store Identity Service");
builder.Services.AddStandardCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Identity Service V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapStoreHealthChecks();

// Migrations, roles and the seeded accounts: applied here in Development, by "--migrate" in a
// deployment; pending migrations stop the start
if (await app.PrepareDatabaseAsync<IdentityDbContext>(args, seed: async services =>
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        await SeedRolesAsync(services.GetRequiredService<RoleManager<IdentityRole>>(), logger);
        await SeedUsersAsync(services.GetRequiredService<UserManager<ApplicationUser>>(), builder.Configuration, app.Environment, logger);
    }))
{
    return;
}

app.Run();

// Helper methods for comprehensive seeding
static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
{
    var roles = new[] { Roles.TrueAdmin, Roles.DemoAdmin, Roles.User };

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
        await userManager.AddToRoleAsync(adminUser, Roles.TrueAdmin);
        logger.LogInformation("True Admin created successfully: {Email}", adminEmail);
    }
    else
    {
        logger.LogError("Failed to create True Admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}

static async Task SeedDemoAdminAsync(UserManager<ApplicationUser> userManager, ILogger logger)
{
    var demoAdminEmail = SeedAccounts.DemoAdminEmail;
    var demoAdminPassword = SeedAccounts.DemoAdminPassword;
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
            await userManager.AddToRoleAsync(demoAdminUser, Roles.DemoAdmin);
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
    if (await userManager.FindByEmailAsync(SeedAccounts.DemoUserEmail) == null)
    {
        var demoUser = new ApplicationUser
        {
            UserName = SeedAccounts.DemoUserEmail,
            Email = SeedAccounts.DemoUserEmail,
            EmailConfirmed = true,
            FirstName = "Demo",
            LastName = "User",
            SimpleAddress = "123 Demo Street, Demo City, DC 12345",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(demoUser, SeedAccounts.DemoUserPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(demoUser, Roles.User);
            logger.LogInformation("Demo Store User created: {Email} (Use demo login endpoint - no password required)", SeedAccounts.DemoUserEmail);
        }
        else
        {
            logger.LogError("Failed to create Demo Store User: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        logger.LogInformation("Demo Store User already exists: {Email}", SeedAccounts.DemoUserEmail);
    }
}
