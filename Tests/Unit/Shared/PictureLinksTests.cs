using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Pictures;
using Store.ContentService.Data;
using Store.ProductService.Data;
using Store.Tests.Unit.TestSupport;
using Xunit;

namespace Store.Tests.Unit.Shared;

/// <summary>
/// The demo data points its pictures at the container <c>Pictures:BaseUrl</c> names: Azurite's in
/// the local stack, the public pictures account in Azure.
/// </summary>
public class PictureLinksTests
{
    [Theory]
    [InlineData("https://storepx1.blob.core.windows.net/product-images/")]
    [InlineData("https://storepx1.blob.core.windows.net/product-images")]
    public void A_picture_is_addressed_by_its_file_name_in_the_container_with_or_without_the_trailing_slash(string baseUrl)
    {
        var links = new PictureLinks(Options.Create(new PictureOptions { BaseUrl = baseUrl }));

        Assert.Equal("https://storepx1.blob.core.windows.net/product-images/Maker-Modenza.webp", links.Of("Maker-Modenza.webp"));
    }

    [Fact]
    public void Every_picture_of_the_demo_data_is_in_the_configured_container()
    {
        const string container = "https://storepx1.blob.core.windows.net/product-images/";
        var links = new PictureLinks(Options.Create(new PictureOptions { BaseUrl = container }));

        var pictures = DemoCatalogue.Products(links).SelectMany(p => p.Images.Select(i => i.Url).Prepend(p.Image))
            .Concat(DemoContent.Makers(links).Select(m => m.CoverImage))
            .Concat(DemoContent.Collections(links).Select(c => c.CoverImage))
            .Concat(DemoContent.Articles(links).Select(a => a.CoverImage))
            .Concat(DemoContent.Lookbooks(links).Select(l => l.Image))
            .ToList();

        Assert.NotEmpty(pictures);
        Assert.All(pictures, url => Assert.StartsWith(container, url));
        Assert.DoesNotContain(pictures, url => url.Contains(DemoPictures.BaseUrl, StringComparison.Ordinal));
    }
}
