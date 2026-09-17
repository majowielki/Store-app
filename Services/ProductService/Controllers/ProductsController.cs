using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Api;
using Store.Contracts.Authorization;
using Store.Contracts.Catalog;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.DTOs.Responses;
using Store.ProductService.Services;

namespace Store.ProductService.Controllers;

/// <summary>
/// The catalogue. Reads are public and show active products only; writes need the true
/// administrator. Errors arrive as problem responses from the shared exception handler.
/// </summary>
[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Id of the signed-in administrator, for the audit trail.</summary>
    private string? ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    /// <summary>A page of the public catalogue, filtered and sorted by the query.</summary>
    [HttpGet]
    public Task<PagedResponse<ProductResponse>> GetProducts([FromQuery] ProductQueryParams queryParams)
        => _productService.GetProductsAsync(queryParams);

    /// <summary>One active product; 404 for an unknown or deleted id.</summary>
    [HttpGet("{id:int}")]
    public Task<ProductResponse> GetProduct(int id)
        => _productService.GetProductAsync(id);

    /// <summary>The values the catalogue can be filtered by.</summary>
    [HttpGet("meta")]
    public ProductsMeta GetProductsMeta()
        => _productService.GetProductsMeta();

    /// <summary>
    /// Every product, inactive ones included, sorted by <paramref name="sortBy"/> (id, price,
    /// title, company) in <paramref name="sortDir"/> (asc, desc).
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = Policies.Admin)]
    public Task<PagedResponse<ProductResponse>> GetProductsAdmin(
        [FromQuery] ProductQueryParams queryParams,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null)
        => _productService.GetProductsForAdminAsync(queryParams, sortBy, sortDir);

    /// <summary>Creates a product; the body is validated before the action runs.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<ActionResult<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = await _productService.CreateProductAsync(request, ActorId);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>Partial update: absent fields keep their value, the nullable ones can be cleared with null.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public Task<ProductResponse> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        => _productService.UpdateProductAsync(id, request, ActorId);

    /// <summary>
    /// Removes the product from the public catalogue but keeps the row, so past orders keep a
    /// valid reference and an admin can reactivate it with an update.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _productService.DeleteProductAsync(id, ActorId);
        return NoContent();
    }

    /// <summary>
    /// What another service needs to know about a product (title, image, colours, the price the
    /// customer pays right now). Service-to-service only: callers present the internal API key.
    /// </summary>
    [HttpGet("{id:int}/snapshot")]
    [Authorize(Policy = Policies.InternalService)]
    public async Task<ProductSnapshot> GetSnapshot(int id)
        => await _productService.GetSnapshotAsync(id) ?? throw new NotFoundException("Product", id);
}
