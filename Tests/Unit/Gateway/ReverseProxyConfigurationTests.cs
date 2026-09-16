using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Store.Contracts.Authorization;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace Store.Tests.Unit.Gateway;

/// <summary>
/// Loads the gateway configuration exactly the way the host does (appsettings.json + the
/// environment file) and checks the parts that broke production before:
/// every environment must expose the same routes, point them at existing clusters, contain
/// no "${VAR}" placeholders (.NET does not expand them) and validate tokens with the same
/// issuer/audience that IdentityService issues.
/// </summary>
public class ReverseProxyConfigurationTests
{
    private static readonly string ConfigRoot = Path.Combine(AppContext.BaseDirectory, "Config");

    private static readonly string[] ExpectedRoutes =
    {
        "identity-route", "products-route", "cart-route", "orders-route", "audit-route", "admin-orders-route", "admin-route"
    };

    public static TheoryData<string> Environments => new() { "Development", "Production" };

    private static IConfiguration LoadConfiguration(string service, string environment)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Path.Combine(ConfigRoot, service))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();
    }

    private static IProxyConfig LoadProxyConfig(string environment)
    {
        var configuration = LoadConfiguration("Gateway", environment);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IProxyConfigProvider>().GetConfig();
    }

    [Theory]
    [MemberData(nameof(Environments))]
    public void Every_environment_exposes_all_routes(string environment)
    {
        var config = LoadProxyConfig(environment);

        var routeIds = config.Routes.Select(r => r.RouteId).OrderBy(id => id).ToArray();

        Assert.Equal(ExpectedRoutes.OrderBy(id => id), routeIds);
    }

    [Theory]
    [MemberData(nameof(Environments))]
    public void Every_route_points_at_an_existing_cluster_with_an_absolute_destination(string environment)
    {
        var config = LoadProxyConfig(environment);
        var clusters = config.Clusters.ToDictionary(c => c.ClusterId);

        foreach (var route in config.Routes)
        {
            Assert.True(clusters.ContainsKey(route.ClusterId!),
                $"Route '{route.RouteId}' references unknown cluster '{route.ClusterId}' in {environment}");

            var destinations = clusters[route.ClusterId!].Destinations;
            Assert.NotNull(destinations);
            Assert.NotEmpty(destinations!);

            foreach (var destination in destinations!.Values)
            {
                Assert.True(Uri.TryCreate(destination.Address, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https",
                    $"Cluster '{route.ClusterId}' has an invalid destination '{destination.Address}' in {environment}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Environments))]
    public void Protected_routes_keep_their_authorization_policies(string environment)
    {
        var routes = LoadProxyConfig(environment).Routes.ToDictionary(r => r.RouteId);

        Assert.Equal("auth", routes["identity-route"].RateLimiterPolicy);
        Assert.Equal(Policies.User, routes["cart-route"].AuthorizationPolicy);
        Assert.Equal(Policies.User, routes["orders-route"].AuthorizationPolicy);
        Assert.Equal(Policies.Admin, routes["audit-route"].AuthorizationPolicy);
        Assert.Equal(Policies.Admin, routes["admin-route"].AuthorizationPolicy);
        Assert.Equal(Policies.Admin, routes["admin-orders-route"].AuthorizationPolicy);
        // The more specific admin route must win over the identity catch-all
        Assert.Equal("orders-cluster", routes["admin-orders-route"].ClusterId);
        Assert.True(routes["admin-orders-route"].Order < routes["admin-route"].Order);
    }

    [Theory]
    [MemberData(nameof(Environments))]
    public void Configuration_contains_no_unexpanded_placeholders(string environment)
    {
        var configuration = LoadConfiguration("Gateway", environment);

        var placeholders = configuration.AsEnumerable()
            .Where(kv => kv.Value is not null && kv.Value.Contains("${"))
            .Select(kv => $"{kv.Key} = {kv.Value}")
            .ToArray();

        Assert.True(placeholders.Length == 0,
            $"Unexpanded placeholders in {environment}: {string.Join("; ", placeholders)}");
    }

    [Theory]
    [MemberData(nameof(Environments))]
    public void Gateway_validates_tokens_with_the_issuer_and_audience_identity_issues(string environment)
    {
        var gateway = LoadConfiguration("Gateway", environment);
        var identity = LoadConfiguration("IdentityService", environment);

        Assert.Equal(identity["JwtSettings:Issuer"], gateway["JwtSettings:Issuer"]);
        Assert.Equal(identity["JwtSettings:Audience"], gateway["JwtSettings:Audience"]);
        Assert.False(string.IsNullOrWhiteSpace(gateway["JwtSettings:Issuer"]));
        Assert.False(string.IsNullOrWhiteSpace(gateway["JwtSettings:Audience"]));
    }

    [Theory]
    [MemberData(nameof(Environments))]
    public void No_environment_file_ships_a_jwt_secret(string environment)
    {
        var configuration = LoadConfiguration("Gateway", environment);

        Assert.Null(configuration["JwtSettings:SecretKey"]);
        Assert.Null(configuration["Jwt:Key"]);
    }
}
