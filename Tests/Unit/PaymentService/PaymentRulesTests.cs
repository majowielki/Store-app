using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Webhooks;
using Store.PaymentService.DTOs;
using Store.PaymentService.Providers;
using Store.PaymentService.Validators;
using Store.PaymentService.Webhooks;
using Store.Tests.Unit.TestSupport;
using Xunit;

namespace Store.Tests.Unit.PaymentService;

public class WebhookSignatureTests
{
    private const string Secret = "unit-test-webhook-secret-that-is-long-enough";
    private const string Body = """{"id":"1","type":"payment.succeeded"}""";
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);

    [Fact]
    public void A_body_signed_with_the_secret_checks_out()
    {
        var header = WebhookSignature.Create(Secret, Now, Body);

        Assert.StartsWith($"t={Now.ToUnixTimeSeconds()},v1=", header);
        Assert.Equal(WebhookSignatureCheck.Valid, WebhookSignature.Verify(header, Body, Secret, Now.AddMinutes(4), Tolerance));
    }

    [Fact]
    public void A_changed_body_or_another_secret_does_not()
    {
        var header = WebhookSignature.Create(Secret, Now, Body);

        Assert.Equal(WebhookSignatureCheck.Mismatch, WebhookSignature.Verify(header, Body.Replace("succeeded", "refunded"), Secret, Now, Tolerance));
        Assert.Equal(WebhookSignatureCheck.Mismatch, WebhookSignature.Verify(header, Body, Secret + "x", Now, Tolerance));
    }

    [Fact]
    public void An_old_signature_is_taken_for_a_replay()
    {
        var header = WebhookSignature.Create(Secret, Now, Body);

        Assert.Equal(WebhookSignatureCheck.Stale, WebhookSignature.Verify(header, Body, Secret, Now.AddMinutes(6), Tolerance));
        Assert.Equal(WebhookSignatureCheck.Stale, WebhookSignature.Verify(header, Body, Secret, Now.AddMinutes(-6), Tolerance));
    }

    [Theory]
    [InlineData(null, WebhookSignatureCheck.Missing)]
    [InlineData("", WebhookSignatureCheck.Missing)]
    [InlineData("v1=abc", WebhookSignatureCheck.Malformed)]
    [InlineData("t=1790596800", WebhookSignatureCheck.Malformed)]
    [InlineData("t=soon,v1=abc", WebhookSignatureCheck.Malformed)]
    public void A_header_without_a_time_and_a_signature_is_refused(string? header, WebhookSignatureCheck expected)
    {
        Assert.Equal(expected, WebhookSignature.Verify(header, Body, Secret, Now, Tolerance));
    }
}

public class CardRulesTests
{
    [Theory]
    [InlineData("4242 4242 4242 4242", ChargeResult.Approved)]
    [InlineData("4000-0000-0000-3220", ChargeResult.AuthenticationRequired)]
    [InlineData("4000000000009995", ChargeResult.InsufficientFunds)]
    [InlineData("4000 0000 0000 0002", ChargeResult.Declined)]
    public void The_test_cards_answer_the_way_the_analysis_describes(string number, ChargeResult expected)
    {
        var provider = new TestCardProvider();

        Assert.True(provider.Accepts(number));
        Assert.Equal(expected, provider.Charge(new CardDetails(number, 12, 2030, "123"), 100m));
    }

    [Theory]
    [InlineData("5555 5555 5555 4444")] // a valid number, but not a test card
    [InlineData("4111 1111 1111 1111")]
    public void Any_other_card_is_not_taken(string number)
    {
        var provider = new TestCardProvider();

        Assert.False(provider.Accepts(number));
        Assert.Throws<InvalidOperationException>(() => provider.Charge(new CardDetails(number, 12, 2030, "123"), 100m));
    }

    [Theory]
    [InlineData("4242424242424242", true)]
    [InlineData("4242424242424241", false)]
    [InlineData("79927398713", true)]
    [InlineData("", false)]
    [InlineData("4242x24242424242", false)]
    public void The_luhn_check_catches_a_mistyped_digit(string digits, bool valid)
    {
        Assert.Equal(valid, CardNumbers.PassesLuhn(digits));
    }

