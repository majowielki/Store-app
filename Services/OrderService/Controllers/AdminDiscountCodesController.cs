using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Authorization;
using Store.Contracts.Authorization;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Services;

namespace Store.OrderService.Controllers;

/// <summary>
/// Discount codes in the admin panel. Both administrators see them; only the true one may
/// create, change or delete a code.
/// </summary>
[ApiController]
[Route("api/v1/admin/discount-codes")]
[Authorize(Policy = Policies.Admin)]
public class AdminDiscountCodesController : ControllerBase
{
    private readonly DiscountCodeService _codes;

    public AdminDiscountCodesController(DiscountCodeService codes)
    {
        _codes = codes;
    }

    /// <summary>Every code, active or not, in alphabetical order.</summary>
    [HttpGet]
    public Task<List<DiscountCodeResponse>> GetCodes() => _codes.ListAsync();

    /// <summary>One code; 404 when the id is unknown.</summary>
    [HttpGet("{id:int}")]
    public Task<DiscountCodeResponse> GetCode(int id) => _codes.GetAsync(id);

    /// <summary>A new code; 409 when another code has the same letters.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType<DiscountCodeResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DiscountCodeResponse>> CreateCode([FromBody] DiscountCodeRequest request)
    {
        var code = await _codes.CreateAsync(request, User.GetRequiredUserId());
        return CreatedAtAction(nameof(GetCode), new { id = code.Id }, code);
    }

    /// <summary>Replaces a code's rules; the count of its uses stays.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    public Task<DiscountCodeResponse> UpdateCode(int id, [FromBody] DiscountCodeRequest request)
        => _codes.UpdateAsync(id, request, User.GetRequiredUserId());

    /// <summary>Deletes a code no order used; a used one is 409 and can be switched off instead.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCode(int id)
    {
        await _codes.DeleteAsync(id, User.GetRequiredUserId());
        return NoContent();
    }
}
