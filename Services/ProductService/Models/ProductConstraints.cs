namespace Store.ProductService.Models;

/// <summary>
/// One place for the limits the database schema and the request validators must agree on.
/// </summary>
public static class ProductConstraints
{
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 200;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 4000;
    public const int EnumMaxLength = 50;
    public const int ColorMaxLength = 50;
    public const int GroupMaxLength = 100;
    public const int MaterialMaxLength = 100;

    public const decimal MinPrice = 0.01m;
    public const decimal MaxPrice = 999999.99m;
    public const decimal MaxDiscountPercent = 100m;
    public const decimal MaxDimension = 100000m;
}
