using System.Globalization;

namespace Store.OrderService.Models;

/// <summary>Stored by name.</summary>
public enum DiscountKind
{
    /// <summary>A percentage of the subtotal.</summary>
    Percent,

    /// <summary>A fixed amount, never more than the subtotal.</summary>
    Amount
}

/// <summary>
/// A code a customer types in the cart. It may start and end at given times, need a minimum
/// subtotal and be usable a limited number of times in total; <see cref="TimesUsed"/> counts the
/// orders it was applied to.
/// </summary>
public class DiscountCode
{
    public const int MaxCodeLength = 32;

    public int Id { get; set; }

    /// <summary>Upper case; customers may type it in any case.</summary>
    public string Code { get; set; } = string.Empty;

    public DiscountKind Kind { get; set; }

    /// <summary>The percentage (1-100) or the amount in dollars.</summary>
    public decimal Value { get; set; }

    public decimal? MinimumSubtotal { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int? UsageLimit { get; set; }

    public int TimesUsed { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();
}

/// <summary>What a code is worth on a subtotal, or why it cannot be used - in words for the customer.</summary>
public readonly record struct DiscountCheck(decimal Amount, string? Refusal)
{
    public bool IsUsable => Refusal is null;
}

/// <summary>
/// Whether a code can be used on a subtotal now and what it takes off. Free of EF and HTTP so
/// it can be unit tested; the order service asks it both when the cart previews a code and,
/// under a lock on the code, when the order is placed.
/// </summary>
public static class DiscountCodePolicy
{
    public const string UnknownCode = "There is no such code.";

    public static DiscountCheck Check(DiscountCode? code, decimal subtotal, DateTime now)
    {
        if (code is null || !code.IsActive)
        {
            return Refuse(UnknownCode);
        }
        if (code.StartsAt is { } starts && now < starts)
        {
            return Refuse($"This code can be used from {Day(starts)}.");
        }
        if (code.ExpiresAt is { } expires && now >= expires)
        {
            return Refuse($"This code expired on {Day(expires)}.");
        }
        if (code.UsageLimit is { } limit && code.TimesUsed >= limit)
        {
            return Refuse("This code has been used up.");
        }
        if (code.MinimumSubtotal is { } minimum && subtotal < minimum)
        {
            return Refuse($"This code needs an order of at least {Dollars(minimum)}.");
        }

        var amount = code.Kind == DiscountKind.Percent
            ? Math.Round(subtotal * code.Value / 100m, 2, MidpointRounding.AwayFromZero)
            : Math.Min(code.Value, subtotal);
        return new DiscountCheck(amount, null);
    }

    private static DiscountCheck Refuse(string reason) => new(0m, reason);

    private static string Day(DateTime value) => value.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);

    private static string Dollars(decimal value) => "$" + value.ToString("N2", CultureInfo.InvariantCulture);
}
