using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Catalog;
using Store.ProductService.Data;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
using Store.ProductService.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Store.ProductService.Services;

public class ProductService : IProductService
{
    private const int PublicPageSize = 12;
    private const int AdminPageSize = 50;

    private readonly ProductDbContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly IAuditTrail _auditTrail;

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProductService(ProductDbContext context, ILogger<ProductService> logger, IAuditTrail auditTrail)
    {
        _context = context;
        _logger = logger;
        _auditTrail = auditTrail;
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, string? actorId = null)
    {
        var product = new Product
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            SalePrice = request.SalePrice,
            DiscountPercent = request.DiscountPercent,
            Category = request.Category,
            Company = request.Company,
            NewArrival = request.NewArrival,
            Image = request.Image,
            Colors = request.Colors,
            Groups = NormalizeList(request.Groups),
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            DepthCm = request.DepthCm,
            WeightKg = request.WeightKg,
            Materials = NormalizeList(request.Materials),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
        await _auditTrail.RecordAsync("PRODUCT_CREATED", nameof(Product), product.Id.ToString(), actorId, newValues: product);
        return MapToProductResponse(product);
    }

    public async Task<ProductResponse> UpdateProductAsync(int id, UpdateProductRequest request, string? actorId = null)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Product", id);

        var oldValues = JsonSerializer.Serialize(product, AuditJsonOptions);

