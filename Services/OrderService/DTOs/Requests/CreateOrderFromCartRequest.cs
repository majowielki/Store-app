namespace Store.OrderService.DTOs.Requests;

/// <summary>Body of POST /api/orders/from-cart. Rules: <c>CreateOrderFromCartRequestValidator</c>.</summary>
public class CreateOrderFromCartRequest
{
    /// <summary>Ignored on input: the server takes the user from the token.</summary>
    public string UserId { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string? DeliveryAddress { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? Notes { get; set; }

    /// <summary>Also store the delivery address in the user profile.</summary>
    public bool SaveAddress { get; set; }
}
