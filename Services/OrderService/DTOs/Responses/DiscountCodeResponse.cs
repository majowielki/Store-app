namespace Store.OrderService.DTOs.Responses;

/// <summary>A discount code as the admin panel sees it.</summary>
public class DiscountCodeResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? MinimumSubtotal { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? UsageLimit { get; set; }

    /// <summary>Orders the code was applied to.</summary>
    public int TimesUsed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>What a code takes off a subtotal, for the cart to preview before the checkout.</summary>
public class DiscountCodeCheckResponse
{
    public string Code { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public decimal Value { get; set; }

    /// <summary>The amount the code takes off the subtotal it was checked with.</summary>
    public decimal DiscountAmount { get; set; }
}
