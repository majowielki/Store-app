using MassTransit;
using Store.CartService.Services;
using Store.Contracts.Orders.V1;

namespace Store.CartService.Consumers;

/// <summary>
/// Empties the cart an order was placed from. Runs inside the inbox, so a redelivered event
/// is ignored; a cart that was already emptied (or never existed) is not an error either.
/// </summary>
public sealed class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly ICartService _cartService;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(ICartService cartService, ILogger<OrderPlacedConsumer> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var order = context.Message;
        var removed = await _cartService.ClearAfterOrderAsync(order.UserId, order.OrderId);
        _logger.LogInformation("Order {OrderId} placed: removed {Lines} lines from the cart of user {UserId}",
            order.OrderId, removed, order.UserId);
    }
}
