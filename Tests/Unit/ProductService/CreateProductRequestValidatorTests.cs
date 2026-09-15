using Store.ProductService.DTOs.Requests;
using Store.ProductService.Validators;
using Xunit;

namespace Store.Tests.Unit.ProductService;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var model = new CreateProductRequest { Title = "", Description = "desc", Price = 1, Category = 0, Company = 0, Image = "http://img", Colors = new List<string> { "Red" } };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.Title));
    }

    [Fact]
    public void Should_Have_Error_When_Description_Too_Short()
    {
        var model = new CreateProductRequest { Title = "Title", Description = "short", Price = 1, Category = 0, Company = 0, Image = "http://img", Colors = new List<string> { "Red" } };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.Description));
    }

    [Fact]
    public void Should_Have_Error_When_Colors_Empty()
    {
        var model = new CreateProductRequest { Title = "Title", Description = "desc is long enough", Price = 1, Category = 0, Company = 0, Image = "http://img", Colors = new List<string>() };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.Colors));
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Model()
    {
        var model = new CreateProductRequest { Title = "Title", Description = new string('a', 20), Price = 10, Category = 0, Company = 0, Image = "http://img.com", Colors = new List<string> { "Red" } };
        var result = _validator.Validate(model);
        Assert.True(result.IsValid);
    }
}
