using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.AuditLogService.Models;
using Store.AuditLogService.Services;

namespace Store.AuditLogService.Controllers;

/// <summary>
/// Where the shop's pages count the steps towards buying for the purchase funnel (ADR 020). Anyone
/// may send them - visitors are not signed in - and the gateway limits how often; the audit trail
/// itself still takes entries only from events.
/// </summary>
[ApiController]
[Route("api/v1/shop-events")]
[AllowAnonymous]
public class ShopEventsController : ControllerBase
{
    private readonly PurchaseFunnel _funnel;

    public ShopEventsController(PurchaseFunnel funnel)
    {
        _funnel = funnel;
    }

    /// <summary>Counts a product viewed or put in the bag; 202, as nothing is sent back.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Record(RecordShopEventRequest request, CancellationToken cancellationToken)
    {
        await _funnel.RecordAsync(request.Kind!.Value, request.ProductId, cancellationToken);
        return Accepted();
    }
}
