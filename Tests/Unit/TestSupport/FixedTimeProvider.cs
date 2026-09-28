namespace Store.Tests.Unit.TestSupport;

/// <summary>A clock stopped at one moment, for rules that depend on the date.</summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;
}
