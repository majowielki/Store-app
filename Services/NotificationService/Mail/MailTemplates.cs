using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;
using System.Globalization;
using System.Net;
using System.Text;

namespace Store.NotificationService.Mail;

/// <summary>
/// The shop's e-mails, written from the events: HTML with inline styles (what e-mail clients
/// understand) in the shop's warm editorial look, and the same words as plain text. Everything a
/// customer typed or a product carries is HTML-encoded; the links lead to the shop.
/// </summary>
public sealed class MailTemplates
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    private readonly string _shop;

    public MailTemplates(string shopUrl)
    {
        _shop = shopUrl.TrimEnd('/');
    }

    public Email OrderPaid(OrderPaid order)
    {
        var card = order.CardLast4 is null ? string.Empty : $" ({CardName(order.CardBrand)} •••• {order.CardLast4})";
        var arrival = Window(order.DeliveryFrom, order.DeliveryTo);
        var html = new StringBuilder()
            .Append(Paragraph($"Hi {Encode(FirstName(order.CustomerName))},"))
            .Append(Paragraph($"We have received your payment of {Money(order.Total)}{Encode(card)}. Your order #{order.OrderId} is being prepared."))
            .Append(Lines(order.Lines))
            .Append(Total("Paid", order.Total))
            .Append(arrival is null ? string.Empty : Paragraph($"It arrives {Encode(arrival)}."))
            .Append(Button("View your order", $"{_shop}/orders/{order.OrderId}"))
            .ToString();
        var text = new StringBuilder()
            .AppendLine($"Hi {FirstName(order.CustomerName)},")
            .AppendLine()
            .AppendLine($"We have received your payment of {Money(order.Total)}{card}. Your order #{order.OrderId} is being prepared.")
            .AppendLine()
            .Append(TextLines(order.Lines))
            .AppendLine($"Paid: {Money(order.Total)}")
            .AppendLine(arrival is null ? string.Empty : $"It arrives {arrival}.")
            .AppendLine($"View your order: {_shop}/orders/{order.OrderId}")
            .ToString();
        return new Email(order.UserEmail, $"Thank you for your order #{order.OrderId}", Layout("Thank you for your order", html), text, "order-paid");
    }

    public Email PaymentDeclined(PaymentDeclined payment)
    {
        var reason = payment.Reason switch
        {
            PaymentDeclineReasons.InsufficientFunds => "the card did not have enough funds",
            PaymentDeclineReasons.AuthenticationFailed => "the 3-D Secure check was not passed",
            _ => "the bank declined the card"
        };
        var until = payment.RetryUntil is { } deadline
            ? $"We hold your pieces until {deadline.ToString("HH:mm", English)} UTC on {deadline.ToString("ddd, MMM d", English)} - pay with another card before then to keep them."
            : "Pay with another card to keep your pieces.";
        var link = $"{_shop}/orders/{payment.OrderId}/pay";
        var html = new StringBuilder()
            .Append(Paragraph($"Hi {Encode(FirstName(payment.CustomerName))},"))
            .Append(Paragraph($"Your payment for order #{payment.OrderId} did not go through: {Encode(reason)}. Nothing was charged."))
            .Append(Paragraph(Encode(until)))
            .Append(Button("Pay with another card", link))
            .ToString();
        var text = $"Hi {FirstName(payment.CustomerName)},\n\nYour payment for order #{payment.OrderId} did not go through: {reason}. Nothing was charged.\n\n{until}\n\nPay with another card: {link}\n";
        return new Email(payment.UserEmail, $"Your payment for order #{payment.OrderId} did not go through", Layout("Your payment did not go through", html), text, "payment-declined");
    }

    public Email OrderShipped(OrderShipped order)
    {
        var arrival = Window(order.DeliveryFrom, order.DeliveryTo);
        var html = new StringBuilder()
            .Append(Paragraph($"Hi {Encode(FirstName(order.CustomerName))},"))
            .Append(Paragraph($"Your order #{order.OrderId} has left our workshop{(arrival is null ? "." : $" and arrives {Encode(arrival)}.")}"))
            .Append(Lines(order.Lines))
            .Append(Button("Follow your order", $"{_shop}/orders/{order.OrderId}"))
            .ToString();
        var text = new StringBuilder()
            .AppendLine($"Hi {FirstName(order.CustomerName)},")
            .AppendLine()
            .AppendLine($"Your order #{order.OrderId} has left our workshop{(arrival is null ? "." : $" and arrives {arrival}.")}")
            .AppendLine()
            .Append(TextLines(order.Lines))
            .AppendLine($"Follow your order: {_shop}/orders/{order.OrderId}")
            .ToString();
        return new Email(order.UserEmail, $"Your order #{order.OrderId} is on its way", Layout("Your order is on its way", html), text, "order-shipped");
    }

    public Email ProductBackInStock(ProductBackInStock product)
    {
        var link = $"{_shop}/products/{product.ProductId}";
        var html = new StringBuilder()
            .Append($"<img src=\"{Encode(product.ProductImage)}\" alt=\"{Encode(product.ProductTitle)}\" width=\"496\" style=\"display:block;width:100%;max-width:496px;height:auto;border-radius:12px;margin:0 0 20px\">")
            .Append(Paragraph($"Good news: <strong>{Encode(product.ProductTitle)}</strong> is back in stock, at {Money(product.Price)}."))
            .Append(Paragraph("It sold out once already - order soon if you would like one."))
            .Append(Button("Shop it now", link))
            .Append(Paragraph("<span style=\"color:#8a7f73;font-size:13px\">You asked us to tell you when it was back. We will not write about it again.</span>"))
            .ToString();
        var text = $"Good news: {product.ProductTitle} is back in stock, at {Money(product.Price)}.\n\nIt sold out once already - order soon if you would like one.\n\nShop it now: {link}\n\nYou asked us to tell you when it was back. We will not write about it again.\n";
        return new Email(product.SubscriberEmail, $"{product.ProductTitle} is back in stock", Layout("It is back", html), text, "back-in-stock");
    }

    private static string Layout(string heading, string content) =>
        "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"></head>" +
        "<body style=\"margin:0;padding:0;background:#f4efe7;color:#2b2622;font-family:Helvetica,Arial,sans-serif\">" +
        "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f4efe7\"><tr><td align=\"center\" style=\"padding:32px 16px\">" +
        "<table role=\"presentation\" width=\"560\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;max-width:560px;background:#fffdf9;border-radius:20px\"><tr><td style=\"padding:32px\">" +
        "<p style=\"margin:0 0 24px;font-family:Georgia,serif;font-size:24px\">store.</p>" +
        $"<h1 style=\"margin:0 0 20px;font-family:Georgia,serif;font-size:30px;font-weight:normal;line-height:1.15\">{Encode(heading)}</h1>" +
        content +
        "</td></tr></table>" +
        "<p style=\"margin:20px 0 0;color:#8a7f73;font-size:12px\">Store - furniture and home. A demo shop: no real orders, payments or deliveries.</p>" +
        "</td></tr></table></body></html>";

    private static string Paragraph(string html) => $"<p style=\"margin:0 0 16px;font-size:15px;line-height:1.6\">{html}</p>";

    private static string Button(string label, string url) =>
        $"<p style=\"margin:24px 0 8px\"><a href=\"{Encode(url)}\" style=\"display:inline-block;background:#2b2622;color:#fffdf9;text-decoration:none;padding:12px 24px;border-radius:999px;font-size:14px\">{Encode(label)}</a></p>";

    private static string Lines(IReadOnlyList<OrderItem> lines)
    {
        var rows = new StringBuilder("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:8px 0 16px;border-top:1px solid #e8e0d5\">");
        foreach (var line in lines)
        {
            rows.Append("<tr>")
                .Append($"<td style=\"padding:10px 0;border-bottom:1px solid #e8e0d5;font-size:14px\">{Encode(line.ProductTitle)} × {line.Quantity}</td>")
                .Append($"<td align=\"right\" style=\"padding:10px 0;border-bottom:1px solid #e8e0d5;font-size:14px\">{Money(line.UnitPrice * line.Quantity)}</td>")
                .Append("</tr>");
        }

        return rows.Append("</table>").ToString();
    }

    private static string Total(string label, decimal amount) =>
        $"<p style=\"margin:0 0 16px;font-size:15px;text-align:right\"><strong>{Encode(label)}: {Money(amount)}</strong></p>";

    private static string TextLines(IReadOnlyList<OrderItem> lines)
    {
        var text = new StringBuilder();
        foreach (var line in lines)
        {
            text.AppendLine($"- {line.ProductTitle} × {line.Quantity}: {Money(line.UnitPrice * line.Quantity)}");
        }

        return text.AppendLine().ToString();
    }

    /// <summary>"Wed, Sep 30 – Fri, Oct 2", or one day; null without a window.</summary>
    private static string? Window(DateOnly? from, DateOnly? to)
    {
        if (from is null || to is null)
        {
            return null;
        }

        var first = from.Value.ToString("ddd, MMM d", English);
        return from == to ? first : $"{first} – {to.Value.ToString("ddd, MMM d", English)}";
    }

    private static string Money(decimal amount) => "$" + amount.ToString("#,##0.00", English);

    private static string CardName(string? brand) => brand switch
    {
        null or "" => "Card",
        "mastercard" => "Mastercard",
        "amex" => "American Express",
        _ => char.ToUpperInvariant(brand[0]) + brand[1..]
    };

    /// <summary>The first word of the name as given at checkout; "there" without one.</summary>
    private static string FirstName(string customerName)
    {
        var first = customerName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrEmpty(first) ? "there" : first;
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
