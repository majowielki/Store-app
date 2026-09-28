using Microsoft.EntityFrameworkCore;
using Store.Contracts.Catalog;
using Store.ProductService.Data;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.Models;
using System.Linq.Expressions;
using System.Text.Json;

namespace Store.ProductService.Services;

/// <summary>
/// The catalogue's filters as SQL: every query parameter of the listing narrows the products in the
/// database, nothing is filtered in memory. The listing, the filter counts and the suggestions share it.
/// </summary>
public static class ProductFilters
{
    /// <summary>The value of a menu that filters nothing ("all categories").</summary>
    public const string All = "all";

    /// <summary>At most this many ids of <see cref="ProductQueryParams.Ids"/> are looked at (a wishlist, a comparison).</summary>
    public const int MaxIds = 100;

    /// <summary>What a checkbox of the shop's filter form sends when ticked.</summary>
    private static readonly HashSet<string> Ticked = new(StringComparer.OrdinalIgnoreCase) { "true", "on", "1" };

    private static readonly char[] RangeSeparators = [',', '-'];

    /// <summary>The colour families by their key ("white"), the values of the shop's colour menu.</summary>
    private static readonly Dictionary<string, Color> Families = Enum.GetValues<Color>()
        .Where(color => color != Color.All)
        .ToDictionary(color => Key(color), StringComparer.OrdinalIgnoreCase);

    /// <summary>A product on sale: a sale price or a discount.</summary>
    public static readonly Expression<Func<Product, bool>> IsOnSale =
        p => (p.SalePrice.HasValue && p.SalePrice.Value > 0) || (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0);

    /// <summary>
    /// <paramref name="query"/> narrowed by every filter of <paramref name="queryParams"/>; the search is
    /// the <paramref name="search"/> plan's words (corrected when the typed ones found nothing).
    /// </summary>
    public static IQueryable<Product> Apply(IQueryable<Product> query, ProductQueryParams queryParams, SearchPlan search)
    {
        if (!string.IsNullOrEmpty(queryParams.Group) && !IsAll(queryParams.Group))
        {
            var groupFilter = queryParams.Group.ToLower();
            query = query.Where(p => p.Groups.Any(g => g.ToLower() == groupFilter));
        }

        if (search.Query is { } words)
        {
            query = query.Where(p => p.SearchVector.Matches(EF.Functions.ToTsQuery(ProductSearch.Config, ProductDbContext.Unaccent(words))));
        }

        if (!string.IsNullOrEmpty(queryParams.Category) && !IsAll(queryParams.Category)
            && Enum.TryParse<Category>(queryParams.Category, true, out var category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (!string.IsNullOrEmpty(queryParams.Company) && !IsAll(queryParams.Company)
            && Enum.TryParse<Company>(queryParams.Company, true, out var company))
        {
            query = query.Where(p => p.Company == company);
        }

        if (!string.IsNullOrEmpty(queryParams.Price))
        {
            var priceParts = queryParams.Price.Split(RangeSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (priceParts.Length == 2)
            {
                if (decimal.TryParse(priceParts[0], out var minPrice))
                    query = query.Where(p => (p.SalePrice ?? p.Price) >= minPrice);
                if (decimal.TryParse(priceParts[1], out var maxPrice))
                    query = query.Where(p => (p.SalePrice ?? p.Price) <= maxPrice);
            }
        }

        if (!string.IsNullOrEmpty(queryParams.Materials))
        {
            var materialsFilter = queryParams.Materials.ToLower().Split(',');
            query = query.Where(p => p.Materials.Any(m => materialsFilter.Contains(m.ToLower())));
        }

        // "colors=black,natural-oak" from the API, "color=black" from the shop's filter form
        var colors = queryParams.Colors ?? queryParams.Color;
        if (!string.IsNullOrEmpty(colors) && !IsAll(colors))
        {
            var finishes = colors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .SelectMany(FinishKeys)
                .ToArray();
            query = query.Where(p => p.Colors.Any(c => finishes.Contains(c)));
        }

        if (IsTicked(queryParams.Sale))
        {
            query = query.Where(IsOnSale);
        }

        if (IsTicked(queryParams.NewArrival))
        {
            query = query.Where(p => p.NewArrival);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Slugs))
        {
            var slugs = queryParams.Slugs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(p => slugs.Contains(p.Slug));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Ids))
        {
            // Anything that is not a number is ignored rather than refused: the list comes from a browser
            var ids = queryParams.Ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => int.TryParse(id, out var value) ? value : 0)
                .Where(id => id > 0)
                .Take(MaxIds)
                .ToArray();
            query = query.Where(p => ids.Contains(p.Id));
        }

        return query;
    }

    /// <summary>The spelling enum values have in JSON ("tvStands"), so filters and products agree.</summary>
    public static string Key<TEnum>(TEnum value) where TEnum : struct, Enum
        => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

    /// <summary>The finishes a colour of the filter stands for: a family's ("white"), or the one it names ("natural-oak").</summary>
    private static IEnumerable<string> FinishKeys(string color)
        => Families.TryGetValue(color, out var family)
            ? FinishCatalogue.OfFamily(family).Select(finish => finish.Key)
            : [color.ToLowerInvariant()];

    private static bool IsAll(string value) => string.Equals(value, All, StringComparison.OrdinalIgnoreCase);

    private static bool IsTicked(string? value) => value is not null && Ticked.Contains(value.Trim());
}
