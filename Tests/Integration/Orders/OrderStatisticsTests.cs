using Microsoft.Extensions.DependencyInjection;
using Store.OrderService.Data;
using Store.OrderService.Models;
using Store.OrderService.Services;
using Store.Tests.Integration.TestSupport;
using System.Net;
using Xunit;

namespace Store.Tests.Integration.Orders;

[Collection(PostgresTests.Name)]
public sealed class OrderStatisticsTests(OrderApiFactory factory) : IClassFixture<OrderApiFactory>
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task Unbounded_reporting_windows_are_validation_errors(int days)
    {
        using var admin = factory.CreateClient().AsTrueAdmin();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"/api/v1/admin/orders/stats?days={days}")).StatusCode);
    }

    [Fact]
    public async Task Placed_value_includes_unpaid_cancelled_and_refunded_orders_and_gross_lines()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var statistics = new OrderStatistics(db, TimeProvider.System);
        var before = await statistics.GetOrderStatsAsync();
        foreach (var status in new[] { OrderStatus.AwaitingPayment, OrderStatus.Cancelled, OrderStatus.Refunded })
        {
            db.Orders.Add(new Order
            {
                UserId = "statistics-semantics",
                UserEmail = "statistics@test.local",
                CustomerName = "Test",
                Total = 9960m,
                Subtotal = 10000m,
                DiscountAmount = 50m,
                DeliveryFee = 10m,
                Status = status,
                Lines = [new OrderLine { ProductId = 990001, ProductTitle = "Statistics fixture", UnitPrice = 1m, Quantity = 10000 }]
            });
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var after = await statistics.GetOrderStatsAsync();
        Assert.Equal(before.TotalOrders + 3, after.TotalOrders);
        Assert.Equal(before.TotalRevenue + 29880m, after.TotalRevenue);
        var product = Assert.Single(after.TopProducts, p => p.ProductId == 990001);
        Assert.Equal(30000m, product.Revenue);
        Assert.Equal(30000, product.Quantity);
    }
}
