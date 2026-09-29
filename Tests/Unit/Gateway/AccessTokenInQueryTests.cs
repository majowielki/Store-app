using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Store.GatewayService.Security;
using Xunit;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Store.Tests.Unit.Gateway;

/// <summary>
/// What the gateway forwards for a request that brought its access token in the query: the
/// token in the Authorization header, and an address without it.
/// </summary>
public class AccessTokenInQueryTests
{
    private const string Destination = "http://orderservice:5006";

    private static async Task<HttpRequestMessage> ForwardAsync(RouteConfig route, string pathAndQuery, string? authorization = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddReverseProxy().AddTransforms(AccessTokenInQuery.Apply);
        using var provider = services.BuildServiceProvider();
        var transformer = provider.GetRequiredService<ITransformBuilder>().Build(route, new ClusterConfig { ClusterId = "orders-cluster" });

        var context = new DefaultHttpContext();
        var queryStart = pathAndQuery.IndexOf('?');
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = queryStart < 0 ? pathAndQuery : pathAndQuery[..queryStart];
        context.Request.QueryString = queryStart < 0 ? QueryString.Empty : new QueryString(pathAndQuery[queryStart..]);
        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        var proxyRequest = new HttpRequestMessage();
        await transformer.TransformRequestAsync(context, proxyRequest, Destination, CancellationToken.None);
        return proxyRequest;
    }

    private static RouteConfig Route(bool tokenInQuery) => new()
    {
        RouteId = "route",
        ClusterId = "orders-cluster",
        Match = new RouteMatch { Path = "/api/v1/admin/orders/live" },
        Metadata = tokenInQuery ? new Dictionary<string, string> { [AccessTokenInQuery.MetadataKey] = "true" } : null
    };

    [Fact]
    public async Task On_a_route_that_allows_it_the_token_moves_from_the_query_to_the_header()
    {
        var forwarded = await ForwardAsync(Route(tokenInQuery: true), $"/api/v1/admin/orders/live?id=1&{AccessTokenInQuery.QueryParameter}=token-1");

        Assert.Equal("Bearer", forwarded.Headers.Authorization?.Scheme);
        Assert.Equal("token-1", forwarded.Headers.Authorization?.Parameter);
        Assert.Equal($"{Destination}/api/v1/admin/orders/live?id=1", forwarded.RequestUri!.ToString());
    }

    [Fact]
    public async Task A_header_the_request_brought_wins_over_the_query()
    {
        var forwarded = await ForwardAsync(Route(tokenInQuery: true),
            $"/api/v1/admin/orders/live?{AccessTokenInQuery.QueryParameter}=from-query", authorization: "Bearer from-header");

        Assert.Equal("from-header", forwarded.Headers.Authorization?.Parameter);
        Assert.DoesNotContain(AccessTokenInQuery.QueryParameter, forwarded.RequestUri!.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Any_other_route_forwards_the_request_untouched()
    {
        var forwarded = await ForwardAsync(Route(tokenInQuery: false), $"/api/v1/admin/orders/live?{AccessTokenInQuery.QueryParameter}=token-1");

        Assert.Null(forwarded.Headers.Authorization);
        Assert.Contains($"{AccessTokenInQuery.QueryParameter}=token-1", forwarded.RequestUri!.Query, StringComparison.Ordinal);
    }
}
