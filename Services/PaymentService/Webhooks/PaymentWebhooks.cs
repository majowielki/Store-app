using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Store.BuildingBlocks.Serialization;
using Store.Contracts.Payments.Webhooks;
using Store.PaymentService.Data;
using Store.PaymentService.Models;

namespace Store.PaymentService.Webhooks;

/// <summary>Where the webhooks go and the secret they are signed with. Section "PaymentWebhooks".</summary>
public sealed class PaymentWebhookOptions
{
    public const string SectionName = "PaymentWebhooks";

    /// <summary>The shop's webhook endpoint (the order service, on the internal network).</summary>
    [Required, Url]
    public string Url { get; init; } = string.Empty;

    /// <summary>Shared with the order service, min. 32 characters: PAYMENT_WEBHOOK_SECRET in compose.</summary>
    [Required, MinLength(32)]
    public string SigningSecret { get; init; } = string.Empty;

    /// <summary>Seconds between two looks for webhooks due.</summary>
    [Range(1, 60)]
    public int DispatchIntervalSeconds { get; init; } = 2;
}

/// <summary>
/// Writes a webhook for a change of a payment, next to the change and in its transaction: the
/// event is never lost when the shop is down, and never sent for a change that was rolled back.
/// <see cref="WebhookDispatcher"/> delivers it.
/// </summary>
public sealed class PaymentWebhooks
{
    private readonly PaymentDbContext _context;
    private readonly TimeProvider _time;

    public PaymentWebhooks(PaymentDbContext context, TimeProvider time)
    {
        _context = context;
        _time = time;
    }

    public void Enqueue(Payment payment, string type, string? failureReason = null)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var id = Guid.NewGuid();
        var body = new PaymentWebhookEvent(id, type, now, new PaymentWebhookData(
            payment.Id, payment.OrderId, payment.Amount, payment.Currency, payment.CardBrand, payment.CardLast4, failureReason));

        _context.WebhookDeliveries.Add(new WebhookDelivery
        {
            Id = id,
            PaymentId = payment.Id,
            Type = type,
            Payload = JsonSerializer.Serialize(body, StoreJson.CamelCase),
            NextAttemptAt = now,
            CreatedAt = now
        });
    }
}

/// <summary>When a webhook that failed is tried again: after 1, 5, 30 and 30 minutes - five attempts in all (ADR 011).</summary>
public static class WebhookSchedule
{
    public static readonly IReadOnlyList<TimeSpan> RetryDelays =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30)];

    public static int MaxAttempts => RetryDelays.Count + 1;

    /// <summary>How long to wait after the <paramref name="attemptsMade"/>-th failed attempt; null once there are no attempts left.</summary>
    public static TimeSpan? DelayAfter(int attemptsMade)
        => attemptsMade >= 1 && attemptsMade <= RetryDelays.Count ? RetryDelays[attemptsMade - 1] : null;
}
