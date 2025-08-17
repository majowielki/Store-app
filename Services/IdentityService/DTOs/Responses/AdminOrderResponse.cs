namespace Store.IdentityService.DTOs.Responses;

public class AdminOrderResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal OrderTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}