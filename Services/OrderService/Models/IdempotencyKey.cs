namespace Store.OrderService.Models;

/// <summary>
/// A checkout the service already answered. A client that retries with the same
/// <c>Idempotency-Key</c> (after a timeout, a lost response, a double click) gets the order it
/// created the first time instead of a second one. Rows expire after <see cref="Lifetime"/>.
/// </summary>
public class IdempotencyKey
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    /// <summary>The key the client sent; a UUID generated per checkout attempt.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Keys are scoped to the customer who sent them.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Hash of the request body, to tell a retry from a different request reusing the key.</summary>
    public string RequestHash { get; set; } = string.Empty;

    /// <summary>The order created for this key.</summary>
    public int OrderId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
