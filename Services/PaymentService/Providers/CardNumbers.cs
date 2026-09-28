using Store.Contracts.Payments;
using Store.PaymentService.Models;

namespace Store.PaymentService.Providers;

/// <summary>What can be read off a card number without the network.</summary>
public static class CardNumbers
{
    /// <summary>The fewest and the most digits a card number has.</summary>
    public const int MinDigits = 12;
    public const int MaxDigits = 19;

    /// <summary>The digits alone: people type spaces and dashes.</summary>
    public static string Normalize(string? number)
        => new((number ?? string.Empty).Where(c => c is not (' ' or '-')).ToArray());

    /// <summary>Whether the digits are as many as a card number has, and digits only.</summary>
    public static bool HasCardLength(string digits)
        => digits.Length is >= MinDigits and <= MaxDigits && digits.All(char.IsAsciiDigit);

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

    /// <summary>The brand by the leading digits (<see cref="CardBrands"/>); <see cref="CardBrands.Unknown"/> for any other.</summary>
    public static string Brand(string digits) => digits switch
    {
        ['4', ..] => CardBrands.Visa,
        ['5', >= '1' and <= '5', ..] => CardBrands.Mastercard,
        ['2', ..] when int.TryParse(digits.AsSpan(0, Math.Min(4, digits.Length)), out var prefix) && prefix is >= 2221 and <= 2720 => CardBrands.Mastercard,
        ['3', '4' or '7', ..] => CardBrands.Amex,
        _ => CardBrands.Unknown
    };

    public static string Last4(string digits) => digits.Length >= CardLast4.Length ? digits[^CardLast4.Length..] : digits;

    /// <summary>What a payment keeps of a typed number: the brand and the last four digits.</summary>
    public static CardSummary Summarize(string number)
    {
        var digits = Normalize(number);
        return new CardSummary(Brand(digits), Last4(digits));
    }
}