        // Absent fields keep their value; the Optional ones can also be cleared by sending null
        if (request.Title is not null) product.Title = request.Title;
        if (request.Description is not null) product.Description = request.Description;
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.SalePrice.IsSet) product.SalePrice = request.SalePrice.Value;
        if (request.DiscountPercent.IsSet) product.DiscountPercent = request.DiscountPercent.Value;
        if (request.Category.HasValue) product.Category = request.Category.Value;
        if (request.Company.HasValue) product.Company = request.Company.Value;
        if (request.NewArrival.HasValue) product.NewArrival = request.NewArrival.Value;
        if (request.Image is not null) product.Image = request.Image;
        if (request.Colors is not null) product.Colors = request.Colors;
        if (request.Groups is not null) product.Groups = NormalizeList(request.Groups);
        if (request.WidthCm.IsSet) product.WidthCm = request.WidthCm.Value;
        if (request.HeightCm.IsSet) product.HeightCm = request.HeightCm.Value;
        if (request.DepthCm.IsSet) product.DepthCm = request.DepthCm.Value;
        if (request.WeightKg.IsSet) product.WeightKg = request.WeightKg.Value;
        if (request.Materials is not null) product.Materials = NormalizeList(request.Materials);
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;

        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product updated successfully with ID: {ProductId}", product.Id);
        await _auditTrail.RecordAsync("PRODUCT_UPDATED", nameof(Product), product.Id.ToString(), actorId, oldValues: oldValues, newValues: product);
        return MapToProductResponse(product);
    }

    public async Task DeleteProductAsync(int id, string? actorId = null)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Product", id);

        var oldValues = JsonSerializer.Serialize(product, AuditJsonOptions);

        // Soft delete: past orders keep a valid product id and the admin panel can restore it
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deactivated with ID: {ProductId}", id);
        await _auditTrail.RecordAsync("PRODUCT_DELETED", nameof(Product), id.ToString(), actorId, oldValues: oldValues);
    }

    public Task<PagedResponse<ProductResponse>> GetProductsAsync(ProductQueryParams queryParams)
    {
        var query = ApplyFilters(_context.Products.AsNoTracking().Where(p => p.IsActive), queryParams);

        query = (queryParams.Order ?? string.Empty).ToLowerInvariant() switch
        {
            "z-a" => query.OrderByDescending(p => p.Title),
            "high" => query.OrderByDescending(p => p.SalePrice ?? p.Price),
            "low" => query.OrderBy(p => p.SalePrice ?? p.Price),
            _ => query.OrderBy(p => p.Title)
        };

        return PageAsync(query, queryParams, PublicPageSize);
    }

    public async Task<ProductResponse> GetProductAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
            ?? throw new NotFoundException("Product", id);

        return MapToProductResponse(product);
    }

    public ProductsMeta GetProductsMeta()
    {
        // "all" first, so a menu can default to it; keys spelled like the product fields
        var categories = new List<string> { "all" };
        categories.AddRange(Enum.GetValues<Category>().Where(c => c != Category.All).Select(Key));

        var groups = new List<string> { "all" };
        groups.AddRange(Enum.GetValues<Group>().Where(g => g != Group.All).Select(Key));

        var companies = new List<string> { "all" };
        companies.AddRange(Enum.GetValues<Company>().Where(c => c != Company.All).Select(Key));

        var colors = new List<string> { "all" };
        colors.AddRange(Enum.GetValues<Colors>().Where(c => c != Colors.All).Select(Key));

        var groupCategoryMap = Enum.GetValues<Group>()
            .Where(group => group != Group.All)
            .Select(group => new GroupWithCategories
            {
                Key = Key(group),
                Name = group.GetDisplayName(),
                Categories = group.GetCategories()
                    .Select(c => new OptionItem { Key = Key(c), Name = c.GetDisplayName() })
                    .ToList()
            })
            .ToList();

        return new ProductsMeta
        {
            Categories = categories,
            Groups = groups,
            Companies = companies,
            Colors = colors,
            GroupCategoryMap = groupCategoryMap
        };
    }

    public Task<PagedResponse<ProductResponse>> GetProductsForAdminAsync(ProductQueryParams queryParams, string? sortBy, string? sortDir)
    {
        // Inactive products too: the admin panel restores them from here
        var query = ApplyFilters(_context.Products.AsNoTracking(), queryParams);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy ?? "id").ToLowerInvariant() switch
        {
            "price" => desc ? query.OrderByDescending(p => p.SalePrice ?? p.Price) : query.OrderBy(p => p.SalePrice ?? p.Price),
            "title" => desc ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title),
            "company" => desc ? query.OrderByDescending(p => p.Company) : query.OrderBy(p => p.Company),
            _ => desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id)
        };

        return PageAsync(query, queryParams, AdminPageSize);
    }

    public async Task<ProductSnapshot?> GetSnapshotAsync(int id)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product?.ToSnapshot();
    }

    /// <summary>Every filter translates to SQL; nothing is filtered or paginated in memory.</summary>
    private static IQueryable<Product> ApplyFilters(IQueryable<Product> query, ProductQueryParams queryParams)
    {
        if (!string.IsNullOrEmpty(queryParams.Group) && !IsAll(queryParams.Group))
        {
            var groupFilter = queryParams.Group.ToLower();
            query = query.Where(p => p.Groups.Any(g => g.ToLower() == groupFilter));
        }

        if (!string.IsNullOrEmpty(queryParams.Search))
        {
            var pattern = $"%{queryParams.Search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Title, pattern) ||
                EF.Functions.ILike(p.Description, pattern));
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
            var priceParts = queryParams.Price.Split(new[] { ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
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

        // "colors=black,white" from the API, "color=black" from the shop's filter form
        var colors = queryParams.Colors ?? queryParams.Color;
        if (!string.IsNullOrEmpty(colors) && !IsAll(colors))
        {
            var colorsFilter = colors.ToLower().Split(',');
            query = query.Where(p => p.Colors.Any(c => colorsFilter.Contains(c.ToLower())));
        }

        // Checkbox-style values from the UI: "true", "on", "1"
        if (queryParams.Sale?.Trim().ToLowerInvariant() is "true" or "on" or "1")
        {
            query = query.Where(p => (p.SalePrice.HasValue && p.SalePrice.Value > 0) || (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0));
        }

        return query;
    }

    private static async Task<PagedResponse<ProductResponse>> PageAsync(IQueryable<Product> query, ProductQueryParams queryParams, int defaultPageSize)
    {
        var paging = new PagedQuery { Page = queryParams.Page ?? 1, PageSize = queryParams.PageSize ?? defaultPageSize }
            .Normalized(defaultPageSize);

        var totalCount = await query.CountAsync();
        var products = await query.Skip(paging.Skip).Take(paging.PageSize).ToListAsync();

        return new PagedResponse<ProductResponse>(products.Select(MapToProductResponse).ToList(), totalCount, paging);
    }

    private static bool IsAll(string value) => string.Equals(value, "all", StringComparison.OrdinalIgnoreCase);

    /// <summary>The spelling enum values have in JSON ("tvStands"), so filters and products agree.</summary>
    private static string Key<TEnum>(TEnum value) where TEnum : struct, Enum
        => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

    private static List<string> NormalizeList(IEnumerable<string>? values)
        => values?.Select(v => v.Trim().ToLowerInvariant()).Where(v => v.Length > 0).Distinct().ToList() ?? new List<string>();

    private static ProductResponse MapToProductResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            Price = product.Price,
            SalePrice = product.SalePrice,
            DiscountPercent = product.DiscountPercent,
            EffectivePrice = product.EffectivePrice,
            Category = product.Category,
            Company = product.Company,
            NewArrival = product.NewArrival,
            Image = product.Image,
            Colors = product.Colors.Select(c => c.ToLowerInvariant()).ToList(),
            Groups = product.Groups,
            WidthCm = product.WidthCm,
            HeightCm = product.HeightCm,
            DepthCm = product.DepthCm,
            WeightKg = product.WeightKg,
            Materials = product.Materials,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
