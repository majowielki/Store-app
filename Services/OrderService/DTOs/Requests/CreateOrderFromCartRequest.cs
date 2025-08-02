using System.ComponentModel.DataAnnotations;

namespace Store.OrderService.DTOs.Requests;

public class CreateOrderFromCartRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;
    
    [StringLength(300)]
    public string? DeliveryAddress { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Notes { get; set; }
}