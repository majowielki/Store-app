using MassTransit;
using Store.Contracts.Orders.V1;
using Store.ProductService.Services;

namespace Store.ProductService.Consumers;

/// <summary>Gives back the units a cancelled order held, whatever the reason it was cancelled for.</summary>
public sealed class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly IStockLedger _stock;

    public OrderCancelledConsumer(IStockLedger stock)
    {
        _stock = stock;
    }

    public Task Consume(ConsumeContext<OrderCancelled> context)
        => _stock.ReleaseAsync(context.Message.OrderId, context.CancellationToken);
}
