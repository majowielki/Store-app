using Store.OrderService.DTOs.Requests;
using Store.OrderService.Validators;
using Xunit;

namespace Store.Tests.Unit.OrderService;

public class CreateOrderFromCartRequestValidatorTests
{
    private readonly CreateOrderFromCartRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_CustomerName_Too_Short()
    {
        var model = new CreateOrderFromCartRequest { UserId = "1", UserEmail = "a@b.com", CustomerName = "J" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderFromCartRequest.CustomerName));
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Model()
    {
        var model = new CreateOrderFromCartRequest { UserId = "1", UserEmail = "a@b.com", CustomerName = "John Doe" };
        var result = _validator.Validate(model);
        Assert.True(result.IsValid);
    }
}
