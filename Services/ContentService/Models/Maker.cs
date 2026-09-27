using Store.Contracts.Catalog;

namespace Store.ContentService.Models;

/// <summary>
/// A workshop behind the catalogue's products. It is tied to a catalogue company, so its page
/// lists that company's products without keeping a list of its own.
/// </summary>
public class Maker : ContentEntry
{
    public string Name { get; set; } = string.Empty;

    public Company Company { get; set; }

    /// <summary>One line under the name ("Modern classics in oak and walnut").</summary>
    public string Tagline { get; set; } = string.Empty;

    /// <summary>The maker's story in Markdown.</summary>
    public string Story { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public int? FoundedYear { get; set; }

    public string CoverImage { get; set; } = string.Empty;
}
