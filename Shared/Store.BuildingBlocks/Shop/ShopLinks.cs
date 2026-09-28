using Microsoft.Extensions.Options;
using System.Globalization;

namespace Store.BuildingBlocks.Shop;

/// <summary>
/// The addresses of the shop's pages under <see cref="ShopOptions.Url"/>: the routes of the UI's
/// router (<c>UI/store-app.UI/src/App.tsx</c>), written once for every service that links to them.
/// </summary>
public sealed class ShopLinks
{
    private readonly string _shop;

    public ShopLinks(IOptions<ShopOptions> options)
    {
        _shop = options.Value.Url.TrimEnd('/');
    }

    /// <summary>The home page.</summary>
    public string Home => $"{_shop}/";

    /// <summary>A product's page.</summary>
    public string Product(int productId) => $"{_shop}/products/{Id(productId)}";

    /// <summary>The customer's order page.</summary>
    public string Order(int orderId) => $"{_shop}/orders/{Id(orderId)}";

    /// <summary>The payment page of an order still waiting for its payment.</summary>
    public string PayOrder(int orderId) => $"{Order(orderId)}/pay";

    /// <summary>A maker's page.</summary>
    public string Maker(string slug) => $"{_shop}/makers/{Slug(slug)}";

    /// <summary>A collection's page.</summary>
    public string Collection(string slug) => $"{_shop}/collections/{Slug(slug)}";

    /// <summary>A journal article.</summary>
    public string Article(string slug) => $"{_shop}/journal/{Slug(slug)}";

    /// <summary>A lookbook's page.</summary>
    public string Lookbook(string slug) => $"{_shop}/looks/{Slug(slug)}";

    private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);

    private static string Slug(string slug) => Uri.EscapeDataString(slug);
}
