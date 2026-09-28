using Store.ProductService.DTOs.Responses;
using Store.ProductService.Models;

namespace Store.ProductService.Services;

/// <summary>A product as the API answers with it: the catalogue entry, its availability and rating, and for the admin panel the stock behind them.</summary>
public sealed class ProductMapper
{
    private readonly StockPolicy _stock;

    public ProductMapper(StockPolicy stock)
    {
        _stock = stock;
    }

    /// <summary>The product as a listing shows it; <paramref name="forAdmin"/> adds the stock figures behind the availability.</summary>
    public ProductResponse ToResponse(Product product, bool forAdmin) => Fill(product, new ProductResponse(), forAdmin);

    /// <summary>The product as its page shows it: the listing entry with its gallery and the points on its main picture.</summary>
    public ProductDetailResponse ToDetail(Product product, bool forAdmin)
    {
        var response = Fill(product, new ProductDetailResponse(), forAdmin);
        response.Images = product.Images
            .OrderBy(image => image.SortOrder)
            .Select(image => new ProductImageDto { Url = image.Url, Alt = image.Alt })
            .ToList();
        response.Hotspots = product.Hotspots
            .Select(point => new ProductHotspotDto { X = point.X, Y = point.Y, ProductSlug = point.ProductSlug })
            .ToList();
        return response;
    }

    private TResponse Fill<TResponse>(Product product, TResponse response, bool forAdmin) where TResponse : ProductResponse
    {
        response.Id = product.Id;
        response.Title = product.Title;
        response.Slug = product.Slug;
        response.Description = product.Description;
        response.Price = product.Price;
        response.SalePrice = product.SalePrice;
        response.DiscountPercent = product.DiscountPercent;
        response.EffectivePrice = product.EffectivePrice;
        response.Category = product.Category;
        response.Company = product.Company;
        response.NewArrival = product.NewArrival;
        response.Image = product.Image;
        response.Colors = product.Colors.Select(c => c.ToLowerInvariant()).ToList();
        response.Groups = product.Groups;
        response.WidthCm = product.WidthCm;
        response.HeightCm = product.HeightCm;
        response.DepthCm = product.DepthCm;
        response.WeightKg = product.WeightKg;
        response.Materials = product.Materials;
        response.IsActive = product.IsActive;
        response.Availability = _stock.For(product.AvailableQuantity);
        response.AvailableQuantity = product.AvailableQuantity;
        response.RatingAverage = product.RatingAverage;
        response.RatingCount = product.RatingCount;
        if (forAdmin)
        {
            response.StockQuantity = product.StockQuantity;
            response.ReservedQuantity = product.ReservedQuantity;
        }

        response.CreatedAt = product.CreatedAt;
        response.UpdatedAt = product.UpdatedAt;
        return response;
    }
}
