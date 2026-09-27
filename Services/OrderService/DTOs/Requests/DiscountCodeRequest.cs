namespace Store.OrderService.DTOs.Requests;

/// <summary>Body of POST and PUT /api/v1/admin/discount-codes. Rules: <c>DiscountCodeRequestValidator</c>.</summary>
public class DiscountCodeRequest
{
    /// <summary>Letters, digits and dashes; stored in upper case.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Percent or Amount.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The percentage (1-100) or the amount in dollars.</summary>
    public decimal Value { get; set; }

    /// <summary>The smallest subtotal the code works on; none for any order.</summary>
    public decimal? MinimumSubtotal { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    /// <summary>How many orders in total may use the code; none for no limit.</summary>
    public int? UsageLimit { get; set; }

    public bool IsActive { get; set; } = true;
}
