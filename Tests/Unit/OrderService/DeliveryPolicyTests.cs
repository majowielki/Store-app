using Store.OrderService.Models;
using Xunit;

namespace Store.Tests.Unit.OrderService;

public class DeliveryPolicyTests
{
    private static readonly DeliveryOptions Rules = new();

    // The warehouse is two hours ahead of UTC in late September and October 2026 (summer time until Oct 25)
    private static DeliveryWindow At(string utc) => DeliveryPolicy.Estimate(DateTimeOffset.Parse(utc), Rules);

    private static DeliveryWindow Window(string from, string to) => new(DateOnly.Parse(from), DateOnly.Parse(to));

    [Fact]
    public void An_order_on_a_weekday_morning_leaves_the_next_day()
    {
        // Monday 10:00: packed Monday, leaves Tuesday, 2 to 4 business days on the road
        Assert.Equal(Window("2026-10-01", "2026-10-05"), At("2026-09-28T08:00:00Z"));
    }

    [Fact]
    public void The_cut_off_hour_moves_the_order_to_the_next_business_day()
    {
        Assert.Equal(Window("2026-10-01", "2026-10-05"), At("2026-09-28T11:59:00Z")); // 13:59
        Assert.Equal(Window("2026-10-02", "2026-10-06"), At("2026-09-28T12:00:00Z")); // 14:00
    }

    [Fact]
    public void The_cut_off_is_the_warehouses_local_hour_not_utc()
    {
        // 12:30 UTC is before 14:00 in UTC but 14:30 in the warehouse
        Assert.Equal(Window("2026-10-02", "2026-10-06"), At("2026-09-28T12:30:00Z"));
        // After the clocks go back (Oct 25) it is one hour ahead: 12:30 UTC is 13:30 there
        Assert.Equal(Window("2026-10-29", "2026-11-02"), At("2026-10-26T12:30:00Z"));
    }

    [Theory]
    [InlineData("2026-10-02T15:00:00Z")] // Friday 17:00
    [InlineData("2026-10-03T08:00:00Z")] // Saturday
    [InlineData("2026-10-04T20:00:00Z")] // Sunday 22:00
    public void Friday_afternoon_and_the_weekend_count_as_monday(string utc)
    {
        // Packed Monday Oct 5, leaves Tuesday, arrives Thursday to the next Monday
        Assert.Equal(Window("2026-10-08", "2026-10-12"), At(utc));
    }

    [Fact]
    public void Transit_never_ends_on_a_weekend()
    {
        // Wednesday morning: leaves Thursday, 2 business days is Monday, 4 is Wednesday
        Assert.Equal(Window("2026-10-05", "2026-10-07"), At("2026-09-30T07:00:00Z"));
    }

    [Fact]
    public void Times_come_from_the_options()
    {
        var rules = new DeliveryOptions { CutoffHour = 9, TransitDaysMin = 1, TransitDaysMax = 1 };

        // Monday 10:00 is after a 9:00 cut-off: packed Tuesday, leaves Wednesday, arrives Thursday
        var window = DeliveryPolicy.Estimate(DateTimeOffset.Parse("2026-09-28T08:00:00Z"), rules);

        Assert.Equal(Window("2026-10-01", "2026-10-01"), window);
    }

    [Theory]
    [InlineData("2026-03-29T00:59:00Z", 1)] // the last Sunday of March, just before 02:00
    [InlineData("2026-03-29T01:00:00Z", 2)] // summer time from 02:00, which becomes 03:00
    [InlineData("2026-10-25T00:59:00Z", 2)] // the last Sunday of October, just before 03:00
    [InlineData("2026-10-25T01:00:00Z", 1)] // back to winter time
    public void The_warehouse_clock_follows_the_european_summer_time(string utc, int hoursAhead)
    {
        Assert.Equal(TimeSpan.FromHours(hoursAhead), DeliveryPolicy.WarehouseTime.GetUtcOffset(DateTimeOffset.Parse(utc)));
    }
}

public class OrderStatusFlowTests
{
    [Theory]
    [InlineData(OrderStatus.Placed, OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.Placed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.AwaitingPayment, OrderStatus.Paid)]
    [InlineData(OrderStatus.AwaitingPayment, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Refunded)]
    public void An_order_moves_forward_or_is_cancelled_before_it_ships(OrderStatus from, OrderStatus to)
    {
        Assert.True(OrderStatusFlow.CanMove(from, to));
    }

    [Theory]
    [InlineData(OrderStatus.Placed, OrderStatus.Paid)]
    [InlineData(OrderStatus.Placed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Placed, OrderStatus.Placed)]
    [InlineData(OrderStatus.AwaitingPayment, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Paid, OrderStatus.Placed)]
    [InlineData(OrderStatus.Paid, OrderStatus.Refunded)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid)]
    [InlineData(OrderStatus.Refunded, OrderStatus.Cancelled)]
    public void Skipping_a_step_going_back_or_leaving_a_final_status_is_refused(OrderStatus from, OrderStatus to)
    {
        Assert.False(OrderStatusFlow.CanMove(from, to));
    }

    [Fact]
    public void Shipped_and_refunded_are_final()
    {
        Assert.Empty(OrderStatusFlow.NextFrom(OrderStatus.Shipped));
        Assert.Empty(OrderStatusFlow.NextFrom(OrderStatus.Refunded));
        Assert.Equal([OrderStatus.AwaitingPayment, OrderStatus.Cancelled], OrderStatusFlow.NextFrom(OrderStatus.Placed));
    }

    [Theory]
    [InlineData(OrderStatus.Placed, new[] { OrderStatus.Cancelled })]
    [InlineData(OrderStatus.AwaitingPayment, new[] { OrderStatus.Cancelled })]
    [InlineData(OrderStatus.Paid, new[] { OrderStatus.Shipped, OrderStatus.Cancelled })]
    [InlineData(OrderStatus.Shipped, new OrderStatus[0])]
    [InlineData(OrderStatus.Cancelled, new OrderStatus[0])]
    [InlineData(OrderStatus.Refunded, new OrderStatus[0])]
    public void The_administrator_only_ships_a_paid_order_and_cancels_one_not_shipped(OrderStatus from, OrderStatus[] moves)
    {
        Assert.Equal(moves, OrderStatusFlow.AdministratorMovesFrom(from));
    }
}
