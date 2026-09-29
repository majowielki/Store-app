using MassTransit;
using Microsoft.AspNetCore.Http.Connections;

namespace Store.OrderService.Live;

/// <summary>Registration of the admin panel's live feed of orders (<see cref="LiveOrdersHub"/>).</summary>
public static class LiveOrdersExtensions
{
    /// <summary>Names this instance's queue of order events; a new one on every start.</summary>
    private static readonly string InstanceId = Guid.NewGuid().ToString("N")[..12];

    public static IServiceCollection AddLiveOrders(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }

    /// <summary>
    /// The relay listens on a queue of this instance's own, which the broker removes when the
    /// instance stops: every instance hears every order event (a shared queue would hand each
    /// event to one of them), and nothing piles up for an instance that is gone. The broker is the
    /// only backplane the hub needs.
    /// </summary>
    public static IBusRegistrationConfigurator AddLiveOrdersRelay(this IBusRegistrationConfigurator bus)
    {
        bus.AddConsumer<LiveOrdersRelay>().Endpoint(endpoint =>
        {
            endpoint.InstanceId = InstanceId;
            endpoint.Temporary = true;
        });
        return bus;
    }

    public static IEndpointConventionBuilder MapLiveOrders(this IEndpointRouteBuilder endpoints)
        => endpoints.MapHub<LiveOrdersHub>(LiveOrdersHub.Path, options =>
        {
            // WebSockets alone, without the negotiate request: its connection id would tie the
            // second request to the instance that answered the first
            options.Transports = HttpTransportType.WebSockets;
            // The connection ends when the access token it was opened with expires; the panel
            // reconnects with a fresh one
            options.CloseOnAuthenticationExpiration = true;
        });
}
