// Enable nullable annotations in this file
#nullable enable
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Messaging;
using Store.Contracts.Catalog;
using Store.ProductService.Data;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
using Store.ProductService.Models;
using System.Text.Json.Serialization;

namespace Store.ProductService.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly IAuditTrail _auditTrail;

    private static readonly System.Text.Json.JsonSerializerOptions AuditJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    public ProductService(ProductDbContext context, ILogger<ProductService> logger, IAuditTrail auditTrail)
    {
        _context = context;
        _logger = logger;
        _auditTrail = auditTrail;
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, string? actorId = null)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {ProductTitle}", request.Title);
            throw;
        }
    }

    public async Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request, string? actorId = null)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return null;
            }

            var oldValues = System.Text.Json.JsonSerializer.Serialize(product, AuditJsonOptions);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(int id, string? actorId = null)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return false;
            }

            var oldValues = System.Text.Json.JsonSerializer.Serialize(product, AuditJsonOptions);

            // Soft delete: past orders keep a valid product id and the admin panel can restore it
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product deactivated with ID: {ProductId}", id);
            await _auditTrail.RecordAsync("PRODUCT_DELETED", nameof(Product), id.ToString(), actorId, oldValues: oldValues);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
            throw;
        }
    }

    // Frontend-compatible methods
    public async Task<ProductsResponse> GetProductsForFrontendAsync(ProductQueryParams queryParams)
    {
        try
        {
            // Every filter below translates to SQL; nothing is filtered or paginated in memory
            var query = _context.Products.AsNoTracking().Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(queryParams.Group) && queryParams.Group.ToLower() != "all")
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

            if (!string.IsNullOrEmpty(queryParams.Category) && queryParams.Category.ToLower() != "all")
            {
                if (Enum.TryParse<Category>(queryParams.Category, true, out var category))
                {
                    query = query.Where(p => p.Category == category);
                }
            }

            if (!string.IsNullOrEmpty(queryParams.Company) && queryParams.Company.ToLower() != "all")
            {
                if (Enum.TryParse<Company>(queryParams.Company, true, out var company))
                {
                    query = query.Where(p => p.Company == company);
                }
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

            if (!string.IsNullOrEmpty(queryParams.Colors))
            {
                var colorsFilter = queryParams.Colors.ToLower().Split(',');
                query = query.Where(p => p.Colors.Any(c => colorsFilter.Contains(c.ToLower())));
            }

            // Sale filter
            if (!string.IsNullOrEmpty(queryParams.Sale))
            {
                var saleValue = queryParams.Sale.Trim().ToLower();
                if (saleValue == "true" || saleValue == "on" || saleValue == "1")
                {
                    query = query.Where(p => (p.SalePrice.HasValue && p.SalePrice.Value > 0) || (p.DiscountPercent.HasValue && p.DiscountPercent.Value > 0));
                }
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(queryParams.Order))
            {
                query = queryParams.Order.ToLower() switch
                {
                    "a-z" => query.OrderBy(p => p.Title),
                    "z-a" => query.OrderByDescending(p => p.Title),
                    "high" => query.OrderByDescending(p => p.SalePrice ?? p.Price),
                    "low" => query.OrderBy(p => p.SalePrice ?? p.Price),
                    _ => query.OrderBy(p => p.Title)
                };
            }
            else
            {
                query = query.OrderBy(p => p.Title);
            }

            const int pageSize = 12; // Standard page size for frontend
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var currentPage = queryParams.Page.GetValueOrDefault(1);
            if (currentPage < 1) currentPage = 1;
            var products = await query.Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

            var meta = await GetProductsMetaAsync();
            meta.Pagination = new PaginationMeta
            {
                Page = currentPage,
                PageCount = totalPages,
                PageSize = pageSize,
                Total = totalCount
            };

            return new ProductsResponse
            {
                Data = products.Select(MapToProductData).ToList(),
                Meta = meta
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for frontend");
            throw;
        }
    }

    public async Task<SingleProductResponse> GetProductForFrontendAsync(int id)
    {
        try
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
                throw new ArgumentException($"Product with ID {id} not found");

            return new SingleProductResponse
            {
                Data = MapToProductData(product),
                Meta = new { }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product for frontend with ID: {ProductId}", id);
            throw;
        }
    }

    public Task<ProductsMeta> GetProductsMetaAsync()
    {
        try
        {
            // Include 'all' as the first option so the frontend can default to it
            var categories = new List<string> { "all" };
            categories.AddRange(
                Enum.GetValues<Category>()
                    .Where(c => c != Category.All)
                    .Select(c => c.ToString().ToLower())
            );

            var groups = new List<string> { "all" };
            groups.AddRange(
                Enum.GetValues<Group>()
                    .Where(g => g != Group.All)
                    .Select(g => g.ToString().ToLower())
            );

            var companies = new List<string> { "all" };
            companies.AddRange(
                Enum.GetValues<Company>()
                    .Where(c => c != Company.All)
                    .Select(c => c.ToString().ToLower())
            );

            var colors = new List<string> { "all" };
            colors.AddRange(
                Enum.GetValues<Colors>()
                    .Where(c => c != Colors.All)
                    .Select(c => c.ToString().ToLower())
            );

            // Build group -> categories map for the UI dropdowns
            var groupCategoryMap = new List<GroupWithCategories>();
            foreach (var group in Enum.GetValues<Group>())
            {
                if (group == Group.All) continue; // UI handles 'all' separately

                var cats = group.GetCategories()
                    .Select(c => new OptionItem
                    {
                        Key = c.ToString().ToLower(),
                        Name = c.GetDisplayName()
                    })
                    .ToList();

                groupCategoryMap.Add(new GroupWithCategories
                {
                    Key = group.ToString().ToLower(),
                    Name = group.GetDisplayName(),
                    Categories = cats
                });
            }

            var result = new ProductsMeta
            {
                Categories = categories,
                Groups = groups,
                Companies = companies,
                Colors = colors,
                GroupCategoryMap = groupCategoryMap,
                Pagination = new PaginationMeta() // Will be set by calling method
            };
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products meta");
            throw;
        }
    }

    public async Task<ProductsResponse> GetProductsForAdminAsync(ProductQueryParams queryParams, string? sortBy, string? sortDir)
    {
        try
        {
            var query = _context.Products.AsNoTracking(); // Admin: show all, not just IsActive

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                var pattern = $"%{queryParams.Search.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.Title, pattern) ||
                    EF.Functions.ILike(p.Description, pattern));
            }

            if (!string.IsNullOrEmpty(queryParams.Category) && queryParams.Category.ToLower() != "all")
            {
                if (Enum.TryParse<Category>(queryParams.Category, true, out var category))
                {
                    query = query.Where(p => p.Category == category);
                }
            }

            if (!string.IsNullOrEmpty(queryParams.Company) && queryParams.Company.ToLower() != "all")
            {
                if (Enum.TryParse<Company>(queryParams.Company, true, out var company))
                {
                    query = query.Where(p => p.Company == company);
                }
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

            if (!string.IsNullOrEmpty(queryParams.Colors))
            {
                var colorsFilter = queryParams.Colors.ToLower().Split(',');
                query = query.Where(p => p.Colors.Any(c => colorsFilter.Contains(c.ToLower())));
            }

            // Advanced sorting for admin
            bool desc = (sortDir ?? "asc").ToLower() == "desc";
            query = (sortBy ?? "id").ToLower() switch
            {
                "id" => desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
                "price" => desc ? query.OrderByDescending(p => p.SalePrice ?? p.Price) : query.OrderBy(p => p.SalePrice ?? p.Price),
                "title" => desc ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title),
                "company" => desc ? query.OrderByDescending(p => p.Company) : query.OrderBy(p => p.Company),
                _ => query.OrderBy(p => p.Id)
            };

            int pageSize = queryParams.PageSize.GetValueOrDefault(50); // Default 50 for admin
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 1000) pageSize = 1000;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var currentPage = queryParams.Page.GetValueOrDefault(1);
            if (currentPage < 1) currentPage = 1;
            var skip = (currentPage - 1) * pageSize;

            var products = await query.Skip(skip).Take(pageSize).ToListAsync();

            var meta = await GetProductsMetaAsync();
            meta.Pagination = new PaginationMeta
            {
                Page = currentPage,
                PageCount = totalPages,
                PageSize = pageSize,
                Total = totalCount
            };

            return new ProductsResponse
            {
                Data = products.Select(MapToProductData).ToList(),
                Meta = meta
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for admin");
            throw;
        }
    }

    public async Task<ProductSnapshot?> GetSnapshotAsync(int id)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product?.ToSnapshot();
    }

    // Helper methods
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
            Colors = product.Colors,
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

    private static ProductData MapToProductData(Product product)
    {
        // Compute sale info consistently
        decimal? salePrice = product.SalePrice;
        decimal? discountPercent = product.DiscountPercent;
        if (!salePrice.HasValue && discountPercent.HasValue && discountPercent.Value > 0)
        {
            salePrice = Math.Round(product.Price * (1 - (discountPercent.Value / 100m)), 2);
        }
        else if (salePrice.HasValue && (!discountPercent.HasValue || discountPercent.Value <= 0))
        {
            var computed = product.Price == 0 ? 0 : Math.Round((1 - (salePrice.Value / product.Price)) * 100m, 2);
            discountPercent = computed;
        }

        return new ProductData
        {
            Id = product.Id,
            Attributes = new ProductAttributes
            {
                Category = product.Category.ToString().ToLower(),
                Company = product.Company.ToString().ToLower(),
                CreatedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Description = product.Description,
                NewArrival = product.NewArrival,
                Image = product.Image,
                Price = product.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                SalePrice = salePrice.HasValue ? salePrice.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : null,
                DiscountPercent = discountPercent,
                EffectivePrice = product.EffectivePrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                PublishedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Title = product.Title,
                UpdatedAt = product.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Colors = product.Colors.Select(c => c.ToLower()).ToList(),
                Groups = product.Groups.Select(g => g.ToLower()).Distinct().ToList(),
                WidthCm = product.WidthCm,
                HeightCm = product.HeightCm,
                DepthCm = product.DepthCm,
                WeightKg = product.WeightKg,
                Materials = product.Materials.Select(m => m.ToLower()).ToList(),
                IsActive = product.IsActive
            }
        };
    }
}
