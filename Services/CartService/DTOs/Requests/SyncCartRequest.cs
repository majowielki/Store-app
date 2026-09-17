namespace Store.CartService.DTOs.Requests;

/// <summary>One line of a guest cart merged into the server cart at sign-in.</summary>
public class SyncCartItemRequest
{
    public int ProductId { get; set; }

    public int Quantity { get; set; } = 1;

    public string Color { get; set; } = string.Empty;
}

/// <summary>Body of POST /api/v1/cart/sync. Rules: <c>SyncCartRequestValidator</c>.</summary>
public class SyncCartRequest
{
    public List<SyncCartItemRequest> Items { get; set; } = new();
}
