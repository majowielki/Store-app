using Store.Contracts.Catalog;
using Store.ProductService.Data;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.Validators;
using Xunit;

namespace Store.Tests.Unit.ProductService;

/// <summary>
/// The demo catalogue is data typed by hand; these checks keep it consistent with the pictures
/// in Blobs/ and with the rules the API applies to the same fields.
/// </summary>
public class DemoCatalogueTests
{
    private static readonly string Blobs = Path.Combine(RepositoryRoot(), "Blobs");

    public static TheoryData<string> Titles()
    {
        var titles = new TheoryData<string>();
        foreach (var product in DemoCatalogue.Products())
        {
            titles.Add(product.Title);
        }

        return titles;
    }

    [Theory]
    [MemberData(nameof(Titles))]
    public void Every_product_has_its_main_picture_and_the_two_gallery_shots_in_Blobs(string title)
    {
        var product = DemoCatalogue.Products().Single(p => p.Title == title);
        var picture = Path.GetFileNameWithoutExtension(new Uri(product.Image).Segments[^1]);

        Assert.EndsWith("-1", picture, StringComparison.Ordinal);
        var name = picture[..^2];
        foreach (var shot in new[] { "-1", "-2", "-3" })
        {
            Assert.True(File.Exists(Path.Combine(Blobs, name + shot + ".webp")), $"Blobs/{name}{shot}.webp is missing");
        }
    }

    [Theory]
    [MemberData(nameof(Titles))]
    public void Every_product_passes_the_rules_of_the_create_endpoint(string title)
    {
        var product = DemoCatalogue.Products().Single(p => p.Title == title);
        var request = new CreateProductRequest
        {
            Title = product.Title,
            Description = product.Description,
            Price = product.Price,
            SalePrice = product.SalePrice,
            DiscountPercent = product.DiscountPercent,
            Category = product.Category,
            Company = product.Company,
            NewArrival = product.NewArrival,
            Image = product.Image,
            Colors = product.Colors,
            Groups = product.Groups,
            WidthCm = product.WidthCm,
            HeightCm = product.HeightCm,
            DepthCm = product.DepthCm,
            WeightKg = product.WeightKg,
            Materials = product.Materials
        };

        var result = new CreateProductRequestValidator().Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.True(product.SalePrice is null || product.SalePrice < product.Price, "a sale price must be lower than the list price");
        Assert.All(product.Colors, color => Assert.True(Enum.TryParse<Color>(color, out _), $"{color} is not a colour the shop filters by"));
    }

    [Fact]
    public void Titles_are_unique_and_no_retired_product_comes_back()
    {
        var titles = DemoCatalogue.Products().Select(p => p.Title).ToList();

        Assert.Equal(titles.Count, titles.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Empty(titles.Intersect(DemoCatalogue.RetiredTitles, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Every_room_of_the_shop_offers_at_least_eight_products()
    {
        var products = DemoCatalogue.Products();
        var rooms = Enum.GetValues<Group>().Where(g => g != Group.All).Select(g => g.ToString().ToLowerInvariant());

        Assert.All(rooms, room => Assert.True(
            products.Count(p => p.Groups.Contains(room)) >= 8,
            $"{room} has only {products.Count(p => p.Groups.Contains(room))} products"));
        Assert.All(products, p => Assert.All(p.Groups, group => Assert.Contains(group, rooms)));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Store.Microservices.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found above " + AppContext.BaseDirectory);
    }
}
