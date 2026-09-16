using Xunit.Sdk;

namespace Store.Tests.Integration.TestSupport;

/// <summary>
/// Waits for an effect that arrives asynchronously (an outbox delivery, a consumer run) by
/// polling the observable state. The harness's own Any() waits stop working once the bus has
/// been idle for its inactivity timeout, which happens between tests that share a factory.
/// </summary>
public static class Eventually
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    /// <summary>Runs <paramref name="assertion"/> until it stops throwing or the timeout passes (then the last failure surfaces).</summary>
    public static async Task AssertAsync(Func<Task> assertion, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            try
            {
                await assertion();
                return;
            }
            catch (XunitException) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(Interval);
            }
        }
    }

    /// <summary>Waits until <paramref name="condition"/> is true; false when the timeout passes first.</summary>
    public static async Task<bool> BecomesTrueAsync(Func<Task<bool>> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }
            await Task.Delay(Interval);
        }

        return await condition();
    }

    public static Task<bool> BecomesTrueAsync(Func<bool> condition, TimeSpan? timeout = null)
        => BecomesTrueAsync(() => Task.FromResult(condition()), timeout);
}
