using Store.OrderService.Models;
using Xunit;

namespace Store.Tests.Unit.OrderService;

public class PricingPolicyTests
{
    private static readonly PricingOptions Rules = new();

    [Fact]
    public void First_order_gets_the_discount_and_delivery_is_judged_on_the_subtotal()
    {
        // The example from the audit: 320 in the cart, first order. The cart page showed
        // 0 delivery (320 >= 299) and 256 to pay; the server used to charge 10 more.
        var totals = PricingPolicy.Calculate(320m, isFirstOrder: true, Rules);

        Assert.Equal(320m, totals.Subtotal);
        Assert.Equal(64m, totals.DiscountAmount);
        Assert.Equal(PricingPolicy.FirstOrderDiscountReason, totals.DiscountReason);
        Assert.Equal(0m, totals.DeliveryFee);
        Assert.Equal(256m, totals.Total);
    }

    [Fact]
    public void Later_orders_pay_full_price()
    {
        var totals = PricingPolicy.Calculate(320m, isFirstOrder: false, Rules);

        Assert.Equal(0m, totals.DiscountAmount);
        Assert.Null(totals.DiscountReason);
        Assert.Equal(320m, totals.Total);
    }

    [Fact]
    public void Small_orders_pay_for_delivery()
    {
        var totals = PricingPolicy.Calculate(100m, isFirstOrder: false, Rules);

        Assert.Equal(10m, totals.DeliveryFee);
        Assert.Equal(110m, totals.Total);
    }

    [Fact]
    public void Discount_is_rounded_to_cents()
    {
        var totals = PricingPolicy.Calculate(33.33m, isFirstOrder: true, Rules);

        Assert.Equal(6.67m, totals.DiscountAmount);
        Assert.Equal(33.33m - 6.67m + 10m, totals.Total);
    }

    [Fact]
    public void Threshold_and_rates_come_from_options()
    {
        var rules = new PricingOptions { FreeDeliveryThreshold = 50m, DeliveryFee = 7m, FirstOrderDiscountPercent = 10m };

        var totals = PricingPolicy.Calculate(40m, isFirstOrder: true, rules);

        Assert.Equal(4m, totals.DiscountAmount);
        Assert.Equal(7m, totals.DeliveryFee);
        Assert.Equal(43m, totals.Total);
    }
}
