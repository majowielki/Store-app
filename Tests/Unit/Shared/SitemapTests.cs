using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Shop;
using System.Xml.Linq;
using Xunit;

namespace Store.Tests.Unit.Shared;

/// <summary>
/// The sitemaps the catalogue and the content service answer with, and the shop addresses in
/// them: the format search engines read (sitemaps.org), with the UI's routes.
/// </summary>
public class SitemapTests
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static readonly ShopLinks Links = new(Options.Create(new ShopOptions { Url = "https://shop.example/" }));

    [Fact]
    public void A_sitemap_lists_each_page_with_the_day_it_changed()
    {
        var xml = Sitemap.Render(
        [
            new SitemapEntry(Links.Product(7), new DateTime(2026, 9, 28, 23, 59, 0, DateTimeKind.Utc)),
            new SitemapEntry(Links.Home)
        ]);

        var document = XDocument.Parse(xml);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml);
        Assert.Equal(Ns + "urlset", document.Root!.Name);
        var urls = document.Root.Elements(Ns + "url").ToList();
        Assert.Equal(["https://shop.example/products/7", "https://shop.example/"], urls.Select(u => u.Element(Ns + "loc")!.Value));
        Assert.Equal("2026-09-28", urls[0].Element(Ns + "lastmod")!.Value);
        Assert.Null(urls[1].Element(Ns + "lastmod"));
    }

    [Fact]
    public void Addresses_are_escaped_for_the_xml_and_the_url()
    {
        var xml = Sitemap.Render([new SitemapEntry(Links.Article("oak & linen")), new SitemapEntry("https://shop.example/products?room=living&sale=on")]);

        Assert.Contains("<loc>https://shop.example/journal/oak%20%26%20linen</loc>", xml);
        Assert.Contains("room=living&amp;sale=on", xml);
    }

    [Fact]
    public void One_sitemap_holds_at_most_fifty_thousand_pages()
    {
        var pages = Enumerable.Range(1, Sitemap.MaxEntries + 1).Select(id => new SitemapEntry(Links.Product(id)));

        Assert.Throws<ArgumentOutOfRangeException>(() => Sitemap.Render(pages));
    }

    [Fact]
    public void The_links_follow_the_routes_of_the_shop()
    {
        Assert.Equal("https://shop.example/", Links.Home);
        Assert.Equal("https://shop.example/orders/12/pay", Links.PayOrder(12));
        Assert.Equal("https://shop.example/makers/oak-and-iron", Links.Maker("oak-and-iron"));
        Assert.Equal("https://shop.example/collections/warm-minimal", Links.Collection("warm-minimal"));
        Assert.Equal("https://shop.example/journal/caring-for-oak", Links.Article("caring-for-oak"));
        Assert.Equal("https://shop.example/looks/slow-sunday", Links.Lookbook("slow-sunday"));
    }
}
