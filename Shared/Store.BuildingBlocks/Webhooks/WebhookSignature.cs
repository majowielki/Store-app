using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Store.BuildingBlocks.Webhooks;

/// <summary>Why a signature was accepted or refused.</summary>
public enum WebhookSignatureCheck
{
    Valid,
    Missing,
    Malformed,
    /// <summary>Signed too long ago (or in the future): a replay, or a clock far off.</summary>
    Stale,
    Mismatch
}

/// <summary>
/// Signs and checks webhook bodies the way card providers do: an HMAC-SHA256 over
/// "{timestamp}.{body}" with a shared secret, sent as <c>Store-Signature: t={unix seconds},v1={hex}</c>.
/// The timestamp inside the signature lets the receiver refuse an old request replayed by
/// someone who captured it; the receiver still tells repeats apart by the event id.
/// </summary>
public static class WebhookSignature
{
    public const string HeaderName = "Store-Signature";

    /// <summary>The header value for <paramref name="body"/> sent at <paramref name="timestamp"/>.</summary>
    public static string Create(string secret, DateTimeOffset timestamp, string body)
    {
        var seconds = timestamp.ToUnixTimeSeconds();
        return $"t={seconds.ToString(CultureInfo.InvariantCulture)},v1={Compute(secret, seconds, body)}";
    }

    /// <summary>
    /// Whether <paramref name="header"/> signs <paramref name="body"/> with <paramref name="secret"/>
    /// no further than <paramref name="tolerance"/> from <paramref name="now"/>.
    /// </summary>
    public static WebhookSignatureCheck Verify(string? header, string body, string secret, DateTimeOffset now, TimeSpan tolerance)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return WebhookSignatureCheck.Missing;
        }

        long? seconds = null;
        var signatures = new List<string>();
        foreach (var part in header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0) continue;
            var (key, value) = (part[..separator], part[(separator + 1)..]);
            if (key == "t" && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)) seconds = parsed;
            else if (key == "v1") signatures.Add(value);
        }

        if (seconds is null || signatures.Count == 0)
        {
            return WebhookSignatureCheck.Malformed;
        }

        if (Math.Abs(now.ToUnixTimeSeconds() - seconds.Value) > tolerance.TotalSeconds)
        {
            return WebhookSignatureCheck.Stale;
        }

        var expected = Encoding.ASCII.GetBytes(Compute(secret, seconds.Value, body));
        // Constant time: how many characters matched must not leak through the response time
        return signatures.Any(signature => CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(signature)))
            ? WebhookSignatureCheck.Valid
            : WebhookSignatureCheck.Mismatch;
    }

    private static string Compute(string secret, long seconds, string body)
    {
        var payload = Encoding.UTF8.GetBytes($"{seconds.ToString(CultureInfo.InvariantCulture)}.{body}");
        return Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payload));
    }
}
