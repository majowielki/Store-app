using System.ComponentModel.DataAnnotations;

namespace Store.CartService.DTOs.Requests;

public class SyncCartItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; } = 1;

    [Required]
    [StringLength(50)]
    public string Color { get; set; } = string.Empty;
}

public class SyncCartRequest
{
    [Required]
    public List<SyncCartItemRequest> Items { get; set; } = new();
}
