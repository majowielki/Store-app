namespace Store.BuildingBlocks.Api;

/// <summary>
/// The Idempotency-Key header: a request repeated with the same key (a retry after a timeout, a
/// second click) gets what the first one made instead of making it again.
/// </summary>
public static class IdempotencyKeyHeader
{
    public const string Name = "Idempotency-Key";

    /// <summary>The longest key a service keeps (the length of its column).</summary>
    public const int MaxLength = 128;
}
