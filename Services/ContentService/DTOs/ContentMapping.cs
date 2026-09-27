using Store.ContentService.Models;

namespace Store.ContentService.DTOs;

/// <summary>Entity to response, and request into entity (a request replaces every field).</summary>
public static class ContentMapping
{
    public static MakerResponse ToResponse(this Maker m) => new(
        m.Id, m.Slug, m.Name, m.Company, m.Tagline, m.Story, m.Location, m.FoundedYear, m.CoverImage, m.IsPublished, m.UpdatedAt);

    public static CollectionResponse ToResponse(this Collection c) => new(
        c.Id, c.Slug, c.Title, c.Summary, c.Body, c.CoverImage, c.ProductSlugs, c.SortOrder, c.IsPublished, c.UpdatedAt);

    public static ArticleResponse ToResponse(this Article a) => new(
        a.Id, a.Slug, a.Title, a.Excerpt, a.Body, a.CoverImage, a.Author, a.PublishedAt, a.ProductSlugs, a.IsPublished, a.UpdatedAt);

    public static LookbookResponse ToResponse(this Lookbook l) => new(
        l.Id, l.Slug, l.Title, l.Summary, l.Image,
        l.Hotspots.Select(h => new HotspotDto { X = h.X, Y = h.Y, ProductSlug = h.ProductSlug }).ToList(),
        l.SortOrder, l.IsPublished, l.UpdatedAt);

    public static void ApplyTo(this MakerRequest r, Maker m)
    {
        m.Slug = r.Slug;
        m.Name = r.Name.Trim();
        m.Company = r.Company;
        m.Tagline = r.Tagline.Trim();
        m.Story = r.Story;
        m.Location = r.Location.Trim();
        m.FoundedYear = r.FoundedYear;
        m.CoverImage = r.CoverImage;
        m.IsPublished = r.IsPublished;
    }

    public static void ApplyTo(this CollectionRequest r, Collection c)
    {
        c.Slug = r.Slug;
        c.Title = r.Title.Trim();
        c.Summary = r.Summary.Trim();
        c.Body = r.Body;
        c.CoverImage = r.CoverImage;
        c.ProductSlugs = [.. r.ProductSlugs];
        c.SortOrder = r.SortOrder;
        c.IsPublished = r.IsPublished;
    }

    public static void ApplyTo(this ArticleRequest r, Article a, DateTime now)
    {
        a.Slug = r.Slug;
        a.Title = r.Title.Trim();
        a.Excerpt = r.Excerpt.Trim();
        a.Body = r.Body;
        a.CoverImage = r.CoverImage;
        a.Author = r.Author.Trim();
        a.PublishedAt = r.PublishedAt?.ToUniversalTime() ?? (a.PublishedAt == default ? now : a.PublishedAt);
        a.ProductSlugs = [.. r.ProductSlugs];
        a.IsPublished = r.IsPublished;
    }

    public static void ApplyTo(this LookbookRequest r, Lookbook l)
    {
        l.Slug = r.Slug;
        l.Title = r.Title.Trim();
        l.Summary = r.Summary.Trim();
        l.Image = r.Image;
        l.Hotspots = r.Hotspots.Select(h => new Hotspot { X = h.X, Y = h.Y, ProductSlug = h.ProductSlug }).ToList();
        l.SortOrder = r.SortOrder;
        l.IsPublished = r.IsPublished;
    }
}
