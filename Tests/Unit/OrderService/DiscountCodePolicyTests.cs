using Store.OrderService.DTOs.Requests;
using Store.OrderService.Models;
using Store.OrderService.Validators;
using Xunit;

namespace Store.Tests.Unit.OrderService;

public class DiscountCodePolicyTests
{
    private static readonly DateTime Now = new(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);

    private static DiscountCode Code(DiscountKind kind = DiscountKind.Percent, decimal value = 10m) => new() { Code = "TEST", Kind = kind, Value = value };

    [Fact]
    public void A_percentage_is_taken_off_the_subtotal_and_rounded_to_cents()
    {
        var check = DiscountCodePolicy.Check(Code(value: 15m), 33.33m, Now);

        Assert.True(check.IsUsable);
        Assert.Equal(5m, check.Amount);
    }

    [Fact]
    public void An_amount_never_takes_off_more_than_the_subtotal()
    {
        Assert.Equal(50m, DiscountCodePolicy.Check(Code(DiscountKind.Amount, 50m), 400m, Now).Amount);
        Assert.Equal(30m, DiscountCodePolicy.Check(Code(DiscountKind.Amount, 50m), 30m, Now).Amount);
    }

    [Fact]
    public void An_unknown_or_switched_off_code_is_simply_unknown()
    {
        var switchedOff = Code();
        switchedOff.IsActive = false;

        Assert.Equal(DiscountCodePolicy.UnknownCode, DiscountCodePolicy.Check(null, 100m, Now).Refusal);
        Assert.Equal(DiscountCodePolicy.UnknownCode, DiscountCodePolicy.Check(switchedOff, 100m, Now).Refusal);
    }

    [Fact]
    public void Each_refusal_says_why()
    {
        var early = Code();
        early.StartsAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var expired = Code();
        expired.ExpiresAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var usedUp = Code();
        usedUp.UsageLimit = 3;
        usedUp.TimesUsed = 3;
        var minimum = Code();
        minimum.MinimumSubtotal = 1500m;

        Assert.Equal("This code can be used from Oct 1, 2026.", DiscountCodePolicy.Check(early, 100m, Now).Refusal);
        Assert.Equal("This code expired on Sep 1, 2026.", DiscountCodePolicy.Check(expired, 100m, Now).Refusal);
        Assert.Equal("This code has been used up.", DiscountCodePolicy.Check(usedUp, 100m, Now).Refusal);
        Assert.Equal("This code needs an order of at least $1,500.00.", DiscountCodePolicy.Check(minimum, 100m, Now).Refusal);
        Assert.All(new[] { early, expired, usedUp, minimum }, code => Assert.Equal(0m, DiscountCodePolicy.Check(code, 100m, Now).Amount));
    }

    [Fact]
    public void A_code_works_on_the_minimum_itself_and_until_the_moment_it_expires()
    {
        var code = Code();
        code.MinimumSubtotal = 100m;
        code.ExpiresAt = Now.AddSeconds(1);
        code.UsageLimit = 3;
        code.TimesUsed = 2;

        Assert.True(DiscountCodePolicy.Check(code, 100m, Now).IsUsable);
        Assert.False(DiscountCodePolicy.Check(code, 100m, Now.AddSeconds(1)).IsUsable);
    }

    [Fact]
    public void The_larger_discount_wins_and_they_never_add_up()
    {
        var rules = new PricingOptions();

        // First order: 20% of 400 is 80, a $50 code loses
        var firstOrder = PricingPolicy.Calculate(400m, isFirstOrder: true, rules, codeDiscount: 50m);
        Assert.Equal((80m, PricingPolicy.FirstOrderDiscountReason), (firstOrder.DiscountAmount, firstOrder.DiscountReason));

        // A later order: the code is the only discount
        var later = PricingPolicy.Calculate(400m, isFirstOrder: false, rules, codeDiscount: 50m);
        Assert.Equal((50m, PricingPolicy.CodeDiscountReason), (later.DiscountAmount, later.DiscountReason));
        Assert.Equal(350m, later.Total);

        // A tie keeps the code unused
        var tie = PricingPolicy.Calculate(250m, isFirstOrder: true, rules, codeDiscount: 50m);
        Assert.Equal(PricingPolicy.FirstOrderDiscountReason, tie.DiscountReason);
    }
}

public class DiscountCodeRequestValidatorTests
{
    private readonly DiscountCodeRequestValidator _validator = new();

    private static DiscountCodeRequest Valid() => new() { Code = "OAK-50", Kind = "Amount", Value = 50m, MinimumSubtotal = 400m, UsageLimit = 10 };

    [Fact]
    public void A_complete_code_is_valid()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData(nameof(DiscountCodeRequest.Code), "no spaces")]
    [InlineData(nameof(DiscountCodeRequest.Code), "AB")]
    [InlineData(nameof(DiscountCodeRequest.Kind), "Free")]
    [InlineData(nameof(DiscountCodeRequest.Value), "0")]
    [InlineData(nameof(DiscountCodeRequest.Value), "101%")]
    [InlineData(nameof(DiscountCodeRequest.UsageLimit), "0")]
    [InlineData(nameof(DiscountCodeRequest.ExpiresAt), "before start")]
    public void A_broken_field_is_reported_by_name(string field, string what)
    {
        var request = Valid();
        switch (field, what)
        {
            case (nameof(DiscountCodeRequest.Code), _): request.Code = what; break;
            case (nameof(DiscountCodeRequest.Kind), _): request.Kind = what; break;
            case (nameof(DiscountCodeRequest.Value), "0"): request.Value = 0m; break;
            case (nameof(DiscountCodeRequest.Value), _): request.Kind = "Percent"; request.Value = 101m; break;
            case (nameof(DiscountCodeRequest.UsageLimit), _): request.UsageLimit = 0; break;
            default:
                request.StartsAt = new DateTime(2026, 10, 1);
                request.ExpiresAt = new DateTime(2026, 9, 1);
                break;
        }

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == field);
    }
}
