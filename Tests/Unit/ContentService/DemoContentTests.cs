using Store.ContentService.Data;
using Store.ContentService.DTOs;
using Store.ContentService.Validators;
using Store.ProductService.Data;
using Store.Tests.Unit.TestSupport;
using Xunit;

namespace Store.Tests.Unit.ContentService;

/// <summary>
/// The demo content names catalogue products by slug and pictures by URL; nothing checks either
/// at run time, so these tests do: every product exists in the demo catalogue, every picture in
/// Blobs/, and every entry passes the rules the admin endpoints apply.
/// </summary>
public class DemoContentTests
{
    private static readonly HashSet<string> CatalogueSlugs = DemoCatalogue.Products(DemoPictures.Links).Select(p => p.Slug).ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void Every_product_the_content_names_is_in_the_demo_catalogue()
    {
        var named = DemoContent.Collections(DemoPictures.Links).SelectMany(c => c.ProductSlugs)
            .Concat(DemoContent.Articles(DemoPictures.Links).SelectMany(a => a.ProductSlugs))
            .Concat(DemoContent.Lookbooks(DemoPictures.Links).SelectMany(l => l.Hotspots.Select(h => h.ProductSlug)))
            .Distinct();

        Assert.All(named, slug => Assert.Contains(slug, CatalogueSlugs));
    }

    [Fact]
    public void Every_picture_of_the_content_is_in_Blobs()
    {
        var pictures = DemoContent.Makers(DemoPictures.Links).Select(m => m.CoverImage)
            .Concat(DemoContent.Collections(DemoPictures.Links).Select(c => c.CoverImage))
            .Concat(DemoContent.Articles(DemoPictures.Links).Select(a => a.CoverImage))
            .Concat(DemoContent.Lookbooks(DemoPictures.Links).Select(l => l.Image));

        Assert.All(pictures, url => Assert.True(
            File.Exists(Repository.PathTo("Blobs", new Uri(url).Segments[^1])), $"{url} has no file in Blobs/"));
    }

    [Fact]
    public void Every_entry_passes_the_rules_of_the_admin_endpoints()
    {
        var makers = new MakerRequestValidator(TimeProvider.System);
        var collections = new CollectionRequestValidator();
        var articles = new ArticleRequestValidator();
        var lookbooks = new LookbookRequestValidator();

        Assert.All(DemoContent.Makers(DemoPictures.Links), m => AssertValid(makers.Validate(new MakerRequest
        {
            Slug = m.Slug,
            Name = m.Name,
            Company = m.Company,
            Tagline = m.Tagline,
            Story = m.Story,
            Location = m.Location,
            FoundedYear = m.FoundedYear,
            CoverImage = m.CoverImage,
            IsPublished = m.IsPublished
        })));
        Assert.All(DemoContent.Collections(DemoPictures.Links), c => AssertValid(collections.Validate(new CollectionRequest
        {
            Slug = c.Slug,
            Title = c.Title,
            Summary = c.Summary,
            Body = c.Body,
            CoverImage = c.CoverImage,
            ProductSlugs = c.ProductSlugs,
            SortOrder = c.SortOrder,
            IsPublished = c.IsPublished
        })));
        Assert.All(DemoContent.Articles(DemoPictures.Links), a => AssertValid(articles.Validate(new ArticleRequest
        {
            Slug = a.Slug,
            Title = a.Title,
            Excerpt = a.Excerpt,
            Body = a.Body,
            CoverImage = a.CoverImage,
            Author = a.Author,
            PublishedAt = a.PublishedAt,
            ProductSlugs = a.ProductSlugs,
            IsPublished = a.IsPublished
        })));
        Assert.All(DemoContent.Lookbooks(DemoPictures.Links), l => AssertValid(lookbooks.Validate(new LookbookRequest
        {
            Slug = l.Slug,
            Title = l.Title,
            Summary = l.Summary,
            Image = l.Image,
            SortOrder = l.SortOrder,
            IsPublished = l.IsPublished,
            Hotspots = l.Hotspots.Select(h => new HotspotDto { X = h.X, Y = h.Y, ProductSlug = h.ProductSlug }).ToList()
        })));
    }

    private static void AssertValid(FluentValidation.Results.ValidationResult result)
        => Assert.True(result.IsValid, string.Join("; ", result.Errors));
}
