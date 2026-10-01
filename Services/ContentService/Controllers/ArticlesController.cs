using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.ContentService.DTOs;
using Store.ContentService.Models;
using Store.ContentService.Services;
using Store.Contracts.Authorization;

namespace Store.ContentService.Controllers;

/// <summary>
/// The journal, newest first. The public reads show published entries only and answer 304 to an
/// unchanged If-None-Match; the admin endpoints see everything, and only the true administrator
/// may change anything.
/// </summary>
[ApiController]
[Route("api/v1/content")]
public class ArticlesController : ControllerBase
{
    private readonly ContentStore<Article> _store;
    private readonly TimeProvider _time;

    public ArticlesController(ContentStore<Article> store, TimeProvider time)
    {
        _store = store;
        _time = time;
    }

    private string? ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    /// <summary>The published articles.</summary>
    [HttpGet("articles")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<List<ArticleResponse>>> List()
    {
        var entries = await _store.PublishedAsync(q => q.OrderByDescending(e => e.PublishedAt), cancellationToken: HttpContext.RequestAborted);
        return this.OkUnlessUnchanged(entries.Select(e => e.ToResponse()).ToList(), entries);
    }

    /// <summary>One published entry; 404 for an unknown or unpublished slug.</summary>
    [HttpGet("articles/{slug}")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<ArticleResponse>> Get(string slug)
    {
        var entry = await _store.PublishedAsync(slug, cancellationToken: HttpContext.RequestAborted);
        return this.OkUnlessUnchanged(entry.ToResponse(), [entry]);
    }

    /// <summary>Every entry, unpublished ones too.</summary>
    [HttpGet("admin/articles")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<List<ArticleResponse>> ListForAdmin()
        => (await _store.AllAsync(q => q.OrderByDescending(e => e.PublishedAt), cancellationToken: HttpContext.RequestAborted)).Select(e => e.ToResponse()).ToList();

    [HttpGet("admin/articles/{id:int}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ArticleResponse> GetForAdmin(int id)
        => (await _store.FindAsync(id, cancellationToken: HttpContext.RequestAborted)).ToResponse();

    /// <summary>Creates an entry; 409 when the slug is taken.</summary>
    [HttpPost("admin/articles")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ArticleResponse>> Create([FromBody] ArticleRequest request)
    {
        var entry = new Article();
        request.ApplyTo(entry, _time.GetUtcNow().UtcDateTime);
        await _store.CreateAsync(entry, ActorId, cancellationToken: HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetForAdmin), new { id = entry.Id }, entry.ToResponse());
    }

    /// <summary>Replaces an entry with the request; 409 when the new slug is taken.</summary>
    [HttpPut("admin/articles/{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<ArticleResponse> Update(int id, [FromBody] ArticleRequest request)
        => (await _store.UpdateAsync(id, entry => request.ApplyTo(entry, _time.GetUtcNow().UtcDateTime), ActorId, cancellationToken: HttpContext.RequestAborted)).ToResponse();

    [HttpDelete("admin/articles/{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await _store.DeleteAsync(id, ActorId, cancellationToken: HttpContext.RequestAborted);
        return NoContent();
    }
}
