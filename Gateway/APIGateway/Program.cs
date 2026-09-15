using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Store.Shared.Extensions;
using Store.Shared.Authorization;
using Store.Shared.MessageBus;
using Store.Shared.Middleware;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

using Store.GatewayService.HealthChecks;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });

// JWT Authentication - key, issuer and audience come from validated JwtOptions (SEC-02)
builder.Services.AddJwtAuthentication(builder.Configuration, options =>
    {
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(2);
        options.TokenValidationParameters.RequireExpirationTime = true;

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                // Surface details to help client-side recovery and logging (dev-friendly)
                // Sanitize header value to avoid CR/LF or invalid characters that Kestrel rejects
                var rawMsg = context.Exception.Message ?? "invalid token";
                var safeMsg = rawMsg.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
                context.Response.Headers["WWW-Authenticate"] =
                    $"Bearer error=\"invalid_token\", error_description=\"{safeMsg}\"";
                // Exception type and IdentityModel's PII-free message only - no headers,
                // tokens or claims in logs (SEC-05); successful validations are not logged (MIN-17)
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GatewayAuth");
                logger.LogWarning("JWT authentication failed at gateway: {ErrorType}: {Error}",
                    context.Exception.GetType().Name, context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Add hint header without suppressing default behavior
                if (!string.IsNullOrEmpty(context.ErrorDescription))
                {
                    var safe = context.ErrorDescription.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
                    context.Response.Headers["WWW-Authenticate-Error"] = safe;
                }
                return Task.CompletedTask;
            }
        };
    });

// Authorization - shared policies User / Admin / AdminWrite, referenced by YARP routes (MAJ-09)
builder.Services.AddStoreAuthorization();

// RabbitMQ Message Bus with error handling
try
{
    builder.Services.AddRabbitMQ(builder.Configuration);
    builder.Services.AddMessageBusSubscriptions();
}
catch (Exception ex)
{
    var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger<Program>();
    logger.LogWarning(ex, "RabbitMQ setup failed, continuing without message bus");
}

// HTTP Client
builder.Services.AddHttpClient();

// YARP Reverse Proxy z przekazywaniem Authorization
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(context =>
        {
            var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                context.ProxyRequest.Headers.Remove("Authorization");
                context.ProxyRequest.Headers.Add("Authorization", authHeader);
            }
            return ValueTask.CompletedTask;
        });
    });

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Gateway is running"))
    .AddCheck<RabbitMQHealthCheck>("rabbitmq");

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ApiPolicy", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 100;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });
    
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Store Gateway API", 
        Version = "v1",
        Description = "API Gateway for Store microservices"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
    else
    {
        options.AddPolicy("Production", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
                .Get<string[]>() ?? new[] { "https://localhost:3000" };
                
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Gateway API V1");
        c.RoutePrefix = "swagger";
    });
}

// Rate limiting
app.UseRateLimiter();

// CORS
var corsPolicy = app.Environment.IsDevelopment() ? "AllowAll" : "Production";
app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
});

app.MapReverseProxy();
app.MapControllers();

app.Run();
