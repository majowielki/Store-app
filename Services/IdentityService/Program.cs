using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Store.IdentityService.Data;
using Store.IdentityService.Models;
using Store.IdentityService.Services;
using Store.Shared.Middleware;
using Store.Shared.Utility;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Email confirmation settings
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "your-very-long-secret-key-here-at-least-32-characters";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization - Clean role-based policies
builder.Services.AddAuthorization(options =>
{
    // True Admin only - for create, update, delete operations
    options.AddPolicy("TrueAdminOnly", policy =>
        policy.RequireClaim("role", Constants.Role_TrueAdmin));

    // Demo Admin or True Admin - for viewing admin interfaces
    options.AddPolicy("AdminAccess", policy =>
        policy.RequireClaim("role", Constants.Role_TrueAdmin, Constants.Role_DemoAdmin));

    // Any authenticated user
    options.AddPolicy("UserAccess", policy =>
        policy.RequireClaim("role", Constants.Role_User, Constants.Role_DemoAdmin, Constants.Role_TrueAdmin));
});

// Services
builder.Services.AddScoped<IAuthService, AuthService>();

// Health Checks - Make them optional to prevent startup failures
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database", failureStatus: HealthStatus.Degraded);

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
app.UseMiddleware<GlobalExceptionMiddleware>();

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
app.MapHealthChecks("/health");

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
                await SeedUsersAsync(userManager, builder.Configuration, logger);
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

static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
{
    // 1. Seed True Admin - Use environment variables or secure configuration
    await SeedTrueAdminAsync(userManager, configuration, logger);
    
    // 2. Seed Demo Admin
    await SeedDemoAdminAsync(userManager, logger);
    
    // 3. Seed Demo Store User
    await SeedDemoStoreUserAsync(userManager, logger);
}

static async Task SeedTrueAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
{
    // Use environment variables for maximum security
    var adminEmail = Environment.GetEnvironmentVariable("TRUE_ADMIN_EMAIL") 
                    ?? configuration["TrueAdmin:Email"] 
                    ?? "trueadmin@store.com";
    
    // Password should come from environment variables or secure key vault
    var adminPassword = Environment.GetEnvironmentVariable("TRUE_ADMIN_PASSWORD") 
                       ?? configuration["TrueAdmin:Password"];
    
    // If no password is configured, generate a secure random one and log instructions
    if (string.IsNullOrEmpty(adminPassword))
    {
        adminPassword = GenerateSecurePassword();
        logger.LogWarning("===============================================");
        logger.LogWarning("TRUE ADMIN CREDENTIALS GENERATED:");
        logger.LogWarning("Email: {Email}", adminEmail);
        logger.LogWarning("Password: {Password}", adminPassword);
        logger.LogWarning("===============================================");
        logger.LogWarning("IMPORTANT: Save these credits securely!");
        logger.LogWarning("Set TRUE_ADMIN_PASSWORD environment variable for production!");
        logger.LogWarning("===============================================");
    }
    
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
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
    else
    {
        logger.LogInformation("True Admin already exists: {Email}", adminEmail);
    }
}

static async Task SeedDemoAdminAsync(UserManager<ApplicationUser> userManager, ILogger logger)
{
    const string demoAdminEmail = "demoadmin@store.com";
    const string demoAdminPassword = "DemoAdmin123!"; // This can be public as it's for demo purposes
    
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
            UpdatedAt = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(demoAdminUser, demoAdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(demoAdminUser, Constants.Role_DemoAdmin);
            logger.LogInformation("Demo Admin created - Email: {Email} / Password: {Password}", demoAdminEmail, demoAdminPassword);
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

static string GenerateSecurePassword()
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
    var random = new Random();
    var password = new char[16];
    
    // Ensure at least one of each required character type
    password[0] = chars[random.Next(0, 26)]; // Uppercase
    password[1] = chars[random.Next(26, 52)]; // Lowercase  
    password[2] = chars[random.Next(52, 62)]; // Digit
    password[3] = chars[random.Next(62, chars.Length)]; // Special char
    
    // Fill the rest randomly
    for (int i = 4; i < password.Length; i++)
    {
        password[i] = chars[random.Next(chars.Length)];
    }
    
    // Shuffle the password
    for (int i = password.Length - 1; i > 0; i--)
    {
        int j = random.Next(i + 1);
        (password[i], password[j]) = (password[j], password[i]);
    }
    
    return new string(password);
}