namespace Store.OrderService.DTOs.Responses;

public class OrderStatsResponse
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public List<TimeBucketStats> Daily { get; set; } = new();
    public List<TimeBucketStats> Weekly { get; set; } = new();
    public List<TopProductStats> TopProducts { get; set; } = new();
}

public class TimeBucketStats
{
    public DateTime BucketStart { get; set; } // start of day or start of ISO week (UTC)
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}

public class TopProductStats
{
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}
