using System.ComponentModel.DataAnnotations;

namespace Store.ReviewService.Moderation;

/// <summary>
/// The model that reads new reviews before the administrator (ADR 019). Section "ReviewModel".
/// It reads only when it is enabled and has an API key; otherwise every review waits for the
/// administrator, as it always did.
/// </summary>
public sealed class ReviewModelOptions
{
    public const string SectionName = "ReviewModel";

    /// <summary>The feature flag.</summary>
    public bool Enabled { get; init; }

    /// <summary>The Anthropic API key - a secret: user secrets locally, Key Vault in Azure.</summary>
    public string? ApiKey { get; init; }

    /// <summary>The model that reads the reviews: a small, fast one is enough to tell clean from doubtful.</summary>
    [Required]
    public string Model { get; init; } = "claude-haiku-4-5";

    /// <summary>The longest answer: a verdict and one sentence.</summary>
    [Range(64, 4096)]
    public int MaxTokens { get; init; } = 300;

    /// <summary>Seconds one request to the model may take before it is given up (and tried again, see <see cref="MaxRetries"/>).</summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 20;

    /// <summary>Attempts after the first when the model is overloaded or unreachable; then the review waits for the administrator.</summary>
    [Range(0, 5)]
    public int MaxRetries { get; init; } = 2;

    /// <summary>Reviews read at the same time by one instance; each holds a database connection while the model answers.</summary>
    [Range(1, 16)]
    public int ConcurrentReviews { get; init; } = 2;

    /// <summary>Whether the model reads new reviews: enabled, and with a key to call it with.</summary>
    public bool IsOn => Enabled && !string.IsNullOrWhiteSpace(ApiKey);

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
