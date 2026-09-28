using Microsoft.EntityFrameworkCore;
using Store.Contracts.Catalog;
using Store.ProductService.Data;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
using Store.ProductService.Models;

namespace Store.ProductService.Services;

/// <summary>
/// Finding one's way in the catalogue: the values its menus offer with how many products each
/// would show, and the best matches of a search while it is being typed.
/// </summary>
public interface IProductDiscovery
{
    /// <summary>The filter values with how many products each shows under the rest of the query, and the search's correction.</summary>
    Task<ProductsMeta> GetMetaAsync(ProductQueryParams queryParams);

    /// <summary>The best matches of a search while it is typed, corrected when it finds nothing as typed.</summary>
    Task<ProductSuggestions> SuggestAsync(string? search, int limit);

    /// <summary>Every finish products are sold in, with the swatch the shop draws for it.</summary>
    IReadOnlyList<FinishResponse> GetFinishes();
}

public sealed class ProductDiscovery : IProductDiscovery
{
    /// <summary>How many matches the search box shows unless asked for another number, and the most it shows.</summary>
    public const int DefaultSuggestions = 6;
    public const int MaxSuggestions = 12;

    /// <summary>The finishes never change while the service runs, so they are mapped once.</summary>
    private static readonly IReadOnlyList<FinishResponse> Finishes = FinishCatalogue.All.Select(FinishResponse.From).ToList();

    private readonly ProductDbContext _context;
    private readonly ProductSearch _search;
    private readonly ProductMapper _mapper;

    public ProductDiscovery(ProductDbContext context, ProductSearch search, ProductMapper mapper)
    {
        _context = context;
        _search = search;
        _mapper = mapper;
    }

    private IQueryable<Product> ActiveProducts => _context.Products.AsNoTracking().Where(p => p.IsActive);

    public async Task<ProductsMeta> GetMetaAsync(ProductQueryParams queryParams)
    {
        var meta = Options();
        var plan = await _search.PlanAsync(queryParams.Search);
        meta.SearchCorrection = plan.Correction;
        meta.Counts = await CountAsync(queryParams, plan);
        return meta;
    }

    public async Task<ProductSuggestions> SuggestAsync(string? search, int limit)
    {
        var plan = await _search.PlanAsync(search);
        var suggestions = new ProductSuggestions { Query = search?.Trim() ?? string.Empty, Correction = plan.Correction };
        if (plan.Query is not { } words)
        {
            return suggestions;
        }

        var query = ProductFilters.Apply(ActiveProducts, new ProductQueryParams(), plan);
        suggestions.TotalCount = await query.CountAsync();
        var best = await ProductOrdering.ByRelevance(query, words)
            .Take(Math.Clamp(limit, 1, MaxSuggestions))
            .ToListAsync();
        suggestions.Products = best.Select(product => _mapper.ToResponse(product, forAdmin: false)).ToList();
        return suggestions;
    }

    public IReadOnlyList<FinishResponse> GetFinishes() => Finishes;

    /// <summary>The values of every menu, "all" first so a menu can default to it; keys spelled like the product fields.</summary>
    private static ProductsMeta Options() => new()
    {
        Categories = WithAll(Enum.GetValues<Category>().Where(c => c != Category.All)),
        Groups = WithAll(Enum.GetValues<Group>().Where(g => g != Group.All)),
        Companies = WithAll(Enum.GetValues<Company>().Where(c => c != Company.All)),
        Colors = WithAll(Enum.GetValues<Color>().Where(c => c != Color.All)),
        GroupCategoryMap = Enum.GetValues<Group>()
            .Where(group => group != Group.All)
            .Select(group => new GroupWithCategories
            {
                Key = ProductFilters.Key(group),
                Name = group.GetDisplayName(),
                Categories = group.GetCategories()
                    .Select(c => new OptionItem { Key = ProductFilters.Key(c), Name = c.GetDisplayName() })
                    .ToList()
            })
            .ToList()
    };

    private static List<string> WithAll<TEnum>(IEnumerable<TEnum> values) where TEnum : struct, Enum
        => [ProductFilters.All, .. values.Select(ProductFilters.Key)];

    /// <summary>
    /// The counts of every filter value under the rest of the query: each menu's own filter is left
    /// out of its counts, so picking another category still shows how many it has.
    /// </summary>
    private async Task<FilterCounts> CountAsync(ProductQueryParams queryParams, SearchPlan plan)
    {
        IQueryable<Product> Without(Action<ProductQueryParams> clear)
        {
            var rest = queryParams.Copy();
            clear(rest);
            return ProductFilters.Apply(ActiveProducts, rest, plan);
        }

        var categories = await Without(q => q.Category = null)
            .GroupBy(p => p.Category)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        var companies = await Without(q => q.Company = null)
            .GroupBy(p => p.Company)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        // The families of the finishes live in code, not in the database: the finishes of the matching
        // products are read and a product counts once under each family its finishes belong to
        var finishes = await Without(q => q.Colors = q.Color = null)
            .Select(p => p.Colors)
            .ToListAsync();
        var colors = finishes
            .SelectMany(keys => keys.Select(FinishCatalogue.Find).OfType<Finish>().Select(finish => finish.Family).Distinct())
            .GroupBy(family => family)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToList();
        var groups = await Without(q => q.Group = null)
            .SelectMany(p => p.Groups)
            .GroupBy(group => group)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        return new FilterCounts
        {
            Total = await ProductFilters.Apply(ActiveProducts, queryParams, plan).CountAsync(),
            Categories = categories.ToDictionary(c => ProductFilters.Key(c.Key), c => c.Count),
            Companies = companies.ToDictionary(c => ProductFilters.Key(c.Key), c => c.Count),
            Colors = colors.ToDictionary(c => ProductFilters.Key(c.Key), c => c.Count),
            Groups = groups.ToDictionary(g => g.Key, g => g.Count),
            Sale = await Without(q => q.Sale = null).CountAsync(ProductFilters.IsOnSale),
            NewArrival = await Without(q => q.NewArrival = null).CountAsync(p => p.NewArrival)
        };
    }
}
