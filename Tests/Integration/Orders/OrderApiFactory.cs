using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Serialization;
using Store.Contracts.Cart;
using Store.Contracts.Catalog;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments;
using Store.Contracts.Payments.V1;
using Store.OrderService.Data;
using Store.OrderService.Saga;
using Store.Tests.Integration.TestSupport;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

namespace Store.Tests.Integration.Orders;

/// <summary>
/// OrderService against its real database. The cart and the catalogue are replaced at the HTTP
/// boundary by <see cref="FakeUpstreams"/>; the resilience pipeline, the typed clients and the
/// outbox stay real, with the bus on the in-memory transport.
/// </summary>
public sealed class OrderApiFactory : StoreApiFactory<OrderDbContext>
{
    public OrderApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    public FakeUpstreams Upstreams { get; } = new();

    protected override string? DatabaseName => "store_order_test";

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("Services:CartService", "http://cart.test");
        builder.UseSetting("Services:ProductService", "http://catalog.test");
        builder.UseSetting("Services:PaymentService", "http://payment.test");
        builder.UseSetting("PaymentWebhooks:SigningSecret", TestTokens.WebhookSecret);
        // The deadline job looks every second, so a test sees an overdue order cancelled quickly
        builder.UseSetting("OrderSaga:DeadlineCheckSeconds", "1");
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => Upstreams));
    }

    protected override void ConfigureTestBus(IBusRegistrationConfigurator bus)
    {
        bus.AddConsumer<OrderEventProbe>();
        // The harness keeps sagas in memory unless told otherwise; the order saga must find the row
        // the checkout writes, so it keeps its PostgreSQL repository
        bus.AddSagaStateMachine<OrderStateMachine, OrderState>().EntityFrameworkRepository(repository =>
        {
            repository.ExistingDbContext<OrderDbContext>();
            repository.UsePostgres();
            repository.ConcurrencyMode = ConcurrencyMode.Pessimistic;
        });
    }
}

/// <summary>
/// Plays the cart, the catalogue and the payment service for the order service under test.
/// </summary>
public sealed class FakeUpstreams : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, CartSnapshot> _carts = new();
    private readonly ConcurrentDictionary<int, ProductSnapshot> _products = new();
    private readonly ConcurrentDictionary<int, PaymentSnapshot> _payments = new();

    /// <summary>The Idempotency-Key of every payment the order service opened, in order.</summary>
    public ConcurrentQueue<string> PaymentKeys { get; } = new();

    /// <summary>The payment the order service opened for an order, if any.</summary>
    public PaymentSnapshot? PaymentOf(int orderId) => _payments.GetValueOrDefault(orderId);

    public void AddProduct(int id, decimal effectivePrice, string title = "Fake product", bool isActive = true, int available = 50)
        => _products[id] = new ProductSnapshot(id, title, "https://example.test/fake.jpg", "Modenza", new[] { "black" }, effectivePrice, effectivePrice, isActive, DateTime.UtcNow, available);

    public void SetCart(string userId, params (int ProductId, int Quantity, decimal PriceInCart)[] lines)
        => _carts[userId] = new CartSnapshot(
            userId,
            lines.Select(l => new CartLineSnapshot(l.ProductId, $"Line {l.ProductId}", "https://example.test/fake.jpg", "Modenza", "black", l.PriceInCart, l.Quantity)).ToList(),
            DateTime.UtcNow);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var host = request.RequestUri?.Host ?? string.Empty;
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (host == "cart.test" && request.Method == HttpMethod.Get && path.StartsWith("/api/v1/cart/internal/", StringComparison.Ordinal))
        {
            var userId = Uri.UnescapeDataString(path["/api/v1/cart/internal/".Length..]);
            return Task.FromResult(_carts.TryGetValue(userId, out var cart)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(cart) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        if (host == "catalog.test" && request.Method == HttpMethod.Get
            && path.StartsWith("/api/v1/products/", StringComparison.Ordinal) && path.EndsWith("/snapshot", StringComparison.Ordinal)
            && int.TryParse(path["/api/v1/products/".Length..^"/snapshot".Length], out var productId))
        {
            return Task.FromResult(_products.TryGetValue(productId, out var product)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(product) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        if (host == "payment.test" && request.Method == HttpMethod.Post && path == "/api/v1/payments/internal")
        {
            return OpenPaymentAsync(request, cancellationToken);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }

    private async Task<HttpResponseMessage> OpenPaymentAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = (await request.Content!.ReadFromJsonAsync<CreatePaymentRequest>(cancellationToken))!;
        PaymentKeys.Enqueue(request.Headers.GetValues(IdempotencyKeyHeader.Name).Single());
        var created = false;
        var payment = _payments.GetOrAdd(body.OrderId, orderId =>
        {
            created = true;
            return new PaymentSnapshot(Guid.NewGuid(), orderId, body.Amount, body.Currency, PaymentStatus.RequiresPaymentMethod, DateTime.UtcNow);
        });
        return new HttpResponseMessage(created ? HttpStatusCode.Created : HttpStatusCode.OK) { Content = JsonContent.Create(payment, options: StoreJson.Web) };
    }
}

/// <summary>
/// Receives what the order saga publishes from its consumers (paid, cancelled, refunds, status
/// changes), so tests can see it as consumed.
/// </summary>
public sealed class OrderEventProbe :
    IConsumer<OrderPaid>,
    IConsumer<OrderCancelled>,
    IConsumer<OrderRefunded>,
    IConsumer<OrderStatusChanged>,
    IConsumer<PaymentRefundRequested>
{
    public Task Consume(ConsumeContext<OrderPaid> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<OrderCancelled> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<OrderRefunded> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<OrderStatusChanged> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<PaymentRefundRequested> context) => Task.CompletedTask;
}
