using Store.OrderService.DTOs.Requests;
using Store.OrderService.Validators;
using Xunit;

namespace Store.Tests.Unit.OrderService;

public class UpdateOrderStatusRequestValidatorTests
{
    private readonly UpdateOrderStatusRequestValidator _validator = new();

    [Theory]
    [InlineData("Shipped")]
    [InlineData("Cancelled")]
    public void The_administrator_ships_and_cancels(string status)
    {
        Assert.True(_validator.Validate(new UpdateOrderStatusRequest { Status = status }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Delivered")]
    [InlineData("shipped")]
    [InlineData("1")]
    // Payments move an order on their own
    [InlineData("Paid")]
    [InlineData("AwaitingPayment")]
    [InlineData("Refunded")]
    [InlineData("Placed")]
    public void Anything_else_is_refused(string status)
    {
        var result = _validator.Validate(new UpdateOrderStatusRequest { Status = status });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateOrderStatusRequest.Status));
    }
}
