using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
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

    [Fact]
    public void Gallery_holds_at_most_twelve_pictures_each_with_a_url_and_a_description()
    {
        CreateProductRequest Model(IEnumerable<ProductImageDto> images) => new()
        {
            Title = "Title", Description = new string('a', 20), Price = 10, Image = "http://img.com", Colors = ["Red"], Images = images.ToList()
        };

        var picture = new ProductImageDto { Url = "https://img.test/a.webp", Alt = "A detail" };
        Assert.True(_validator.Validate(Model(Enumerable.Repeat(picture, 12))).IsValid);
        Assert.False(_validator.Validate(Model(Enumerable.Repeat(picture, 13))).IsValid);
        Assert.False(_validator.Validate(Model([new ProductImageDto { Url = "not a url", Alt = "A detail" }])).IsValid);
        Assert.False(_validator.Validate(Model([new ProductImageDto { Url = "https://img.test/a.webp", Alt = "" }])).IsValid);
    }

    [Theory]
    [InlineData(50, 50, "oak-bath-stool", true)]
    [InlineData(0, 100, "8-drawer-dresser", true)]
    [InlineData(101, 50, "oak-bath-stool", false)]
    [InlineData(50, -1, "oak-bath-stool", false)]
    [InlineData(50, 50, "Oak Bath Stool", false)]
    [InlineData(50, 50, "", false)]
    public void A_point_stays_on_the_picture_and_names_a_product_by_slug(decimal x, decimal y, string slug, bool valid)
    {
        var model = new CreateProductRequest
        {
            Title = "Title", Description = new string('a', 20), Price = 10, Image = "http://img.com", Colors = ["Red"],
            Hotspots = [new ProductHotspotDto { X = x, Y = y, ProductSlug = slug }]
        };

        Assert.Equal(valid, _validator.Validate(model).IsValid);
    }
}
