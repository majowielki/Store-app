namespace Store.BuildingBlocks.Api;

/// <summary>Bounded UTC-midnight reporting windows shared by orders and funnel statistics.</summary>
public static class StatisticsWindow
{
    public const int DefaultDays = 30;
    public const int MaxDays = 3650;

    public static DateTime Since(TimeProvider time, int days)
    {
        if (days is < 1 or > MaxDays)
            throw new DomainValidationException($"Days must be between 1 and {MaxDays}.");
        return time.GetUtcNow().UtcDateTime.Date.AddDays(-days);
    }
}
