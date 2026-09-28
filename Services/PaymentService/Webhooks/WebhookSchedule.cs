using Microsoft.Extensions.Options;

namespace Store.PaymentService.Webhooks;

/// <summary>When a webhook that failed is tried again (<see cref="PaymentWebhookOptions.RetryDelaysMinutes"/>), and when it is given up.</summary>
public sealed class WebhookSchedule
{
    private readonly TimeSpan[] _delays;

    public WebhookSchedule(IOptions<PaymentWebhookOptions> options)
    {
        var minutes = options.Value.RetryDelaysMinutes is { Length: > 0 } configured ? configured : PaymentWebhookOptions.DefaultRetryDelaysMinutes;
        _delays = minutes.Select(delay => TimeSpan.FromMinutes(delay)).ToArray();
    }

    /// <summary>The first attempt and one after each delay.</summary>
    public int MaxAttempts => _delays.Length + 1;

    /// <summary>How long to wait after the <paramref name="attemptsMade"/>-th failed attempt; null once there are no attempts left.</summary>
    public TimeSpan? DelayAfter(int attemptsMade)
        => attemptsMade >= 1 && attemptsMade <= _delays.Length ? _delays[attemptsMade - 1] : null;
}
