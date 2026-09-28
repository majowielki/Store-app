using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace Store.ProductService.Models;

/// <summary>How a product's availability is shown to customers; the API writes it in camelCase.</summary>
public enum StockAvailability
{
    InStock,
    LowStock,
    OutOfStock
}

/// <summary>When a product counts as running low. Section "Stock".</summary>
public sealed class StockOptions
{
    public const string SectionName = "Stock";

    public const int DefaultLowStockThreshold = 3;

    /// <summary>At most this many units left is "only a few left".</summary>
    [Range(1, 100)]
    public int LowStockThreshold { get; init; } = DefaultLowStockThreshold;
}

/// <summary>The availability a customer sees for the units a new order can still get.</summary>
public sealed class StockPolicy
{
    private readonly int _lowStockThreshold;

    public StockPolicy(IOptions<StockOptions> options)
    {
        _lowStockThreshold = options.Value.LowStockThreshold;
    }

    public StockAvailability For(int availableQuantity) => availableQuantity switch
    {
        <= 0 => StockAvailability.OutOfStock,
        _ when availableQuantity <= _lowStockThreshold => StockAvailability.LowStock,
        _ => StockAvailability.InStock
    };
}
