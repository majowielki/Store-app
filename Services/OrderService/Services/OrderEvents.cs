using Store.Contracts.Orders.V1;
using Store.OrderService.Models;

namespace Store.OrderService.Services;

/// <summary>The events that tell other services about an order, built from the order in one place.</summary>
public static class OrderEvents
{
    public static List<OrderItem> Items(Order order)
        => order.Lines.Select(l => new OrderItem(l.ProductId, l.ProductTitle, l.Quantity, l.UnitPrice)).ToList();

    public static OrderPlaced Placed(Order order, bool saveAddress)
        => new(order.Id, order.UserId, order.UserEmail, order.CustomerName, order.DeliveryAddress, saveAddress,
            order.Subtotal, order.DiscountAmount, order.DeliveryFee, order.Total,
            order.Lines.Select(l => new OrderPlacedLine(l.ProductId, l.ProductTitle, l.Quantity, l.UnitPrice)).ToList(),
            order.CreatedAt);

    public static OrderPaid Paid(Order order, DateTime at)
        => new(order.Id, order.UserId, order.UserEmail, order.CustomerName, order.Total, order.CardBrand, order.CardLast4, Items(order),
            order.DeliveryFrom, order.DeliveryTo, at);

    public static OrderShipped Shipped(Order order, DateTime at)
        => new(order.Id, order.UserId, order.UserEmail, order.CustomerName, Items(order), order.DeliveryFrom, order.DeliveryTo, at);

    /// <param name="order">The order</param>
    /// <param name="reason">One of <see cref="OrderCancellationReasons"/></param>
    /// <param name="at">When</param>
    public static OrderCancelled Cancelled(Order order, string reason, DateTime at) => new(order.Id, order.UserId, reason, at);

    public static OrderRefunded Refunded(Order order, decimal amount, DateTime at) => new(order.Id, order.UserId, amount, at);
}
