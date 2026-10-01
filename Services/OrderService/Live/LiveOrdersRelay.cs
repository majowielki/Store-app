using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Store.Contracts.Orders.V1;
using Store.OrderService.Models;

namespace Store.OrderService.Live;

/// <summary>
/// Passes the order events on to the admin panels connected to this instance. The events leave
/// the outbox only once the change is committed, so a panel told about an order finds it; and
/// every instance listens on a queue of its own (<see cref="LiveOrdersExtensions"/>), so a panel
/// hears every change whichever instance made it and whichever one it is connected to.
/// </summary>
public sealed class LiveOrdersRelay : IConsumer<OrderPlaced>, IConsumer<OrderStatusChanged>
{
    private readonly IHubContext<LiveOrdersHub, ILiveOrdersClient> _hub;

    public LiveOrdersRelay(IHubContext<LiveOrdersHub, ILiveOrdersClient> hub)
    {
        _hub = hub;
    }

    public Task Consume(ConsumeContext<OrderPlaced> context)
        => _hub.Clients.All.OrderChanged(new LiveOrderUpdate(context.Message.OrderId, nameof(OrderStatus.Placed), context.Message.PlacedAt));

    public Task Consume(ConsumeContext<OrderStatusChanged> context)
        => _hub.Clients.All.OrderChanged(new LiveOrderUpdate(context.Message.OrderId, context.Message.Status, context.Message.ChangedAt));
}
