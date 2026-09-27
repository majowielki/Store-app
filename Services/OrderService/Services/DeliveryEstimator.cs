using Microsoft.Extensions.Options;
using Store.OrderService.Models;

namespace Store.OrderService.Services;

/// <summary>The delivery window of an order placed now, by <see cref="DeliveryPolicy"/>.</summary>
public sealed class DeliveryEstimator
{
    private readonly DeliveryOptions _options;
    private readonly TimeProvider _time;

    public DeliveryEstimator(IOptions<DeliveryOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
    }

    public DeliveryWindow EstimateNow() => DeliveryPolicy.Estimate(_time.GetUtcNow(), _options);
}
