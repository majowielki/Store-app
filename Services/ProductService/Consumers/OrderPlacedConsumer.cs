using MassTransit;
using Store.Contracts.Orders.V1;
using Store.ProductService.Services;

namespace Store.ProductService.Consumers;

/// <summary>
/// Reserves the stock of a placed order: every line or none. The answer (reserved or
/// unavailable) goes to the order saga through the outbox with the reservation itself.
/// </summary>
public sealed class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly IStockLedger _stock;

    public OrderPlacedConsumer(IStockLedger stock)
    {
        _stock = stock;
    }

    public Task Consume(ConsumeContext<OrderPlaced> context)
        => _stock.ReserveAsync(context.Message, context.CancellationToken);
}
