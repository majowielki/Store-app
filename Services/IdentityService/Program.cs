using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Observability;
using Store.BuildingBlocks.OpenApi;
using Store.BuildingBlocks.Persistence;
using Store.IdentityService;
using Store.IdentityService.Consumers;
using Store.IdentityService.Data;
using Store.IdentityService.Models;
using Store.IdentityService.Seeding;
using Store.IdentityService.Services;

var builder = WebApplication.CreateBuilder(args);

// Traces, metrics and logs through OTLP (see Store.BuildingBlocks.Observability)
builder.AddStoreObservability("identity");

builder.Services.AddStandardApiControllers();

// FluentValidation validators for the request models; the shared controller setup runs them before the action
builder.Services.AddValidatorsFromAssemblyContaining<Store.IdentityService.Validators.RegisterRequestValidator>();

// Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity without the cookie stack: this service issues bearer tokens and never signs anyone
// in with a cookie, so there is nothing to redirect to /Account/Login
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    // Password settings - the one definition, shared with the request validator
    options.Password.RequireDigit = PasswordPolicy.RequireDigit;
    options.Password.RequireLowercase = PasswordPolicy.RequireLowercase;
    options.Password.RequireNonAlphanumeric = PasswordPolicy.RequireNonAlphanumeric;
    options.Password.RequireUppercase = PasswordPolicy.RequireUppercase;
    options.Password.RequiredLength = PasswordPolicy.MinLength;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<IdentityDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

// JWT Authentication - key, issuer, audience and the validation rules come from the shared setup
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization - shared policies User / Admin / AdminWrite
builder.Services.AddStoreAuthorization();

// Services: tokens of a session, accounts, and the daily purge of refresh tokens nobody can present any more
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();

// Demo accounts and the password-less demo logins exist only where Demo:Enabled says so
builder.Services.AddStoreOptions<DemoOptions>(builder.Configuration, DemoOptions.SectionName);
builder.Services.AddScoped<IdentitySeeder>();

// Message bus: a placed order may carry a delivery address to store in the profile; profile changes reach the audit service as events
builder.Services.AddStoreMessaging<IdentityDbContext>(builder.Configuration, serviceName: "identity", bus => bus.AddConsumer<OrderPlacedConsumer>());

// Health checks: /health/live, /health/ready (database), /health (details)
builder.Services.AddStoreHealthChecks(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddSwaggerWithJwt("Store Identity Service");
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseStoreProblemDetails();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Identity Service V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapStoreHealthChecks();

// Migrations, roles and the seeded accounts: applied here in Development, by "--migrate" in a
// deployment; pending migrations stop the start
if (await app.PrepareDatabaseAsync<IdentityDbContext>(args, seed: services => services.GetRequiredService<IdentitySeeder>().SeedAsync()))
{
    return;
}

app.Run();
