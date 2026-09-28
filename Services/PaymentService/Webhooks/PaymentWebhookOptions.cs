using System.ComponentModel.DataAnnotations;

namespace Store.PaymentService.Webhooks;

/// <summary>Where the webhooks go, the secret they are signed with and how their delivery is retried. Section "PaymentWebhooks".</summary>
public sealed class PaymentWebhookOptions : IValidatableObject
{
    public const string SectionName = "PaymentWebhooks";

    /// <summary>After 1, 5, 30 and 30 minutes - five attempts in all, the way ADR 011 describes the provider.</summary>
    public static readonly IReadOnlyList<int> DefaultRetryDelaysMinutes = [1, 5, 30, 30];

    /// <summary>The shop's webhook endpoint (the order service, on the internal network).</summary>
    [Required, Url]
    public string Url { get; init; } = string.Empty;

    /// <summary>Shared with the order service, min. 32 characters: PAYMENT_WEBHOOK_SECRET in compose.</summary>
    [Required, MinLength(32)]
    public string SigningSecret { get; init; } = string.Empty;

    /// <summary>Seconds between two looks for webhooks due.</summary>
    [Range(1, 60)]
    public int DispatchIntervalSeconds { get; init; } = 2;

    /// <summary>At most this many webhooks per look; the rest wait for the next one.</summary>
    [Range(1, 1000)]
    public int MaxPerRound { get; init; } = 20;

    /// <summary>How long the shop has to answer one delivery.</summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// Minutes to wait after each failed attempt before the next one; after the last delay's
    /// attempt fails, the webhook is given up. <see cref="DefaultRetryDelaysMinutes"/> when not set.
    /// </summary>
    public int[]? RetryDelaysMinutes { get; init; }

    public TimeSpan DispatchInterval => TimeSpan.FromSeconds(DispatchIntervalSeconds);

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RetryDelaysMinutes is { } delays && delays.Any(minutes => minutes <= 0))
        {
            yield return new ValidationResult("Every retry delay is at least a minute", [nameof(RetryDelaysMinutes)]);
        }
    }
}
