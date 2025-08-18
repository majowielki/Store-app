// Enable nullable annotations in this file
#nullable enable
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Store.ProductService.Data;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
using Store.Shared.Models;
using Store.Shared.Services;
using Store.Shared.Utility;

namespace Store.ProductService.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly IAuditLogClient _auditLogClient;

    private static readonly System.Text.Json.JsonSerializerOptions AuditJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    public ProductService(ProductDbContext context, ILogger<ProductService> logger, IAuditLogClient auditLogClient)
    {
        _context = context;
        _logger = logger;
        _auditLogClient = auditLogClient;
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request)
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
                Groups = request.Groups?.Select(g => g.ToLower()).Distinct().ToList() ?? new(),
                WidthCm = request.WidthCm,
                HeightCm = request.HeightCm,
                DepthCm = request.DepthCm,
                WeightKg = request.WeightKg,
                Materials = request.Materials?.Select(m => m.ToLower()).Distinct().ToList() ?? new(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
            // Audit log: product created
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "PRODUCT_CREATED",
                EntityName = nameof(Product),
                EntityId = product.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                NewValues = System.Text.Json.JsonSerializer.Serialize(product, AuditJsonOptions),
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Source = "ProductService" }, AuditJsonOptions)
            });
            return MapToProductResponse(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {ProductTitle}", request.Title);
            // Audit log: product creation failed
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "PRODUCT_CREATION_FAILED",
                EntityName = nameof(Product),
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Exception = ex.Message, Source = "ProductService" }, AuditJsonOptions)
            });
            throw;
        }
    }

    public async Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return null;
            }

            var oldValues = System.Text.Json.JsonSerializer.Serialize(product, AuditJsonOptions);

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Title))
                product.Title = request.Title;
            
            if (!string.IsNullOrEmpty(request.Description))
                product.Description = request.Description;
            
            if (request.Price.HasValue)
                product.Price = request.Price.Value;

            if (request.SalePrice.HasValue)
                product.SalePrice = request.SalePrice.Value;

            if (request.DiscountPercent.HasValue)
                product.DiscountPercent = request.DiscountPercent.Value;
            
            if (request.Category.HasValue)
                product.Category = request.Category.Value;
            
            if (request.Company.HasValue)
                product.Company = request.Company.Value;
            
            if (request.NewArrival.HasValue)
                product.NewArrival = request.NewArrival.Value;
            
            if (!string.IsNullOrEmpty(request.Image))
                product.Image = request.Image;
            
            if (request.Colors != null && request.Colors.Any())
                product.Colors = request.Colors;

            if (request.Groups != null)
                product.Groups = request.Groups.Select(g => g.ToLower()).Distinct().ToList();

            // New fields
            if (request.WidthCm.HasValue) product.WidthCm = request.WidthCm.Value;
            if (request.HeightCm.HasValue) product.HeightCm = request.HeightCm.Value;
            if (request.DepthCm.HasValue) product.DepthCm = request.DepthCm.Value;
            if (request.WeightKg.HasValue) product.WeightKg = request.WeightKg.Value;
            if (request.Materials != null) product.Materials = request.Materials.Select(m => m.ToLower()).Distinct().ToList();

            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product updated successfully with ID: {ProductId}", product.Id);
            // Audit log: product updated
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "PRODUCT_UPDATED",
                EntityName = nameof(Product),
                EntityId = product.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                OldValues = oldValues,
                NewValues = System.Text.Json.JsonSerializer.Serialize(product, AuditJsonOptions),
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Source = "ProductService" }, AuditJsonOptions)
            });
            return MapToProductResponse(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
            // Audit log: product update failed
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "PRODUCT_UPDATE_FAILED",
                EntityName = nameof(Product),
                EntityId = id.ToString(),
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Exception = ex.Message, Source = "ProductService" }, AuditJsonOptions)
            });
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return false;
            }

            var oldValues = System.Text.Json.JsonSerializer.Serialize(product, AuditJsonOptions);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product deleted successfully with ID: {ProductId}", id);
            // Audit log: product deleted
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "PRODUCT_DELETED",
                EntityName = nameof(Product),
                EntityId = id.ToString(),
                Timestamp = DateTime.UtcNow,
                OldValues = oldValues,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Source = "ProductService" }, AuditJsonOptions)
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
            // Audit log: product deletion failed
            await _auditLogClient.CreateAuditLogAsync(new Store.Shared.Models.AuditLog
            {
                Action = "PRODUCT_DELETION_FAILED",
                EntityName = nameof(Product),
                EntityId = id.ToString(),
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = System.Text.Json.JsonSerializer.Serialize(new { Exception = ex.Message, Source = "ProductService" }, AuditJsonOptions)
            });
            throw;
        }
    }

    // Frontend-compatible methods
    public async Task<ProductsResponse> GetProductsForFrontendAsync(ProductQueryParams queryParams)
    {
        try
        {
            var query = _context.Products.AsNoTracking().Where(p => p.IsActive);

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                var searchLower = queryParams.Search.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower));
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
                var searchLower = queryParams.Search.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower));
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

    // Helper methods
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
                Price = product.Price.ToString("F2"),
                SalePrice = salePrice.HasValue ? salePrice.Value.ToString("F2") : null,
                DiscountPercent = discountPercent,
                PublishedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Title = product.Title,
                UpdatedAt = product.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Colors = product.Colors.Select(c => c.ToLower()).ToList(),
                Groups = product.Groups.Select(g => g.ToLower()).Distinct().ToList(),
                WidthCm = product.WidthCm,
                HeightCm = product.HeightCm,
                DepthCm = product.DepthCm,
                WeightKg = product.WeightKg,
                Materials = product.Materials.Select(m => m.ToLower()).ToList()
            }
        };
    }
}