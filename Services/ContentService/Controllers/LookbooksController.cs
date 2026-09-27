using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Contracts.Authorization;
using Store.ContentService.DTOs;
using Store.ContentService.Models;
using Store.ContentService.Services;

namespace Store.ContentService.Controllers;

/// <summary>
/// Rooms with points leading to the products in them. The public reads show published entries only and answer 304 to an
/// unchanged If-None-Match; the admin endpoints see everything, and only the true administrator
/// may change anything.
/// </summary>
[ApiController]
[Route("api/v1/content")]
public class LookbooksController : ControllerBase
{
    private readonly ContentStore<Lookbook> _store;

    public LookbooksController(ContentStore<Lookbook> store)
    {
        _store = store;
    }

    private string? ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    /// <summary>The published lookbooks.</summary>
    [HttpGet("lookbooks")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<List<LookbookResponse>>> List()
    {
        var entries = await _store.PublishedAsync(q => q.OrderBy(e => e.SortOrder).ThenBy(e => e.Title));
        return this.OkUnlessUnchanged(entries.Select(e => e.ToResponse()).ToList(), entries);
    }

    /// <summary>One published entry; 404 for an unknown or unpublished slug.</summary>
    [HttpGet("lookbooks/{slug}")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<LookbookResponse>> Get(string slug)
    {
        var entry = await _store.PublishedAsync(slug);
        return this.OkUnlessUnchanged(entry.ToResponse(), [entry]);
    }

    /// <summary>Every entry, unpublished ones too.</summary>
    [HttpGet("admin/lookbooks")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<List<LookbookResponse>> ListForAdmin()
        => (await _store.AllAsync(q => q.OrderBy(e => e.SortOrder).ThenBy(e => e.Title))).Select(e => e.ToResponse()).ToList();

    [HttpGet("admin/lookbooks/{id:int}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<LookbookResponse> GetForAdmin(int id)
        => (await _store.FindAsync(id)).ToResponse();

    /// <summary>Creates an entry; 409 when the slug is taken.</summary>
    [HttpPost("admin/lookbooks")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType<LookbookResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<LookbookResponse>> Create([FromBody] LookbookRequest request)
    {
        var entry = new Lookbook();
        request.ApplyTo(entry);
        await _store.CreateAsync(entry, ActorId);
        return CreatedAtAction(nameof(GetForAdmin), new { id = entry.Id }, entry.ToResponse());
    }

    /// <summary>Replaces an entry with the request; 409 when the new slug is taken.</summary>
    [HttpPut("admin/lookbooks/{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<LookbookResponse> Update(int id, [FromBody] LookbookRequest request)
        => (await _store.UpdateAsync(id, entry => request.ApplyTo(entry), ActorId)).ToResponse();

    [HttpDelete("admin/lookbooks/{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await _store.DeleteAsync(id, ActorId);
        return NoContent();
    }
}
