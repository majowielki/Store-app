namespace Store.OrderService.DTOs.Requests;

/// <summary>Body of PATCH /api/v1/admin/orders/{id}/status. Rules: <c>UpdateOrderStatusRequestValidator</c>.</summary>
public class UpdateOrderStatusRequest
{
    /// <summary>The status to move the order to: Shipped or Cancelled.</summary>
    public string Status { get; set; } = string.Empty;
}
