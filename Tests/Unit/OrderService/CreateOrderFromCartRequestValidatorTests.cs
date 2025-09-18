using FluentValidation;
using Xunit;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.Validators;

namespace Store.Tests.Unit.OrderService;

public class CreateOrderFromCartRequestValidatorTests
{
    private readonly CreateOrderFromCartRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_UserId_Is_Empty()
    {
        var model = new CreateOrderFromCartRequest { UserId = "", UserEmail = "a@b.com", CustomerName = "John" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderFromCartRequest.UserId));
    }

    [Fact]
    public void Should_Have_Error_When_UserEmail_Is_Invalid()
    {
        var model = new CreateOrderFromCartRequest { UserId = "1", UserEmail = "not-an-email", CustomerName = "John" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderFromCartRequest.UserEmail));
    }

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
