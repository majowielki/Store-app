namespace Store.ProductService.DTOs.Responses;

/// <summary>
/// One product as its own page shows it: everything of a catalogue entry plus the gallery and
/// the points on the main picture. The points are returned as stored; one leading to a product
/// the catalogue does not list (retired, or not added yet) is for the client to leave out.
/// </summary>
public class ProductDetailResponse : ProductResponse
{
    /// <summary>Pictures shown after the main one (image), in order.</summary>
    public List<ProductImageDto> Images { get; set; } = new();

    public List<ProductHotspotDto> Hotspots { get; set; } = new();
}

/// <summary>A gallery picture and what it shows.</summary>
public class ProductImageDto
{
    public string Url { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
}

/// <summary>A point on the main picture, in percent of its width (x) and height (y), and the product it leads to.</summary>
public class ProductHotspotDto
{
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public string ProductSlug { get; set; } = string.Empty;
}
