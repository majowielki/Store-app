using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Messaging;
using Store.BuildingBlocks.Persistence;
using Store.Contracts.Audit;
using Store.Contracts.Catalog;
using Store.ProductService.Data;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
using Store.ProductService.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Store.ProductService.Services;

/// <summary>
/// The catalogue's products: what the administrator creates, changes and retires, and the listings
/// and pages the shop and the admin panel read. Searching and the menus' counts are
/// <see cref="IProductDiscovery"/>; the stock is <see cref="IStockLedger"/>.
/// </summary>
public class ProductService : IProductService
{
    private const int PublicPageSize = 12;
    private const int AdminPageSize = 50;

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ProductDbContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly IAuditTrail _auditTrail;
    private readonly TimeProvider _time;
    private readonly ProductSearch _search;
    private readonly ProductMapper _mapper;

    public ProductService(
        ProductDbContext context,
        ILogger<ProductService> logger,
        IAuditTrail auditTrail,
        TimeProvider time,
        ProductSearch search,
        ProductMapper mapper)
    {
        _context = context;
        _logger = logger;
        _auditTrail = auditTrail;
        _time = time;
        _search = search;
        _mapper = mapper;
    }

    public async Task<ProductDetailResponse> CreateProductAsync(CreateProductRequest request, string? actorId = null)
    {
        await using var transaction = await _context.BeginStoreTransactionAsync();
        var product = new Product
        {
            Title = request.Title,
            Slug = await UniqueSlugAsync(request.Title),
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
            Images = ToGallery(request.Images ?? []),
            Hotspots = ToHotspots(request.Hotspots ?? []),
            StockQuantity = request.StockQuantity,
            CreatedAt = _time.GetUtcNow().UtcDateTime,
            UpdatedAt = _time.GetUtcNow().UtcDateTime
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
        await _auditTrail.RecordAsync(AuditActions.ProductCreated, nameof(Product), product.Id.ToString(), actorId, newValues: product);
        if (transaction is not null) await transaction.CommitAsync();
        return _mapper.ToDetail(product, forAdmin: true);
    }

    public async Task<ProductDetailResponse> UpdateProductAsync(int id, UpdateProductRequest request, string? actorId = null)
    {
        await using var transaction = await _context.BeginStoreTransactionAsync();
        var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException(nameof(Product), id);

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

        if (request.Images is not null)
        {
            // The pictures left out are deleted with the relationship
            product.Images.Clear();
            product.Images.AddRange(ToGallery(request.Images));
        }

        if (request.Hotspots is not null) product.Hotspots = ToHotspots(request.Hotspots);

        product.UpdatedAt = _time.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product updated successfully with ID: {ProductId}", product.Id);
        await _auditTrail.RecordAsync(AuditActions.ProductUpdated, nameof(Product), product.Id.ToString(), actorId, oldValues: oldValues, newValues: product);
        if (transaction is not null) await transaction.CommitAsync();
        return _mapper.ToDetail(product, forAdmin: true);
    }

    public async Task DeleteProductAsync(int id, string? actorId = null)
    {
        await using var transaction = await _context.BeginStoreTransactionAsync();
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException(nameof(Product), id);

        var oldValues = JsonSerializer.Serialize(product, AuditJsonOptions);

        // Soft delete: past orders keep a valid product id and the admin panel can restore it
        product.IsActive = false;
        product.UpdatedAt = _time.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deactivated with ID: {ProductId}", id);
        await _auditTrail.RecordAsync(AuditActions.ProductDeleted, nameof(Product), id.ToString(), actorId, oldValues: oldValues);
        if (transaction is not null) await transaction.CommitAsync();
    }

    public async Task<PagedResponse<ProductResponse>> GetProductsAsync(ProductQueryParams queryParams)
    {
        var plan = await _search.PlanAsync(queryParams.Search);
        var query = ProductOrdering.Apply(ProductFilters.Apply(ActiveProducts, queryParams, plan), queryParams.Order, plan);
        return await PageAsync(query, queryParams, PublicPageSize, forAdmin: false);
    }

    public async Task<PagedResponse<ProductResponse>> GetProductsForAdminAsync(ProductQueryParams queryParams, string? sortBy, string? sortDir)
    {
        // Inactive products too: the admin panel restores them from here
        var plan = await _search.PlanAsync(queryParams.Search, includeInactive: true);
        var query = ProductOrdering.ApplyAdmin(ProductFilters.Apply(_context.Products.AsNoTracking(), queryParams, plan), sortBy, sortDir);
        return await PageAsync(query, queryParams, AdminPageSize, forAdmin: true);
    }

    public Task<ProductDetailResponse> GetProductAsync(int id) => GetDetailAsync(id, forAdmin: false);

    public Task<ProductDetailResponse> GetProductForAdminAsync(int id) => GetDetailAsync(id, forAdmin: true);

    public async Task<ProductSnapshot?> GetSnapshotAsync(int id)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product?.ToSnapshot();
    }

    private IQueryable<Product> ActiveProducts => _context.Products.AsNoTracking().Where(p => p.IsActive);

    private async Task<ProductDetailResponse> GetDetailAsync(int id, bool forAdmin)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id && (p.IsActive || forAdmin))
            ?? throw new NotFoundException(nameof(Product), id);

        return _mapper.ToDetail(product, forAdmin);
    }

    private async Task<PagedResponse<ProductResponse>> PageAsync(IQueryable<Product> query, ProductQueryParams queryParams, int defaultPageSize, bool forAdmin)
    {
        var paging = new PagedQuery { Page = queryParams.Page ?? 1, PageSize = queryParams.PageSize ?? defaultPageSize }
            .Normalized(defaultPageSize);

        var totalCount = await query.CountAsync();
        var products = await query.Skip(paging.Skip).Take(paging.PageSize).ToListAsync();

        return new PagedResponse<ProductResponse>(products.Select(product => _mapper.ToResponse(product, forAdmin)).ToList(), totalCount, paging);
    }

    /// <summary>The slug of the title, or the same with "-2", "-3"... when another product has it.</summary>
    private async Task<string> UniqueSlugAsync(string title)
    {
        var slug = ProductSlug.From(title);
        var taken = (await _context.Products
                .Where(p => p.Slug == slug || p.Slug.StartsWith(slug + "-"))
                .Select(p => p.Slug)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        var candidate = slug;
        for (var n = 2; taken.Contains(candidate); n++)
        {
            candidate = $"{slug}-{n}";
        }

        return candidate;
    }

    private static List<string> NormalizeList(IEnumerable<string>? values)
        => values?.Select(v => v.Trim().ToLowerInvariant()).Where(v => v.Length > 0).Distinct().ToList() ?? new List<string>();

    private static List<ProductImage> ToGallery(IEnumerable<ProductImageDto> images)
        => images.Select((image, index) => new ProductImage { Url = image.Url.Trim(), Alt = image.Alt.Trim(), SortOrder = index }).ToList();

    private static List<ProductHotspot> ToHotspots(IEnumerable<ProductHotspotDto> points)
        => points.Select(point => new ProductHotspot { X = point.X, Y = point.Y, ProductSlug = point.ProductSlug }).ToList();
}
