namespace Store.OrderService.DTOs.Responses;

/// <summary>What the shop says about its orders in public (GET /api/v1/orders/stats).</summary>
public class ShopOrderStatsResponse
{
    /// <summary>Orders customers paid for and did not have refunded: paid or shipped.</summary>
    public int PaidOrders { get; set; }
}
