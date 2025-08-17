namespace Store.OrderService.DTOs.Responses;

public class HasOrdersResponse
{
    public bool HasOrders { get; set; }
    public int OrdersCount { get; set; }
}
