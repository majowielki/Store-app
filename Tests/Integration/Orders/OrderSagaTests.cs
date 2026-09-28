using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;
using Store.OrderService.Data;
using Store.OrderService.Saga;
using Store.Tests.Integration.TestSupport;
using System.Net.Http.Json;
using Xunit;

namespace Store.Tests.Integration.Orders;

/// <summary>
/// The order saga on the MassTransit test harness, with its real repository in PostgreSQL: every
/// road an order can take after checkout - paid, out of stock, a refused card, the deadline, a
/// refund - driven by the events the stock and the payment webhook publish.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class OrderSagaTests : IClassFixture<OrderApiFactory>
{
    private readonly OrderApiFactory _factory;
    private readonly OrderJourney _journey;

    public OrderSagaTests(OrderApiFactory factory)
    {
        _factory = factory;
        _journey = new OrderJourney(factory);
    }

    private async Task<string> SagaStateAsync(int orderId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        return (await context.OrderStates.AsNoTracking().SingleAsync(s => s.OrderId == orderId)).CurrentState;
    }

    private async Task<decimal> TotalAsync(int orderId) => (await _journey.OrderAsync(orderId)).GetProperty("total").GetDecimal();

    [Fact]
    public async Task Reserved_and_paid_order_is_paid()
    {
        var orderId = await _journey.PlaceAsync("saga-paid", 201, price: 400m);
        Assert.Equal("ReservingStock", await SagaStateAsync(orderId));

        await _journey.PublishAsync(OrderJourney.Reserved(orderId));
        await _journey.WaitForStatusAsync(orderId, "AwaitingPayment");
        var waiting = await _journey.OrderAsync(orderId);
        // The customer has fifteen minutes from the reservation
        var due = waiting.GetProperty("paymentDueAt").GetDateTime();
        Assert.InRange(due - DateTime.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15.5));

        var paymentId = Guid.NewGuid();
        await _journey.PublishAsync(OrderJourney.Accepted(orderId, await TotalAsync(orderId), paymentId));
        await _journey.WaitForStatusAsync(orderId, "Paid");

        Assert.Equal("Paid", await SagaStateAsync(orderId));
        Assert.True(await _journey.ConsumedAsync<OrderPaid>(e => e.OrderId == orderId && e.CardLast4 == "4242" && e.Lines.Single().ProductId == 201));
        // The same payment reported twice changes nothing
        await _journey.PublishAsync(OrderJourney.Accepted(orderId, await TotalAsync(orderId), paymentId));
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Single(_journey.Consumed<OrderPaid>(e => e.OrderId == orderId));
    }

    [Fact]
    public async Task Order_the_stock_cannot_serve_is_cancelled()
    {
        var orderId = await _journey.PlaceAsync("saga-out-of-stock", 202);

        await _journey.PublishAsync(new StockUnavailable(orderId, [new StockShortage(202, "Sold-out chair", 1, 0)], DateTime.UtcNow));

        await _journey.WaitForStatusAsync(orderId, "Cancelled");
        Assert.Equal("out-of-stock", (await _journey.OrderAsync(orderId)).GetProperty("cancellationReason").GetString());
        Assert.True(await _journey.ConsumedAsync<OrderCancelled>(e => e.OrderId == orderId && e.Reason == OrderCancellationReasons.OutOfStock));
    }

    [Fact]
    public async Task Refused_card_leaves_the_order_waiting_until_the_deadline_cancels_it()
    {
        var orderId = await _journey.AwaitingPaymentAsync("saga-declined", 203);

        await _journey.PublishAsync(new PaymentDeclined(Guid.NewGuid(), orderId, "saga-declined", "saga-declined@test.local", "Saga Buyer",
            PaymentDeclineReasons.CardDeclined, DateTime.UtcNow.AddMinutes(15), DateTime.UtcNow));
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal("AwaitingPayment", (await _journey.OrderAsync(orderId)).GetProperty("status").GetString());

        await _journey.PublishAsync(new OrderPaymentTimedOut(orderId, DateTime.UtcNow));

        await _journey.WaitForStatusAsync(orderId, "Cancelled");
        Assert.Equal("payment-timed-out", (await _journey.OrderAsync(orderId)).GetProperty("cancellationReason").GetString());
        Assert.True(await _journey.ConsumedAsync<OrderCancelled>(e => e.OrderId == orderId && e.Reason == OrderCancellationReasons.PaymentTimedOut));
    }

    [Fact]
    public async Task Deadline_job_cancels_an_order_left_unpaid()
    {
        var orderId = await _journey.AwaitingPaymentAsync("saga-deadline", 204);

        // Fifteen minutes later, as far as the job can tell
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            await context.OrderStates.Where(s => s.OrderId == orderId)
                .ExecuteUpdateAsync(s => s.SetProperty(state => state.PaymentDueAt, DateTime.UtcNow.AddSeconds(-1)));
        }

        await _journey.WaitForStatusAsync(orderId, "Cancelled");
        Assert.Equal("Cancelled", await SagaStateAsync(orderId));
    }

    [Fact]
    public async Task Paid_order_the_administrator_cancels_is_refunded()
    {
        var orderId = await _journey.PaidAsync("saga-refund", 205, price: 350m);
        var total = await TotalAsync(orderId);
        using var admin = _factory.CreateClient().AsTrueAdmin();

        var cancelled = await admin.PatchAsJsonAsync($"/api/v1/admin/orders/{orderId}/status", new { status = "Cancelled" });

        cancelled.EnsureSuccessStatusCode();
        Assert.True(await _journey.ConsumedAsync<PaymentRefundRequested>(e => e.OrderId == orderId && e.Reason == RefundReasons.OrderCancelled && e.Amount == total));
        Assert.Equal("Refunding", await SagaStateAsync(orderId));

        await _journey.PublishAsync(new PaymentRefunded(Guid.NewGuid(), orderId, total, DateTime.UtcNow));

        await _journey.WaitForStatusAsync(orderId, "Refunded");
        Assert.True(await _journey.ConsumedAsync<OrderRefunded>(e => e.OrderId == orderId && e.Amount == total));
        Assert.Equal(["Placed", "AwaitingPayment", "Paid", "Cancelled", "Refunded"],
            (await _journey.OrderAsync(orderId)).GetProperty("statusHistory").EnumerateArray().Select(c => c.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task Payment_arriving_after_the_deadline_is_given_back()
    {
        var orderId = await _journey.AwaitingPaymentAsync("saga-late-payment", 206);
        await _journey.PublishAsync(new OrderPaymentTimedOut(orderId, DateTime.UtcNow));
        await _journey.WaitForStatusAsync(orderId, "Cancelled");

        var paymentId = Guid.NewGuid();
        await _journey.PublishAsync(OrderJourney.Accepted(orderId, 130m, paymentId));

        Assert.True(await _journey.ConsumedAsync<PaymentRefundRequested>(e =>
            e.OrderId == orderId && e.PaymentId == paymentId && e.Amount == 130m && e.Reason == RefundReasons.PaidAfterCancellation));
        await _journey.PublishAsync(new PaymentRefunded(paymentId, orderId, 130m, DateTime.UtcNow));
        await _journey.WaitForStatusAsync(orderId, "Refunded");
    }

    [Fact]
    public async Task Payment_racing_a_cancellation_by_the_administrator_is_given_back()
    {
        var orderId = await _journey.AwaitingPaymentAsync("saga-race", 207);
        using var admin = _factory.CreateClient().AsTrueAdmin();

        // The administrator cancels; the payment reaches the saga before the cancellation does
        var cancelled = await admin.PatchAsJsonAsync($"/api/v1/admin/orders/{orderId}/status", new { status = "Cancelled" });
        cancelled.EnsureSuccessStatusCode();
        await _journey.PublishAsync(OrderJourney.Accepted(orderId, await TotalAsync(orderId)));

        Assert.True(await _journey.ConsumedAsync<PaymentRefundRequested>(e => e.OrderId == orderId));
        Assert.Equal("Cancelled", (await _journey.OrderAsync(orderId)).GetProperty("status").GetString());
        Assert.Empty(_journey.Consumed<OrderPaid>(e => e.OrderId == orderId));
    }

    [Fact]
    public async Task Saga_is_found_by_the_order_id()
    {
        Assert.Equal(OrderSagaIds.For(42), OrderSagaIds.For(42));
        Assert.NotEqual(OrderSagaIds.For(42), OrderSagaIds.For(43));

        var orderId = await _journey.PlaceAsync("saga-ids", 208);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        Assert.Equal(OrderSagaIds.For(orderId), (await context.OrderStates.SingleAsync(s => s.OrderId == orderId)).CorrelationId);
    }
}
