namespace Store.IdentityService.DTOs.Responses;

public class PaginatedOrderResponse
{
    public List<AdminOrderResponse> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
