using MassTransit;
using Store.BuildingBlocks.Api;
using Store.Contracts.Orders.V1;
using Store.IdentityService.Services;

namespace Store.IdentityService.Consumers;

/// <summary>
/// Stores the delivery address in the customer's profile when the checkout asked for it.
/// The order service no longer calls the identity service with the customer's token for
/// this; the event carries the intent and the inbox keeps the update idempotent.
/// </summary>
public sealed class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly IAuthService _authService;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(IAuthService authService, ILogger<OrderPlacedConsumer> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var order = context.Message;
        if (!order.SaveAddress || string.IsNullOrWhiteSpace(order.DeliveryAddress))
        {
            return;
        }

        try
        {
            await _authService.UpdateAddressAsync(order.UserId, order.DeliveryAddress);
            _logger.LogInformation("Order {OrderId}: delivery address saved to the profile of user {UserId}", order.OrderId, order.UserId);
        }
        catch (NotFoundException)
        {
            // The account is gone; there is nothing to retry. Any other failure propagates and the bus retries.
            _logger.LogWarning("Order {OrderId}: user {UserId} no longer exists, address not saved", order.OrderId, order.UserId);
        }
        catch (ForbiddenException)
        {
            // The shared demo accounts keep their address
            _logger.LogInformation("Order {OrderId}: user {UserId} is a demo account, address not saved", order.OrderId, order.UserId);
        }
    }
}
