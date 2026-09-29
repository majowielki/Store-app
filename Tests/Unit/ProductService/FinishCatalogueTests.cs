using Store.CartService.DTOs.Requests;
using Store.CartService.Validators;
using Store.Contracts.Catalog;
using Store.OrderService.Models;
using Store.ProductService.Data;
using Store.ProductService.Models;
using Store.ProductService.Services;
using Store.Tests.Unit.TestSupport;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Unit.ProductService;

/// <summary>
/// The finishes are data typed by hand; these checks keep them usable as keys everywhere a colour
/// goes and in step with the swatch pictures the shop draws them with.
/// </summary>
public class FinishCatalogueTests
{
    private static readonly string Swatches = Repository.PathTo("UI", "store-app.UI", "src", "assets", "swatches");

    [Fact]
    public void Every_finish_is_listed_once_under_its_own_key()
    {
        var declared = typeof(FinishCatalogue)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(Finish))
            .Select(field => (Finish)field.GetValue(null)!)
            .ToList();

        Assert.Equal(declared.Count, FinishCatalogue.All.Count);
        Assert.All(declared, finish => Assert.Contains(finish, FinishCatalogue.All));
        Assert.Equal(FinishCatalogue.All.Count, FinishCatalogue.All.Select(finish => finish.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(FinishCatalogue.All, finish => Assert.Same(finish, FinishCatalogue.Find(finish.Key)));
    }

    // Carts and orders keep the colour a customer chose; a key must fit where they keep it
    [Fact]
    public void Keys_are_lowercase_words_that_fit_a_cart_line_and_an_order_line()
    {
        var cartLine = new AddCartItemRequestValidator();
        Assert.All(FinishCatalogue.All, finish =>
        {
            Assert.Matches(ProductConstraints.SlugPattern, finish.Key);
            Assert.True(cartLine.Validate(new AddCartItemRequest { ProductId = 1, Color = finish.Key }).IsValid, $"{finish.Key} does not fit a cart line");
            Assert.InRange(finish.Key.Length, 1, OrderConstraints.ColorMaxLength);
            Assert.False(string.IsNullOrWhiteSpace(finish.Name));
        });
    }

    // The colour filter reads a family's key as the whole family, so no finish may be called like one
    [Fact]
    public void No_key_is_spelled_like_a_colour_family()
    {
        var families = Enum.GetValues<Color>().Select(ProductFilters.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain(FinishCatalogue.All, finish => families.Contains(finish.Key));
        Assert.DoesNotContain(FinishCatalogue.All, finish => finish.Family == Color.All);
    }

    [Fact]
    public void A_swatch_has_one_or_two_parts_each_a_colour_and_maybe_a_surface()
    {
        Assert.All(FinishCatalogue.All, finish =>
        {
            Assert.InRange(finish.Swatch.Count, 1, 2);
            Assert.All(finish.Swatch, part => Assert.Matches("^#[0-9a-f]{6}$", part.Color));
        });
    }

    // Scripts/make-swatches.cs writes one picture per surface; the UI imports them by these names
    [Fact]
    public void Every_surface_has_its_picture_and_some_finish_shows_it()
    {
        var shown = FinishCatalogue.All.SelectMany(finish => finish.Swatch).Select(part => part.Texture).OfType<SwatchTexture>().ToHashSet();

        Assert.All(Enum.GetValues<SwatchTexture>(), texture =>
        {
            var file = JsonNamingPolicy.KebabCaseLower.ConvertName(texture.ToString()) + ".webp";
            Assert.True(File.Exists(Path.Combine(Swatches, file)), $"{file} is missing from the swatch pictures");
            Assert.Contains(texture, shown);
        });
    }

    [Fact]
    public void Every_demo_product_is_sold_in_known_finishes()
    {
        Assert.All(DemoCatalogue.Products(DemoPictures.Links), product =>
        {
            Assert.NotEmpty(product.Colors);
            Assert.All(product.Colors, key => Assert.True(FinishCatalogue.Exists(key), $"{product.Title}: {key} is not a finish"));
        });
    }

    [Fact]
    public void A_family_lists_its_finishes()
    {
        Assert.Contains(FinishCatalogue.NaturalOak, FinishCatalogue.OfFamily(Color.Brown));
        Assert.Contains(FinishCatalogue.CreamRust, FinishCatalogue.OfFamily(Color.Orange));
        Assert.DoesNotContain(FinishCatalogue.NaturalOak, FinishCatalogue.OfFamily(Color.White));
        Assert.Empty(FinishCatalogue.OfFamily(Color.Purple));
        Assert.Null(FinishCatalogue.Find("Natural-Oak"));
        Assert.False(FinishCatalogue.Exists(null));
    }
}
