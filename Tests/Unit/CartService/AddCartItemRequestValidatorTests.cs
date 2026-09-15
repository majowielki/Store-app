using Store.CartService.DTOs.Requests;
using Store.CartService.Validators;
using Xunit;

namespace Store.Tests.Unit.CartService;

public class AddCartItemRequestValidatorTests
{
    private readonly AddCartItemRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ProductId_Is_Zero()
    {
        var model = new AddCartItemRequest { ProductId = 0, Quantity = 1, Color = "Red" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddCartItemRequest.ProductId));
    }

    [Fact]
    public void Should_Have_Error_When_Quantity_Is_Out_Of_Range()
    {
        var model = new AddCartItemRequest { ProductId = 1, Quantity = 0, Color = "Red" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddCartItemRequest.Quantity));
    }

    [Fact]
    public void Should_Have_Error_When_Color_Is_Empty()
    {
        var model = new AddCartItemRequest { ProductId = 1, Quantity = 1, Color = "" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddCartItemRequest.Color));
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Model()
    {
        var model = new AddCartItemRequest { ProductId = 1, Quantity = 2, Color = "Blue" };
        var result = _validator.Validate(model);
        Assert.True(result.IsValid);
    }
}
