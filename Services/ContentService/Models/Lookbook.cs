namespace Store.ContentService.Models;

/// <summary>A room photographed as a whole, with points on the picture leading to the products in it.</summary>
public class Lookbook : ContentEntry
{
    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Image { get; set; } = string.Empty;

    public List<Hotspot> Hotspots { get; set; } = new();

    /// <summary>Lower comes first where lookbooks are listed.</summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// A point on a lookbook picture: its position in percent of the picture's width and height (so
/// it stays in place at any size) and the catalogue product it stands for.
/// </summary>
public class Hotspot
{
    public decimal X { get; set; }

    public decimal Y { get; set; }

    public string ProductSlug { get; set; } = string.Empty;
}
