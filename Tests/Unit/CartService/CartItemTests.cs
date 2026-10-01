using Store.CartService.Models;
using Xunit;

namespace Store.Tests.Unit.CartService;

public sealed class CartItemTests
{
    [Fact]
    public void Line_revision_changes_even_when_the_clock_does_not_advance()
    {
        var now = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var line = new CartItem { UpdatedAt = now };
        line.Touch(now);
        Assert.Equal(now.AddTicks(10), line.UpdatedAt);
        line.Touch(now.AddDays(-1));
        Assert.Equal(now.AddTicks(20), line.UpdatedAt);
    }
}
