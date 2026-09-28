namespace Store.ProductService.Models;

/// <summary>
/// One place for the limits the database schema and the request validators must agree on.
/// </summary>
public static class ProductConstraints
{
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 200;
    /// <summary>A title's worth of slug plus the "-2" that tells two equal titles apart.</summary>
    public const int SlugMaxLength = 220;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 4000;
    public const int EnumMaxLength = 50;
    public const int GroupMaxLength = 100;
    public const int MaterialMaxLength = 100;
    public const int ImageUrlMaxLength = 2048;
    public const int ImageAltMaxLength = 200;
    /// <summary>Pictures after the main one.</summary>
    public const int MaxGalleryImages = 12;
    public const int MaxHotspots = 20;
    /// <summary>Lowercase letters and digits in dash-separated runs, the shape <see cref="ProductSlug"/> makes.</summary>
    public const string SlugPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

    public const decimal MinPrice = 0.01m;
    public const decimal MaxPrice = 999999.99m;
    public const decimal MaxDiscountPercent = 100m;
    public const decimal MaxDimension = 100000m;

    /// <summary>Units an administrator can put on hand for one product.</summary>
    public const int MaxStockQuantity = 100000;
    public const int EmailMaxLength = 256;
}
