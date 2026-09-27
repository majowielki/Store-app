using System.Diagnostics.CodeAnalysis;

namespace Store.ContentService.Models;

/// <summary>A curated set of products around one idea ("Warm Minimal", "Small Spaces").</summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "A collection is what the shop calls it; it is not a .NET collection type.")]
public class Collection : ContentEntry
{
    public string Title { get; set; } = string.Empty;

    /// <summary>A sentence or two for cards and the top of the page.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>The longer text of the page, in Markdown.</summary>
    public string Body { get; set; } = string.Empty;

    public string CoverImage { get; set; } = string.Empty;

    /// <summary>The products of the collection, by catalogue slug, in the order they are shown.</summary>
    public List<string> ProductSlugs { get; set; } = new();

    /// <summary>Lower comes first where collections are listed.</summary>
    public int SortOrder { get; set; }
}
