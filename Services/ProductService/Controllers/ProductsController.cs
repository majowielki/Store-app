using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
using Store.ProductService.Services;
using Store.Shared.Controllers;
using Store.Shared.Utility;
using System.Security.Claims;

namespace Store.ProductService.Controllers;

public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger) 
        : base(logger)
    {
        _productService = productService;
    }

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
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if user is demo admin and deny access
        if (IsDemoAdmin())
        {
            return StatusCode(403, "Demo admin is not authorized to create products");
        }

        try
        {
            var product = await _productService.CreateProductAsync(request);
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if user is demo admin and deny access
        if (IsDemoAdmin())
        {
            return StatusCode(403, "Demo admin is not authorized to update products");
        }

        try
        {
            var product = await _productService.UpdateProductAsync(id, request);
            
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
    /// Delete a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        // Check if user is demo admin and deny access
        if (IsDemoAdmin())
        {
            return StatusCode(403, "Demo admin is not authorized to delete products");
        }

        try
        {
            var success = await _productService.DeleteProductAsync(id);
            
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
    /// Check if the current user is a demo admin
    /// </summary>
    /// <returns>True if demo admin, false otherwise</returns>
    private bool IsDemoAdmin()
    {
        var userRole = User?.FindFirst(ClaimTypes.Role)?.Value;
        var userEmail = User?.FindFirst(ClaimTypes.Email)?.Value;
        
        // Check if user has demo admin role or email
        return userRole?.ToLower() == Constants.Role_DemoAdmin.ToLower() || 
               userEmail?.ToLower() == Constants.DemoAdminEmail.ToLower() ||
               User?.FindFirst("demo")?.Value == "true";
    }
}
