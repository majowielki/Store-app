using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Contracts.Authorization;
using Store.Contracts.Catalog;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
using Store.ProductService.Services;

namespace Store.ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>Id of the signed-in administrator, for the audit trail.</summary>
    private string? ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Get all products with filtering and pagination (Frontend compatible)
    /// Returns: ProductsResponse = { data: Product[]; meta: ProductsMeta; }
    /// Product = { id: number; attributes: { category, company, createdAt, description, featured, image, price, publishedAt, title, updatedAt, colors } }
    /// </summary>
    /// <param name="queryParams">Query parameters for filtering and pagination</param>
    /// <returns>Products response in frontend format</returns>
    [HttpGet]
    public async Task<ActionResult<ProductsResponse>> GetProducts([FromQuery] ProductQueryParams queryParams)
    {
        try
        {
            var result = await _productService.GetProductsForFrontendAsync(queryParams);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, "An error occurred while retrieving products");
        }
    }

    /// <summary>
    /// Get a specific product by ID (Frontend compatible)
    /// Returns: SingleProductResponse = { data: Product; meta: {} }
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details in frontend format</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SingleProductResponse>> GetProduct(int id)
    {
        try
        {
            var product = await _productService.GetProductForFrontendAsync(id);
            return Ok(product);
        }
        catch (ArgumentException)
        {
            return NotFound($"Product with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID: {ProductId}", id);
            return StatusCode(500, "An error occurred while retrieving the product");
        }
    }

    /// <summary>
    /// Search products that contain the searched phrase
    /// </summary>
    /// <param name="search">Search term</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <returns>List of matching products in frontend format</returns>
    [HttpGet("search")]
    public async Task<ActionResult<ProductsResponse>> SearchProducts(
        [FromQuery] string search,
        [FromQuery] int page = 1)
    {
        if (string.IsNullOrEmpty(search))
        {
            return BadRequest("Search term is required");
        }

        try
        {
            var queryParams = new ProductQueryParams { Search = search, Page = page };
            var result = await _productService.GetProductsForFrontendAsync(queryParams);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term: {SearchTerm}", search);
            return StatusCode(500, "An error occurred while searching products");
        }
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="request">Product creation data</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<ActionResult<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
    {
        // FluentValidation will handle validation automatically; demo-admin is rejected by the policy (403)
        try
        {
            var product = await _productService.CreateProductAsync(request, ActorId);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {ProductTitle}", request.Title);
            return StatusCode(500, "An error occurred while creating the product");
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Product update data</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        // FluentValidation rejects invalid fields before the action runs; absent fields keep their value
        try
        {
            var product = await _productService.UpdateProductAsync(id, request, ActorId);

            if (product == null)
            {
                return NotFound($"Product with ID {id} not found");
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
            return StatusCode(500, "An error occurred while updating the product");
        }
    }

    /// <summary>
    /// Delete a product: it disappears from the public catalogue but stays in the database, so
    /// past orders keep a valid reference and an admin can reactivate it with an update.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        try
        {
            var success = await _productService.DeleteProductAsync(id, ActorId);

            if (!success)
            {
                return NotFound($"Product with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
            return StatusCode(500, "An error occurred while deleting the product");
        }
    }

    /// <summary>
    /// What another service needs to know about a product (title, image, colours, the price the
    /// customer pays right now). Service-to-service only: callers present the internal API key.
    /// </summary>
    [HttpGet("{id}/snapshot")]
    [Authorize(Policy = Policies.InternalService)]
    public async Task<ActionResult<ProductSnapshot>> GetSnapshot(int id)
    {
        var snapshot = await _productService.GetSnapshotAsync(id);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    /// <summary>
    /// Get products metadata (categories, companies)
    /// </summary>
    /// <returns>Products metadata</returns>
    [HttpGet("meta")]
    public async Task<ActionResult<ProductsMeta>> GetProductsMeta()
    {
        try
        {
            var meta = await _productService.GetProductsMetaAsync();
            return Ok(meta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products meta");
            return StatusCode(500, "An error occurred while retrieving products metadata");
        }
    }

    /// <summary>
    /// Get all products for admin with advanced sorting and pagination
    /// </summary>
    /// <param name="queryParams">Query parameters for filtering, sorting, and pagination</param>
    /// <param name="sortBy">Column to sort by (id, price, title, company)</param>
    /// <param name="sortDir">Sort direction (asc, desc)</param>
    /// <returns>Products response in frontend format</returns>
    [HttpGet("admin")]
    // Returns inactive products too - admins only
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<ProductsResponse>> GetProductsAdmin(
    [FromQuery] ProductQueryParams queryParams,
    [FromQuery] string? sortBy = null,
    [FromQuery] string? sortDir = null)
    {
        try
        {
            var result = await _productService.GetProductsForAdminAsync(queryParams, sortBy, sortDir);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin products");
            return StatusCode(500, "An error occurred while retrieving products for admin");
        }
    }
}
