namespace Store.Tests.Integration.TestSupport;

/// <summary>
/// The real time, moved forward on demand: a test lets a window pass (a grace period, a deadline)
/// without waiting for it. The service under test gets it as its <see cref="TimeProvider"/>.
/// </summary>
public sealed class TestClock : TimeProvider
{
    private long _offsetTicks;

    public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + TimeSpan.FromTicks(Interlocked.Read(ref _offsetTicks));

    /// <summary>Moves the clock forward by <paramref name="by"/>; disposing the result moves it back.</summary>
    public IDisposable Advance(TimeSpan by)
    {
        Interlocked.Add(ref _offsetTicks, by.Ticks);
        return new Rewind(this, by);
    }

    private sealed class Rewind(TestClock clock, TimeSpan by) : IDisposable
    {
        private int _done;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _done, 1) == 0)
            {
                Interlocked.Add(ref clock._offsetTicks, -by.Ticks);
            }
        }
    }
}
