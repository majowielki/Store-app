using Store.OrderService.DTOs.Requests;
using Store.OrderService.Validators;
using Xunit;

namespace Store.Tests.Unit.OrderService;

public class UpdateOrderStatusRequestValidatorTests
{
    private readonly UpdateOrderStatusRequestValidator _validator = new();

    [Theory]
    [InlineData("Paid")]
    [InlineData("Shipped")]
    [InlineData("Cancelled")]
    public void A_status_name_is_valid(string status)
    {
        Assert.True(_validator.Validate(new UpdateOrderStatusRequest { Status = status }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Delivered")]
    [InlineData("paid")]
    [InlineData("1")]
    public void Anything_else_is_refused(string status)
    {
        var result = _validator.Validate(new UpdateOrderStatusRequest { Status = status });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateOrderStatusRequest.Status));
    }
}
