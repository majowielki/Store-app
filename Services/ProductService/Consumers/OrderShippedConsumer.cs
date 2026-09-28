using MassTransit;
using Store.Contracts.Orders.V1;
using Store.ProductService.Services;

namespace Store.ProductService.Consumers;

/// <summary>Takes the units of a shipped order off the stock; until then they were only held.</summary>
public sealed class OrderShippedConsumer : IConsumer<OrderShipped>
{
    private readonly IStockLedger _stock;

    public OrderShippedConsumer(IStockLedger stock)
    {
        _stock = stock;
    }

    public Task Consume(ConsumeContext<OrderShipped> context)
        => _stock.ShipAsync(context.Message.OrderId, context.CancellationToken);
}
