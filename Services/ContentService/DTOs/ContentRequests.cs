using Store.Contracts.Catalog;

namespace Store.ContentService.DTOs;

// What the admin panel sends to create or replace an entry; a PUT replaces every field.

public class MakerRequest
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Company Company { get; set; }
    public string Tagline { get; set; } = string.Empty;
    /// <summary>Markdown.</summary>
    public string Story { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int? FoundedYear { get; set; }
    public string CoverImage { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}

public class CollectionRequest
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    /// <summary>Markdown.</summary>
    public string Body { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
    /// <summary>Catalogue product slugs, in the order the collection shows them.</summary>
    public List<string> ProductSlugs { get; set; } = new();
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}

public class ArticleRequest
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    /// <summary>Markdown.</summary>
    public string Body { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    /// <summary>The date shown with the article; empty means the time it is saved.</summary>
    public DateTime? PublishedAt { get; set; }
    public List<string> ProductSlugs { get; set; } = new();
    public bool IsPublished { get; set; }
}

public class LookbookRequest
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public List<HotspotDto> Hotspots { get; set; } = new();
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}

/// <summary>A point on a lookbook picture, in percent of its width (x) and height (y).</summary>
public class HotspotDto
{
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public string ProductSlug { get; set; } = string.Empty;
}
