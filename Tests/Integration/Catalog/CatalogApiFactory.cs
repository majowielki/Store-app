using MassTransit;
using Store.Contracts.Catalog.V1;
using Store.ProductService.Data;
using Store.Tests.Integration.TestSupport;

namespace Store.Tests.Integration.Catalog;

public sealed class CatalogApiFactory : StoreApiFactory<ProductDbContext>
{
    public CatalogApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    protected override string? DatabaseName => "store_product_test";

    protected override void ConfigureTestBus(IBusRegistrationConfigurator bus)
        => bus.AddConsumer<StockEventProbe>();
}

/// <summary>Receives what the stock publishes from its consumers, so tests can see it as consumed.</summary>
public sealed class StockEventProbe :
    IConsumer<StockReserved>,
    IConsumer<StockUnavailable>,
    IConsumer<StockReleased>,
    IConsumer<ProductBackInStock>
{
    public Task Consume(ConsumeContext<StockReserved> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<StockUnavailable> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<StockReleased> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<ProductBackInStock> context) => Task.CompletedTask;
}