    [Theory]
    [InlineData("4242424242424242", "visa")]
    [InlineData("5555555555554444", "mastercard")]
    [InlineData("2223003122003222", "mastercard")]
    [InlineData("378282246310005", "amex")]
    [InlineData("6011111111111117", "card")]
    public void The_brand_is_read_off_the_leading_digits(string digits, string brand)
    {
        Assert.Equal(brand, CardNumbers.Brand(digits));
    }

    [Fact]
    public void A_card_never_prints_its_number()
    {
        var card = new CardDetails("4242 4242 4242 4242", 12, 2030, "123");

        Assert.Equal("card ending 4242", card.ToString());
        Assert.DoesNotContain("4242424242424242", card.ToString());
        Assert.Equal(nameof(ConfirmPaymentRequest), new ConfirmPaymentRequest { CardNumber = "4242424242424242" }.ToString());
    }
}

public class ConfirmPaymentRequestValidatorTests
{
    private readonly ConfirmPaymentRequestValidator _validator =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero)), new TestCardProvider());

    private static ConfirmPaymentRequest Card(string number = "4242 4242 4242 4242", int month = 12, int year = 2030, string cvc = "123")
        => new() { CardNumber = number, ExpMonth = month, ExpYear = year, Cvc = cvc };

    [Fact]
    public void A_test_card_valid_until_a_later_month_passes()
    {
        Assert.True(_validator.Validate(Card()).IsValid);
        // Valid until the end of its month
        Assert.True(_validator.Validate(Card(month: 9, year: 2026)).IsValid);
    }

    [Theory]
    [InlineData("", "Enter the card number.")]
    [InlineData("4242", "A card number has 12 to 19 digits.")]
    [InlineData("4242 4242 4242 4241", "That card number is not valid - check the digits.")]
    [InlineData("5555 5555 5555 4444", "This is a demo shop: pay with one of the test cards listed with the form.")]
    public void A_number_that_is_no_test_card_is_refused_without_repeating_it(string number, string message)
    {
        var result = _validator.Validate(Card(number));

        var error = Assert.Single(result.Errors);
        Assert.Equal(message, error.ErrorMessage);
    }

    [Theory]
    [InlineData(8, 2026)]
    [InlineData(12, 2025)]
    public void An_expired_card_is_refused(int month, int year)
    {
        var result = _validator.Validate(Card(month: month, year: year));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "The card has expired.");
    }

    [Theory]
    [InlineData(0, 2030, "123")]
    [InlineData(13, 2030, "123")]
    [InlineData(12, 30, "123")]
    [InlineData(12, 2030, "12")]
    [InlineData(12, 2030, "12a")]
    public void The_month_the_year_and_the_code_must_be_readable(int month, int year, string cvc)
    {
        Assert.False(_validator.Validate(Card(month: month, year: year, cvc: cvc)).IsValid);
    }
}

public class WebhookScheduleTests
{
    private static WebhookSchedule Schedule(int[]? delaysMinutes = null)
        => new(Options.Create(new PaymentWebhookOptions { RetryDelaysMinutes = delaysMinutes }));

    [Fact]
    public void A_failed_webhook_is_retried_after_1_5_30_and_30_minutes_and_then_given_up()
    {
        var schedule = Schedule();

        Assert.Equal(5, schedule.MaxAttempts);
        Assert.Equal(
            [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30)],
            Enumerable.Range(1, 4).Select(attempts => schedule.DelayAfter(attempts)!.Value));
        Assert.Null(schedule.DelayAfter(5));
    }

    [Fact]
    public void The_delays_come_from_the_configuration_when_it_sets_them()
    {
        var schedule = Schedule([2, 10]);

        Assert.Equal(3, schedule.MaxAttempts);
        Assert.Equal(TimeSpan.FromMinutes(10), schedule.DelayAfter(2));
        Assert.Null(schedule.DelayAfter(3));
    }
}
