namespace Store.ContentService.Models;

/// <summary>A journal article: advice, care guides, room stories.</summary>
public class Article : ContentEntry
{
    public string Title { get; set; } = string.Empty;

    /// <summary>The short text on the article's card and under its title.</summary>
    public string Excerpt { get; set; } = string.Empty;

    /// <summary>The article in Markdown.</summary>
    public string Body { get; set; } = string.Empty;

    public string CoverImage { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    /// <summary>The date the shop shows and sorts the journal by; newest first.</summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>Products the article talks about, by catalogue slug; shown under it.</summary>
    public List<string> ProductSlugs { get; set; } = new();
}
