using Microsoft.EntityFrameworkCore;
using Store.ProductService.Data;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
using Store.Shared.Models;
using Store.Shared.Utility;

namespace Store.ProductService.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ProductDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
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
                Category = request.Category,
                Company = request.Company,
                Featured = request.Featured,
                Image = request.Image,
                Colors = request.Colors,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
            return MapToProductResponse(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {ProductTitle}", request.Title);
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

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Title))
                product.Title = request.Title;
            
            if (!string.IsNullOrEmpty(request.Description))
                product.Description = request.Description;
            
            if (request.Price.HasValue)
                product.Price = request.Price.Value;
            
            if (request.Category.HasValue)
                product.Category = request.Category.Value;
            
            if (request.Company.HasValue)
                product.Company = request.Company.Value;
            
            if (request.Featured.HasValue)
                product.Featured = request.Featured.Value;
            
            if (!string.IsNullOrEmpty(request.Image))
                product.Image = request.Image;
            
            if (request.Colors != null && request.Colors.Any())
                product.Colors = request.Colors;

            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product updated successfully with ID: {ProductId}", product.Id);
            return MapToProductResponse(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
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

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product deleted successfully with ID: {ProductId}", id);
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
            var query = _context.Products.AsNoTracking().Where(p => p.IsActive);

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                query = query.Where(p => p.Title.Contains(queryParams.Search) || 
                                       p.Description.Contains(queryParams.Search));
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

            // Apply price filter if provided (format: "min,max" or "min-max")
            if (!string.IsNullOrEmpty(queryParams.Price))
            {
                var priceParts = queryParams.Price.Split(new[] { ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (priceParts.Length == 2)
                {
                    if (decimal.TryParse(priceParts[0], out var minPrice))
                        query = query.Where(p => p.Price >= minPrice);
                    if (decimal.TryParse(priceParts[1], out var maxPrice))
                        query = query.Where(p => p.Price <= maxPrice);
                }
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(queryParams.Order))
            {
                query = queryParams.Order.ToLower() switch
                {
                    "a-z" => query.OrderBy(p => p.Title),
                    "z-a" => query.OrderByDescending(p => p.Title),
                    "high" => query.OrderByDescending(p => p.Price),
                    "low" => query.OrderBy(p => p.Price),
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

            var products = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

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

    public async Task<ProductsMeta> GetProductsMetaAsync()
    {
        try
        {
            var categories = Enum.GetValues<Category>()
                .Where(c => c != Category.All)
                .Select(c => c.ToString().ToLower())
                .ToList();

            var companies = Enum.GetValues<Company>()
                .Where(c => c != Company.All)
                .Select(c => c.ToString().ToLower())
                .ToList();

            return new ProductsMeta
            {
                Categories = categories,
                Companies = companies,
                Pagination = new PaginationMeta() // Will be set by calling method
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products meta");
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
            Category = product.Category,
            Company = product.Company,
            Image = product.Image,
            Colors = product.Colors,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    private static ProductData MapToProductData(Product product)
    {
        return new ProductData
        {
            Id = product.Id,
            Attributes = new ProductAttributes
            {
                Category = product.Category.ToString().ToLower(),
                Company = product.Company.ToString().ToLower(),
                CreatedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Description = product.Description,
                Featured = product.Featured,
                Image = product.Image,
                Price = product.Price.ToString("F2"),
                PublishedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Title = product.Title,
                UpdatedAt = product.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Colors = product.Colors.Select(c => c.ToLower()).ToList()
            }
        };
    }
}