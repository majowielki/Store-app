namespace Store.OrderService.DTOs.Responses;

public class OrderListResponse
{
    public IEnumerable<OrderResponse> Orders { get; set; } = new List<OrderResponse>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}