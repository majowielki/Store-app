using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Store.GatewayService.Security;

/// <summary>
/// A browser cannot put a header on a WebSocket request, so the SignalR client sends the access
/// token in the query string instead. The gateway accepts it there only on the routes whose
/// metadata allow it (<see cref="MetadataKey"/>), and moves it into the Authorization header before
/// forwarding: the service reads the header as it does on any request, and the token is left out
/// of the forwarded address and of the log lines that print it.
/// </summary>
public static class AccessTokenInQuery
{
    /// <summary>Route metadata: "true" lets the route's requests carry the access token in the query.</summary>
    public const string MetadataKey = "AccessTokenInQuery";

    /// <summary>The query parameter the SignalR client puts the token in.</summary>
    public const string QueryParameter = "access_token";

    /// <summary>
    /// JwtBearer's <see cref="JwtBearerEvents.OnMessageReceived"/>: on a route that allows it, a
    /// request without an Authorization header is authenticated with the token from the query.
    /// </summary>
    public static Task ReadFromQuery(MessageReceivedContext context)
    {
        var route = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<RouteModel>()?.Config;
        if (AllowedOn(route)
            && string.IsNullOrEmpty(context.Request.Headers.Authorization)
            && context.Request.Query.TryGetValue(QueryParameter, out var token))
        {
            context.Token = token.ToString();
        }

        return Task.CompletedTask;
    }

    /// <summary>YARP transforms of a route that allows it: the token leaves the query for the Authorization header.</summary>
    public static void Apply(TransformBuilderContext context)
    {
        if (!AllowedOn(context.Route))
        {
            return;
        }

        context.AddQueryRemoveKey(QueryParameter);
        context.AddRequestTransform(transform =>
        {
            if (transform.ProxyRequest.Headers.Authorization is null
                && transform.HttpContext.Request.Query.TryGetValue(QueryParameter, out var token))
            {
                transform.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token.ToString());
            }

            return ValueTask.CompletedTask;
        });
    }

    private static bool AllowedOn(RouteConfig? route)
        => route?.Metadata is { } metadata
            && metadata.TryGetValue(MetadataKey, out var value)
            && bool.TryParse(value, out var allowed)
            && allowed;
}
