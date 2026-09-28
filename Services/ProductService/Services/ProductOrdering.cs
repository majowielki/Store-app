using Microsoft.EntityFrameworkCore;
using Store.ProductService.Data;
using Store.ProductService.Models;

namespace Store.ProductService.Services;

/// <summary>The orders of the shop's listing, as its "order" query parameter names them.</summary>
public static class ProductOrders
{
    public const string TitleAscending = "a-z";
    public const string TitleDescending = "z-a";
    public const string PriceDescending = "high";
    public const string PriceAscending = "low";

    /// <summary>Best rated first: by the average, then by how many reviews it rests on.</summary>
    public const string BestRated = "rating";
}

/// <summary>The columns of the admin listing, as its "sortBy" parameter names them, and the direction that reverses them.</summary>
public static class ProductAdminSorts
{
    public const string Id = "id";
    public const string Price = "price";
    public const string Title = "title";
    public const string Company = "company";

    /// <summary>The units a new order can still get.</summary>
    public const string Stock = "stock";
    public const string Rating = "rating";

    /// <summary>The "sortDir" that sorts from the largest; anything else sorts from the smallest.</summary>
    public const string Descending = "desc";
}

/// <summary>Sorting the product listings in SQL.</summary>
public static class ProductOrdering
{
    /// <summary>
    /// The shop's listing in <paramref name="order"/> (<see cref="ProductOrders"/>). Without one, a
    /// search puts the best matches first (words in the title before the description) and a plain
    /// listing goes by title.
    /// </summary>
    public static IQueryable<Product> Apply(IQueryable<Product> query, string? order, SearchPlan search)
        => (order ?? string.Empty).ToLowerInvariant() switch
        {
            ProductOrders.TitleDescending => query.OrderByDescending(p => p.Title),
            ProductOrders.PriceDescending => query.OrderByDescending(p => p.SalePrice ?? p.Price),
            ProductOrders.PriceAscending => query.OrderBy(p => p.SalePrice ?? p.Price),
            ProductOrders.BestRated => query.OrderByDescending(p => p.RatingAverage).ThenByDescending(p => p.RatingCount).ThenBy(p => p.Title),
            ProductOrders.TitleAscending => query.OrderBy(p => p.Title),
            _ when search.Query is { } words => ByRelevance(query, words),
            _ => query.OrderBy(p => p.Title)
        };

    /// <summary>The best matches of <paramref name="words"/> first, then by title.</summary>
    public static IQueryable<Product> ByRelevance(IQueryable<Product> query, string words)
        => query.OrderByDescending(p => p.SearchVector.Rank(EF.Functions.ToTsQuery(ProductSearch.Config, ProductDbContext.Unaccent(words))))
            .ThenBy(p => p.Title);

    /// <summary>The admin listing by <paramref name="sortBy"/> (<see cref="ProductAdminSorts"/>, the id by default) in <paramref name="sortDir"/>.</summary>
    public static IQueryable<Product> ApplyAdmin(IQueryable<Product> query, string? sortBy, string? sortDir)
    {
        var desc = string.Equals(sortDir, ProductAdminSorts.Descending, StringComparison.OrdinalIgnoreCase);
        return (sortBy ?? ProductAdminSorts.Id).ToLowerInvariant() switch
        {
            ProductAdminSorts.Price => desc ? query.OrderByDescending(p => p.SalePrice ?? p.Price) : query.OrderBy(p => p.SalePrice ?? p.Price),
            ProductAdminSorts.Title => desc ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title),
            ProductAdminSorts.Company => desc ? query.OrderByDescending(p => p.Company) : query.OrderBy(p => p.Company),
            ProductAdminSorts.Stock => desc
                ? query.OrderByDescending(p => p.StockQuantity - p.ReservedQuantity)
                : query.OrderBy(p => p.StockQuantity - p.ReservedQuantity),
            ProductAdminSorts.Rating => desc
                ? query.OrderByDescending(p => p.RatingAverage).ThenByDescending(p => p.RatingCount)
                : query.OrderBy(p => p.RatingAverage).ThenBy(p => p.RatingCount),
            _ => desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id)
        };
    }
}
