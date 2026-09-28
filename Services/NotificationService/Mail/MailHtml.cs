using Store.Contracts.Orders.V1;
using Store.Contracts.Payments;
using System.Globalization;
using System.Net;
using System.Text;

namespace Store.NotificationService.Mail;

/// <summary>The shop's warm editorial look, as the inline styles of an e-mail can carry it.</summary>
internal static class MailTheme
{
    public const string Page = "#f4efe7";
    public const string Card = "#fffdf9";
    public const string Ink = "#2b2622";
    public const string Muted = "#8a7f73";
    public const string Rule = "#e8e0d5";
    public const string Serif = "Georgia,serif";
    public const string Sans = "Helvetica,Arial,sans-serif";

    /// <summary>The width of the card the content sits in, and of a picture inside it.</summary>
    public const int CardWidth = 560;
    public const int ImageWidth = 496;

    public const string Wordmark = "store.";
    public const string Footer = "Store - furniture and home. A demo shop: no real orders, payments or deliveries.";
}

/// <summary>How the e-mails write dates and amounts: in English, whatever the server's culture.</summary>
internal static class MailFormats
{
    public static readonly CultureInfo English = CultureInfo.InvariantCulture;

    /// <summary>"Wed, Sep 30".</summary>
    public const string Day = "ddd, MMM d";

    /// <summary>"10:30".</summary>
    public const string Time = "HH:mm";

    /// <summary>The shop charges in dollars (<see cref="Currencies.Usd"/>).</summary>
    public const string CurrencySymbol = "$";
    public const string Amount = "#,##0.00";

    public static string Money(decimal amount) => CurrencySymbol + amount.ToString(Amount, English);

    /// <summary>"Wed, Sep 30 – Fri, Oct 2", or one day; null without a window.</summary>
    public static string? Window(DateOnly? from, DateOnly? to)
    {
        if (from is null || to is null)
        {
            return null;
        }

        var first = from.Value.ToString(Day, English);
        return from == to ? first : $"{first} – {to.Value.ToString(Day, English)}";
    }

    /// <summary>A card brand as a customer reads it: "Visa", "Mastercard", "American Express".</summary>
    public static string CardName(string? brand) => brand switch
    {
        null or "" or CardBrands.Unknown => "Card",
        CardBrands.Mastercard => "Mastercard",
        CardBrands.Amex => "American Express",
        _ => char.ToUpperInvariant(brand[0]) + brand[1..]
    };

    /// <summary>The first word of the name as given at checkout; "there" without one.</summary>
    public static string FirstName(string customerName)
    {
        var first = customerName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrEmpty(first) ? "there" : first;
    }
}

/// <summary>
/// The pieces every e-mail is built from: HTML with inline styles (what e-mail clients understand)
/// and the same words as plain text. Everything a customer typed or a product carries is
/// HTML-encoded here.
/// </summary>
internal static class MailHtml
{
    public static string Layout(string heading, string content) =>
        "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"></head>" +
        $"<body style=\"margin:0;padding:0;background:{MailTheme.Page};color:{MailTheme.Ink};font-family:{MailTheme.Sans}\">" +
        $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:{MailTheme.Page}\"><tr><td align=\"center\" style=\"padding:32px 16px\">" +
        $"<table role=\"presentation\" width=\"{MailTheme.CardWidth}\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;max-width:{MailTheme.CardWidth}px;background:{MailTheme.Card};border-radius:20px\"><tr><td style=\"padding:32px\">" +
        $"<p style=\"margin:0 0 24px;font-family:{MailTheme.Serif};font-size:24px\">{MailTheme.Wordmark}</p>" +
        $"<h1 style=\"margin:0 0 20px;font-family:{MailTheme.Serif};font-size:30px;font-weight:normal;line-height:1.15\">{Encode(heading)}</h1>" +
        content +
        "</td></tr></table>" +
        $"<p style=\"margin:20px 0 0;color:{MailTheme.Muted};font-size:12px\">{MailTheme.Footer}</p>" +
        "</td></tr></table></body></html>";

    public static string Paragraph(string html) => $"<p style=\"margin:0 0 16px;font-size:15px;line-height:1.6\">{html}</p>";

    /// <summary>A small grey line, for the fine print.</summary>
    public static string Note(string text) => $"<span style=\"color:{MailTheme.Muted};font-size:13px\">{Encode(text)}</span>";

    public static string Button(string label, string url) =>
        $"<p style=\"margin:24px 0 8px\"><a href=\"{Encode(url)}\" style=\"display:inline-block;background:{MailTheme.Ink};color:{MailTheme.Card};text-decoration:none;padding:12px 24px;border-radius:999px;font-size:14px\">{Encode(label)}</a></p>";

    public static string Image(string url, string alt) =>
        $"<img src=\"{Encode(url)}\" alt=\"{Encode(alt)}\" width=\"{MailTheme.ImageWidth}\" style=\"display:block;width:100%;max-width:{MailTheme.ImageWidth}px;height:auto;border-radius:12px;margin:0 0 20px\">";

    /// <summary>The product lines of an order with what each cost.</summary>
    public static string Lines(IReadOnlyList<OrderItem> lines)
    {
        var rows = new StringBuilder($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:8px 0 16px;border-top:1px solid {MailTheme.Rule}\">");
        foreach (var line in lines)
        {
            rows.Append("<tr>")
                .Append($"<td style=\"padding:10px 0;border-bottom:1px solid {MailTheme.Rule};font-size:14px\">{Encode(line.ProductTitle)} × {line.Quantity}</td>")
                .Append($"<td align=\"right\" style=\"padding:10px 0;border-bottom:1px solid {MailTheme.Rule};font-size:14px\">{MailFormats.Money(line.UnitPrice * line.Quantity)}</td>")
                .Append("</tr>");
        }

        return rows.Append("</table>").ToString();
    }

    public static string Total(string label, decimal amount) =>
        $"<p style=\"margin:0 0 16px;font-size:15px;text-align:right\"><strong>{Encode(label)}: {MailFormats.Money(amount)}</strong></p>";

    /// <summary>The product lines as plain text, followed by an empty line.</summary>
    public static string TextLines(IReadOnlyList<OrderItem> lines)
    {
        var text = new StringBuilder();
        foreach (var line in lines)
        {
            text.AppendLine($"- {line.ProductTitle} × {line.Quantity}: {MailFormats.Money(line.UnitPrice * line.Quantity)}");
        }

        return text.AppendLine().ToString();
    }

    public static string Encode(string text) => WebUtility.HtmlEncode(text);
}
