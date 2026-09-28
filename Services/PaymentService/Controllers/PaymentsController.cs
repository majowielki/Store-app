using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.BuildingBlocks.Authorization;
using Store.PaymentService.DTOs;
using Store.PaymentService.Providers;
using Store.PaymentService.Services;

namespace Store.PaymentService.Controllers;

/// <summary>
/// A payment as the customer handles it: they confirm it with a card and answer the 3-D Secure
/// window. The outcome reaches the shop as a signed webhook, not through the browser; the shop
/// opens the payment through <see cref="InternalPaymentsController"/>.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly PaymentProcessor _payments;

    public PaymentsController(PaymentProcessor payments)
    {
        _payments = payments;
    }

    private string UserId => User.GetRequiredUserId();

    /// <summary>The cards the payment page may use and what each one does; empty where real cards are charged.</summary>
    [HttpGet("test-cards")]
    public IReadOnlyList<TestCard> TestCards([FromServices] IPaymentProvider provider) => provider.TestCards;

    /// <summary>A payment of the signed-in customer; 403 for another customer's.</summary>
    [HttpGet("{id:guid}")]
    public async Task<PaymentResponse> Get(Guid id)
        => PaymentResponse.From(await _payments.GetAsync(id, UserId));

    /// <summary>
    /// Pays with a card. The answer is the payment after the charge: succeeded, waiting for the
    /// 3-D Secure check (requiresAction), or still open with the reason the card was refused. A
    /// payment already paid, cancelled or waiting for its check is 409.
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<PaymentResponse> Confirm(Guid id, [FromBody] ConfirmPaymentRequest request)
        => PaymentResponse.From(await _payments.ConfirmAsync(id, UserId,
            new CardDetails(request.CardNumber, request.ExpMonth, request.ExpYear, request.Cvc)));

    /// <summary>The customer's answer in the 3-D Secure window: approved pays, rejected refuses the card.</summary>
    [HttpPost("{id:guid}/authenticate")]
    public async Task<PaymentResponse> Authenticate(Guid id, [FromBody] AuthenticatePaymentRequest request)
        => PaymentResponse.From(await _payments.AuthenticateAsync(id, UserId, request.Approve));
}
