using Microsoft.EntityFrameworkCore;
using Store.OrderService.Models;

namespace Store.OrderService.Data;

/// <summary>
/// The demo discount codes: one for any order, one with a minimum, one with a limit of uses and
/// one that has run out, so every message of the cart's code field can be seen. Codes the
/// database lacks are added, matched by their letters; what is there already is left alone.
/// </summary>
public static class DiscountCodeSeeder
{
    public static async Task SeedAsync(OrderDbContext context, TimeProvider time)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var demo = new[]
        {
            new DiscountCode { Code = "WELCOME10", Kind = DiscountKind.Percent, Value = 10m },
            new DiscountCode { Code = "LINEN15", Kind = DiscountKind.Percent, Value = 15m, MinimumSubtotal = 150m },
            new DiscountCode { Code = "OAK50", Kind = DiscountKind.Amount, Value = 50m, MinimumSubtotal = 400m, UsageLimit = 500 },
            new DiscountCode { Code = "SUMMER25", Kind = DiscountKind.Percent, Value = 25m, ExpiresAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc) },
        };

        var known = (await context.DiscountCodes.Select(c => c.Code).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        foreach (var code in demo.Where(c => !known.Contains(c.Code)))
        {
            code.CreatedAt = code.UpdatedAt = now;
            context.DiscountCodes.Add(code);
        }

        await context.SaveChangesAsync();
    }
}
