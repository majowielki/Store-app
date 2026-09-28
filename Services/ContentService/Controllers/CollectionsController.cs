using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.ContentService.DTOs;
using Store.ContentService.Models;
using Store.ContentService.Services;
using Store.Contracts.Authorization;

namespace Store.ContentService.Controllers;

/// <summary>
/// Curated sets of products. The public reads show published entries only and answer 304 to an
/// unchanged If-None-Match; the admin endpoints see everything, and only the true administrator
/// may change anything.
/// </summary>
[ApiController]
[Route("api/v1/content")]
public class CollectionsController : ControllerBase
{
    private readonly ContentStore<Collection> _store;

    public CollectionsController(ContentStore<Collection> store)
    {
        _store = store;
    }

    private string? ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    /// <summary>The published collections.</summary>
    [HttpGet("collections")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<List<CollectionResponse>>> List()
    {
        var entries = await _store.PublishedAsync(q => q.OrderBy(e => e.SortOrder).ThenBy(e => e.Title));
        return this.OkUnlessUnchanged(entries.Select(e => e.ToResponse()).ToList(), entries);
    }

    /// <summary>One published entry; 404 for an unknown or unpublished slug.</summary>
    [HttpGet("collections/{slug}")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<CollectionResponse>> Get(string slug)
    {
        var entry = await _store.PublishedAsync(slug);
        return this.OkUnlessUnchanged(entry.ToResponse(), [entry]);
    }

    /// <summary>Every entry, unpublished ones too.</summary>
    [HttpGet("admin/collections")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<List<CollectionResponse>> ListForAdmin()
        => (await _store.AllAsync(q => q.OrderBy(e => e.SortOrder).ThenBy(e => e.Title))).Select(e => e.ToResponse()).ToList();

    [HttpGet("admin/collections/{id:int}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<CollectionResponse> GetForAdmin(int id)
        => (await _store.FindAsync(id)).ToResponse();

    /// <summary>Creates an entry; 409 when the slug is taken.</summary>
    [HttpPost("admin/collections")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType<CollectionResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CollectionResponse>> Create([FromBody] CollectionRequest request)
    {
        var entry = new Collection();
        request.ApplyTo(entry);
        await _store.CreateAsync(entry, ActorId);
        return CreatedAtAction(nameof(GetForAdmin), new { id = entry.Id }, entry.ToResponse());
    }

    /// <summary>Replaces an entry with the request; 409 when the new slug is taken.</summary>
    [HttpPut("admin/collections/{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<CollectionResponse> Update(int id, [FromBody] CollectionRequest request)
        => (await _store.UpdateAsync(id, entry => request.ApplyTo(entry), ActorId)).ToResponse();

    [HttpDelete("admin/collections/{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await _store.DeleteAsync(id, ActorId);
        return NoContent();
    }
}
