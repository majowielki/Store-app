namespace Store.ProductService.Models;

/// <summary>
/// What the stock did for one order: the units it holds and where the order stands. The row is
/// made by whichever event about the order comes first, so a cancellation that overtakes the
/// order it cancels leaves a row that tells the late "order placed" there is nothing to reserve.
/// </summary>
public class StockOrder
{
    /// <summary>Id of the order in the order service.</summary>
    public int OrderId { get; set; }

    public StockOrderStatus Status { get; set; }

    /// <summary>The units held, one line per product; none unless the order got its stock.</summary>
    public List<StockOrderLine> Lines { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Units of one product an order holds.</summary>
public class StockOrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }
}

/// <summary>Stored by name.</summary>
public enum StockOrderStatus
{
    /// <summary>Every line got its units; they count in <see cref="Product.ReservedQuantity"/>.</summary>
    Reserved,

    /// <summary>A line lacked units, so nothing was reserved.</summary>
    Unavailable,

    /// <summary>The order was cancelled: its units, if it had any, are available again.</summary>
    Released,

    /// <summary>The order left the warehouse: its units are off the stock.</summary>
    Shipped
}
