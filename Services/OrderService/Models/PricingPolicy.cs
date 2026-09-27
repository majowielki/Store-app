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
    public const string CodeDiscountReason = "code";

    /// <remarks>
    /// Discounts do not add up: the first-order discount and what a usable discount code takes
    /// off (<c>codeDiscount</c>) compete, and the larger one is taken - the first-order discount
    /// on a tie, so the code stays unused.
    /// </remarks>
    public static OrderTotals Calculate(decimal subtotal, bool isFirstOrder, PricingOptions options, decimal codeDiscount = 0m)
    {
        var firstOrder = isFirstOrder
            ? Math.Round(subtotal * options.FirstOrderDiscountPercent / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var (discount, reason) = codeDiscount > firstOrder
            ? (codeDiscount, CodeDiscountReason)
            : (firstOrder, firstOrder > 0 ? FirstOrderDiscountReason : null);
        var deliveryFee = subtotal > 0 && subtotal < options.FreeDeliveryThreshold ? options.DeliveryFee : 0m;

        return new OrderTotals(
            Subtotal: subtotal,
            DiscountAmount: discount,
            DiscountReason: reason,
            DeliveryFee: deliveryFee,
            Total: subtotal - discount + deliveryFee);
    }
}
