using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Shop;
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
    private readonly IProductDiscovery _discovery;
    private readonly IStockLedger _stock;
    private readonly IStockAlerts _alerts;

    public ProductsController(IProductService productService, IProductDiscovery discovery, IStockLedger stock, IStockAlerts alerts)
    {
        _productService = productService;
        _discovery = discovery;
        _stock = stock;
        _alerts = alerts;
    }

    /// <summary>Id of the signed-in administrator, for the audit trail.</summary>
    private string? ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    /// <summary>A page of the public catalogue, filtered and sorted by the query.</summary>
    [HttpGet]
    public Task<PagedResponse<ProductResponse>> GetProducts([FromQuery] ProductQueryParams queryParams)
        => _productService.GetProductsAsync(queryParams);

    /// <summary>One active product; 404 for an unknown or deleted id.</summary>
    [HttpGet("{id:int}")]
    public Task<ProductDetailResponse> GetProduct(int id)
        => _productService.GetProductAsync(id);

    /// <summary>
    /// The values the catalogue can be filtered by, with how many active products each would show
    /// together with the rest of the query (the same parameters as the listing), and the search's
    /// correction when the typed words found nothing.
    /// </summary>
    [HttpGet("meta")]
    public Task<ProductsMeta> GetProductsMeta([FromQuery] ProductQueryParams queryParams)
        => _discovery.GetMetaAsync(queryParams);

    /// <summary>
    /// Every finish products are sold in - the keys their colors list - with its name, the colour
    /// family the filter counts it under and the swatch to draw.
    /// </summary>
    [HttpGet("finishes")]
    public IReadOnlyList<FinishResponse> GetFinishes() => _discovery.GetFinishes();

    /// <summary>
    /// The best matches of a search while it is typed (words match as word starts, the title first),
    /// with the corrected search when the typed one finds nothing ("sfoa" finds the sofas).
    /// </summary>
    [HttpGet("suggest")]
    public Task<ProductSuggestions> Suggest([FromQuery] string? q, [FromQuery] int limit = ProductDiscovery.DefaultSuggestions)
        => _discovery.SuggestAsync(q, limit);

    /// <summary>The sitemap of the catalogue: every active product's page in the shop (the UI serves it as /sitemap-products.xml).</summary>
    [HttpGet("sitemap.xml")]
    [Produces(Sitemap.ContentType)]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ContentResult> GetSitemap([FromServices] ProductSitemap sitemap, CancellationToken cancellationToken)
        => Content(await sitemap.RenderAsync(cancellationToken), Sitemap.ContentType);

    /// <summary>
    /// Every product, inactive ones included, sorted by <paramref name="sortBy"/> (id, price,
    /// title, company, stock - the units available) in <paramref name="sortDir"/> (asc, desc).
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = Policies.Admin)]
    public Task<PagedResponse<ProductResponse>> GetProductsAdmin(
        [FromQuery] ProductQueryParams queryParams,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null)
        => _productService.GetProductsForAdminAsync(queryParams, sortBy, sortDir);

    /// <summary>One product with its gallery and points, inactive ones too, for the admin form; 404 for an unknown id.</summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = Policies.Admin)]
    public Task<ProductDetailResponse> GetProductAdmin(int id)
        => _productService.GetProductForAdminAsync(id);

    /// <summary>Creates a product; the body is validated before the action runs.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDetailResponse>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = await _productService.CreateProductAsync(request, ActorId);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>Partial update: absent fields keep their value, the nullable ones can be cleared with null.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public Task<ProductDetailResponse> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        => _productService.UpdateProductAsync(id, request, ActorId);

    /// <summary>
    /// Removes the product from the public catalogue but keeps the row, so past orders keep a
    /// valid reference and an admin can reactivate it with an update.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _productService.DeleteProductAsync(id, ActorId);
        return NoContent();
    }

    /// <summary>
    /// Sets the units on hand after a count; it cannot go below the units held for orders not
    /// shipped yet (422). Visitors waiting for the product are told when it is back.
    /// </summary>
    [HttpPut("{id:int}/stock")]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<ProductDetailResponse> SetStock(int id, [FromBody] SetStockRequest request)
    {
        await _stock.SetStockAsync(id, request.StockQuantity, ActorId);
        return await _productService.GetProductForAdminAsync(id);
    }

    /// <summary>
    /// Asks for an e-mail once a product that ran out can be bought again; anyone may ask, and
    /// asking twice changes nothing. A product in stock answers 409: it can be bought now.
    /// </summary>
    [HttpPost("{id:int}/notify")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> NotifyWhenBack(int id, [FromBody] StockAlertRequest request)
    {
        await _alerts.SubscribeAsync(id, request.Email);
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
