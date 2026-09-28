using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Contracts.Authorization;
using Store.Contracts.Payments;
using Store.PaymentService.Services;
using System.Text.Json;

namespace Store.PaymentService.Controllers;

/// <summary>What the shop itself calls, with the internal API key.</summary>
[ApiController]
[Route("api/v1/payments/internal")]
[Authorize(Policy = Policies.InternalService)]
public class InternalPaymentsController : ControllerBase
{
    private const int IdempotencyKeyMaxLength = 128;

    private readonly PaymentProcessor _payments;

    public InternalPaymentsController(PaymentProcessor payments)
    {
        _payments = payments;
    }

    /// <summary>
    /// Opens the payment of an order; the order service calls it with the Idempotency-Key
    /// "order-{id}". Opening it again returns the same payment (200 instead of 201); the same order
    /// with another amount or customer is 409.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<PaymentSnapshot>(StatusCodes.Status201Created)]
    [ProducesResponseType<PaymentSnapshot>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentSnapshot>> Open(
        [FromBody] CreatePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > IdempotencyKeyMaxLength)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"An Idempotency-Key header of at most {IdempotencyKeyMaxLength} characters is required.");
        }

        var (payment, created) = await _payments.OpenAsync(request, idempotencyKey);
        var snapshot = new PaymentSnapshot(payment.Id, payment.OrderId, payment.Amount, payment.Currency,
            JsonNamingPolicy.CamelCase.ConvertName(payment.Status.ToString()), payment.CreatedAt);
        return created ? StatusCode(StatusCodes.Status201Created, snapshot) : Ok(snapshot);
    }
}
