using System.Text.RegularExpressions;

namespace Store.ReviewService.Services;

/// <summary>
/// What a review may not contain (ADR 012): links, e-mail addresses, phone numbers and the words
/// of an English and a Polish profanity list. These are the automatic checks before the
/// administrator reads it; they catch the obvious, the administrator the rest.
/// </summary>
public static partial class ReviewContentRules
{
    public const string LinkMessage = "Links are not allowed in a review.";
    public const string EmailMessage = "E-mail addresses are not allowed in a review.";
    public const string PhoneMessage = "Phone numbers are not allowed in a review.";
    public const string ProfanityMessage = "Please keep the review free of swear words.";

    // http://, www., or a host ending in a common top-level domain ("shop.pl", "cheap-sofas.com")
    [GeneratedRegex(@"(https?://|www\.|\b[\p{L}0-9-]+\.(com|net|org|pl|io|shop|store|info|biz|eu|de|uk|co|app|xyz|online|site)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex Link();

    [GeneratedRegex(@"[^\s@]+@[^\s@]+\.[^\s@]+")]
    private static partial Regex Email();

    // A run of digits, possibly spaced, dashed, dotted or bracketed, with a leading + allowed; nine
    // digits make a phone number (a Polish one has nine), fewer are a date, a size or an order number
    [GeneratedRegex(@"\+?\d[\d\s().-]{5,}\d")]
    private static partial Regex DigitRun();

    // English words as whole words (with their usual endings), Polish ones as stems that start a word
    [GeneratedRegex(
        @"\b(fuck\w*|motherfuck\w*|shit\w*|bullshit|bitch\w*|cunt\w*|asshole\w*|arsehole\w*|dickhead\w*|bastard\w*|slut\w*|whore\w*|wanker\w*|twat\w*)\b"
        + @"|\b(kurw|chuj|huj|pierdol|pierdal|jeb|pizd|skurw|dziwk|cip[aeoy]\b|zajeb|wyjeb|spierd|debil|kutas|fiut)\w*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Profanity();

    /// <summary>The first rule the text breaks, as a message for its author; null when it breaks none.</summary>
    public static string? Problem(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Email().IsMatch(text)) return EmailMessage;
        if (Link().IsMatch(text)) return LinkMessage;
        if (DigitRun().Matches(text).Any(match => match.Value.Count(char.IsAsciiDigit) >= 9)) return PhoneMessage;
        if (Profanity().IsMatch(text)) return ProfanityMessage;
        return null;
    }
}
