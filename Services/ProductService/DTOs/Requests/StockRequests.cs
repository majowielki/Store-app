namespace Store.ProductService.DTOs.Requests;

/// <summary>Body of PUT /api/v1/products/{id}/stock: the units on hand after a count.</summary>
public class SetStockRequest
{
    public int StockQuantity { get; set; }
}

/// <summary>Body of POST /api/v1/products/{id}/notify: where to write when the product is back.</summary>
public class StockAlertRequest
{
    public string Email { get; set; } = string.Empty;
}
