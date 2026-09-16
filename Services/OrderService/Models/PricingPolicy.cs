using System.ComponentModel.DataAnnotations;

namespace Store.OrderService.Models;

/// <summary>Pricing rules, bound from the <c>Pricing</c> section; the defaults are the store's current rules.</summary>
public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    /// <summary>Orders with a subtotal at or above this amount ship for free.</summary>
    [Range(0, 1_000_000)]
    public decimal FreeDeliveryThreshold { get; init; } = 299m;

    [Range(0, 10_000)]
    public decimal DeliveryFee { get; init; } = 10m;

    /// <summary>Percentage taken off the subtotal of a customer's first order.</summary>
    [Range(0, 100)]
    public decimal FirstOrderDiscountPercent { get; init; } = 20m;
}

/// <summary>The amounts of an order, as <see cref="PricingPolicy"/> computes them.</summary>
public readonly record struct OrderTotals(decimal Subtotal, decimal DiscountAmount, string? DiscountReason, decimal DeliveryFee, decimal Total);

/// <summary>
/// The store's pricing rules in one place, free of EF and HTTP so they can be unit tested.
/// The delivery threshold is compared with the subtotal before the discount - the same way
/// the cart page shows it to the customer.
/// </summary>
public static class PricingPolicy
{
    public const string FirstOrderDiscountReason = "first-order";

    public static OrderTotals Calculate(decimal subtotal, bool isFirstOrder, PricingOptions options)
    {
        var discount = isFirstOrder
            ? Math.Round(subtotal * options.FirstOrderDiscountPercent / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var deliveryFee = subtotal > 0 && subtotal < options.FreeDeliveryThreshold ? options.DeliveryFee : 0m;

        return new OrderTotals(
            Subtotal: subtotal,
            DiscountAmount: discount,
            DiscountReason: discount > 0 ? FirstOrderDiscountReason : null,
            DeliveryFee: deliveryFee,
            Total: subtotal - discount + deliveryFee);
    }
}
