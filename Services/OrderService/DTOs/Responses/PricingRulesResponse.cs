namespace Store.OrderService.DTOs.Responses;

/// <summary>
/// The rules the order service prices an order by, so the cart page can preview the amounts
/// the same way instead of keeping its own copy of the numbers. The order itself is always
/// priced here, at checkout.
/// </summary>
public class PricingRulesResponse
{
    /// <summary>Orders with a subtotal (before the discount) at or above this amount ship for free.</summary>
    public decimal FreeDeliveryThreshold { get; set; }

    /// <summary>Charged below the threshold.</summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>Percentage taken off the subtotal of a customer's first order.</summary>
    public decimal FirstOrderDiscountPercent { get; set; }
}
