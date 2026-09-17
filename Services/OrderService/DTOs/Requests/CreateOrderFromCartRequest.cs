using System.Text.Json.Serialization;

namespace Store.OrderService.DTOs.Requests;

/// <summary>Body of POST /api/v1/orders/from-cart. Rules: <c>CreateOrderFromCartRequestValidator</c>.</summary>
public class CreateOrderFromCartRequest
{
    /// <summary>Not part of the request: the controller sets it from the token.</summary>
    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Not part of the request either: the e-mail on the order is the account's, from the token.</summary>
    [JsonIgnore]
    public string UserEmail { get; set; } = string.Empty;

    public string? DeliveryAddress { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? Notes { get; set; }

    /// <summary>Also store the delivery address in the user profile.</summary>
    public bool SaveAddress { get; set; }
}
