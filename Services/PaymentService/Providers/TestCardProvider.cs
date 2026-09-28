namespace Store.PaymentService.Providers;

/// <summary>A card as the customer typed it. Lives only for the request; never stored or logged.</summary>
public sealed record CardDetails(string Number, int ExpMonth, int ExpYear, string Cvc)
{
    // A record prints its members; a card must not end up in a log that way
    public override string ToString() => $"card ending {CardNumbers.Last4(CardNumbers.Normalize(Number))}";
}

/// <summary>What the card network answers to a charge.</summary>
public enum ChargeResult
{
    Approved,
    /// <summary>The bank wants the customer's 3-D Secure approval first.</summary>
    AuthenticationRequired,
    Declined,
    InsufficientFunds
}

/// <summary>
/// The card network behind the payment service (ADR 011). The demo charges no real card: it runs
/// on <see cref="TestCardProvider"/>; a real provider in test mode would go behind this interface.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Whether the provider will take this number at all.</summary>
    bool Accepts(string cardNumber);

    ChargeResult Charge(CardDetails card, decimal amount);
}

/// <summary>
/// The test cards of the analysis, numbered like Stripe's test mode: 4242 4242 4242 4242 pays,
/// 4000 0000 0000 3220 asks for 3-D Secure, 4000 0000 0000 9995 has no funds, 4000 0000 0000 0002
/// is declined. Any other number is refused before it is charged, so nobody can type a real card
/// into the demo.
/// </summary>
public sealed class TestCardProvider : IPaymentProvider
{
    public bool Accepts(string cardNumber) => TestCards.Outcomes.ContainsKey(CardNumbers.Normalize(cardNumber));

    public ChargeResult Charge(CardDetails card, decimal amount)
        => TestCards.Outcomes.TryGetValue(CardNumbers.Normalize(card.Number), out var result)
            ? result
            : throw new InvalidOperationException("Only the test cards can be charged.");
}

public static class TestCards
{
    public const string Succeeds = "4242424242424242";
    public const string RequiresAuthentication = "4000000000003220";
    public const string InsufficientFunds = "4000000000009995";
    public const string Declined = "4000000000000002";

    public static IReadOnlyDictionary<string, ChargeResult> Outcomes { get; } = new Dictionary<string, ChargeResult>
    {
        [Succeeds] = ChargeResult.Approved,
        [RequiresAuthentication] = ChargeResult.AuthenticationRequired,
        [InsufficientFunds] = ChargeResult.InsufficientFunds,
        [Declined] = ChargeResult.Declined,
    };
}

/// <summary>What can be read off a card number without the network.</summary>
public static class CardNumbers
{
    /// <summary>The digits alone: people type spaces and dashes.</summary>
    public static string Normalize(string? number)
        => new((number ?? string.Empty).Where(c => c is not (' ' or '-')).ToArray());

    /// <summary>The Luhn checksum every card number carries; catches a mistyped digit.</summary>
    public static bool PassesLuhn(string digits)
    {
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit)) return false;

        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var digit = digits[^(i + 1)] - '0';
            if (i % 2 == 1)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
        }

        return sum % 10 == 0;
    }

    /// <summary>visa, mastercard or amex by the leading digits; "card" for anything else.</summary>
    public static string Brand(string digits) => digits switch
    {
        ['4', ..] => "visa",
        ['5', >= '1' and <= '5', ..] => "mastercard",
        ['2', ..] when int.TryParse(digits.AsSpan(0, Math.Min(4, digits.Length)), out var prefix) && prefix is >= 2221 and <= 2720 => "mastercard",
        ['3', '4' or '7', ..] => "amex",
        _ => "card"
    };

    public static string Last4(string digits) => digits.Length >= 4 ? digits[^4..] : digits;
}
