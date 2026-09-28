namespace Store.ProductService.Models;

/// <summary>A visitor waiting for a product that ran out; removed once they are told it is back.</summary>
public class StockAlert
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
