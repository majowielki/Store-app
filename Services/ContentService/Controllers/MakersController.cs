using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.ContentService.DTOs;
using Store.ContentService.Models;
using Store.ContentService.Services;
using Store.Contracts.Authorization;

namespace Store.ContentService.Controllers;

/// <summary>
/// The workshops behind the catalogue. The public reads show published entries only and answer 304 to an
/// unchanged If-None-Match; the admin endpoints see everything, and only the true administrator
/// may change anything.
/// </summary>
[ApiController]
[Route("api/v1/content")]
public class MakersController : ControllerBase
{
    private readonly ContentStore<Maker> _store;

    public MakersController(ContentStore<Maker> store)
    {
        _store = store;
    }

    private string? ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    /// <summary>The published makers.</summary>
    [HttpGet("makers")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<List<MakerResponse>>> List()
    {
        var entries = await _store.PublishedAsync(q => q.OrderBy(e => e.Name));
        return this.OkUnlessUnchanged(entries.Select(e => e.ToResponse()).ToList(), entries);
    }

    /// <summary>One published entry; 404 for an unknown or unpublished slug.</summary>
    [HttpGet("makers/{slug}")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<MakerResponse>> Get(string slug)
    {
        var entry = await _store.PublishedAsync(slug);
        return this.OkUnlessUnchanged(entry.ToResponse(), [entry]);
    }

    /// <summary>Every entry, unpublished ones too.</summary>
    [HttpGet("admin/makers")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<List<MakerResponse>> ListForAdmin()
        => (await _store.AllAsync(q => q.OrderBy(e => e.Name))).Select(e => e.ToResponse()).ToList();

    [HttpGet("admin/makers/{id:int}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<MakerResponse> GetForAdmin(int id)
        => (await _store.FindAsync(id)).ToResponse();

    /// <summary>Creates an entry; 409 when the slug is taken.</summary>
    [HttpPost("admin/makers")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType<MakerResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MakerResponse>> Create([FromBody] MakerRequest request)
    {
        var entry = new Maker();
        request.ApplyTo(entry);
        await _store.CreateAsync(entry, ActorId);
        return CreatedAtAction(nameof(GetForAdmin), new { id = entry.Id }, entry.ToResponse());
    }

    /// <summary>Replaces an entry with the request; 409 when the new slug is taken.</summary>
    [HttpPut("admin/makers/{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public async Task<MakerResponse> Update(int id, [FromBody] MakerRequest request)
        => (await _store.UpdateAsync(id, entry => request.ApplyTo(entry), ActorId)).ToResponse();

    [HttpDelete("admin/makers/{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await _store.DeleteAsync(id, ActorId);
        return NoContent();
    }
}
