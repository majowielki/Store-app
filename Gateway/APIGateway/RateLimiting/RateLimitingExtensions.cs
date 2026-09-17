using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Configuration;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Store.GatewayService.RateLimiting;

/// <summary>
/// The gateway's rate limiting: sliding windows per client on every proxied route, attached
/// by name in the YARP route configuration. A signed-in user is one client wherever they
/// come from; an anonymous request counts against its address, which the forwarded-headers
/// middleware has restored by then.
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddStoreRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStoreOptions<AuthRateLimitOptions>(configuration, AuthRateLimitOptions.SectionName);
        services.AddStoreOptions<RouteRateLimitOptions>(configuration, RouteRateLimitOptions.SectionName);

        services.AddRateLimiter(options =>
        {
            // Sign-in traffic: the credential endpoints get the stricter limit, per address
            options.AddPolicy(AuthRateLimitOptions.PolicyName, context =>
            {
                var limits = Options<AuthRateLimitOptions>(context);
                var path = context.Request.Path.Value ?? string.Empty;
                var isCredentialEndpoint = AuthRateLimitOptions.CredentialPaths
                    .Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));

                var (bucket, permitLimit) = isCredentialEndpoint
                    ? ("auth-credentials", limits.CredentialPermitLimit)
                    : ("auth", limits.PermitLimit);

                return SlidingWindow($"{bucket}:{ClientAddress(context)}", permitLimit, limits.WindowSeconds);
            });

            options.AddPolicy(RouteRateLimitOptions.ApiPolicyName, context =>
            {
                var limits = Options<RouteRateLimitOptions>(context);
                return SlidingWindow($"api:{ClientKey(context)}", limits.ApiPermitLimit, limits.WindowSeconds);
            });

            options.AddPolicy(RouteRateLimitOptions.AdminPolicyName, context =>
            {
                var limits = Options<RouteRateLimitOptions>(context);
                return SlidingWindow($"admin:{ClientKey(context)}", limits.AdminPermitLimit, limits.WindowSeconds);
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Nothing queues (QueueLimit 0), so a refused lease carries no retry time of its own;
            // the window of the policy that refused is the honest answer
            options.OnRejected = (context, _) =>
            {
                var policy = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                var windowSeconds = policy == AuthRateLimitOptions.PolicyName
                    ? Options<AuthRateLimitOptions>(context.HttpContext).WindowSeconds
                    : Options<RouteRateLimitOptions>(context.HttpContext).WindowSeconds;
                context.HttpContext.Response.Headers.RetryAfter = windowSeconds.ToString();
                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    private static T Options<T>(HttpContext context) where T : class
        => context.RequestServices.GetRequiredService<IOptions<T>>().Value;

    private static string ClientAddress(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>The user behind the token when there is one, the client address otherwise.</summary>
    private static string ClientKey(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userId) ? $"ip:{ClientAddress(context)}" : $"user:{userId}";
    }

    private static RateLimitPartition<string> SlidingWindow(string key, int permitLimit, int windowSeconds)
        => RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            SegmentsPerWindow = 6,
            QueueLimit = 0
        });
}
