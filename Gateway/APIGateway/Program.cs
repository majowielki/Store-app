using Microsoft.AspNetCore.HttpOverrides;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authentication;
using Store.BuildingBlocks.Authorization;
using Store.BuildingBlocks.Configuration;
using Store.BuildingBlocks.Health;
using Store.BuildingBlocks.Observability;
using Store.GatewayService.Cors;
using Store.GatewayService.RateLimiting;
using Store.GatewayService.Security;

// The gateway does one thing: it proxies /api/v1 to the services, applying the shared
// authentication, the authorization policies named on the routes and the rate limits. It
// serves nothing of its own but the health endpoints.
var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Traces, metrics and logs through OTLP (see Store.BuildingBlocks.Observability)
builder.AddStoreObservability("gateway");

// Errors the gateway produces itself (401, 403, 404, 429) are problem responses like the services'
builder.Services.AddStoreProblemDetails();

// JWT Authentication - key, issuer, audience and the validation rules come from the shared setup
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization - shared policies User / Admin / AdminWrite, referenced by YARP routes
builder.Services.AddStoreAuthorization();

// YARP forwards the Authorization header and the client address (X-Forwarded-*) on its own
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Health checks: the gateway has no database and no broker of its own
builder.Services.AddStoreHealthChecks();

// Rate limiting: sliding windows per client, attached to the routes in appsettings.json by name
builder.Services.AddStoreRateLimiting(builder.Configuration);

// CORS only matters when the UI is served from another origin than this gateway; the normal
// deployment proxies /api from the UI's own origin and needs none
builder.Services.AddStoreOptions<CorsOptions>(builder.Configuration, CorsOptions.SectionName);
builder.Services.AddStoreCors(builder.Configuration);

var app = builder.Build();

// Behind the Container Apps ingress (or the UI's nginx) the client address arrives in
// X-Forwarded-For; without this every user would share one rate-limit bucket. ForwardLimit = 1
// trusts only the entry appended by the nearest proxy.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
// Defaults only trust loopback proxies; the ingress is not one
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseStoreProblemDetails();
app.UseSecurityHeaders();
app.UseStoreCors();

// Authentication runs before the rate limiter, so a signed-in user is one bucket wherever
// they come from; authorization decides after the limit has been applied
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Health checks: /health/live, /health/ready, /health (details)
app.MapStoreHealthChecks();

app.MapReverseProxy();

app.Run();
