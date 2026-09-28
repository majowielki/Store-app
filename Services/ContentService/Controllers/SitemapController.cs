using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Shop;
using Store.ContentService.Services;

namespace Store.ContentService.Controllers;

/// <summary>The editorial pages for search engines; the UI serves this as /sitemap-content.xml.</summary>
[ApiController]
[Route("api/v1/content")]
public class SitemapController : ControllerBase
{
    private readonly ContentSitemap _sitemap;

    public SitemapController(ContentSitemap sitemap)
    {
        _sitemap = sitemap;
    }

    /// <summary>Every published maker, collection, article and lookbook at its address in the shop.</summary>
    [HttpGet("sitemap.xml")]
    [Produces(Sitemap.ContentType)]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ContentResult> Get(CancellationToken cancellationToken)
        => Content(await _sitemap.RenderAsync(cancellationToken), Sitemap.ContentType);
}
