using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authorization;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.DTOs.Responses;
using Store.OrderService.Models;
using Store.OrderService.Services;
using System.Security.Claims;

namespace Store.OrderService.Controllers;

/// <summary>The signed-in customer's orders. The admin panel has its own routes under /admin/orders.</summary>
[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private const int IdempotencyKeyMaxLength = 128;

    private readonly IOrderService _orderService;
    private readonly PricingOptions _pricing;
    private readonly DeliveryEstimator _delivery;

    public OrdersController(IOrderService orderService, IOptions<PricingOptions> pricing, DeliveryEstimator delivery)
    {
        _orderService = orderService;
        _pricing = pricing.Value;
        _delivery = delivery;
    }

    /// <summary>
    /// The rules an order is priced by (delivery fee and its free-delivery threshold, first-order
    /// discount), for the cart page to preview the amounts the way the checkout will compute them,
    /// and the days an order placed now should arrive between.
    /// </summary>
    [HttpGet("pricing-rules")]
    [AllowAnonymous]
    public PricingRulesResponse GetPricingRules()
    {
        var delivery = _delivery.EstimateNow();
        return new PricingRulesResponse
        {
            FreeDeliveryThreshold = _pricing.FreeDeliveryThreshold,
            DeliveryFee = _pricing.DeliveryFee,
            FirstOrderDiscountPercent = _pricing.FirstOrderDiscountPercent,
            DeliveryFrom = delivery.From,
            DeliveryTo = delivery.To
        };
    }

    /// <summary>
    /// What a discount code takes off <paramref name="subtotal"/>, for the cart to preview; a
    /// code that cannot be used (unknown, expired, used up, below its minimum) is 422 saying why.
    /// The order checks the code again when it is placed.
    /// </summary>
    [HttpGet("discount-codes/{code}")]
    [AllowAnonymous]
    public Task<DiscountCodeCheckResponse> CheckDiscountCode(string code, [FromQuery] decimal subtotal, [FromServices] DiscountCodeService codes)
        => codes.CheckAsync(code, Math.Max(0m, subtotal));

    private string UserId => User.GetRequiredUserId();

    /// <summary>
    /// Places an order from the cart. Send an Idempotency-Key header (a UUID per checkout
    /// attempt): a retry with the same key returns the order created the first time.
    /// </summary>
    [HttpPost("from-cart")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderResponse>> CreateOrderFromCart(
        [FromBody] CreateOrderFromCartRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        // Who orders and where the confirmation goes come from the token, not from the body
        request.UserId = UserId;
        request.UserEmail = User.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new UnauthorizedAccessException("The token carries no e-mail address");
        if (idempotencyKey is { Length: > IdempotencyKeyMaxLength })
        {
            throw new DomainValidationException($"Idempotency-Key must be at most {IdempotencyKeyMaxLength} characters");
        }

        var order = await _orderService.CreateOrderFromCartAsync(request, string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim());
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>One order: the customer's own, or any order for an administrator (masked for the demo one).</summary>
    [HttpGet("{id:int}")]
    public async Task<OrderResponse> GetOrder(int id)
    {
        if (User.IsStoreAdmin())
        {
            var adminOrder = await _orderService.GetOrderForAdminAsync(id);
            return adminOrder.ForViewer(User);
        }

        return await _orderService.GetOrderAsync(id, UserId);
    }

    /// <summary>
    /// Opens the payment of the customer's order once its stock is reserved; calling it again gives
    /// the same payment. 409 while the stock is still being reserved (try again), once the order is
    /// paid or cancelled, or after its payment deadline.
    /// </summary>
    [HttpPost("{id:int}/payment")]
    public Task<OrderPaymentResponse> StartPayment(int id)
        => _orderService.StartPaymentAsync(id, UserId);

    /// <summary>The customer's orders, newest first.</summary>
    [HttpGet("my-orders")]
    public Task<PagedResponse<OrderResponse>> GetMyOrders([FromQuery] PagedQuery paging)
        => _orderService.GetUserOrdersAsync(UserId, paging);

    /// <summary>Whether the customer has ordered before - the first order is discounted.</summary>
    [HttpGet("has-orders")]
    public async Task<HasOrdersResponse> HasOrders()
    {
        var count = await _orderService.GetUserOrdersCountAsync(UserId);
        return new HasOrdersResponse { HasOrders = count > 0, OrdersCount = count };
    }
}
