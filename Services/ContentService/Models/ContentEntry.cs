namespace Store.ContentService.Models;

/// <summary>
/// What every kind of content shares: an address (the slug, unique within its kind), whether the
/// shop shows it and when it last changed - the last is what the public ETags are made of.
/// </summary>
public abstract class ContentEntry
{
    public int Id { get; set; }

    /// <summary>Lowercase words joined by dashes, unique within the kind ("warm-minimal").</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Unpublished entries exist only in the admin panel.</summary>
    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
