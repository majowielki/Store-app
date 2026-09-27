using Store.Contracts.Catalog;

namespace Store.ContentService.DTOs;

public record MakerResponse(
    int Id, string Slug, string Name, Company Company, string Tagline, string Story, string Location,
    int? FoundedYear, string CoverImage, bool IsPublished, DateTime UpdatedAt);

public record CollectionResponse(
    int Id, string Slug, string Title, string Summary, string Body, string CoverImage,
    List<string> ProductSlugs, int SortOrder, bool IsPublished, DateTime UpdatedAt);

public record ArticleResponse(
    int Id, string Slug, string Title, string Excerpt, string Body, string CoverImage, string Author,
    DateTime PublishedAt, List<string> ProductSlugs, bool IsPublished, DateTime UpdatedAt);

public record LookbookResponse(
    int Id, string Slug, string Title, string Summary, string Image, List<HotspotDto> Hotspots,
    int SortOrder, bool IsPublished, DateTime UpdatedAt);
