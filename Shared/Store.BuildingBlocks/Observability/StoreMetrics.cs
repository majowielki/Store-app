using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Store.BuildingBlocks.Observability;

/// <summary>
/// The store's business metrics, one meter for every service; each service records the ones
/// that happen in it. Names follow the OpenTelemetry convention (dots, lower case) so a
/// dashboard or an alert reads the same in every backend.
/// </summary>
public sealed class StoreMetrics : IDisposable
{
    public const string MeterName = "Store";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _ordersPlaced;
    private readonly Histogram<double> _orderValue;
    private readonly Counter<long> _cartItemsAdded;
    private readonly Counter<long> _loginsFailed;
    private readonly Counter<long> _priceMismatches;

    public StoreMetrics()
    {
        _ordersPlaced = _meter.CreateCounter<long>("store.orders.placed", "{order}", "Orders placed");
        _orderValue = _meter.CreateHistogram<double>("store.orders.value", "USD", "What customers paid per order");
        _cartItemsAdded = _meter.CreateCounter<long>("store.cart.items.added", "{item}", "Pieces added to carts");
        _loginsFailed = _meter.CreateCounter<long>("store.auth.login.failed", "{attempt}", "Sign-in attempts refused");
        _priceMismatches = _meter.CreateCounter<long>("store.catalog.price_mismatch", "{line}", "Cart lines whose price differed from the catalogue when read or ordered");
    }

    /// <summary>An order was placed; the discount tag tells first orders from the rest.</summary>
    public void OrderPlaced(decimal total, string? discountReason)
    {
        var tags = new TagList { { "discount", discountReason ?? "none" } };
        _ordersPlaced.Add(1, tags);
        _orderValue.Record((double)total, tags);
    }

    public void CartItemsAdded(int quantity) => _cartItemsAdded.Add(quantity);

    /// <summary>A refused sign-in; the reason is coarse on purpose (no e-mail, no address).</summary>
    public void LoginFailed(string reason) => _loginsFailed.Add(1, new TagList { { "reason", reason } });

    public void PriceMismatch(int lines, string where) => _priceMismatches.Add(lines, new TagList { { "where", where } });

    /// <summary>
    /// Messages written to the outbox and not yet handed to the broker; a value that keeps
    /// growing means the broker is unreachable or the delivery service stopped. Registered once
    /// by the outbox monitor of the service, which counts them off the request path.
    /// </summary>
    public void ObserveOutboxPending(Func<long> pending)
        => _meter.CreateObservableGauge("store.outbox.pending", pending, "{message}", "Outbox messages waiting for the broker");

    public void Dispose() => _meter.Dispose();
}
